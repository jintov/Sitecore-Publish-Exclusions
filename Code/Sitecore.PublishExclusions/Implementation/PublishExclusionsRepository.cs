namespace Sitecore.PublishExclusions
{
    using Sitecore.Collections;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.PublishExclusions.Model;
    using Sitecore.Publishing;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Class responsible for fetching publishing exclusion data from Sitecore
    /// </summary>
    public class PublishExclusionsRepository : IPublishExclusionsRepository
    {
        #region Members

        private Database masterDB = null;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the global publish exclusion configuration
        /// </summary>
        public virtual PublishExclusionConfiguration GlobalConfiguration { get; private set; }

        /// <summary>
        /// Gets the collection of publish exclusions configured
        /// </summary>
        public virtual List<PublishExclusion> PublishExclusions { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of Publish Exclusions Repository
        /// </summary>
        public PublishExclusionsRepository()
        {
            try
            {
                string masterDBName = Settings.GetSetting("Sitecore.PublishExclusions.MasterDBName", "master");
                masterDB = Factory.GetDatabase(masterDBName);
            }
            catch (Exception ex)
            {
                Log.Error("Sitecore.PublishExclusions : Master Database is null", ex, this);
                masterDB = null;
            }
        }

        #endregion

        #region Public Methods

        #region Interface Implementations

        /// <summary>
        /// Initializes the repository on startup by reading all publish exclusions data
        /// </summary>
        public virtual void Initialize()
        {
            if (masterDB == null)
                return;

            InitializeGlobalConfiguration();
            InitializeAllPublishExclusions();
        }

        /// <summary>
        /// Re-initializes the repository when any publish exclusions data is modified
        /// </summary>
        public virtual void ReInitialize()
        {
            Initialize();
        }

        #endregion

        #region Others

        /// <summary>
        /// Returns the list of mandatory Sitecore items that can never be excluded from publish
        /// </summary>
        /// <returns>List of mandatory items to be published</returns>
        public virtual List<string> GetMandatoryExclusionOverrides()
        {
            //Languages will always be published, as it is required for Sitecore to publish any content
            return new List<string>() { "/sitecore/System/Languages" };
        }

        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        /// Reads global publish configuration from Sitecore
        /// Reads the item "/sitecore/system/Modules/Publish Exclusions/Global Configuration"
        /// </summary>
        private void InitializeGlobalConfiguration()
        {
            GlobalConfiguration = new PublishExclusionConfiguration();

            Item configurationItem = masterDB.GetItem(SitecoreIDs.Items.GlobalConfiguration, Language.Parse("en"));
            if (configurationItem != null)
            {
                GlobalConfiguration.ReturnItemsToPublishQueue = ((CheckboxField)configurationItem.Fields[SitecoreIDs.Fields.ReturnItemsToPublishQueue]).Checked;
                GlobalConfiguration.ShowContentEditorWarnings = ((CheckboxField)configurationItem.Fields[SitecoreIDs.Fields.ShowContentEditorWarnings]).Checked;
            }
        }

        /// <summary>
        /// Reads all publish exclusions from Sitecore
        /// Reads them from "/sitecore/system/Modules/Publish Exclusions/Exclusions Repository" folder
        /// </summary>
        private void InitializeAllPublishExclusions()
        {
            PublishExclusions = new List<PublishExclusion>();

            Item exclusionsRepository = masterDB.GetItem(SitecoreIDs.Items.ExclusionsRepository, Language.Parse("en"));
            if (exclusionsRepository == null)
                return;

            PublishExclusions =
                exclusionsRepository
                .GetChildren(ChildListOptions.IgnoreSecurity | ChildListOptions.SkipSorting)
                .Where(i => i.TemplateID == SitecoreIDs.Templates.PublishExclusion)
                .Select(i =>
                    new PublishExclusion()
                    {
                        Name = i.Name,
                        PublishingTarget = masterDB.GetItem(i.Fields[SitecoreIDs.Fields.PublishingTarget].Source + i.Fields[SitecoreIDs.Fields.PublishingTarget].Value).Name,
                        PublishingTargetID = i.Fields[SitecoreIDs.Fields.PublishingTarget].Value,
                        PublishModes = ((MultilistField)i.Fields[SitecoreIDs.Fields.PublishModes])
                                        .GetItems()
                                        .Select(pm => GetPublishMode(pm.Name))
                                        .ToList(),
                        ExcludedNodes = GetItemPaths(i, SitecoreIDs.Fields.ExcludedNodes),
                        ExclusionOverrides = GetCombinedOverrides(i, SitecoreIDs.Fields.ExclusionOverrides)
                    }
                )
                .ToList();
        }

        private PublishMode GetPublishMode(string mode)
        {
            switch (mode.ToLowerInvariant())
            {
                case "full": return PublishMode.Full;
                case "smart": return PublishMode.Smart;
                case "incremental": return PublishMode.Incremental;
                case "single item": return PublishMode.SingleItem;
                default: return PublishMode.Unknown;
            }
        }

        private List<string> GetCombinedOverrides(Item item, ID fieldID)
        {
            List<string> overrides = GetItemPaths(item, fieldID);
            overrides.AddRange(GetMandatoryExclusionOverrides());
            return overrides;
        }

        private List<string> GetItemPaths(Item item, ID fieldID)
        {
            MultilistField field = item.Fields[fieldID];
            if (field != null)
            {
                return field.GetItems()
                        .Where(i => i != null && i.Paths != null)
                        .Select(i => FormatItemPath(i.Paths.Path))
                        .ToList();
            }

            return new List<string>();
        }

        private string FormatItemPath(string path)
        {
            return path.EndsWith("/") ? path : string.Format("{0}{1}", path, "/");
        }

        #endregion
    }
}
