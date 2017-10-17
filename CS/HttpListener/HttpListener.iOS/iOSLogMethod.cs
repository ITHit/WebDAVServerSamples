using HttpListenerLibrary;
using UIKit;

namespace HttpListener.iOS
{
    /// <summary>
    /// Represents logging messages to the application view on devices, running iOS.
    /// </summary>
    public class iOSLogMethod : ILogMethod
    {
        /// <summary>
        /// <see cref="UIViewController"/> instance.
        /// </summary>
        private UIViewController viewController;

        /// <summary>
        /// Holds logging text.
        /// </summary>
        private UITextView textView;

        /// <summary>
        /// Creates new instance of this class.
        /// </summary>
        /// <param name="viewController"><see cref="UIViewController"/> instance.</param>
        /// <param name="textView">Output text view.</param>
        public iOSLogMethod(UIViewController viewController, UITextView textView)
        {
            this.viewController = viewController;
            this.textView = textView;
        }

        /// <summary>
        /// Logs message to the view.
        /// </summary>
        /// <param name="message">Message text.</param>
        public void LogOutput(string message)
        {
            viewController.InvokeOnMainThread(() => textView.InsertText($"{message}\n\n"));
        }
    }
}