using Android.App;
using Android.Content;
using HttpListenerLibrary;

namespace HttpListener.Android
{
    /// <summary>
    /// Represents logging messages to the application view on devices, running Android.
    /// </summary>
    public class AndroidLogMethod : ILogMethod
    {
        /// <summary>
        /// Logging intent service.
        /// </summary>
        IntentService logService;

        /// <summary>
        /// Creates new instance of this class.
        /// </summary>
        /// <param name="logService">Logging intent service.</param>
        public AndroidLogMethod(IntentService logService)
        {
            this.logService = logService;
        }

        /// <summary>
        /// Logs message to the view.
        /// </summary>
        /// <param name="message">Message text.</param>
        public void LogOutput(string message)
        {
            Intent BroadcastIntent = new Intent(ListenerBroadcastReceiver.LOG_OUTPUT);
            BroadcastIntent.AddCategory(Intent.CategoryDefault);
            BroadcastIntent.PutExtra("message", message);
            logService.SendBroadcast(BroadcastIntent);
        }
    }
}