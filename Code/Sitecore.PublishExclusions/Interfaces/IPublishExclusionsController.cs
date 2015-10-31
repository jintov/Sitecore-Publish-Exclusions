namespace Sitecore.PublishExclusions
{
    using Sitecore.Data.Items;
    using Sitecore.PublishExclusions.Model;
    using Sitecore.Publishing.Pipelines.PublishItem;
    using System.Collections.Generic;

    /// <summary>
    /// Interface that any Publish Exclusions Controller will need to implement for the Publish Exclusions functionality
    /// </summary>
    public interface IPublishExclusionsController
    {
        #region Properties

        /// <summary>
        /// Instance of the repository used by the controller
        /// </summary>
        IPublishExclusionsRepository Repository { get; }

        /// <summary>
        /// Whether items that are skipped / excluded from incremental publish should be returned back to the Publish Queue or not
        /// true if item needs to be returned to Publish Queue, else false
        /// </summary>
        bool ReturnItemsToPublishQueue { get; }

        /// <summary>
        /// Whether a warning should be shown in Content Editor when the item is configured for publish exclusion
        /// true if warning should be shown, else false
        /// </summary>
        bool ShowContentEditorWarnings { get; }

        #endregion

        #region Methods

        /// <summary>
        /// To initialize the controller and/or any dependent repository
        /// </summary>
        void Initialize();

        /// <summary>
        /// To reinitialize the controller and/or any dependent repository, in case of any changes to the exclusion rules
        /// </summary>
        void ReInitialize();

        /// <summary>
        /// Determines if the current item that is being published is excluded from publish for the publishing target and publish mode
        /// </summary>
        /// <param name="context">current item's publish context</param>
        /// <returns>true if item is excluded from current publish, else false</returns>
        bool IsExcludedForCurrentPublish(PublishItemContext context);

        /// <summary>
        /// Returns all applicable publish exclusions for the item being passed
        /// </summary>
        /// <param name="item">Item for which publish exclusions are to be retrieved</param>
        /// <returns>Applicable publish exclusions for the item</returns>
        List<PublishExclusion> GetAllPublishExclusions(Item item);

        #endregion
    }
}
