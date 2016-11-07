namespace Sitecore.PublishExclusions.PublishingService.Repositories
{
    using Sitecore.Framework.Publishing.Data;
    using Sitecore.Framework.Publishing.Data.Repository;
    using System;

    /// <summary>
    /// Class that builds the Publish Exclusions Repository from the configuration 
    /// by leveraging the DefaultRepositoryBuilder of Sitecore's Publishing Service
    /// </summary>
    public class PublishExclusionsRepositoryBuilder : DefaultRepositoryBuilder<IPublishExclusionsRepository, PublishExclusionsRepository, IDatabaseConnection>
    {
        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="services">An instance of IServiceProvider</param>
        public PublishExclusionsRepositoryBuilder(IServiceProvider services) : base(services)
        {
        }

        #endregion
    }
}
