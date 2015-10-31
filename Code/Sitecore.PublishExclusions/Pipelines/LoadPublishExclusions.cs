namespace Sitecore.PublishExclusions
{
    using Sitecore;
    using Sitecore.Data.Events;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Events;
    using Sitecore.Pipelines;
    using System;

    /// <summary>
    /// Contains all methods called by Sitecore (pipelines and event handlers) for the Publish Exclusions functionality
    /// </summary>
    public class LoadPublishExclusions
    {
        #region Sitecore event handling methods 
        
        #region Sitecore startup methods

        /// <summary>
        /// Called by the Sitecore initialize pipeline to load all configured publish exclusions
        /// </summary>
        /// <param name="args">Pipeline arguments</param>
        public virtual void Process(PipelineArgs args)
        {
            PublishExclusionsContext.Current.Initialize();
        }

        #endregion

        #region Called by Sitecore's item modification events

        /// <summary>
        /// Event handler to re-load configured publish exclusions, when publish exclusions is saved
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="args">event arguments</param>
        public virtual void OnItemSaved(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!ShouldExecuteEventHandlers(args))
                return;

            SitecoreEventArgs saveEventArgs = args as SitecoreEventArgs;
            Item contextItem = saveEventArgs.Parameters[0] as Item;
            ReinitializeRepository(contextItem);
        }

        /// <summary>
        /// Remote event handler to re-load configured publish exclusions, when publish exclusions is saved
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="args">event arguments</param>
        public virtual void OnItemSavedRemote(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!ShouldExecuteEventHandlers(args))
                return;

            ItemSavedRemoteEventArgs saveEventArgs = args as ItemSavedRemoteEventArgs;
            Item contextItem = saveEventArgs.Item as Item;
            ReinitializeRepository(contextItem);
        }

        /// <summary>
        /// Event handler to re-load configured publish exclusions, when any of the publish exclusions is deleted
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="args">event arguments</param>
        public virtual void OnItemDeleted(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!ShouldExecuteEventHandlers(args))
                return;

            Item contextItem = Event.ExtractParameter(args, 0) as Item;
            ReinitializeRepository(contextItem);
        }

        /// <summary>
        /// Remote event handler to re-load configured publish exclusions, when any of the publish exclusions is deleted
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="args">event arguments</param>
        public virtual void OnItemDeletedRemote(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!ShouldExecuteEventHandlers(args))
                return;

            ItemDeletedRemoteEventArgs deleteEventArgs = args as ItemDeletedRemoteEventArgs;
            Item contextItem = deleteEventArgs.Item as Item;
            ReinitializeRepository(contextItem);
        }

        #endregion

        #endregion

        #region Other Methods

        /// <summary>
        /// Determines whether any of the Sitecore item modification event handlers should execute or not
        /// This can be overridden to add any custom implementation logic
        /// </summary>
        /// <param name="args">event arguments</param>
        /// <returns>true if handlers should execute, else false</returns>
        public virtual bool ShouldExecuteEventHandlers(EventArgs args)
        {
            //Do not run event handlers during a publish
            if (Context.Site != null && Context.Site.Name.Equals("publisher", StringComparison.OrdinalIgnoreCase))
                return false;

            //Do not run event handlers during a package installation
            if (Context.Job != null && Context.Job.Name.Equals("install", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        /// <summary>
        /// Reinitializes the repository if required
        /// </summary>
        /// <param name="item"></param>
        public virtual void ReinitializeRepository(Item item)
        {
            if (item != null &&
                (item.TemplateID == SitecoreIDs.Templates.PublishConfiguration || item.TemplateID == SitecoreIDs.Templates.PublishExclusion))
            {
                PublishExclusionsContext.Current.ReInitialize();
            }
        }

        #endregion
    }
}
