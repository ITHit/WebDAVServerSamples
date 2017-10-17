using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content;
using Android.Content.PM;

namespace HttpListener.Android
{
    [Activity(Label = "WebDAV Server", MainLauncher = true, Icon = "@drawable/icon", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : Activity
    {
        /// <summary>
        /// BroadcastReceiver instance, which outputs logs on the view.
        /// </summary>
        private ListenerBroadcastReceiver receiver;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            Intent listenerIntent = new Intent(this, typeof(ListenerIntentService));
            StartService(listenerIntent);
        }

        protected override void OnResume()
        {
            base.OnResume();

            RegisterBroadcastReceiver();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            UnregisterReceiver(receiver);
        }

        /// <summary>
        /// Registers <see cref="ListenerBroadcastReceiver"/> in the application.
        /// </summary>
        private void RegisterBroadcastReceiver()
        {
            IntentFilter filter = new IntentFilter(ListenerBroadcastReceiver.LOG_OUTPUT);
            filter.AddCategory(Intent.CategoryDefault);
            receiver = new ListenerBroadcastReceiver(this);
            RegisterReceiver(receiver, filter);
        }

        /// <summary>
        /// Outputs message to the view.
        /// </summary>
        /// <param name="message">Text for output.</param>
        public void Output(string message)
        {
            RunOnUiThread(() => FindViewById<TextView>(Resource.Id.LogOutput).Append($"{message}\n\n"));
        }
    }
}

