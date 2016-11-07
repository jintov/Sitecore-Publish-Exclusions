namespace Sitecore.PublishExclusions.PublishingService.Repositories
{
    using Sitecore.Framework.Conditions;
    using Sitecore.Framework.Publishing.Data;
    using Sitecore.PublishExclusions.PublishingService.Providers;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents Publish Exclusions Repository class
    /// </summary>
    public class PublishExclusionsRepository : IPublishExclusionsRepository
    {
        #region Instance Fields

        private readonly IDataStore<IDatabaseConnection> _dataStore;
        private readonly IPublishExclusionsProvider<IDatabaseConnection> _dataProvider;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the Publish Exclusions Repository
        /// </summary>
        /// <param name="dataProvider">Publish Exclusions data provider</param>
        /// <param name="dataStore">Publish Exclusions data store</param>
        public PublishExclusionsRepository(IPublishExclusionsProvider<IDatabaseConnection> dataProvider, IDataStore<IDatabaseConnection> dataStore)
        {
            Condition.Requires<IPublishExclusionsProvider<IDatabaseConnection>>(dataProvider, "dataProvider").IsNotNull<IPublishExclusionsProvider<IDatabaseConnection>>();
            Condition.Requires<IDataStore<IDatabaseConnection>>(dataStore, "dataStore").IsNotNull<IDataStore<IDatabaseConnection>>();

            _dataStore = dataStore;
            _dataProvider = dataProvider;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Deletes excluded items from publish manifest by leveraging the corresponding method of the data provider
        /// </summary>
        /// <param name="manifestId">Current publish job's manifest id</param>
        /// <param name="publishingTargetId">Current publishing target's id</param>
        /// <param name="publishType">Current publishing mode / type</param>
        /// <returns>Asynchronous task that deletes excluded items from publish manifest</returns>
        public async Task DeleteExcludedItemsFromPublishManifest(Guid manifestId, Guid publishingTargetId, string publishType)
        {
            await _dataProvider.DeleteExcludedItemsFromPublishManifest(this._dataStore.Connection, manifestId, publishingTargetId, publishType);
        }

        #endregion
    }
}
