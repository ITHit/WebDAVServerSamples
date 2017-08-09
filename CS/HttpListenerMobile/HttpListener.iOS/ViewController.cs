using HttpListenerLibrary;
using Microsoft.Extensions.DependencyInjection;
using System;

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

            JsonConfigurationModel configuration = Application.serviceProvider.GetService<JsonConfigurationModel>();
            configuration.DavLoggerOptions.LogOutput = Output;
            UIApplication.SharedApplication.BeginBackgroundTask(() => { });
            Application.serviceProvider.GetService<WebDAVHttpListener>().RunListener();
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
            LogOutput.InsertText($"{message}\n");
        }
    }
}