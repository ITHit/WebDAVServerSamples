using Android.App;
using Android.OS;
using System.IO;
using HttpListenerLibrary;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using ITHit.WebDAV.Server;
using System.Threading.Tasks;

namespace HttpListener.Android
{
    [Activity(Label = "HttpListener.Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            // SetContentView (Resource.Layout.Main);

            string documentsFolderPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

            JsonConfigurationModel jsonConfiguration = JsonConfigurationReader.ReadConfiguration(Assets.Open("appsettings.webdav.json"));

            // Copy storage files directory from application assets to application files folder.
            InitUserStorage(jsonConfiguration.DavContextOptions.RepositoryPath, "StorageTemplate", documentsFolderPath);

            JsonConfigurationReader.ValidateConfiguration(jsonConfiguration, documentsFolderPath);

            jsonConfiguration.DavContextOptions.GetFileContentFunc = GetFileFromAssets;

            // Create collection of services, which will be available in DI Container.
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddConfiguration(jsonConfiguration);
            serviceCollection.AddLogger();
            serviceCollection.AddTransient<WebDAVHttpListener>();
            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            serviceProvider.GetService<WebDAVHttpListener>().RunListener();
        }

        /// <summary>
        /// Initializes user files. Copies storage content from assets to application files folder.
        /// </summary>
        /// <param name="storageFolderRelativePath">Relative path to item in application files folder.</param>
        /// <param name="assetsFolderRelativePath">Relative path to item in Assets folder.</param>
        /// <param name="destPath">Destination folder.</param>
        /// <param name="replaceExisting">If set to true - replaces old directory. Defaults is false.</param>
        private void InitUserStorage(string storageFolderRelativePath, string assetsFolderRelativePath, string destPath, bool replaceExisting = false)
        {
            string internalFolderPath = Path.Combine(destPath, storageFolderRelativePath);
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
                        string newFolderRelativePath = Path.Combine(storageFolderRelativePath, element);
                        string newAssetRelativePath = Path.Combine(assetsFolderRelativePath, element);
                        try
                        {
                            TryCopyFileFromAssets(newFolderRelativePath, newAssetRelativePath, destPath);
                        }
                        catch (Java.IO.FileNotFoundException)
                        {
                            InitUserStorage(newFolderRelativePath, newAssetRelativePath, destPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Copies file from assets to destination.
        /// </summary>
        /// <param name="filePath">Relative path to item in application files folder.</param>
        /// <param name="assetPath">Relative path to item in Assets folder.</param>
        /// <param name="destPath">Destination path.</param>
        private void TryCopyFileFromAssets(string filePath, string assetPath, string destPath)
        {
            Stream fileStream = Assets.Open(assetPath);
            using (FileStream output = new FileStream(Path.Combine(destPath, filePath), FileMode.Create))
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
            catch(Java.IO.FileNotFoundException exception)
            {
                throw new DavException("File not found in assets: " + filePath, exception, DavStatus.NOT_FOUND);
            }
        }
    }
}

