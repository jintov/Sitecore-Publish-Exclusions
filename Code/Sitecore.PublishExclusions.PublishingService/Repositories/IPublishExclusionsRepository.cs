namespace Sitecore.PublishExclusions.PublishingService.Repositories
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface that any Publish Exclusions Repository has to implement
    /// </summary>
    public interface IPublishExclusionsRepository
    {
        #region Methods

        /// <summary>
        /// Deletes excluded items from publish manifest
        /// </summary>
        /// <param name="manifestId">Current publish job's manifest id</param>
        /// <param name="publishingTargetId">Current publishing target's id</param>
        /// <param name="publishType">Current publishing mode / type</param>
        /// <returns>Asynchronous task that deletes excluded items from publish manifest</returns>
        Task DeleteExcludedItemsFromPublishManifest(Guid manifestId, Guid publishingTargetId, string publishType);

        #endregion
    }
}
