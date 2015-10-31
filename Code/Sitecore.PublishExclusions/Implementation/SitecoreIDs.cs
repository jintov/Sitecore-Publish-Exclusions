namespace Sitecore.PublishExclusions
{
    using Sitecore.Data;

    /// <summary>
    /// Class containing Sitecore IDs of items used by Publish Exclusions module
    /// </summary>
    internal static class SitecoreIDs
    {
        #region Templates

        /// <summary>
        /// Class containing Sitecore IDs of templates
        /// </summary>
        internal static class Templates
        {
            /// <summary>
            /// Sitecore ID of "Publish Configuration" template
            /// </summary>
            internal static readonly ID PublishConfiguration = new ID("{FBB5D6F0-BFA6-4B44-B2B9-A4AD1E2204F9}");

            /// <summary>
            /// Sitecore ID of "Publish Exclusion" template
            /// </summary>
            internal static readonly ID PublishExclusion = new ID("{A23FF669-89DD-4CA7-AB42-6C75E8E891F5}");

        }

        #endregion

        #region Template Fields

        /// <summary>
        /// Class containing Sitecore IDs of template fields
        /// </summary>
        internal static class Fields
        {
            /// <summary>
            /// Sitecore ID of "Return Items To Publish Queue" field
            /// </summary>
            internal static readonly ID ReturnItemsToPublishQueue = new ID("{6EC4C789-A8EA-405E-AB9F-5A19C60FA3E8}");

            /// <summary>
            /// Sitecore ID of "Show Content Editor Warnings" field
            /// </summary>
            internal static readonly ID ShowContentEditorWarnings = new ID("{1A77BB65-5A46-468D-BA46-C9EA126A0385}");

            /// <summary>
            /// Sitecore ID of "Publishing Target" field
            /// </summary>
            internal static readonly ID PublishingTarget = new ID("{7CC04BA0-2E1F-44AB-AC8D-19F8CD05608B}");

            /// <summary>
            /// Sitecore ID of "Publish Modes" field
            /// </summary>
            internal static readonly ID PublishModes = new ID("{4F2D831B-DA0B-4E99-BE3A-577B4431A501}");

            /// <summary>
            /// Sitecore ID of "Excluded Nodes" field
            /// </summary>
            internal static readonly ID ExcludedNodes = new ID("{AF0EB394-C0F6-47DA-827E-A6D517168B59}");

            /// <summary>
            /// Sitecore ID of "Exclusion Overrides" field
            /// </summary>
            internal static readonly ID ExclusionOverrides = new ID("{9F003C6A-9093-4AF1-A373-C3BB0AD886DC}");
        }

        #endregion

        #region Items

        /// <summary>
        /// Class containing Sitecore IDs of items
        /// </summary>
        internal static class Items
        {
            /// <summary>
            /// Sitecore ID of "Global Configuration" item (/sitecore/system/Modules/Publish Exclusions/Global Configuration)
            /// </summary>
            internal static readonly ID GlobalConfiguration = new ID("{408BFC5F-4B97-44B7-BD74-423699C89788}");

            /// <summary>
            /// Sitecore ID of "Exclusions Repository" folder (/sitecore/system/Modules/Publish Exclusions/Exclusions Repository)
            /// </summary>
            internal static readonly ID ExclusionsRepository = new ID("{4FFE5F64-0A24-457A-B173-5544DD15575C}");
        }

        #endregion
    }
}
