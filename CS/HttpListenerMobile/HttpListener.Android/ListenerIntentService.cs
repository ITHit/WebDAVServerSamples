using Android.App;
using Android.Content;
using HttpListenerLibrary;
using ITHit.WebDAV.Server;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            string documentsFolderPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

            JsonConfigurationModel jsonConfiguration = JsonConfigurationReader.ReadConfiguration(Assets.Open("appsettings.webdav.json"));

            // Copy storage files directory from application assets to application files folder.
            InitUserStorage("App_Data", documentsFolderPath);

            JsonConfigurationReader.ValidateConfiguration(jsonConfiguration, documentsFolderPath);

            jsonConfiguration.DavContextOptions.GetFileContentFunc = GetFileFromAssets;
            jsonConfiguration.DavLoggerOptions.LogOutput = SendBroadcast;

            // Create collection of services, which will be available in DI Container.
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddConfiguration(jsonConfiguration);
            serviceCollection.AddLogger();
            serviceCollection.AddTransient<WebDAVHttpListener>();
            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.GetService<WebDAVHttpListener>().RunListener();
        }

        /// <summary>
        /// Sends broadcast using LOG_OUTPUT intent to update view.
        /// </summary>
        /// <param name="message">Text to output.</param>
        private void SendBroadcast(string message)
        {
            Intent BroadcastIntent = new Intent(ListenerBroadcastReceiver.LOG_OUTPUT);
            BroadcastIntent.AddCategory(Intent.CategoryDefault);
            BroadcastIntent.PutExtra("message", message);
            SendBroadcast(BroadcastIntent);
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

        /// <summary>
        /// Retrieves file content by relative file path in Assets.
        /// </summary>
        /// <param name="filePath">Relative file path in Assets.</param>
        /// <returns>File content in string representation.</returns>
        /// <exception cref="DavException">If file with specified path does not exist.</exception>
        private async Task<string> GetFileFromAssets(string filePath)
        {
            try
            {
                using (StreamReader streamReader = new StreamReader(Assets.Open(filePath)))
                {
                    return await streamReader.ReadToEndAsync();
                }
            }
            catch (Java.IO.FileNotFoundException exception)
            {
                throw new DavException("File not found in assets: " + filePath, exception, DavStatus.NOT_FOUND);
            }
        }
    }
}