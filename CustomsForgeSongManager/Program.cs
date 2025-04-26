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
            // check for correct version of .NET
            if (!File.Exists(Constants.SongsInfoPath))
                SysExtensions.IsDotNet4();

            // Start the logger    
            Globals.MyLog = SMLog.Logger;
            SMLog.Log("==== This is the start of a new CFSM run log =====");

            // If we are not running in debug mode...
            // NOTE: Constants.DebugMode is currently set to always return true.
            if (!Constants.DebugMode)
            {
                // non-UI thread exceptions handling.
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    var exception = e.ExceptionObject as Exception;
                    Globals.MyLog.Write(String.Format("<ERROR> Unhandled.Exception:\nSource: {0}\nTarget: {1}\n{2}\n", exception.Source, exception.TargetSite, exception.ToString()));
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
                    Globals.MyLog.Write(String.Format("<ERROR> Application.ThreadException\nSource: {0}\nTarget: {1}\n{2}\n", exception.Source, exception.TargetSite, exception.ToString()));

                    if (MessageBox.Show(String.Format("Application.ThreadException:\n\n{0}\nPlease send us the {1} file if you need help.  Open log file now?",
                        exception.Message.ToString(), Path.GetFileName(AppSettings.Instance.LogFilePath)),
                        "Please Read This Important Message Completely ...", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        Process.Start(AppSettings.Instance.LogFilePath);
                    }
                };
            }

            // Initialize the Directory Manager so we can start using the paths
            DirectoryManager.InitializeDefaultPaths();

            // Load the application settings
            AppSettings settings = FileTools.LoadApplicationSettingsFromFile(DirectoryManager.AppSettingsPath);
            Model.Instance.SetApplicationSettings(settings);

            // Now that we have the settings, we can initialize the logger with the log file path
            SMLog.SetLogFilePath(AppSettings.Instance.LogFilePath);

            // Validate the Rocksmith installation directory
            ValidateRocksmithInstallDirectory();

            // This is used to initialize the Utils and CFSM classes.
            // NOTE: was moved from frmMain constructor to here to ensure it runs before the main form is created.
            TypeExtensions.InitializeClasses(new string[] { "UTILS_INIT", "CFSM_INIT" }, new Type[] { }, new object[] { });


            // 
            Application.SetCompatibleTextRenderingDefault(false);
            Application.EnableVisualStyles();

            // Run the application main form
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
            //    Globals.MyLog.Write(exMessage);
            //    Process.Start(AppSettings.Instance.LogFilePath);
            //}

        }

        /// <summary>
        /// Validates the Rocksmith installation directory, prompting the user to select it if it is not set or does not exist.
        /// </summary>
        private static void ValidateRocksmithInstallDirectory()
        {
            // Get the current Rocksmith installation directory from settings
            string rsDir = AppSettings.Instance.RSInstalledDir;

            // If the directory is not set or does not exist, prompt the user to select it
            if (String.IsNullOrEmpty(rsDir) || !Directory.Exists(rsDir) || !Directory.Exists(Path.Combine(rsDir, "dlc")))
            {
                // Inform the user
                MessageBox.Show(new Form { TopMost = true },
                                        String.Format("Rocksmith Installation Directory Not Found! " +
                                        "{0}Please select the Rocksmith Installation Directory.", Environment.NewLine),
                                        Constants.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);

                string selectedPath = null;
                do
                {
                    using (var fbd = new FolderBrowserDialog())
                    {
                        fbd.Description = "Select Rocksmith 2014 Installation Directory";
                        fbd.SelectedPath = LocalExtensions.GetSteamDirectory();

                        // If the user cancels the dialog, continue the loop and ask again.
                        if (fbd.ShowDialog() != DialogResult.OK)
                        {
                            continue;
                        }
                        else
                        {
                            // Check if the selected path contains the required 'dlc' subdirectory
                            if (!Directory.Exists(Path.Combine(rsDir, "dlc")))
                            {
                                // Show a message for the user to select a valid directory
                                MessageBox.Show(new Form { TopMost = true },
                                    String.Format("Please select a directory that  {0}contains a 'dlc' subdirectory.", Environment.NewLine),
                                    Constants.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            }
                            else
                            {
                                // If the selected path is valid, set isValidPath to true
                                selectedPath = fbd.SelectedPath;
                            }
                        }
                    }
                } while (selectedPath == null);

                // Set the selected path as the Rocksmith installation directory
                DirectoryManager.SetRocksmithInstallationDirectory(selectedPath);
            }

            SMLog.Log("Validated RS2014 Installation Directory: " + AppSettings.Instance.RSInstalledDir);
        }

    }
}