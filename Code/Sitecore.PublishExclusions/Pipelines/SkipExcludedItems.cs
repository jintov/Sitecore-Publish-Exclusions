namespace Sitecore.PublishExclusions
{
    using Data.Items;
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

                if (context.VersionToPublish == null)
                {
                    // Case 1: handled case where deleted items also should get excluded from publishing
                    // Case 2: handled case where shared fields of an item should be excluded from publishing
                    if (context.Action == PublishAction.DeleteTargetItem && context.PublishOptions != null)
                    {
                        Item deletedItem = context.PublishOptions.TargetDatabase.GetItem(context.ItemId);
                        if (deletedItem == null)
                            return;

                        context.VersionToPublish = deletedItem;
                    }
                    else if (context.Action == PublishAction.PublishSharedFields && context.PublishOptions != null)
                    {
                        Item sharedItem = context.PublishOptions.SourceDatabase.GetItem(context.ItemId);
                        if (sharedItem == null)
                            return;

                        context.VersionToPublish = sharedItem;
                    }
                    else
                        return;
                }

                PublishingLog.Debug(string.Format("Sitecore.PublishExclusions : SkipExcludedItems processing item - '{0}'", context.VersionToPublish.Paths.Path));

                // Check if item comes under any one of exluded nodes and also not under included nodes then abort pipeline.
                if (PublishExclusionsContext.Current.IsExcludedForCurrentPublish(context))
                {
                    PublishingLog.Debug(string.Format("Sitecore.PublishExclusions : SkipExcludedItems skipping item - '{0}'", context.VersionToPublish.Paths.Path));

                    string explanation = string.Format(ExplanationTextFormat, context.VersionToPublish.Paths.Path);
                    context.Result = new PublishItemResult(PublishOperation.Skipped, PublishChildAction.Skip, explanation, PublishExclusionsContext.Current.ReturnItemsToPublishQueue);
                    context.AbortPipeline();
                }

                //if publish action item is shared fields and version to publish has been manually set to an item then set it back to null
                if (context.Action == PublishAction.PublishSharedFields && context.VersionToPublish != null)
                    context.VersionToPublish = null;
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
