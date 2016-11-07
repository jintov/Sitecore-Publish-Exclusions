namespace Sitecore.PublishExclusions.PublishingService.Providers
{
    /// <summary>
    /// Contains IDs to the different publish modes that can be configured for publish exclusions when using Publishing Service
    /// </summary>
    public class PublishExclusionsProviderOptions
    {
        /// <summary>
        /// Single item publish, including publidhing child items and related items
        /// </summary>
        public string SingleItemPublish { get; set; } = "{58FB447B-0608-4E01-9359-27D5CF01DC96}";

        /// <summary>
        /// Site publish or incremental publish
        /// </summary>
        public string SitePublish { get; set; } = "{3D44B870-628F-45AB-A2CD-81DB11668AE2}";

        /// <summary>
        /// Full publish, including a full re-publish
        /// </summary>
        public string FullPublish { get; set; } = "{76013B45-C104-4077-B75A-6EBFC8A4BD44}";
    }
}
