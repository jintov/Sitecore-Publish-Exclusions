namespace Sitecore.PublishExclusions.PublishingService.Providers
{
    using Dapper;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Sitecore.Framework.Conditions;
    using Sitecore.Framework.Publishing.Data;
    using System;
    using System.Threading.Tasks;

    public class PublishExclusionsProvider : IPublishExclusionsProvider<IDatabaseConnection>
    {
        #region Instance Fields

        private readonly ILogger<PublishExclusionsProvider> _logger;
        private readonly PublishExclusionsProviderOptions _options;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the Publish Exclusions Provider
        /// </summary>
        /// <param name="logger">Logger instance to log</param>
        /// <param name="options">Provider options containing publish mode IDs</param>
        public PublishExclusionsProvider(ILogger<PublishExclusionsProvider> logger, PublishExclusionsProviderOptions options)
        {
            Condition.Requires<ILogger<PublishExclusionsProvider>>(logger, "logger").IsNotNull<ILogger<PublishExclusionsProvider>>();
            this._logger = logger;
            this._options = options ?? new PublishExclusionsProviderOptions();
        }

        public PublishExclusionsProvider(ILogger<PublishExclusionsProvider> logger, IConfiguration config) :
            this(logger, config.As<PublishExclusionsProviderOptions>())
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Deletes excluded items from publish manifest
        /// </summary>
        /// <param name="connection">Data connection</param>
        /// <param name="manifestId">Current publishing's manifest id</param>
        /// <param name="publishingTargetId">Current publishing's target id</param>
        /// <param name="publishType">Current publishing mode / type</param>
        /// <returns>Asynchronous task that deletes excluded items from publish manifest</returns>
        public async Task DeleteExcludedItemsFromPublishManifest(IDatabaseConnection connection, Guid manifestId, Guid publishingTargetId, string publishType)
        {
            try
            {
                await connection.DbConnection.ExecuteAsync(
                    "Publishing_Delete_ExcludedItemsFromManifest",
                    (object)new
                    {
                        ManifestId = manifestId,
                        PublishingTargetId = publishingTargetId,
                        PublishType = publishType
                    },
                    connection.Transaction,
                    connection.CommandTimeout,
                    System.Data.CommandType.StoredProcedure).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError("Error in deleting publish excluded items from Manifest Steps table.", (object)ex);
                throw;
            }
        }

        #endregion

        #region Private Methods

        private string GetPublishModeId(string publishType)
        {
            switch (publishType.ToLowerInvariant())
            {
                case "single item" : return this._options.SingleItemPublish;
                case "full site publish" : return this._options.SitePublish;
                case "full publish" :
                case "full re-publish" : return this._options.FullPublish;
                default : return "XXX";
            }
        }

        #endregion
    }
}
