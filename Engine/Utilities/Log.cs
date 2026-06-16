using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Utilities
{
    // Dictates the severity layer of an engine log message.
    public enum LogSeverity
    {
        Info,
        Warning,
        Error,
        Print
    }

    //global utility for handling engine wide diagnostic logging, bridges directly into the winforms editor ui console. 
    public static class Log
    {
        // Logs a message with the specified severity level. binds to winforms event
        public static event Action<LogSeverity, string>? OnLogMessage;


        // Logs an informational message with the specified severity level. 
        public static void Info(string message)
        {
            FormatAndBroadcast(LogSeverity.Info, message);
        }

        // Logs an informational message with the specified severity level. 
        public static void Warning(string message)
        {
            FormatAndBroadcast(LogSeverity.Warning, message);
        }

        // Logs an informational message with the specified severity level. 
        public static void Error(string message)
        {
            FormatAndBroadcast(LogSeverity.Error, message);
        }

        //a generic print function for logging messages to the console without severity
        public static void Print(string message)
        {
            FormatAndBroadcast(LogSeverity.Print, message);
        }


        // Assembles the timestamp, tags the severity, and pushes it out to listeners.
        private static void FormatAndBroadcast(LogSeverity severity, string message) 
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string severityTag = severity.ToString().ToUpper();
            string formattedMessage = $"[{timestamp}] [{severityTag}] {message}";

            if(severity == LogSeverity.Print)
            {
                 formattedMessage = $"[{timestamp}] {message}";
            }

            System.Diagnostics.Debug.WriteLine(formattedMessage); // fallback to debug output if no listeners are attached

            OnLogMessage?.Invoke(severity, formattedMessage);
        }

    }
}
