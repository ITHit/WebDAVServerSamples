using HttpListenerLibrary;
using System;

namespace HttpListener.Desktop
{
    /// <summary>
    /// Represents logging messages to the console.
    /// </summary>
    public class DesktopLogMethod : ILogMethod
    {
        /// <summary>
        /// Logs message to the console.
        /// </summary>
        /// <param name="message">Message text.</param>
        public void LogOutput(string message)
        {
            Console.WriteLine(message);
        }
    }
}
