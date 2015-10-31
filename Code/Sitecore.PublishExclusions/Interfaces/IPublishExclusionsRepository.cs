namespace Sitecore.PublishExclusions
{
    using Sitecore.PublishExclusions.Model;
    using System.Collections.Generic;

    /// <summary>
    /// Interface that any Publish Exclusions Repository will need to implement for the Publish Exclusions functionality
    /// </summary>
    public interface IPublishExclusionsRepository
    {
        #region Properties

        /// <summary>
        /// Gets the global publish exclusion configuration
        /// </summary>
        PublishExclusionConfiguration GlobalConfiguration { get; }

        /// <summary>
        /// Get the collection of publish exclusions configured
        /// </summary>
        List<PublishExclusion> PublishExclusions { get; }

        #endregion

        #region Methods

        /// <summary>
        /// To initializes the repository - typically during Sitecore startup
        /// </summary>
        void Initialize();

        /// <summary>
        /// To re-initialize the repository if any changes to publish exclusions have happened
        /// </summary>
        void ReInitialize();

        #endregion
    }
}
