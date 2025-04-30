using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using CustomsForgeSongManager.DataObjects;
using CustomsForgeSongManager.Forms;
using GenTools;
using DF.WinForms.ThemeLib;
using DLogNet;
using DataGridViewTools;
using Mutex = System.Threading.Mutex;
using System.Threading;
using RocksmithToolkitLib.Extensions;
using CustomsForgeSongManager.LocalTools;
using CustomsForgeSongManager.DataManager;


#if WINDOWS

#else
using Mutex = DF.WinForms.ThemeLib.Mutex;
#endif

namespace CustomsForgeSongManager
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // prevent multiple occurrence of this application from running
            using (Mutex mutex = new Mutex(false, @"Global\CUSTOMSFORGESONGMANAGER"))
            {
                if (!mutex.WaitOne(0, false))
                {
                    var pHandle = CrossPlatform.GetHandleFromProcessName(Application.ExecutablePath);
                    if (pHandle != IntPtr.Zero)
                    {
                        //restore the window if minimized
                        CrossPlatform.ShowWindow(pHandle, 9);
                        //bring it to the front
                        CrossPlatform.SetForegroundWindow(pHandle);
                    }

                    return;
                }
            }

            RunApp();
        }

        private static void RunApp()
        {



            //==============================================================
            // Start data managers that don't need to be in the main thread
            //==============================================================


            // Initialize the Directory Manager so we can start using the paths
            DirectoryManager.InitializeDefaultPaths();


            // Start the logger    
            SMLog.Log("==== This is the start of a new CFSM run log =====");

            // Now that we have the directory manager default paths, we can initialize the logger with the log file path
            SMLog.SetLogFilePath(DirectoryManager.LogFilePath);

            // check for correct version of .NET
            if (!File.Exists(DirectoryManager.SongsInfoPath))
                SysExtensions.IsDotNet4();






            // This is used to initialize the Utils and CFSM classes.
            // NOTE: was moved from frmMain constructor to here to ensure it runs before the main form is created.
            TypeExtensions.InitializeClasses(new string[] { "UTILS_INIT", "CFSM_INIT" }, new Type[] { }, new object[] { });



            // If we are not running in debug mode...
            // NOTE: Constants.DebugMode is currently set to always return true.
            if (!Constants.DebugMode)
            {
                // non-UI thread exceptions handling.
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    var exception = e.ExceptionObject as Exception;
                    SMLog.Log(String.Format("<ERROR> Unhandled.Exception:\nSource: {0}\nTarget: {1}\n{2}\n", exception.Source, exception.TargetSite, exception.ToString()));
                    if (MessageBox.Show(String.Format("Unhandled.Exception:\n\n{0}\nPlease send us the {1} file if you need help.  Open log file now?",
                        exception.Message.ToString(), Path.GetFileName(AppSettings.Instance.LogFilePath)),
                        "Please Read This Important Message Completely ...", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        Process.Start(AppSettings.Instance.LogFilePath);
                    }
                };

                // UI thread exceptions handling.
                Application.ThreadException += (s, e) =>
                {
                    var exception = e.Exception;
                    SMLog.Log(String.Format("<ERROR> Application.ThreadException\nSource: {0}\nTarget: {1}\n{2}\n", exception.Source, exception.TargetSite, exception.ToString()));

                    if (MessageBox.Show(String.Format("Application.ThreadException:\n\n{0}\nPlease send us the {1} file if you need help.  Open log file now?",
                        exception.Message.ToString(), Path.GetFileName(AppSettings.Instance.LogFilePath)),
                        "Please Read This Important Message Completely ...", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        Process.Start(AppSettings.Instance.LogFilePath);
                    }
                };
            }

            // Run the application main form
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new frmMain());





            //try
            //{
            //}
            //catch (Exception ex)
            //{
            //    // a more detailed exception message
            //    var exMessage = String.Format("Exception({0}): {1}", ex.GetType().Name, ex.Message);
            //    if (ex.InnerException != null)
            //        exMessage += String.Format(", InnerException({0}): {1}", ex.InnerException.GetType().Name, ex.InnerException.Message);
            //    SMLog.Log(exMessage);
            //    Process.Start(AppSettings.Instance.LogFilePath);
            //}

        }
    }
}