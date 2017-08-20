using HttpListenerLibrary;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;
using UIKit;

namespace HttpListener.iOS
{
    public partial class ViewController : UIViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var logger = new ApplicationViewLogger(Output);

            try
            {
                string documentsFolderPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                string contentRootPath = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().EscapedCodeBase).LocalPath), "Assets");

                JsonConfigurationModel jsonConfiguration = JsonConfigurationReader.ReadConfiguration(Path.Combine(contentRootPath, "appsettings.webdav.json"));

                // Copy storage files from iOS application bundle to Documents directory. (iOS does not allow to modify application bundle in runtime)
                InitUserStorage(Path.Combine(contentRootPath, "App_Data"), Path.Combine(documentsFolderPath, "App_Data"));

                JsonConfigurationReader.ValidateConfiguration(jsonConfiguration, documentsFolderPath);

                jsonConfiguration.DavContextOptions.HtmlPath = contentRootPath;
                
                // Create collection of services, which will be available in DI Container.
                ServiceCollection serviceCollection = new ServiceCollection();
                serviceCollection.AddConfiguration(jsonConfiguration);
                serviceCollection.AddSingleton(logger);
                serviceCollection.AddTransient<WebDAVHttpListener>();
                ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

                UIApplication.SharedApplication.BeginBackgroundTask(() => { });
                serviceProvider.GetService<WebDAVHttpListener>().RunListener();
            }
            catch(Exception ex)
            {
                logger.LogError(ex.Message, ex);
            }
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

        /// <summary>
        /// Outputs message to the view.
        /// </summary>
        /// <param name="message">Text for output.</param>
        public void Output(string message)
        {
            InvokeOnMainThread(() => LogOutput.InsertText($"{message}\n\n"));
        }

        /// <summary>
        /// Initializes user files. Copies content from application bundle to Documents folder.
        /// </summary>
        /// <param name="sourcePath">Source folder path.</param>
        /// <param name="destPath">Destination folder path.</param>
        /// <param name="replaceExisting">If set to true - replaces old directory. Defaults is false.</param>
        /// <exception cref="DirectoryNotFoundException">If source directory does not exist.</exception>
        private static void InitUserStorage(string sourcePath, string destPath, bool replaceExisting = false)
        {
            DirectoryInfo dir = new DirectoryInfo(sourcePath);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourcePath);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            if (replaceExisting && Directory.Exists(destPath))
            {
                Directory.Delete(destPath, true);
            }
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string temppath = Path.Combine(destPath, file.Name);
                    file.CopyTo(temppath, false);
                }

                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destPath, subdir.Name);
                    InitUserStorage(subdir.FullName, temppath);
                }
            }
        }
    }
}