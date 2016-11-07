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
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Publish Handler used for all publishes except Site Publish
    /// </summary>
    public class TreePublishHandler : BaseHandler
    {
        #region Constructors

        public TreePublishHandler(IRequiredPublishFieldsResolver requiredPublishFieldsResolver, 
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

        public TreePublishHandler(IRequiredPublishFieldsResolver requiredPublishFieldsResolver, 
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
            return job.Options.ItemId.HasValue;
        }

        /// <summary>
        /// Creates a stream of items that are candidates for publishing
        /// </summary>
        protected override ISourceObservable<CandidateValidationContext> CreatePublishSourceStream(DateTime started, PublishOptions options, IPublishCandidateSource publishSourceRepository, IPublishValidator validator, IPublisherOperationService publisherOperationService, CancellationTokenSource errorSource)
        {
            IPublishCandidate result = publishSourceRepository.GetNode(options.ItemId.Value).Result;
            if (result == null)
                throw new ArgumentNullException(string.Format("The publish could not be performed from a start item that doesn't exist : {0}.", (object) options.ItemId.Value));

            IPublishCandidate publishCandidate =    result.ParentId.HasValue ? 
                                                    publishSourceRepository.GetNode(result.ParentId.Value).Result : 
                                                    result;

            ISourceObservable<CandidateValidationContext> sourceObservable = 
                (ISourceObservable<CandidateValidationContext>) new TreeNodeSourceProducer(
                    publishSourceRepository, 
                    result, 
                    validator, 
                    options.Descendants, 
                    this._options.SourceTreeReaderBatchSize, 
                    errorSource, 
                    (ILogger) LoggerFactoryExtensions.CreateLogger<TreeNodeSourceProducer>(this._loggerFactory));

            if (PublishOptionsMetadataExtensions.GetItemBucketsEnabled(options) && publishCandidate.Node.TemplateId == PublishOptionsMetadataExtensions.GetBucketTemplateId(options))
                sourceObservable = 
                    (ISourceObservable<CandidateValidationContext>) new BucketNodeSourceProducer(
                        sourceObservable, 
                        publishSourceRepository, 
                        result, 
                        PublishOptionsMetadataExtensions.GetBucketTemplateId(options), 
                        errorSource, 
                        (ILogger) LoggerFactoryExtensions.CreateLogger<BucketNodeSourceProducer>(this._loggerFactory));

            DeletedNodesSourceProducer dnsp = new DeletedNodesSourceProducer(
                sourceObservable,
                started,
                options.Languages,
                options.Targets,
                (IEnumerable<string>)new string[1] { PublishOptionsMetadataExtensions.GetPublishType(options) },
                publisherOperationService,
                this._options.UnpublishedOperationsLoadingBatchSize,
                errorSource,
                (ILogger)LoggerFactoryExtensions.CreateLogger<UnpublishedNodeSourceProducer>(this._loggerFactory),
                (Predicate<PublisherOperation>)(op => Enumerable.Contains<Guid>((IEnumerable<Guid>)op.Path.Ancestors, options.ItemId.Value)));

            return (ISourceObservable<CandidateValidationContext>)dnsp;
        }

        /// <summary>
        /// Creates a stream of items that are to be published
        /// </summary>
        protected override IObservable<CandidateValidationTargetContext> CreateTargetProcessingStream(DateTime started, IPublishCandidateSource publishSourceRepository, IPublishValidator validator, PublishOptions jobOptions, IObservable<CandidateValidationContext> publishStream, IItemIndexService targetIndex, ITestableContentRepository testableContentRepository, IMediaRepository targetMediaRepository, IRequiredPublishFieldsResolver requiredPublishFieldsResolver, CancellationTokenSource errorSource, Guid targetId)
        {
            IPublishCandidateTargetValidator publishCandidateTargetValidator = !PublishOptionsMetadataExtensions.GetItemBucketsEnabled(jobOptions) ? 
                                                            (IPublishCandidateTargetValidator)new PublishTargetParentValidator(publishSourceRepository, targetIndex) : 
                                                            (IPublishCandidateTargetValidator)new PublishTargetBucketParentValidator(publishSourceRepository, targetIndex, PublishOptionsMetadataExtensions.GetBucketTemplateId(jobOptions));

            publishStream = (IObservable<CandidateValidationContext>)new CandidatesParentValidationTargetProducer(
                publishStream,
                publishCandidateTargetValidator, 
                errorSource, 
                (ILogger)LoggerFactoryExtensions.CreateLogger<CandidatesParentValidationTargetProducer>(this._loggerFactory));

            return base.CreateTargetProcessingStream(
                started, 
                publishSourceRepository, 
                validator, 
                jobOptions, 
                publishStream, 
                targetIndex, 
                testableContentRepository, 
                targetMediaRepository, 
                requiredPublishFieldsResolver, 
                errorSource, 
                targetId);
        }

        #endregion  
    }
}
