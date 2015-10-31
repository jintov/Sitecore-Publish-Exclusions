namespace Sitecore.PublishExclusions
{
    using Sitecore;
    using Sitecore.Diagnostics;
    using Sitecore.Pipelines.GetContentEditorWarnings;
    using Sitecore.PublishExclusions.Model;
    using Sitecore.Publishing;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Pipeline processor used to display content editor warnings when an item is configured for publish exclusion
    /// </summary>
    public class IsExcludedFromPublish
    {
        #region Constants

        private const string WarningTitle = "This item is currently configured to be not publishable for";
        
        private const string WarningTextFormat = "Target: {0} and Publish Modes: {1}";

        #endregion

        #region Public Methods
        
        #region Called by Sitecore

        /// <summary>
        /// Called by getContentEditorWarnings pipeline to determine if a warning should be 
        /// displayed or not if the item is configured for publish exclusion.
        /// Override this method for any custom implementation logic.
        /// </summary>
        /// <param name="args">content editor warning arguments</param>
        public virtual void Process(GetContentEditorWarningsArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            try
            {
                if (!PublishExclusionsContext.Current.ShowContentEditorWarnings || args.Item == null ||
                    (Context.ContentDatabase != null && Context.ContentDatabase.Name != "master"))
                    return;

                List<PublishExclusion> exclusions = PublishExclusionsContext.Current.GetAllPublishExclusions(args.Item);
                if (exclusions == null || exclusions.Count == 0)
                    return;

                GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();
                warning.Title = WarningTitle;
                warning.Text = string.Join("<br/>",
                                    exclusions.Select(e => string.Format(WarningTextFormat,
                                                                e.PublishingTarget,
                                                                GetFormattedPublishModes(e.PublishModes))));
            }
            catch (Exception ex)
            {
                Log.Error("Sitecore.PublishExclusions : IsExcludedFromPublish processor - error in evaluating content editor warnings", ex, this);
            }
        }

        #endregion

        #region Others

        /// <summary>
        /// Returns comma-separated string of publish modes.
        /// Override this method if a different formatting is required.
        /// </summary>
        /// <param name="publishModes">List of publish modes that needs to be formatted</param>
        /// <returns>Comma-separated string of publish modes</returns>
        public virtual string GetFormattedPublishModes(List<PublishMode> publishModes)
        {
            if (publishModes == null || publishModes.Count == 0)
                return string.Empty;

            return string.Join(", ", publishModes.Select(pm => Enum.GetName(typeof(PublishMode), pm)));
        }

        #endregion

        #endregion
    }
}
