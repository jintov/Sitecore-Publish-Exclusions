namespace Sitecore.PublishExclusions.Model
{
    using Sitecore.Publishing;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a single "Publish Exclusion" rule
    /// </summary>
    public class PublishExclusion
    {
        #region Properties

        /// <summary>
        /// Name of the publish exclusion item
        /// </summary>
        internal string Name { get; set; }

        /// <summary>
        /// Publishing target name
        /// </summary>
        internal string PublishingTarget { get; set; }

        /// <summary>
        /// ID of the publishing target
        /// </summary>
        internal string PublishingTargetID { get; set; }

        /// <summary>
        /// Selected publish modes
        /// </summary>
        internal List<PublishMode> PublishModes { get; set; }

        /// <summary>
        /// Nodes that have been excluded from publish
        /// </summary>
        internal List<string> ExcludedNodes { get; set; }

        /// <summary>
        /// Nodes that will be published even if configured for exclusion
        /// </summary>
        internal List<string> ExclusionOverrides { get; set; }

        #endregion
    }
}
