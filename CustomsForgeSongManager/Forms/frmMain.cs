using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using CustomsForgeSongManager.DataObjects;
using CustomsForgeSongManager.LocalTools;
using CustomsForgeSongManager.UControls;
using CustomsForgeSongManager.UITheme;
using GenTools;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.ComponentModel;
using DataGridViewTools;
using CustomControls;
using RocksmithToolkitLib;
using System.Data;
using System.Threading;
using RocksmithToolkitLib.Extensions;
using System.Configuration;
using System.Globalization;
using DLogNet;
using CustomsForgeSongManager.DataManager;
using CustomsForgeSongManager.AppEvents;
using CustomsForgeSongManager.AppEvents.Event;

// NOTE: the app is designed for default user screen resolution of 1024x768
// dev screen resolution should be set to this when designing forms and controls
// all png images are 16x16 resolution for buttons, unless higher resolution for some other use

// NOTE: any usage of 'public enum' in code that will be obfuscated must be preceeded with
// [Obfuscation(Exclude = false, Feature = "-rename")]
// so that ConfuserEx does not rename the enumerators
//
// NOTE: for Mac compatiblity use absolute paths ... do not use generic/relative @"./" paths 
//
namespace CustomsForgeSongManager.Forms
{
    public partial class frmMain : Form, IMainForm, IEventConsumer //,ThemedForm
    {
        /// <summary>
        /// Location of the user control (UC) that will be displayed in the tab pages.
        /// </summary>
        private static Point UCLocation = new Point(5, 10);

        /// <summary>
        /// Size of the user control (UC) that will be displayed in the tab pages.
        /// </summary>
        private static Size UCSize = new Size(990, 490);

        /// <summary>
        /// Dock style of the user control (UC) that will be displayed in the tab pages.
        /// </summary>
        private static DockStyle UCDockStyle = DockStyle.Fill;

        private Control currentControl = null;
        public delegate void PlayCall();
        private event PlayCall playFunction;
        private const string APP_SETUP = "CFSMSetup.exe";
        private const string APP_EXE = "CustomsForgeSongManager.exe";

        /// <summary>
        /// Dictionary to hold references to tab pages (e.g SongManager, ArrangementAnalyzer, Settings, etc.).
        /// </summary>
        private Dictionary<Type,INotifyTabChanged> _tabPageViews = new Dictionary<Type, INotifyTabChanged>();

        /// <summary>
        /// Tab index for the currently selected tab.
        /// </summary>
        private int _tabIndex = -1;

#if INNORELEASE
        // depricated method
        private const string SERVER_URL = "http://ignition.customsforge.com/cfsm_uploads/release";
        private const string APP_ARCHIVE = "CFSMSetupRelease.rar";
#endif
#if INNOBETA
        // depricated method
        private const string SERVER_URL = "http://ignition.customsforge.com/cfsm_uploads/beta";
        private const string APP_ARCHIVE = "CFSMSetupBeta.rar";
#endif
#if INNOBUILD // the default build method
        private const string SERVER_URL = "https://ignition4.customsforge.com/files/cfsm/Windows/";
        private const string APP_ARCHIVE = "CFSMSetup.rar";
#endif

        #region Constructor

        public frmMain()
        {
            // Initialize the form and its components
            InitializeComponent();

            // Setup the log textbox
            SMLog.SetMainLogTextBox(tbLog);

            // First - Try to load the application settings
            SettingsManager.Initialize();
            multithreadCheck();


            // Set the form's title
            setApplicationTitle();

            // verify application directory structure
            //FileTools.VerifyCfsmFolders();
            // FileTools.VerifyCfsmFiles(); 
            ValidationTool.ForceValidateRocksmithInstallDirectory(DirectoryManager.RSInstalledDir);
            ValidationTool.ValidateD3D();
            ValidationTool.ValidateSongManagerFolders();

            // create VersionInfo.txt file
            VersionInfo.CreateVersionInfo();

            // Log the runtime environemnt
            logApplicationRuntimeEnvironment();

            //this will initialize classes that need to be initialized right away.
            //TypeExtensions.InitializeClasses(new string[] { "UTILS_INIT", "CFSM_INIT" }, new Type[] { }, new object[] { });



            //=================================================
            //            Form's Child Controls
            //=================================================


            // Set up the top ToolStrip
            // important prevent toolstrip from growing/changing at runtime
            // toolstrip may appear changed in design mode (this is a known VS bug)
            TopToolStripPanel.MaximumSize = new Size(0, 28); // force height and makes tsLable_Tagger positioning work
            tsUtilities.AutoSize = false; // a key to preventing movement
            tsUtilities.Location = new Point(0, 0); // force location
            tsAudioPlayer.AutoSize = false; // a key to preventing movement
            tsAudioPlayer.Visible = false;
            tsAudioPlayer.Location = new Point(tsUtilities.Width + 40, 0); // force location
            tsAudioPlayer.Visible = true;
            playFunction += new PlayCall(PlaySong);

            // If we have notifications enabled
            if (AppSettings.Instance.EnableNotifications)
                // add the notify icon to the logger
                SMLog.Logger.AddTargetNotifyIcon(notifyIcon_Main);

            // TODO: Remove the use of these Globals
            //Globals.Notifier = this.notifyIcon_Main;
            Globals.TsProgressBar_Main = this.tsProgressBar_Main;
            Globals.TsLabel_MainMsg = this.tsLabel_MainMsg;
            Globals.TsLabel_StatusMsg = this.tsLabel_StatusMsg;
            Globals.TsLabel_DisabledCounter = this.tsLabel_DisabledCounter;
            Globals.TsLabel_Cancel = this.tsLabel_Cancel;




            //===========================================================
            //            Event handlers
            //===========================================================


            // ?? Disable the tab control if a scan is in progress ??
            /*
            Globals.OnScanEvent += (s, e) =>
            {
                GenExtensions.InvokeIfRequired(tcMain, a =>
                {
                    tcMain.Enabled = !e.IsScanning;
                });
            };
            */
            // Here's the real fix => handle the SongScanEvent
            AppEventManager.RegisterEventConsumer(this, typeof(SongScanEvent));



            // Hook the PropertyChanged event handler to the AppSettings instance
            //AppSettings.Instance.PropertyChanged += appSettings_PropertyChanged;

            // Event handler to handle form closing events
            this.FormClosing += (object sender, FormClosingEventArgs e) =>
            {
                // Save settings to file
                SettingsManager.SaveApplicationSettingsToFile();

                // Dispose of the DLogger
                SMLog.Logger.RemoveTargetNotifyIcon(notifyIcon_Main);
                SMLog.Logger.Dispose();

                // Dispose of the EventConsumer resources
                AppEventManager.ShutDown();

                // Dispose of the notifyIcon stuff
                // NOTE: this code was retrieved from the DLogNet readme
                notifyIcon_Main.Visible = false;
                if (notifyIcon_Main.Icon != null)
                {
                    notifyIcon_Main.Icon.Dispose(); // dispose of the icon to prevent memory leak
                    notifyIcon_Main.Icon = null; // set to null to prevent further issues
                }
                notifyIcon_Main.Dispose();
                Application.DoEvents();
            };


            //==================================================
            //          Data Processing Before Showing Form
            //==================================================


            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            ResetBottomToolStrip();




            // This was a great idea while RSTK lib was still regularly updated, but now has become a timebomb which breaks expired build, just as current & working builds 
            /*if (!ToolkitVersion.IsRSTKLibValid())
            {
                // throw new ApplicationException(Environment.NewLine + "<WARNING> This version of CFSM has expired." + Environment.NewLine + "Please download and install the latest version.");
                AppSettings.Instance.EnableAutoUpdate = true;
                Globals.Settings.SaveSettingsToFile(Globals.DgvCurrent);

                var diaMsg = "This version of CFSM is no longer supported.  Please close now" + Environment.NewLine +
                             "and restart to automatically update to the latest supported version.";
                CustomControls.BetterDialog2.ShowDialog(diaMsg, "Time To Update ...", null, null, "Ok", Bitmap.FromHicon(SystemIcons.Warning.Handle), "WARNING ...", 0, 150);
            }*/




            // If the display settings aren't set correctly
            if (!ValidationTool.ValidateDisplaySettings(this, this)) // , true, true)) // uncomment for debugging
            {
                // Display settings are adjusted using the above method
                // We just need to log that we adjusted them
                SMLog.Log("+ Adjusted AutoScaleDimensions, AutoScaleMode, and AutoSize ...");
            }

            //============================================================
            //     Logic for the first page to be shown
            //====================================================

            // If this is the first run of the app
            if (AppSettings.Instance.FirstRun)
            {
                // Load the Settings tab
                loadSettingsTabPage();

                // Set the first page shown to be the Settings tab
                tcMain.SelectedIndex = getIndexForTabPage(tpSettings);

                
            }
            else // If this is not the first run of the app
            {
                // Initialize the SongDataManager
                SongDataManager.Initialize();

                // Set the first page shown to be the Song Manager tab
                loadSongManagerTabPage();
                tcMain.SelectedIndex = getIndexForTabPage(tpSongManager);
            }

            // bring CFSM to the front on startup
            this.BringToFront();
            this.WindowState = AppSettings.Instance.FullScreen ? FormWindowState.Maximized : FormWindowState.Normal;
            this.Show(); // triggers Form.Shown event

        }

        #endregion Constructor

        #region Initialization Methods

        /// <summary>
        /// Set the application title based on the version information.
        /// </summary>
        private void setApplicationTitle()
        {
            // Set the application title based on the version information
            string strFormatVersion = "{0} (v{1} - {2})";
#if INNORELEASE
            strFormatVersion = "{0} (v{1} - {2} RELEASE)";
#endif
#if INNOBETA
            strFormatVersion = "{0} (v{1} - {2} BETA)";
#endif
#if INNOBUILD
            strFormatVersion = "{0} (v{1} - {2} BUILD)";
#endif

            if (Constants.DebugMode)
                strFormatVersion = "{0} (v{1} - {2} DEBUG)";

            // Set the application title
            string appTitle = String.Format(strFormatVersion, Constants.ApplicationName, Constants.CustomVersion(), AppSettings.Instance.MacMode ? "MAC" : "PC");
            this.Text = appTitle;
            Constants.AppTitle = appTitle;
        }

        /// <summary>
        /// Log the application runtime environment.
        /// </summary>
        public void logApplicationRuntimeEnvironment()
        {
            DateTime dtuLib = new DateTime();
            try
            {

                var assembly = Assembly.LoadFile(typeof(RocksmithToolkitLib.ToolkitVersion).Assembly.Location);
                var assemblyConfiguration = assembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false).Cast<AssemblyConfigurationAttribute>().FirstOrDefault().Configuration.ToString() ?? "";
                dtuLib = DateTime.Parse(assemblyConfiguration, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            }
            catch
            {
                // exception returns [1/1/0001 12:00:00 AM] 
            }

            // log application runtime environment
            SMLog.Log(String.Format("+ {0}", Constants.AppTitle));
            SMLog.Log(String.Format("+ OS {0} ({1} bit)", Environment.OSVersion, Environment.Is64BitOperatingSystem ? "64" : "32"));
            SMLog.Log(String.Format("+ .NET Framework (v{0})", SysExtensions.DotNetVersion));
            SMLog.Log(String.Format("+ CultureInfo ({0})", CultureInfo.CurrentCulture.ToString()));
            SMLog.Log(String.Format("+ Current Local DateTime [{0}]", DateTime.Now.ToString()));
            SMLog.Log(String.Format("+ Current UTC DateTime [{0}]", DateTime.UtcNow.ToString()));
            SMLog.Log(String.Format("+ RocksmithToolkitLib (v{0}) [{1}]", ToolkitVersion.RSTKLibVersion(), dtuLib));
            SMLog.Log(String.Format("+ Dynamic Difficulty Creator (v{0})", FileVersionInfo.GetVersionInfo(Path.Combine(ExternalApps.TOOLKIT_ROOT, ExternalApps.APP_DDC)).ProductVersion));
            SMLog.Log(String.Format("+ System Display DPI Setting ({0})", GeneralExtension.GetDisplayDpi(this)));
            SMLog.Log(String.Format("+ System Display Screen Scale Factor ({0}%)", GeneralExtension.GetDisplayScalingFactor(this) * 100));

            // SMLog.Log(String.Format("+ App.config Status ({0})", appConfigStatus));

        } 

        /// <summary>
        /// Gets the index of the given tab page in the tcMain.TabPages control.
        /// </summary>
        /// <param name="tp"></param>
        /// <returns>The index of the given tab page</returns>
        private int getIndexForTabPage(TabPage tp)
        {
            return tcMain.TabPages.IndexOf(tp);
        }

        /// <summary>
        /// Event handler for when the an AppSettings property changes value. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void appSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ShowLogWindow":
                    scMain.Panel2Collapsed = !AppSettings.Instance.ShowLogWindow;
                    tsLabel_ShowHideLog.Text = scMain.Panel2Collapsed ? 
                        Properties.Resources.ShowLog : Properties.Resources.HideLog;
                    break;
                case "EnableNotifications":
                    if (AppSettings.Instance.EnableNotifications)
                        SMLog.Logger.AddTargetNotifyIcon(notifyIcon_Main);
                    else
                        SMLog.Logger.RemoveTargetNotifyIcon(notifyIcon_Main);
                    break;
                default:
                    break;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        private void multithreadCheck()
        {
            // Get this system's core count
            var coreCount = SysExtensions.GetCoreCount();
            if (coreCount == 0)
            {
                coreCount = 1;
            }

            // If the system has more than 1 core
            if (coreCount > 1)
            {
                SettingsManager.Settings.MultiThread = 1;
            }
            else
            {
                SettingsManager.Settings.MultiThread = 0;
            }
        }

        #endregion Initialization Methods

        #region Handle AppEvents 

        /// <summary>
        /// Method that handles events from Event Producers.
        /// </summary>
        /// <param name="appEvent"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void HandleEvent(AppEvent appEvent)
        {
            // If this is a Song Scan Event (i.e. related to a file scan for a complete list of songs)
            if (appEvent is SongScanEvent)
            {
                // If a scan has been started 
                if ((appEvent as SongScanEvent).SongScanStarting)
                {
                    // Disable the tabcontrol
                    this.Invoke(delegate
                    {
                        tcMain.Enabled = false;
                    });
                }
                // If a scan has been completed 
                else if ((appEvent as SongScanEvent).SongScanComplete)
                {
                    // Enable the tabcontrol
                    this.Invoke(delegate
                    {
                        tcMain.Enabled = true;
                    });
                }
            }
        }



        #endregion Handle AppEvents

        #region Tab Index Selection Changed Handler

        /// <summary>
        /// Event handler for when the tab selection changes in the main tab control.
        /// This is what kick's off a change between the tabs 
        /// (i.e. arguably the biggest change the application's view can make).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void tcMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            var debugMe = sender;

            // reset toolstrip globals
            //Globals.ResetToolStripGlobals();
            ResetBottomToolStrip();

            // quick fix ... visible on when Song Manager is active
            // avoids playback issues when other tabs are active 
            if (tcMain.SelectedIndex != 0)
            {
                Globals.AudioEngine.Stop();
                tsAudioPlayer.Visible = false;
            }
            else
                tsAudioPlayer.Visible = true;

            // If we have a current tab control, call TabLeave() on it.
            if (currentControl != null && currentControl is INotifyTabChanged)
                (currentControl as INotifyTabChanged).TabLeave();

            switch (tcMain.SelectedTab.Text)
            {
                // passing variables(objects) by value to UControl
                // processing order is important to prevent flashing/jumping display
                case "Song Manager":
                    loadSongManagerTabPage();
                    break;
                case "Arrangement Analyzer":
                    // don't reload grid if already loaded
                    // if the 
                    if (!tpArrangements.Controls.Contains(Globals.ArrangementAnalyzer))
                    {
                        this.tpArrangements.Controls.Clear();
                        this.tpArrangements.Controls.Add(Globals.ArrangementAnalyzer);
                        Globals.ArrangementAnalyzer.Dock = DockStyle.Fill;
                        Globals.ArrangementAnalyzer.Location = UCLocation;
                        Globals.ArrangementAnalyzer.Size = UCSize;
                    }

                    Globals.ArrangementAnalyzer.UpdateToolStrip();
                    currentControl = Globals.ArrangementAnalyzer;
                    break;
                case "Duplicates":
                    // force Duplicates check of Custom Inlays
                    Globals.IncludeInlays = true;
                    // force full rescan on load
                    Globals.RescanDuplicates = true;

                    this.tpDuplicates.Controls.Clear();
                    this.tpDuplicates.Controls.Add(Globals.Duplicates);
                    Globals.Duplicates.Dock = DockStyle.Fill;
                    Globals.Duplicates.Location = UCLocation;
                    Globals.Duplicates.Size = UCSize;
                    Globals.Duplicates.UpdateToolStrip();
                    currentControl = Globals.Duplicates;
                    break;
                case "Renamer":
                    this.tpRenamer.Controls.Clear();
                    this.tpRenamer.Controls.Add(Globals.Renamer);
                    Globals.Renamer.Dock = DockStyle.Fill;
                    Globals.Renamer.Location = UCLocation;
                    Globals.Renamer.Size = UCSize;
                    Globals.Renamer.UpdateToolStrip();
                    currentControl = Globals.Renamer;
                    break;
                case "Setlist Manager":
                    this.tpSetlistManager.Controls.Clear();
                    this.tpSetlistManager.Controls.Add(Globals.SetlistManager);
                    Globals.SetlistManager.Dock = DockStyle.Fill;
                    Globals.SetlistManager.Location = UCLocation;
                    Globals.SetlistManager.Size = UCSize;
                    Globals.SetlistManager.UpdateToolStrip();
                    currentControl = Globals.SetlistManager;
                    break;
                case "Profile Song Lists":
                    this.tpProfileSongLists.Controls.Clear();
                    this.tpProfileSongLists.Controls.Add(Globals.ProfileSongLists);
                    Globals.ProfileSongLists.Dock = DockStyle.Fill;
                    Globals.ProfileSongLists.Location = UCLocation;
                    Globals.ProfileSongLists.Size = UCSize;
                    Globals.ProfileSongLists.UpdateToolStrip();
                    currentControl = Globals.ProfileSongLists;
                    break;
                case "Song Packs":
                    this.tpSongPacks.Controls.Clear();
                    this.tpSongPacks.Controls.Add(Globals.SongPacks);
                    Globals.SongPacks.Dock = DockStyle.Fill;
                    Globals.SongPacks.Location = UCLocation;
                    Globals.SongPacks.Size = UCSize;
                    Globals.SongPacks.UpdateToolStrip();
                    currentControl = Globals.SongPacks;
                    break;
                case "Settings":
                    loadSettingsTabPage();
                    break;
                case "About":
                    tpAbout.Controls.Clear();
                    tpAbout.Controls.Add(Globals.About);
                    Globals.About.Location = UCLocation;
                    Globals.About.Size = UCSize;
                    currentControl = Globals.About;
                    break;
            }

            if (currentControl != null && currentControl is INotifyTabChanged)
                (currentControl as INotifyTabChanged).TabEnter();
        }


        #region Tab Index Selection Changed helper methods

        /// <summary>
        /// Method to load and/or display the Song Manager tab.
        /// NOTE: Any initialization processing required for when any tab is first called should be in it's constructor.
        /// NOTE: The only thing that should be here is what's necessary to display the tab.
        /// NOTE: Displaying =/= Updating the tab's content; Updating the tab's content should be done by the tab/control itself.
        /// </summary>
        private void loadSongManagerTabPage()
        {
            SongManager songManager = null;

            // If the Song Manager is not already loaded, we need to load it.
            if (!_tabPageViews.ContainsKey(typeof(SongManager)))
            {
                // Initialize the Song Manager tab panel (the view that appears when clicking on the Song Manager tab).
                songManager = new SongManager(this, playFunction, DockStyle.Fill, UCLocation, UCSize);

                // Add it to Globals... for now
                Globals.SongManager = songManager;

                // Add it to the tab page that holds it
                tpSongManager.Controls.Add(songManager);

                // Add it to the dictionary
                _tabPageViews.Add(typeof(SongManager), songManager);
            }
            else
            {
                // If the Song Manager is already loaded, we just need to get it.
                songManager = _tabPageViews[typeof(SongManager)] as SongManager;
            }

            // Set it as the current control
            currentControl = songManager;
        }

        /// <summary>
        /// Method to initialize and load the Settings tab.
        /// </summary>
        private void loadSettingsTabPage()
        {
            Settings settings = null;

            // If the Settings tab is not already loaded, we need to load it.
            if (!_tabPageViews.ContainsKey(typeof(Settings)))
            {
                // Initialize the Settings tab panel (the view that appears when clicking on the Settings tab).
                settings = new Settings(UCLocation, UCSize, DockStyle.Fill);

                // Add it to the tab page that holds it
                tpSettings.Controls.Add(settings);

                // Add it to the dictionary
                _tabPageViews.Add(typeof(Settings), settings);
            }
            else
            {
                // If the Song Manager is already loaded, we just need to get it.
                settings = _tabPageViews[typeof(Settings)] as Settings;
            }

            // Set it as the current control
            currentControl = settings;
        }

        /// <summary>
        /// Method to get the user control settings set by the parameters in frmMain.
        /// </summary>
        /// <returns></returns>
        public Tuple<Point, Size, DockStyle> GetUserControlSettings()
        {
            return new Tuple<Point, Size, DockStyle>(UCLocation, UCSize, DockStyle.Fill);
        }

        #endregion


        #endregion Tab Index Selection Changed Handler


        /// <summary>
        /// Reset the ToolStrip values.
        /// The ToolStrip holds the progress bar, main message,
        /// status message, cancel label, and the disabled counter label.
        /// </summary>
        public void ResetBottomToolStrip()
        {
            try
            {
                tsProgressBar_Main.Value = 0;
                tsLabel_MainMsg.Visible = false;
                tsLabel_StatusMsg.Visible = false;
                tsLabel_Cancel.Visible = false;
                tsLabel_Cancel.Text = "Cancel";
                tsLabel_DisabledCounter.Visible = false;
            }
            catch {/* DO NOTHING */}
        }




        private void ShowHelp()
        {
            frmNoteViewer.ViewResourcesFile("CustomsForgeSongManager.Resources.HelpGeneral.rtf", "General Help");
        }

        private static void ShowHideLog()
        {
            AppSettings.Instance.ShowLogWindow = !AppSettings.Instance.ShowLogWindow;
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Globals.PrfldbNeedsUpdate)
                Globals.ProfileSongLists.UpdateProfileSongLists();

            if (Globals.PackageRatingNeedsUpdate && !Globals.UpdateInProgress)
                PackageDataTools.UpdatePackageRating();

            // always wait for any PackageRating updates to finish
            while (Globals.UpdateInProgress)
            {
                Application.DoEvents();
                Thread.Sleep(200);
            }

            AppSettings.Instance.FullScreen = WindowState == FormWindowState.Maximized;
            AppSettings.Instance.WindowWidth = this.Width;
            AppSettings.Instance.WindowHeight = this.Height;
            AppSettings.Instance.WindowTop = this.Location.Y;
            AppSettings.Instance.WindowLeft = this.Location.X;

            SMLog.Log("Application is closing ...");
            Globals.CancelBackgroundScan = true;

            if (Globals.Settings == null || Globals.SongManager == null)
            {
                SMLog.Log("<ERROR> Save on close failed ...");
                return;
            }

            if (AppSettings.Instance.CleanOnClosing)
            {
                // don't use the bulldozer here, instead use the bobcat
                // 'My Documents/CFSM' may contain some original files
                SMLog.Log("User selected Clean On Closing ...");
                GenExtensions.DeleteFile(Constants.LogFilePath);
                GenExtensions.DeleteFile(Constants.SongsInfoPath);
                GenExtensions.DeleteFile(Constants.AppSettingsPath);
                GenExtensions.DeleteDirectory(Constants.GridSettingsFolder);
                GenExtensions.DeleteDirectory(Constants.AudioCacheFolder);
                GenExtensions.DeleteDirectory(Constants.TaggerWorkingFolder);
                GenExtensions.DeleteDirectory(Constants.CachePcPath);
                GenExtensions.DeleteDirectory(Constants.SongPacksFolder);

                AppSettings.Instance.CleanOnClosing = false;
            }
            else
            {
                if (Globals.DgvCurrent.Name == "dgvSongsMaster")
                    Globals.SongManager.TabLeave();
                else if (Globals.DgvCurrent.Name == "dgvArrangements")
                    Globals.ArrangementAnalyzer.TabLeave();
                else
                    Globals.Settings.SaveSettingsToFile(Globals.DgvCurrent);

                // do not SaveSongCollectionToFile when changing compatibility mode
                if (File.Exists(Constants.SongsInfoPath))
                {
                    try
                    {
                        DataGridViewAutoFilterColumnHeaderCell.RemoveFilter(Globals.SongManager.GetGrid());
                        Globals.SongManager.SaveSongCollectionToFile();
                    }
                    catch (Exception ex)
                    {
                        // catch RemoveFilter error on closing (system timing issue???)
                        Debug.WriteLine(ex.Message);
                    }
                }
            }

            SMLog.Log("Application closed normally ...");
        }

        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
                case Keys.F3:
                    ShowHideLog();
                    e.Handled = true;
                    break;
                case Keys.F12:
                    tstripContainer.TopToolStripPanelVisible = !tstripContainer.TopToolStripPanelVisible;
                    tstripContainer.BottomToolStripPanelVisible = tstripContainer.TopToolStripPanelVisible;
                    e.Handled = true;
                    break;
                case Keys.F9: // easter egg - DF ThemeDesigner sandbox
                    if (Constants.DebugMode)
                    {
                        using (ThemeDesigner ts = new ThemeDesigner())
                            ts.ShowDialog();
                    }
                    e.Handled = true;
                    break;
            }
        }
        
      

        private void tsBtnUserProfiles_MouseUp(object sender, MouseEventArgs e)
        {
            Globals.TsProgressBar_Main.Value = 50;

            var resetProfileDirPath = false;
            if (e.Button == MouseButtons.Right)
                resetProfileDirPath = true;

            RocksmithProfile.BackupRestore(resetProfileDirPath);
            Globals.TsProgressBar_Main.Value = 100;
        }

        private void tsBtnHelp_Click(object sender, EventArgs e)
        {
            ShowHelp();
        }

        private void tsBtnLaunchRS_Click(object sender, EventArgs e)
        {
            LocalExtensions.LaunchRocksmith2014();
        }

        private void tsBtnRequest_Click(object sender, EventArgs e)
        {
            Ignition.RequestSongOnCustomsForge();
        }

        private void tsBtnUpdate_Click(object sender, EventArgs e)
        {
            tsBtnUpdate.Enabled = false;
            UpdateCFSM();
            tsBtnUpdate.Enabled = true;
        }

        private void tsBtnUpload_Click(object sender, EventArgs e)
        {
            Ignition.UploadToCustomsForge();
        }

        private void tsLabelCancel_Click(object sender, EventArgs e)
        {
            Globals.TsLabel_Cancel.Text = tsLabel_Cancel.Text == "Cancel" ? "Canceling" : "Cancel";
            tsLabel_Cancel.Enabled = false;
        }

        private void tsLabelClearLog_Click(object sender, EventArgs e)
        {
            tbLog.Clear();
            Globals.TsProgressBar_Main.Value = 0;
        }

        private void tsLabelShowHideLog_Click(object sender, EventArgs e)
        {
            ShowHideLog();
        }

        private void frmMain_Load(object sender, EventArgs e) // done after frmMain()
        {
            // be nice to devs don't check for updates
            if (GeneralExtension.IsInDesignMode)
                return;

            tsBtnUpdate.Visible = false;
            var appExePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), APP_EXE);
            var versInfoUrl = String.Format("{0}/{1}", SERVER_URL, "VersionInfo.txt");

            if (AutoUpdater.NeedsUpdate(appExePath, versInfoUrl))
            {
                if (AppSettings.Instance.EnableAutoUpdate)
                {
                    SMLog.Log("CFSM Auto Update Enabled ...");
                    UpdateCFSM();
                }
                else
                {
                    SMLog.Log("CFSM Update Available ...");
                    tsBtnUpdate.Visible = true;
                }
            }

            //if (Constants.OnMac)
            //    tsBtnUpdate.Visible = true;
        }

        private void UpdateCFSM()
        {
            SMLog.Log("Downloading WebApp: " + APP_ARCHIVE + " ...");
            var tempDir = Constants.TempWorkFolder;
            var downloadUrl = string.Format("{0}/{1}", SERVER_URL, APP_ARCHIVE);

            if (AutoUpdater.DownloadWebApp(downloadUrl, APP_ARCHIVE, tempDir))
            {
                if (ZipUtilities.UnrarDir(Path.Combine(tempDir, APP_ARCHIVE), tempDir))
                {
                    Process proc = new Process();
                    proc.StartInfo.FileName = Path.Combine(tempDir, APP_SETUP);
                    proc.StartInfo.Arguments = "-appupdate";
                    proc.Start();
                    // Kill app abruptly so InnoSetup completes
                    // DO NOT use Application.Exit     
                    Environment.Exit(0);
                }
                else
                    MessageBox.Show(APP_SETUP + " not found ..." + Environment.NewLine + "Please manually download CFSM from the webpage.");
            }
        }

        private delegate void DoSomethingWithGridSelectionAction(DataGridView dg, IEnumerable<DataGridViewRow> selected, DataGridViewColumn colSel, List<int> IgnoreColums);
        private void DoSomethingWithGrid(DoSomethingWithGridSelectionAction action)
        {
            if (action != null && currentControl != null && currentControl is IDataGridViewHolder)
            {
                var dataGrid = (currentControl as IDataGridViewHolder).GetGrid();
                if (dataGrid != null)
                {
                    // selects all rows by default
                    var selected = dataGrid.Rows.Cast<DataGridViewRow>();
                    DataGridViewColumn colSel = null;
                    int colSelIdx = -1;
                    if (dataGrid.Columns.Contains("colSelect"))
                    {
                        colSel = dataGrid.Columns["colSelect"];
                        colSelIdx = colSel.Index;
                        var xselected = selected.Where(r => r.Cells["colSelect"].Value != null).Where(r => Convert.ToBoolean(r.Cells["colSelect"].Value)).ToList();
                        // select specific rows if colSelect is selected
                        if (xselected.Count > 0)
                            selected = xselected;
                    }

                    List<int> ignoreColumns = new List<int>();
                    if (colSel != null)
                        ignoreColumns.Add(colSel.Index);

                    foreach (var c in dataGrid.Columns.Cast<DataGridViewColumn>())
                    {
                        if (c is DataGridViewImageColumn || !c.Visible)
                        {
                            ignoreColumns.Add(c.Index);
                            continue;
                        }
                    }

                    action(dataGrid, selected, colSel, ignoreColumns);
                }
            }
        }

        public void DGV2BBCode()
        {
            DoSomethingWithGrid((dataGrid, selection, colSel, ignoreColumns) =>
            {
                var sbTXT = new StringBuilder();
                string columns = String.Empty;
                var orderedCols = dataGrid.Columns.Cast<DataGridViewColumn>()
                    .Where(c => !ignoreColumns.Contains(c.Index))
                    .OrderBy(x => x.DisplayIndex).ToList();

                foreach (var c in orderedCols)
                {
                    columns += c.HeaderText + ", ";
                }

                sbTXT.AppendLine(columns.Trim(new char[] { ',', ' ' }));
                sbTXT.AppendLine(String.Format("[LIST={0}]", selection.Count()));

                foreach (var row in selection)
                {
                    string s = "";
                    foreach (var c in orderedCols)
                    {
                        s += row.Cells[c.Index].Value == null ? " , " : row.Cells[c.Index].Value.ToString() + ", ";
                    }

                    sbTXT.AppendLine(s.Trim(new char[] { ',', ' ' }) + "[/*]");
                }

                sbTXT.AppendLine("[/LIST]");

                using (var noteViewer = new frmNoteViewer())
                {
                    noteViewer.Text = String.Format("{0} . . . {1}", noteViewer.Text, "Song list to BBCode");

                    noteViewer.PopulateText(sbTXT.ToString(), false);
                    noteViewer.ShowDialog();
                }
            });
        }

        public void DGV2HTML()
        {
            DoSomethingWithGrid((dataGrid, selection, colSel, ignoreColumns) =>
            {
                //in the future maybe add some javascript to sort the html table by column
                var sbTXT = new StringBuilder();
                sbTXT.AppendLine("<html><head>");
                sbTXT.AppendLine("<title>CFSM Songs</title>");
                //add the style directly to so it can be saved correctly without external css.
                sbTXT.AppendLine("<style>");
                sbTXT.AppendLine(Properties.Resources.htmExport);
                sbTXT.AppendLine("</style>");
                // sbTXT.AppendLine("<link rel='stylesheet' type='text/css' href='htmExport.css'>");
                sbTXT.AppendLine("</head><body>");
                sbTXT.AppendLine("<table id='CFMGrid'>");
                sbTXT.AppendLine("<tr>");
                var columns = String.Empty;
                var orderedCols = dataGrid.Columns.Cast<DataGridViewColumn>()
                    .Where(c => !ignoreColumns.Contains(c.Index))
                    .OrderBy(x => x.DisplayIndex).ToList();

                foreach (var c in orderedCols)
                {
                    columns += ((char)9) + String.Format("<th>{0}</th>{1}", c.HeaderText, Environment.NewLine);
                }

                sbTXT.AppendLine(columns.Trim());
                sbTXT.AppendLine("</tr>");
                bool altOn = false;

                foreach (var row in selection)
                {
                    sbTXT.AppendLine("<tr" + (altOn ? " class='alt'>" : ">"));
                    string s = string.Empty;
                    foreach (var c in orderedCols)
                    {
                        s += String.Format("<td>{0}</td>", row.Cells[c.Index].Value == null ? "" : row.Cells[c.Index].Value.ToString());
                    }
                    sbTXT.AppendLine(s.Trim());
                    sbTXT.AppendLine("</tr>");
                    altOn = !altOn;
                }

                sbTXT.AppendLine("</table>");
                sbTXT.AppendLine("</body></html>");

                using (var noteViewer = new frmHtmlViewer())
                {
                    noteViewer.Text = String.Format("{0} . . . {1}", noteViewer.Text, "Song list to HTML");
                    noteViewer.PopulateHtml(sbTXT.ToString());
                    noteViewer.ShowDialog();
                }
            });
        }

        public void DGV2CSV()
        {
            var fileName = String.Format("{0}Grid.csv", Globals.DgvCurrent.Name.Replace("dgv", ""));
            var path = Path.Combine(Constants.WorkFolder, fileName);

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "csv files(*.csv)|*.csv|All files (*.*)|*.*";
                sfd.FileName = path;

                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                path = sfd.FileName;
            }

            DoSomethingWithGrid((dataGrid, selection, colSel, ignoreColumns) =>
            {
                var sbCSV = new StringBuilder();
                char csvSep = ';';

                if (!String.IsNullOrEmpty(tsmiCSVSeperator.Text) && tsmiCSVSeperator.Text.Length == 1)
                    csvSep = Convert.ToChar(tsmiCSVSeperator.Text);
                else // reset CSV seperator to the default character
                    tsmiCSVSeperator.Text = csvSep.ToString();

                sbCSV.AppendLine(String.Format(@"sep={0}", csvSep)); // used by Excel to recognize seperator if Encoding.Unicode is used
                string columns = String.Empty;
                var orderedCols = dataGrid.Columns.Cast<DataGridViewColumn>()
                    .Where(c => !ignoreColumns.Contains(c.Index))
                        .OrderBy(x => x.DisplayIndex).ToList();

                foreach (var c in orderedCols)
                {
                    columns += c.HeaderText + csvSep;
                }

                sbCSV.AppendLine(columns.Trim(new char[] { csvSep, ' ' }));

                foreach (var row in selection)
                {
                    string s = "";
                    foreach (var c in orderedCols)
                    {
                        s += row.Cells[c.Index].Value == null ? csvSep.ToString() : row.Cells[c.Index].Value.ToString() + csvSep;
                    }

                    sbCSV.AppendLine(s.Trim(new char[] { csvSep, ' ' }));
                }

                //using (var noteViewer = new frmNoteViewer())
                //{
                //    noteViewer.Text = String.Format("{0} . . . {1}", noteViewer.Text, "Song list to CSV");

                //    noteViewer.PopulateText(sbCSV.ToString(), false);
                //    noteViewer.ShowDialog();
                //}

                try
                {
                    using (StreamWriter file = new StreamWriter(path, false, Encoding.Unicode)) // Excel does not recognize UTF8
                        file.Write(sbCSV.ToString());

                    SMLog.Log(Globals.DgvCurrent.Name + " data saved to:" + path);
                    GenExtensions.PromptOpen(Path.GetDirectoryName(path), Globals.DgvCurrent.Name + " data saved ...");
                }
                catch (IOException ex)
                {
                    SMLog.Log("<Error>: " + ex.Message);
                }
            });
        }

        public void DGV2XML()
        {
            var fileName = String.Format("{0}Grid.xml", Globals.DgvCurrent.Name.Replace("dgv", ""));
            var path = Path.Combine(Constants.WorkFolder, fileName);

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "xml files(*.xml)|*.xml|All files (*.*)|*.*";
                sfd.FileName = path;

                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                path = sfd.FileName;
            }

            DoSomethingWithGrid((dataGrid, selection, colSel, ignoreColumns) =>
            {
                try
                {
                    DataGridView dgvSelection = new DataGridView();

                    // legacy code method produces undesirable results
                    //foreach (DataGridViewColumn col in dataGrid.Columns)
                    //        dgvSelection.Columns.Add((DataGridViewColumn)col.Clone());

                    //dgvSelection.Rows.Add(selection.Count() - 1);

                    //foreach (DataGridViewRow row in selection)
                    //    foreach (DataGridViewColumn col in dataGrid.Columns)
                    //                        dgvSelection.Rows[row.Index].Cells[col.Index].Value = row.Cells[col.Index].Value == null ? DBNull.Value : row.Cells[col.Index].Value;

                    var orderedCols = dataGrid.Columns.Cast<DataGridViewColumn>()
                        .Where(c => !ignoreColumns.Contains(c.Index))
                        .OrderBy(x => x.DisplayIndex).ToList();

                    foreach (DataGridViewColumn col in orderedCols)
                        dgvSelection.Columns.Add((DataGridViewColumn)col.Clone());

                    dgvSelection.Rows.Add(selection.Count() - 1);

                    var rowNdx = 0;
                    foreach (DataGridViewRow row in selection.Where(x => x.Visible))
                    {
                        var colNdx = 0;
                        foreach (DataGridViewColumn col in orderedCols)
                        {
                            dgvSelection.Rows[rowNdx].Cells[colNdx].Value = row.Cells[col.Index].Value == null ? DBNull.Value : row.Cells[col.Index].Value;
                            colNdx++;
                        }
                        rowNdx++;
                    }

                    DataTable dT = DgvConversion.DataGridViewToDataTable(dgvSelection, true);
                    dT.TableName = "item"; // row node name
                    DataSet dS = new DataSet();
                    dS.DataSetName = Globals.DgvCurrent.Name; // root node name
                    dS.Tables.Add(dT);
                    using (StreamWriter fs = new StreamWriter(path))
                        dS.WriteXml(fs);

                    SMLog.Log(Globals.DgvCurrent.Name + " data saved to:" + path);
                    GenExtensions.PromptOpen(Path.GetDirectoryName(path), Globals.DgvCurrent.Name + " data saved ...");
                }
                catch (IOException ex)
                {
                    SMLog.Log("<Error>: " + ex.Message);
                }
            });
        }

        public void DGV2JSON()
        {
            var fileName = String.Format("{0}Grid.json", Globals.DgvCurrent.Name.Replace("dgv", ""));
            var path = Path.Combine(Constants.WorkFolder, fileName);

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "json files(*.json)|*.json|All files (*.*)|*.*";
                sfd.FileName = path;

                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                path = sfd.FileName;
            }

            DoSomethingWithGrid((dataGrid, selection, colSel, ignoreColumns) =>
            {
                try
                {
                    DataGridView dgvSelection = new DataGridView();

                    var orderedCols = dataGrid.Columns.Cast<DataGridViewColumn>()
                       .Where(c => !ignoreColumns.Contains(c.Index))
                       .OrderBy(x => x.DisplayIndex).ToList();

                    foreach (DataGridViewColumn col in orderedCols)
                        dgvSelection.Columns.Add((DataGridViewColumn)col.Clone());

                    dgvSelection.Rows.Add(selection.Count() - 1);

                    var rowNdx = 0;
                    foreach (DataGridViewRow row in selection.Where(x => x.Visible))
                    {
                        var colNdx = 0;
                        foreach (DataGridViewColumn col in orderedCols)
                        {
                            dgvSelection.Rows[rowNdx].Cells[colNdx].Value = row.Cells[col.Index].Value == null ? DBNull.Value : row.Cells[col.Index].Value;
                            colNdx++;
                        }
                        rowNdx++;
                    }

                    DataTable dT = DgvConversion.DataGridViewToDataTable(dgvSelection, true);
                    dT.TableName = Globals.DgvCurrent.Name;
                    DataSet dS = new DataSet();
                    dS.Tables.Add(dT);
                    JToken serializedJson = JsonConvert.SerializeObject(dS, Formatting.Indented, new JsonSerializerSettings { });
                    using (StreamWriter fs = new StreamWriter(path))
                        fs.Write(serializedJson.ToString());

                    SMLog.Log(Globals.DgvCurrent.Name + " data saved to:" + path);
                    GenExtensions.PromptOpen(Path.GetDirectoryName(path), Globals.DgvCurrent.Name + " data saved ...");
                }
                catch (IOException ex)
                {
                    SMLog.Log("<Error>: " + ex.Message);
                }
            });
        }

        private void tsmiJSON_Click(object sender, EventArgs e)
        {
            DGV2JSON();
        }

        private void tsmiXML_Click(object sender, EventArgs e)
        {
            DGV2XML();
        }


        private void tsmiBBCode_Click(object sender, EventArgs e)
        {
            DGV2BBCode();
        }

        private void tsmiCSV_Click(object sender, EventArgs e)
        {
            tsBtnExport.HideDropDown();
            DGV2CSV();
        }

        private void tsmiHTML_Click(object sender, EventArgs e)
        {
            DGV2HTML();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            // if (!AppSettings.Instance.FullScreen)
            {
                // restore user sreeen settings
                int width = this.Width;
                int height = this.Height;
                int top = this.Location.Y;
                int left = this.Location.X;

                if (AppSettings.Instance.WindowWidth > 0)
                    width = AppSettings.Instance.WindowWidth;
                if (AppSettings.Instance.WindowHeight > 0)
                    height = AppSettings.Instance.WindowHeight;
                if (AppSettings.Instance.WindowTop > 0)
                    top = AppSettings.Instance.WindowTop;
                if (AppSettings.Instance.WindowLeft > 0)
                    left = AppSettings.Instance.WindowLeft;

                this.SetBoundsCore(left, top, width, height, BoundsSpecified.All);
            }
        }

        public void PlaySong()
        {
            if (Globals.AudioEngine.IsPaused() || Globals.AudioEngine.IsPlaying())
            {
                if (Globals.AudioEngine.IsPlaying())
                    SMLog.Log("Playback Paused ...");
                else
                    SMLog.Log("Playback Unpaused ...");

                Globals.AudioEngine.Pause();
            }
            else
            {
                SMLog.Log("Playback Started ...");
                Globals.SongManager.PlaySelectedSong();
            }

            timerAudioProgress.Enabled = (Globals.AudioEngine.IsPlaying());
        }

        private void tsbPlay_Click(object sender, EventArgs e)
        {
            PlaySong();
        }

        private void timerAudioProgress_Tick(object sender, EventArgs e)
        {
            tslblTimer.Text = Globals.AudioEngine.GetSongPosition();
            tspbAudioPosition.Value = Globals.AudioEngine.GetSongCompletedPercentage();
            tsAudioPlayer.Refresh();
        }

        private void tsbStop_Click(object sender, EventArgs e)
        {
            tslblTimer.Text = "00:00";
            tspbAudioPosition.Value = 0;
            SMLog.Log("Playback Stopped ...");
            Globals.AudioEngine.Stop();
            timerAudioProgress.Enabled = (Globals.AudioEngine.IsPlaying());
        }

        private void tspbAudioPosition_MouseDown(object sender, MouseEventArgs e)
        {
            if (!Globals.AudioEngine.IsPlaying())
            {
                Globals.SongManager.PlaySelectedSong();
                timerAudioProgress.Enabled = (Globals.AudioEngine.IsPlaying());
            }

            if (Globals.AudioEngine.IsLoaded())
            {
                SMLog.Log("Playback Seeking ...");
                var pos = (float)e.Location.X / (float)tspbAudioPosition.Width;
                Globals.AudioEngine.Seek(pos * Globals.AudioEngine.GetSongLength());
            }
        }

        public Control GetControl()
        {
            return this;
        }

        private void frmMain_ResizeEnd(object sender, EventArgs e)
        {
            // resize/reposition toolstrip items
            tsUtilities.AutoSize = true;
            tsAudioPlayer.AutoSize = true;
            tsUtilities.Location = new Point(0, 0); // force location
            tsAudioPlayer.Location = new Point(tsUtilities.Width + 40, 0); // force location
            tsUtilities.AutoSize = false; // prevent movement
            tsAudioPlayer.AutoSize = false; // prevent movement
        }

        private void invokeIfRequired(Action action)
        {
            if (InvokeRequired)
                Invoke(new Action(() => action()));
            else
                action();
        }

        /// <summary>
        /// Sets the ToolStrip's Main Message
        /// </summary>
        /// <param name="msg">The message to set.</param>
        public void SetToolStripMainMessage(String msg)
        {
            invokeIfRequired(() =>
            {
                if (tsLabel_MainMsg != null)
                {
                    tsLabel_MainMsg.Text = msg;
                    tsLabel_MainMsg.Visible = true;
                }
            });
        }

        /// <summary>
        /// Sets the ToolStrip's Status Message
        /// </summary>
        /// <param name="msg">The message to set.</param>
        public void SetToolStripStatusMessage(String msg)
        {
            invokeIfRequired(() =>
            {
                if (tsLabel_StatusMsg != null)
                {
                    tsLabel_StatusMsg.Text = msg;
                    tsLabel_StatusMsg.Visible = true;
                }
            });
        }

        /// <summary>
        /// Sets the ToolStrip's Main Progress Bar's Value.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetToolStripMainProgressBarValue(int value)
        {
            invokeIfRequired(() =>
            {
                tsProgressBar_Main.Value = value;
            });
        }


    }
}



