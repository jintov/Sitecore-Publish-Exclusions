namespace Sitecore.PublishExclusions
{
    using Sitecore.Configuration;
    using Sitecore.Diagnostics;
    using System;

    /// <summary>
    /// Context class that provides the current Publish Exclusions Controller
    /// </summary>
    public class PublishExclusionsContext
    {
        #region Members

        private static IPublishExclusionsController currentInstance = null;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current instance (singleton) of the Publish Exclusions Controller
        /// </summary>
        public static IPublishExclusionsController Current
        {
            get
            {
                if (currentInstance == null)
                {
                    string peControllerType = string.Empty; ;

                    try
                    {
                        peControllerType = Settings.GetSetting("Sitecore.PublishExclusions.ControllerType", typeof(PublishExclusionsController).FullName);
                        currentInstance = Activator.CreateInstance(Type.GetType(peControllerType)) as IPublishExclusionsController;
                    }
                    catch (Exception ex)
                    {
                        //Resort to default controller
                        currentInstance = new PublishExclusionsController();
                        Log.Error("Sitecore.PublishExclusions : Exclusions Context could not instantiate controller with type - " + (peControllerType ?? string.Empty), ex, currentInstance);
                    }
                }

                return currentInstance;
            }
        }

        #endregion
    }
}
