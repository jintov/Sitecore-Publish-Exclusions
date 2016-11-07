namespace Sitecore.PublishExclusions.PublishingService.Providers
{
    using Sitecore.Framework.Publishing.Data;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface that any Publish Exclusions provider has to implement
    /// </summary>
    /// <typeparam name="T">Data connection type</typeparam>
    public interface IPublishExclusionsProvider<T> where T : IConnection
    {
        #region Methods

        /// <summary>
        /// Deletes excluded items from publish manifest
        /// </summary>
        /// <param name="connection">Data connection</param>
        /// <param name="manifestId">Current publishing's manifest id</param>
        /// <param name="publishingTargetId">Current publishing's target id</param>
        /// <param name="publishType">Current publishing mode / type</param>
        /// <returns>Asynchronous task that deletes excluded items from publish manifest</returns>
        Task DeleteExcludedItemsFromPublishManifest(T connection, Guid manifestId, Guid publishingTargetId, string publishType);

        #endregion
    }
}
