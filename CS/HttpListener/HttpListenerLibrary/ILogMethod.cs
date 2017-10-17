namespace HttpListenerLibrary
{
    /// <summary>
    /// Represents view logging method.
    /// </summary>
    public interface ILogMethod
    {
        /// <summary>
        /// Logs message to the view.
        /// </summary>
        /// <param name="message">Message text.</param>
        void LogOutput(string message);
    }
}
