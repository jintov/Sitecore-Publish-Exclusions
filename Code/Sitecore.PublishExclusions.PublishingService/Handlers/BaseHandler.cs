namespace Sitecore.PublishExclusions.PublishingService.Handlers
{
    using Microsoft.AspNetCore.Hosting;
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
    using Sitecore.Framework.Publishing.PublishJobQueue.Events;
    using Sitecore.Framework.Publishing.Repository;
    using Sitecore.Framework.Publishing.TemplateGraph;
    using Sitecore.Framework.Publishing.Workflow;
    using Sitecore.PublishExclusions.PublishingService.Repositories;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;
    using sc = Sitecore.Framework.Publishing.PublishJobQueue.Handlers;

    /// <summary>
    /// Base Publish Handler that inherits from Sitecore's default Publish Handler
    /// Contains logic to delete excluded items from publish manifest
    /// Individual publish handlers need to inherit from this
    /// </summary>
    public abstract class BaseHandler : sc.BaseHandler
    {
        #region Instance Fields

        private string _sourceName;

        protected readonly IRepositoryFactory<IPublishExclusionsRepository> _publishExclusionsRepositoryFactory;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the publish handler
        /// </summary>
        public BaseHandler(IRequiredPublishFieldsResolver requiredPublishFieldsResolver, 
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
            PublishJobHandlerOptions options) 
            : base(requiredPublishFieldsResolver, publisherOpsService, dataStoreFactory, mediaRepositoryFactory, targetIndexRepositoryFactory, itemRepositoryFactory, itemRelationshipRepoFactory, itemNodeRepositoryFactory, templateGraphRepositoryFactory, indexablePublishTargetRepositoryFactory, workflowRepositoryFactory, testableContentRepositoryFactory, manifestRepositoryFactory, promoterCoordinator, eventRegistry, loggerFactory, applicationLifetime, options)
        {
            _publishExclusionsRepositoryFactory = publishExclusionsRepositoryFactory;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles the publish job, including deleting excluded items from publish manifest
        /// </summary>
        public override async Task<PublishResult> Handle(Guid jobId, IDataStore sourceDataStore, IEnumerable<IDataStore> targetConnections, IDataStore relationshipDataStore, DateTime started, PublishOptions publishOptions)
        {
            CancellationTokenSource errorSource = CancellationTokenSource.CreateLinkedTokenSource(this._appLifetime.ApplicationStopping);
            if (sourceDataStore == null)
                return PublishResult.Failed(string.Format("No source connection was found with identifier '{0}'.", (object)publishOptions.Source), (Exception)null);
            if (!publishOptions.Targets.Any<string>())
                return PublishResult.Failed("No target connections were specified.", (Exception)null);
            if (!targetConnections.Any<IDataStore>())
                return PublishResult.Failed(string.Format("No target connections were found with identifiers '{0}'.", (object)string.Join(",", publishOptions.Targets)), (Exception)null);
            if (targetConnections.Count<IDataStore>() != publishOptions.Targets.Count<string>())
                this._logger.LogWarning(string.Format("Not all targets specified for the job are registered in the Publishing Service. Targets [{0}] are not registered.", (object)string.Join(", ", publishOptions.Targets.Except<string>(targetConnections.Select<IDataStore, string>((Func<IDataStore, string>)(c => c.Name)), (IEqualityComparer<string>)StringComparer.InvariantCultureIgnoreCase))));

            this._sourceName = sourceDataStore.Name;

            IItemNodeRepository sourceItemNodeRepository = this._itemNodeRepositoryFactory.Create(sourceDataStore);
            sourceItemNodeRepository.InitialiseNodeFilterParameters(publishOptions.Languages.ToArray<string>(), this._requiredPublishFieldsResolver.PublishingFieldsIds).Wait();
            IDataStore dataStore = this.DataStoreFactory.ServiceStore();

            IItemIndexRepository targetIndexRepository = this._targetIndexRepositoryFactory.Create(dataStore);
            IItemRelationshipRepository sourceItemRelationshipRepo = this._itemRelationshipRepoFactory.Create(relationshipDataStore);
            ITemplateGraphRepository sourceTemplateGraphRepository = this._templateGraphRepositoryFactory.Create(sourceDataStore);
            IWorkflowStateRepository workflowRepository = this._workflowRepositoryFactory.Create(sourceDataStore);
            ITestableContentRepository testableContentRepository = this._testableContentRepositoryFactory.Create(sourceDataStore);
            IManifestRepository manifestRepository = this._manifestRepositoryFactory.Create(dataStore);
            IPublishExclusionsRepository publishExclusionsRepository = this._publishExclusionsRepositoryFactory.Create(sourceDataStore);
            ITemplateGraph templateGraph = this.CreateTemplateGraph(publishOptions, sourceTemplateGraphRepository, sourceItemNodeRepository);
            IPublishCandidateSource publishCandidateSource = this.CreatePublishCandidateSource(publishOptions, sourceItemNodeRepository, sourceItemRelationshipRepo, templateGraph, workflowRepository, this._requiredPublishFieldsResolver);
            IPublishValidator publishValidator = this.CreatePublishValidator(started, publishOptions, publishCandidateSource);

            string stage = string.Empty;
            try
            {
                if (this._logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug) || this._logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
                    this._logger.LogDebug("Executing Publish : \nHandler: {HandlerName}, \nSource: {SourceIdentifier}, \nTargets: {TargetIdentifiers}, \nStart Item: {Id}, \nLanguages: {Languages}, \nProcess Descendants: {Descendants}, \nProcess Related Items: {Related}, \nMetadata: {@Metadata}", (object)this.GetType().Name, (object)publishOptions.Source, (object)publishOptions.Targets, (object)publishOptions.ItemId, (object)publishOptions.Languages, (object)publishOptions.Descendants, (object)publishOptions.RelatedItems, (object)publishOptions.Metadata);
                else
                    this._logger.LogInformation("Executing Publish : \nSource: {SourceIdentifier}, \nTargets: {TargetIdentifiers}, \nStart Item: {Id}, \nLanguages: {Languages}, \nProcess Descendants: {Descendants}, \nProcess Related Items: {Related}\n", (object)publishOptions.Source, (object)publishOptions.Targets, (object)publishOptions.ItemId, (object)publishOptions.Languages, (object)publishOptions.Descendants, (object)publishOptions.RelatedItems);
                stage = "Initializing the manifest calculation system (source)";
                this._logger.LogTrace(stage);
                errorSource.Token.ThrowIfCancellationRequested();

                ISourceObservable<CandidateValidationContext> publishSourceStream = this.CreatePublishSourceStream(started, publishOptions, publishCandidateSource, publishValidator, this._publisherOpsService, errorSource);
                Dictionary<Guid, IObservable<ICandidateOperation>> targetOpsStreams = new Dictionary<Guid, IObservable<ICandidateOperation>>();
                foreach (IDataStore targetConnection in targetConnections)
                {
                    errorSource.Token.ThrowIfCancellationRequested();
                    Guid id = targetConnection.Connection.Id;
                    stage = "Initializing the manifest calculation system (target: {TargetId})";
                    this._logger.LogTrace(stage, (object)id);
                    IItemIndexService targetIndex = this.CreateTargetIndex(targetConnection, targetIndexRepository, id);
                    IMediaRepository targetMediaRepository = this._mediaRepositoryFactory.Create(targetConnection);
                    IObservable<ICandidateOperation> operationsStream = this.CreateTargetOperationsStream(started, publishCandidateSource, publishValidator, publishOptions, (IObservable<CandidateValidationContext>)publishSourceStream, targetIndex, testableContentRepository, targetMediaRepository, this._requiredPublishFieldsResolver, errorSource, id);
                    targetOpsStreams.Add(id, operationsStream);
                }

                stage = "Initializing the manifest builder";
                this._logger.LogTrace(stage);
                IManifestSink sink = this.CreateManifestSink(targetOpsStreams, manifestRepository, errorSource);

                stage = "Starting the manifest calculation for all the targets";
                this._logger.LogInformation(stage);
                errorSource.Token.ThrowIfCancellationRequested();
                await publishSourceStream.Pump().ConfigureAwait(false);
                errorSource.Token.ThrowIfCancellationRequested();
                ManifestStatus[] manifestStatusArray = await sink.Complete().ConfigureAwait(false);
                errorSource.Token.ThrowIfCancellationRequested();
                sink = (IManifestSink)null;

                //Delete excluded items from manifest - BEGIN
                stage = "Deleting publish excluded items from the manifest (Target: {0}, Manifest: {1})";
                foreach (ManifestStatus manifestStatus in manifestStatusArray)
                {
                    this._logger.LogInformation(stage, manifestStatus.TargetId, manifestStatus.ManifestId);
                    await publishExclusionsRepository.DeleteExcludedItemsFromPublishManifest(manifestStatus.ManifestId, manifestStatus.TargetId, publishOptions.GetPublishType());
                }
                //Delete excluded items from manifest - END

                stage = "Starting to promote the manifests to all the targets";
                this._logger.LogInformation(stage);
                this._attachManifests.Send(new PublishJobAttachManifestsEvent.Args(jobId, ((IEnumerable<ManifestStatus>)manifestStatusArray).Select<ManifestStatus, Guid>((Func<ManifestStatus, Guid>)(m => m.ManifestId)).ToArray<Guid>()), this.GetType().Name).Wait(errorSource.Token);
                TargetPromoteContext[] promotionContexts = ((IEnumerable<ManifestStatus>)manifestStatusArray).Select<ManifestStatus, TargetPromoteContext>((Func<ManifestStatus, TargetPromoteContext>)(m => new TargetPromoteContext(targetConnections.First<IDataStore>((Func<IDataStore, bool>)(c => c.Connection.Id == m.TargetId)), m, !publishOptions.GetClearAllCaches()))).ToArray<TargetPromoteContext>();
                errorSource.Token.ThrowIfCancellationRequested();

                IManifestOperationResult[] manifestOperationResultArray = 
                    await this._promoterCoordinator.PromoteAll( sourceDataStore, 
                        relationshipDataStore, 
                        (IEnumerable<TargetPromoteContext>)promotionContexts, 
                        this.CreateExecutionStrategy(manifestRepository, errorSource.Token))
                        .ConfigureAwait(false);

                IManifestOperationResult[] array2 = ((IEnumerable<IManifestOperationResult>)manifestOperationResultArray).Where<IManifestOperationResult>((Func<IManifestOperationResult, bool>)(r => r.Operation.Status == TaskStatus.Faulted)).ToArray<IManifestOperationResult>();
                if (((IEnumerable<IManifestOperationResult>)array2).Any<IManifestOperationResult>())
                    throw new AggregateException(string.Format("One or more targets were not succesfully promoted.  The failed target(s) are: {0}.\n See inner exceptions for details.", (object)string.Join("\n", ((IEnumerable<IManifestOperationResult>)array2).Select<IManifestOperationResult, string>((Func<IManifestOperationResult, string>)(r => string.Format("Manifest={0} Target={1}", (object)r.ManifestId, (object)r.DataStore.Connection.Id))))), (IEnumerable<Exception>)((IEnumerable<IManifestOperationResult>)array2).Select<IManifestOperationResult, AggregateException>((Func<IManifestOperationResult, AggregateException>)(r => r.Operation.Exception)));
                Func<IManifestOperationResult, bool> func = (Func<IManifestOperationResult, bool>)(r => r.Operation.IsCanceled);
                if (((IEnumerable<IManifestOperationResult>)manifestOperationResultArray).Any<IManifestOperationResult>(func))
                    errorSource.Cancel();
                errorSource.Token.ThrowIfCancellationRequested();
                return PublishResult.Complete("OK");
            }
            catch (OperationCanceledException ex)
            {
                this._logger.LogWarning("Publish job received a cancellation token during stage: {stage}.", (object)stage);
                return !this._appLifetime.ApplicationStopping.IsCancellationRequested ? PublishResult.Cancelled(string.Format("Job cancelled. Message: {0}", (object)ex.Message)) : PublishResult.Cancelled("Job cancelled. Service is shutting down.");
            }
            catch (Exception ex)
            {
                this._logger.LogError(new EventId(), ex, "There was an error performing the publish during stage: {Stage}", (object)stage);
                return PublishResult.Failed(string.Format("There was an error performing the publish during stage: {0}.", (object)stage), ex);
            }
        }

        /// <summary>
        /// Creates a collection of target operations for items to be published
        /// </summary>
        protected override IObservable<ICandidateOperation> CreateTargetOperationsStream(DateTime started, IPublishCandidateSource publishCandidateSource, IPublishValidator validator, PublishOptions jobOptions, IObservable<CandidateValidationContext> publishStream, IItemIndexService targetIndex, ITestableContentRepository testableContentRepository, IMediaRepository targetMediaRepository, IRequiredPublishFieldsResolver requiredPublishFieldsResolver, CancellationTokenSource errorSource, Guid targetId)
        {
            IObservable<CandidateValidationTargetContext> processingStream1 = this.CreateTargetProcessingStream(started, publishCandidateSource, validator, jobOptions, publishStream, targetIndex, testableContentRepository, targetMediaRepository, this._requiredPublishFieldsResolver, errorSource, targetId);
            RelatedNodesSourceProducer nodesSourceProducer = new RelatedNodesSourceProducer(processingStream1.Where<CandidateValidationTargetContext>((Func<CandidateValidationTargetContext, bool>)(ctx => ctx.IsValid)).Select<CandidateValidationTargetContext, ValidCandidateTargetContext>((Func<CandidateValidationTargetContext, ValidCandidateTargetContext>)(ctx => ctx.AsValid())), publishCandidateSource, validator, this._options.RelatedItemBatchSize, jobOptions.RelatedItems, jobOptions.GetDetectCloneSources(), errorSource, this._logger);
            IObservable<CandidateValidationTargetContext> processingStream2 = this.CreateTargetProcessingStream(started, publishCandidateSource, validator, jobOptions, (IObservable<CandidateValidationContext>)nodesSourceProducer, targetIndex, testableContentRepository, targetMediaRepository, this._requiredPublishFieldsResolver, errorSource, targetId);
            IConnectableObservable<CandidateValidationTargetContext> source = processingStream1.Merge<CandidateValidationTargetContext>(processingStream2).Publish<CandidateValidationTargetContext>();
            source.Connect();
            UpdatedCandidatesOperationsProducer operationsProducer1 = new UpdatedCandidatesOperationsProducer(source.Where<CandidateValidationTargetContext>((Func<CandidateValidationTargetContext, bool>)(ctx => ctx.IsValid)).Select<CandidateValidationTargetContext, ValidCandidateTargetContext>((Func<CandidateValidationTargetContext, ValidCandidateTargetContext>)(ctx => ctx.AsValid())), started, errorSource, this._loggerFactory.CreateLogger<UpdatedCandidatesOperationsProducer>());
            MediaOperationsProducer operationsProducer2 = new MediaOperationsProducer(source.Where<CandidateValidationTargetContext>((Func<CandidateValidationTargetContext, bool>)(ctx => ctx.IsValid)).Select<CandidateValidationTargetContext, ValidCandidateTargetContext>((Func<CandidateValidationTargetContext, ValidCandidateTargetContext>)(ctx => ctx.AsValid())), targetMediaRepository, requiredPublishFieldsResolver.MediaFieldsIds, started, this._options.MediaBatchSize, errorSource, this._loggerFactory.CreateLogger<MediaOperationsProducer>());
            IConnectableObservable<ICandidateOperation> connectableObservable = Observable.Merge<ICandidateOperation>(new IObservable<ICandidateOperation>[3]
            {
                (IObservable<ICandidateOperation>) new DeletedCandidateOperationsProducer(source.Where<CandidateValidationTargetContext>((Func<CandidateValidationTargetContext, bool>) (ctx => !ctx.IsValid)).Select<CandidateValidationTargetContext, Guid>((Func<CandidateValidationTargetContext, Guid>) (ctx => ctx.AsInvalid().Id)), targetIndex, started, this._options.DeletedItemsBatchSize, errorSource, this._sourceName, this._loggerFactory.CreateLogger<DeletedCandidateOperationsProducer>()),
                (IObservable<ICandidateOperation>) operationsProducer1,
                (IObservable<ICandidateOperation>) operationsProducer2
            }).Publish<ICandidateOperation>();
            connectableObservable.Connect();
            return (IObservable<ICandidateOperation>)connectableObservable;
        }

        #endregion
    }
}
