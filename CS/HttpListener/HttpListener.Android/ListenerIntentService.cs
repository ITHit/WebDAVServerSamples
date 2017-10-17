using Android.App;
using Android.Content;
using System;
using System.IO;
using System.Linq;
using SharedMobile;
using HttpListenerLibrary;

namespace HttpListener.Android
{
    /// <summary>
    /// Configures and starts HttpListener.
    /// </summary>
    [Service]
    class ListenerIntentService : IntentService
    {
        /// <summary>
        /// Creates new instance of this class.
        /// </summary>
        public ListenerIntentService() : base("ListenerIntentService")
        {
        }

        protected override void OnHandleIntent(Intent intent)
        {
            ILogMethod logMethod = new AndroidLogMethod(this);

            try
            {
                string documentsFolderPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

                IConfigurationHelper configurationHelper = new AndroidConfigurationHelper(Assets, "appsettings.webdav.json");

                // Copy storage files directory from application assets to application files folder.
                InitUserStorage("App_Data", documentsFolderPath);

                Program.Main(logMethod, configurationHelper);
            }
            catch(Exception ex)
            {
                logMethod.LogOutput(ex.Message);
            }
        }

        /// <summary>
        /// Initializes user files. Copies storage content from assets to application files folder.
        /// </summary>
        /// <param name="assetsFolderRelativePath">Relative path to item in Assets folder.</param>
        /// <param name="destPath">Destination folder.</param>
        /// <param name="replaceExisting">If set to true - replaces old directory. Defaults is false.</param>
        private void InitUserStorage(string assetsFolderRelativePath, string destPath, bool replaceExisting = false)
        {
            string internalFolderPath = Path.Combine(destPath, assetsFolderRelativePath);
            if (replaceExisting && Directory.Exists(internalFolderPath))
            {
                Directory.Delete(internalFolderPath, true);
            }
            if (!Directory.Exists(internalFolderPath))
            {
                Directory.CreateDirectory(internalFolderPath);
                string[] subElements = Assets.List(assetsFolderRelativePath);
                if (subElements.Any())
                {
                    foreach (string element in subElements)
                    {
                        string newAssetRelativePath = Path.Combine(assetsFolderRelativePath, element);
                        try
                        {
                            TryCopyFileFromAssets(newAssetRelativePath, destPath);
                        }
                        catch (Java.IO.FileNotFoundException)
                        {
                            InitUserStorage(newAssetRelativePath, destPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Copies file from assets to destination.
        /// </summary>
        /// <param name="assetPath">Relative path to item in Assets folder.</param>
        /// <param name="destPath">Destination path.</param>
        private void TryCopyFileFromAssets(string assetPath, string destPath)
        {
            Stream fileStream = Assets.Open(assetPath);
            using (FileStream output = new FileStream(Path.Combine(destPath, assetPath), FileMode.Create))
            {
                fileStream.CopyTo(output);
            }
        }
    }
}