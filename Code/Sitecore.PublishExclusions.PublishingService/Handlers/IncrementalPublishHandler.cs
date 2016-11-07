namespace Sitecore.PublishExclusions.PublishingService.Handlers
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Sitecore.Framework.Eventing;
    using Sitecore.Framework.Publishing;
    using Sitecore.Framework.Publishing.ContentTesting;
    using Sitecore.Framework.Publishing.Data;
    using Sitecore.Framework.Publishing.DataPromotion;
    using Sitecore.Framework.Publishing.Item;
    using Sitecore.Framework.Publishing.ItemIndex;
    using Sitecore.Framework.Publishing.Manifest;
    using Sitecore.Framework.Publishing.ManifestCalculation;
    using Sitecore.Framework.Publishing.PublisherOperation;
    using Sitecore.Framework.Publishing.PublishJobQueue;
    using Sitecore.Framework.Publishing.Repository;
    using Sitecore.Framework.Publishing.Workflow;
    using Sitecore.PublishExclusions.PublishingService.Repositories;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Incremental Publish Handler used when doing a Site publish
    /// </summary>
    public class IncrementalPublishHandler : BaseHandler
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of Incremental Publish Handler
        /// </summary>
        public IncrementalPublishHandler(IRequiredPublishFieldsResolver requiredPublishFieldsResolver,
            IPublisherOperationService publisherOpsService,
            IDataStoreFactory dataStoreFactory,
            IRepositoryFactory<IMediaRepository> mediaRepositoryFactory,
            IRepositoryFactory<IItemIndexRepository> targetIndexRepositoryFactory,
            IRepositoryFactory<IItemRepository> itemRepositoryFactory,
            IRepositoryFactory<IItemRelationshipRepository> itemRelationshipRepoFactory,
            IRepositoryFactory<IItemNodeRepository> itemNodeRepositoryFactory,
            IRepositoryFactory<ITemplateGraphRepository> templateGraphRepositoryFactory,
            IRepositoryFactory<IIndexableItemRepository> indexablePublishTargetRepositoryFactory,
            IRepositoryFactory<IWorkflowStateRepository> workflowRepositoryFactory,
            IRepositoryFactory<ITestableContentRepository> testableContentRepositoryFactory,
            IRepositoryFactory<IManifestRepository> manifestRepositoryFactory,
            IRepositoryFactory<IPublishExclusionsRepository> publishExclusionsRepositoryFactory,
            IPromotionCoordinator promoterCoordinator,
            IEventRegistry eventRegistry,
            ILoggerFactory loggerFactory,
            IApplicationLifetime applicationLifetime,
            PublishJobHandlerOptions options = null)
          : base(requiredPublishFieldsResolver, publisherOpsService, dataStoreFactory, mediaRepositoryFactory, targetIndexRepositoryFactory, itemRepositoryFactory, itemRelationshipRepoFactory, itemNodeRepositoryFactory, templateGraphRepositoryFactory, indexablePublishTargetRepositoryFactory, workflowRepositoryFactory, testableContentRepositoryFactory, manifestRepositoryFactory, publishExclusionsRepositoryFactory, promoterCoordinator, eventRegistry, loggerFactory, applicationLifetime, options ?? new PublishJobHandlerOptions())
        {
        }

        /// <summary>
        /// Creates a new instance of Incremental Publish Handler
        /// </summary>
        public IncrementalPublishHandler(IRequiredPublishFieldsResolver requiredPublishFieldsResolver,
            IPublisherOperationService publisherOpsService,
            IDataStoreFactory dataStoreFactory,
            IRepositoryFactory<IMediaRepository> mediaRepositoryFactory,
            IRepositoryFactory<IItemIndexRepository> targetIndexRepositoryFactory,
            IRepositoryFactory<IItemRepository> itemRepositoryFactory,
            IRepositoryFactory<IItemRelationshipRepository> itemRelationshipRepoFactory,
            IRepositoryFactory<IItemNodeRepository> itemNodeRepositoryFactory,
            IRepositoryFactory<ITemplateGraphRepository> templateGraphRepositoryFactory,
            IRepositoryFactory<IIndexableItemRepository> indexablePublishTargetRepositoryFactory,
            IRepositoryFactory<IWorkflowStateRepository> workflowRepositoryFactory,
            IRepositoryFactory<ITestableContentRepository> testableContentRepositoryFactory,
            IRepositoryFactory<IManifestRepository> manifestRepositoryFactory,
            IRepositoryFactory<IPublishExclusionsRepository> publishExclusionsRepositoryFactory,
            IPromotionCoordinator promoterCoordinator,
            IEventRegistry eventRegistry,
            ILoggerFactory loggerFactory,
            IApplicationLifetime applicationLifetime,
            IConfiguration config)
          : this(requiredPublishFieldsResolver, publisherOpsService, dataStoreFactory, mediaRepositoryFactory, targetIndexRepositoryFactory, itemRepositoryFactory, itemRelationshipRepoFactory, itemNodeRepositoryFactory, templateGraphRepositoryFactory, indexablePublishTargetRepositoryFactory, workflowRepositoryFactory, testableContentRepositoryFactory, manifestRepositoryFactory, publishExclusionsRepositoryFactory, promoterCoordinator, eventRegistry, loggerFactory, applicationLifetime, SitecoreConfigurationExtensions.As<PublishJobHandlerOptions>(config))
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines if this publish handler can perform the current publishing job
        /// </summary>
        /// <returns>True if it can publish, else false</returns>
        public override bool CanHandle(PublishJob job, IDataStore from, IEnumerable<IDataStore> to)
        {
            return !job.Options.ItemId.HasValue;
        }

        /// <summary>
        /// Creates a stream of items that are candidates for publishing
        /// </summary>
        protected override ISourceObservable<CandidateValidationContext> CreatePublishSourceStream(DateTime started, PublishOptions options, IPublishCandidateSource publishSourceRepository, IPublishValidator validator, IPublisherOperationService publisherOperationService, CancellationTokenSource errorSource)
        {
            UnpublishedNodeSourceProducer unsp = new UnpublishedNodeSourceProducer(
                started,
                options.Languages,
                options.Targets,
                (IEnumerable<string>)new string[1] { PublishOptionsMetadataExtensions.GetPublishType(options) },
                publishSourceRepository,
                publisherOperationService,
                validator,
                this._options.UnpublishedOperationsLoadingBatchSize,
                errorSource,
                errorSource.Token,
                (ILogger)LoggerFactoryExtensions.CreateLogger<UnpublishedNodeSourceProducer>(this._loggerFactory));

            DeletedNodesSourceProducer dnsp = new DeletedNodesSourceProducer(
                (ISourceObservable<CandidateValidationContext>)unsp,
                started,
                options.Languages,
                options.Targets,
                (IEnumerable<string>)new string[1] { PublishOptionsMetadataExtensions.GetPublishType(options) },
                publisherOperationService,
                this._options.UnpublishedOperationsLoadingBatchSize,
                errorSource,
                (ILogger)LoggerFactoryExtensions.CreateLogger<UnpublishedNodeSourceProducer>(this._loggerFactory),
                null);

            return (ISourceObservable<CandidateValidationContext>)dnsp;
        }

        #endregion
    }
}
