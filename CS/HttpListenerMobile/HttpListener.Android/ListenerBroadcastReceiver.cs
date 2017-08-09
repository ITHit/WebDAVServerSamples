using Android.Content;

namespace HttpListener.Android
{
    /// <summary>
    /// Sends and outputs logs on activity.
    /// </summary>
    [BroadcastReceiver]
    public class ListenerBroadcastReceiver : BroadcastReceiver
    {
        /// <summary>
        /// Activity instance (is used to update view).
        /// </summary>
        private MainActivity activity;

        /// <summary>
        /// Intent filter used by broadcast receiver.
        /// </summary>
        public static readonly string LOG_OUTPUT = "LOG_OUTPUT";

        /// <summary>
        /// Creates new instance of this class.
        /// </summary>
        public ListenerBroadcastReceiver() { }

        /// <summary>
        /// Creates new instance of this class.
        /// </summary>
        /// <param name="activity">Activity instance to update textview.</param>
        public ListenerBroadcastReceiver(MainActivity activity)
        {
            this.activity = activity;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            activity.Output(intent.GetStringExtra("message"));
        }
    }
}