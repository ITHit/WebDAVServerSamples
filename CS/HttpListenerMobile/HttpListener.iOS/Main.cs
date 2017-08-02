using HttpListenerLibrary;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UIKit;

namespace HttpListener.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            string documentsFolderPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string contentRootPath = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().EscapedCodeBase).LocalPath), "Assets");

            JsonConfigurationModel jsonConfiguration = JsonConfigurationReader.ReadConfiguration(Path.Combine(contentRootPath, "appsettings.webdav.json"));

            // Copy storage files from iOS application bundle to Documents directory. (iOS does not allow to modify application bundle in runtime)
            InitUserStorage(Path.Combine(contentRootPath, "StorageTemplate"), Path.Combine(documentsFolderPath, jsonConfiguration.DavContextOptions.RepositoryPath));

            JsonConfigurationReader.ValidateConfiguration(jsonConfiguration, documentsFolderPath);

            jsonConfiguration.DavContextOptions.HtmlPath = contentRootPath;

            // Create collection of services, which will be available in DI Container.
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddConfiguration(jsonConfiguration);
            serviceCollection.AddLogger();
            serviceCollection.AddTransient<WebDAVHttpListener>();
            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            serviceProvider.GetService<WebDAVHttpListener>().RunListener();

            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
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