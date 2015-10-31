namespace Sitecore.PublishExclusions
{
    using Sitecore.Configuration;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.PublishExclusions.Model;
    using Sitecore.Publishing.Pipelines.PublishItem;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Class that controls all functionality related to Publish Exclusions
    /// </summary>
    public class PublishExclusionsController : IPublishExclusionsController
    {
        #region Constructors

        /// <summary>
        /// Create a singleton instance of the Publish Exclusions Controller
        /// </summary>
        public PublishExclusionsController()
        {
            string repositoryName = string.Empty;

            try
            {
                repositoryName = Settings.GetSetting("Sitecore.PublishExclusions.RepositoryType", typeof(PublishExclusionsRepository).FullName);
                Repository = Activator.CreateInstance(Type.GetType(repositoryName)) as IPublishExclusionsRepository;
            }
            catch (Exception ex)
            {
                //Resort to default repository
                Log.Error("Sitecore.PublishExclusions : Exclusions Controller could not instantiate repository with type - " + (repositoryName ?? string.Empty), ex, this);
                Repository = new PublishExclusionsRepository();
            }
        }

        #endregion

        #region IPublishExclusionsController implementation

        #region Properties

        /// <summary>
        /// Instance of the repository used by the handler
        /// </summary>
        public virtual IPublishExclusionsRepository Repository { get; private set; }

        /// <summary>
        /// Whether items that are skipped / excluded from incremental publish should be returned back to the Publish Queue or not
        /// true if item needs to be returned to Publish Queue, else false
        /// </summary>
        public virtual bool ReturnItemsToPublishQueue
        {
            get
            {
                return this.Repository.GlobalConfiguration.ReturnItemsToPublishQueue;
            }
        }

        /// <summary>
        /// Whether a warning should be shown in Content Editor when the item is configured for publish exclusion
        /// true if warning should be shown, else false
        /// </summary>
        public virtual bool ShowContentEditorWarnings
        {
            get
            {
                return this.Repository.GlobalConfiguration.ShowContentEditorWarnings;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the controller and/or any dependent repository
        /// </summary>
        public virtual void Initialize()
        {
            this.Repository.Initialize();
        }

        /// <summary>
        /// Reinitializes the controller and/or any dependent repository, in case of any changes to the exclusion rules
        /// </summary>
        public virtual void ReInitialize()
        {
            this.Initialize();
        }

        /// <summary>
        /// Determines if the current item that is being published is excluded from publish for the publishing target and publish mode
        /// </summary>
        /// <param name="context">current item's publish context</param>
        /// <returns>true if item is excluded from current publish, else false</returns>
        public virtual bool IsExcludedForCurrentPublish(PublishItemContext context)
        {
            Assert.ArgumentNotNull((object)context, "context");
            Assert.ArgumentNotNull((object)context.VersionToPublish, "context.VersionToPublish");
            Assert.ArgumentNotNull((object)context.PublishOptions, "context.PublishOptions");
            Assert.ArgumentNotNull((object)context.PublishOptions.PublishingTargets, "context.PublishOptions.PublishingTargets");
            Assert.ArgumentNotNull((object)context.PublishOptions.PublishingTargets[0], "context.PublishOptions.PublishingTargets[0]");

            string itemPath = FormatItemPath(context.VersionToPublish.Paths.Path);

            PublishExclusion exclusion = Repository.PublishExclusions
                                            .Where(pe => pe.PublishingTargetID == context.PublishOptions.PublishingTargets[0])
                                            .Where(pe => pe.PublishModes.Contains(context.PublishOptions.Mode))
                                            .Where(pe => pe.ExcludedNodes.Exists(p => itemPath.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
                                                !pe.ExclusionOverrides.Exists(p => itemPath.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                                            .FirstOrDefault();

            if (exclusion != null)
                return true;

            return false;
        }

        /// <summary>
        /// Returns all applicable publish exclusions for the item being passed
        /// </summary>
        /// <param name="item">Item for which publish exclusions are to be retrieved</param>
        /// <returns>Applicable publish exclusions for the item</returns>
        public virtual List<PublishExclusion> GetAllPublishExclusions(Item item)
        {
            string itemPath = FormatItemPath(item.Paths.Path);

            return Repository.PublishExclusions
                        .Where(pe => pe.ExcludedNodes.Exists(p => itemPath.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
                            !pe.ExclusionOverrides.Exists(p => itemPath.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

        }

        #endregion

        #endregion

        #region Private methods

        private static string FormatItemPath(string path)
        {
            return path.EndsWith("/") ? path : string.Format("{0}{1}", path, "/");
        }

        #endregion
    }
}
