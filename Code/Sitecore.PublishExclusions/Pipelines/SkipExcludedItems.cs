namespace Sitecore.PublishExclusions
{
    using Sitecore.Diagnostics;
    using Sitecore.Publishing;
    using Sitecore.Publishing.Diagnostics;
    using Sitecore.Publishing.Pipelines.PublishItem;
    using System;

    /// <summary>
    /// Pipeline processor used to skip items from publishing if they are configured for publish exclusion
    /// </summary>
    public class SkipExcludedItems : PublishItemProcessor
    {
        #region Constants

        private const string ExplanationTextFormat = "Item '{0}' configured to be excluded from publish";

        #endregion

        #region Public Methods

        #region Called by Sitecore

        /// <summary>
        /// Called by publishItem pipeline to determine if item should be excluded from publishing or not
        /// </summary>
        /// <param name="context">context of the item being published</param>
        public override void Process(PublishItemContext context)
        {
            ProcessPublishItem(context);
        }

        #endregion

        #region Overridable Methods

        /// <summary>
        /// Determine if current item should be excluded from publishing or not.
        /// If it should be excluded, the current pipeline is aborted.
        /// Override this method for any custom implementation logic.
        /// </summary>
        /// <param name="context">context of the item being published</param>
        protected virtual void ProcessPublishItem(PublishItemContext context)
        {
            try
            {
                Assert.ArgumentNotNull((object)context, "context");
                Assert.ArgumentNotNull((object)context.PublishOptions, "context.PublishOptions");
                Assert.ArgumentNotNull((object)context.VersionToPublish, "context.VersionToPublish");

                PublishingLog.Debug(string.Format("Sitecore.PublishExclusions : SkipExcludedItems processing item - '{0}'", context.VersionToPublish.Paths.Path));

                // Check if item comes under any one of exluded nodes and also not under included nodes then abort pipeline.
                if (PublishExclusionsContext.Current.IsExcludedForCurrentPublish(context))
                {
                    PublishingLog.Debug(string.Format("Sitecore.PublishExclusions : SkipExcludedItems skipping item - '{0}'", context.VersionToPublish.Paths.Path));

                    string explanation = string.Format(ExplanationTextFormat, context.VersionToPublish.Paths.Path);
                    context.Result = new PublishItemResult(PublishOperation.Skipped, PublishChildAction.Skip, explanation, PublishExclusionsContext.Current.ReturnItemsToPublishQueue);
                    context.AbortPipeline();
                }
            }
            catch (Exception ex)
            {
                PublishingLog.Error("Sitecore.PublishExclusions : SkipExcludedItems publish item processor - error in skipping excluded items", ex);
            }
        }

        #endregion

        #endregion
    }
}
