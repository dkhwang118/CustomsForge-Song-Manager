using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DLogNet;


namespace CustomsForgeSongManager.LocalTools
{
    /// <summary>
    /// Song Manager logging class for the CFSM (CustomsForge Song Manager) application.
    /// Wraps the DLogNet class to provide logging functionality.
    /// </summary>
    public static class SMLog
    {
        /// <summary>
        /// Static instance of the DLogger class used for logging.
        /// </summary>
        private static DLogger _logger = new DLogger();

        /// <summary>
        /// Gets the logger instance used by this logger.
        /// Only call this when modifying the logger settings, such as the log file path.
        /// </summary>
        public static DLogger Logger
        {
            get { return _logger; }
        }

        /// <summary>
        /// Flag to indicate whether the application is running in debug mode.
        /// </summary>
        private static bool _debugMode = false;

        /// <summary>
        /// TextBox for displaying log messages in the UI.
        /// Child of frmMain.
        /// </summary>
        private static TextBox _logTextBox = null;

        private static string _logFilePath = string.Empty;

        /// <summary>
        /// Log a message to the log window and the log file with a Thread ID.
        /// </summary>
        /// <param name="threadID">The Thread ID.</param>
        /// <param name="message">The message.</param>
        public static void Log(int threadID, string message)
        {
            try
            {
                if (threadID > -1)
                {
                    string msg = string.Format("[Thread:{0}] {1}", threadID, message);
                    _logger.Write(msg);
                }
                else
                {
                    _logger.Write(message);
                }
            }
            catch
            {
                // just ignore it
            }
        }

        /// <summary>
        /// Log a message to the log window and the log file.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Log(string message)
        {
            Log(-1, message);
        }

        /// <summary>
        /// Set the debug mode for the logger.
        /// </summary>
        /// <param name="debugMode">True if threads calling the "DebugLog()" method wish to write their messages to the log.</param>
        public static void SetDebugMode(bool debugMode)
        {
            _debugMode = debugMode;
        }

        public static void DebugLog(string message)
        {
            if (_debugMode)
                Log(message);
        }

        /// <summary>
        /// Set the TextBox for logging output.
        /// </summary>
        /// <param name="logTextBox"></param>
        public static void SetMainLogTextBox(TextBox logTextBox)
        {
            // Set the log text box as the main logging output.
            _logTextBox = logTextBox;

            // "Add" it as one of the outputs for the logger
            _logger.AddTargetTextBox(logTextBox);
        }

        /// <summary>
        /// Set the log file path for the logger.
        /// </summary>
        /// <param name="logFilePath"></param>
        public static void SetLogFilePath(string logFilePath)
        {
            _logFilePath = logFilePath;
            _logger.AddTargetFile(logFilePath);
        }
    }
}
