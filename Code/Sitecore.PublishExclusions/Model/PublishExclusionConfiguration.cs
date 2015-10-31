namespace Sitecore.PublishExclusions.Model
{
    /// <summary>
    /// Represents the "Global Configuration" item
    /// </summary>
    public class PublishExclusionConfiguration
    {
        #region Properties

        /// <summary>
        /// Whether items that are skipped / excluded from incremental publish should be returned back to the Publish Queue or not
        /// true if item needs to be returned to Publish Queue, else false
        /// </summary>
        internal bool ReturnItemsToPublishQueue { get; set; }

        /// <summary>
        /// Whether a warning should be shown in Content Editor when the item is configured for publish exclusion
        /// true if warning should be shown, else false
        /// </summary>
        internal bool ShowContentEditorWarnings { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of PublishExclusionConfiguration class
        /// By default, ReturnItemsToPublishQueue = false and ShowContentEditorWarnings = true
        /// </summary>
        internal PublishExclusionConfiguration()
        {
            ReturnItemsToPublishQueue = false;
            ShowContentEditorWarnings = true;
        }

        #endregion
    }
}
