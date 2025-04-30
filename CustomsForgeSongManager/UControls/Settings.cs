using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using CustomsForgeSongManager.DataObjects;
using CustomsForgeSongManager.LocalTools;
using GenTools;
using DataGridViewTools;
using CustomControls;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;
using CustomsForgeSongManager.Forms;
using DLogNet;
using CustomsForgeSongManager.DataManager;

namespace CustomsForgeSongManager.UControls
{    /// <summary>
     /// This is the view model for the Settings tab.
     /// This view has controls from the "RS2014 Installation Directory" line at the top
     /// to the "Reload Settings" button on the bottom.
     /// </summary>
    public partial class Settings : UserControl, INotifyTabChanged
    {
        private bool isDirty = false;
        private List<ColumnOrderItem> columnOrderList;

        #region Constructor

        public Settings(Point controlLocation, Size controlSize, DockStyle dockStyle)
        {
            InitializeComponent();
            // save AppSettings.Instance
            Leave += Settings_Leave;

            // set control properties
            this.Location = controlLocation;
            this.Size = controlSize;
            this.Dock = dockStyle;

            // Populate the view
            populateView();
        }

        #endregion Constructor

        #region INotifyTabChanged Implementation

        /// <summary>
        /// Called when the tab is selected.
        /// </summary>
        public void TabEnter()
        {
            cueDgvSettingsPath.Text = String.Empty;
            btnSettingsSave.Text = "Save Settings  ";

            // Populate the view
            populateView();
        }

        public void TabLeave()
        {
            // Handle saving the application settings
            // Validate the RS2014 installation directory
            //ValidateRsDir();
            ValidationTool.ValidateRocksmithInstallDirectory(cueRsDir.Text);

            // autosave Settings
            //SaveSettingsToFile(Globals.DgvCurrent);
            SettingsManager.SaveApplicationSettingsToFile();

            // Handle grid settings
            // If we hace a current dgv
            RADataGridView dgvCurrent = SettingsManager.GetCurrentDataGridView();
            if (dgvCurrent != null)
            {
                //if (String.IsNullOrEmpty(Globals.DgvCurrent.Name))
                //    throw new DataException("<ERROR> TabLeave DgvCurrent.Name is null/empty ...");

                // reload modified column settings to the actual dgvCurrent
                //((RADataGridView)Globals.DgvCurrent).ReLoadColumnOrder(RAExtensions.ManagerGridSettings.ColumnOrder);
                
                //dgvCurrent.ReLoadColumnOrder(SettingsManager.GetSettingsForDataGridView(dgvCurrent).ColumnOrder);
                dgvCurrent.ReLoadColumnOrder(columnOrderList);
                SMLog.Log("Reloaded Modified Column Settings: " + Globals.DgvCurrent.Name);

                // save custom grid settings
                // If the user input a path for this dgv settings file besides the default one
                if (!String.IsNullOrEmpty(cueDgvSettingsPath.Text)
                    && cueDgvSettingsPath.Text != DirectoryManager.GetGridSettingsPathForGridName(dgvCurrent.Name))
                {
                    SerialExtensions.SaveToFile(cueDgvSettingsPath.Text, RAExtensions.SaveColumnOrder(columnOrderList));
                }

                // Save the settings to the default path for now anyways
                SettingsManager.SaveDataGridViewSettings(dgvCurrent);
            }



            // force the initial load on FirstRun
            if (AppSettings.Instance.FirstRun)
            {
                //Globals.ReloadSongManager = true;
                AppSettings.Instance.FirstRun = false;
            }
        }

        #endregion INotifyTabChanged Implementation

        /// <summary>
        /// Private method to load the current settings held by SettingsManager into
        /// the Settings view.
        /// </summary>
        private void loadAppSettingsIntoView()
        {
            // Get the latest settings
            AppSettings settings = SettingsManager.GetApplicationSettings();

            // Load the install directory into the cueRsDir textbox
            //cueRsDir.Text = DirectoryManager.RSInstalledDir;

            // These element values should be set when the tab page is selected
            // => no need to load them if another process is just trying to load application settings values
            cueRsDir.Text = settings.RSInstalledDir;
            chkIncludeRS1CompSongs.Checked = settings.IncludeRS1CompSongs;
            chkIncludeRS2BaseSongs.Checked = settings.IncludeRS2BaseSongs;
            chkIncludeCustomPacks.Checked = settings.IncludeCustomPacks;
            chkIncludeArrangementData.Checked = settings.IncludeArrangementData;
            chkEnableAutoUpdate.Checked = settings.EnableAutoUpdate;
            chkEnableNotifications.Checked = settings.EnableNotifications;
            chkEnableQuarantine.Checked = settings.EnableQuarantine;
            chkValidateD3D.Checked = settings.ValidateD3D;
            chkMacMode.Checked = settings.MacMode;
            chkCleanOnClosing.Checked = settings.CleanOnClosing;
        }

        /*
        public void LoadSettingsFromFile(DataGridView dgvCurrent = null, bool verbose = false)
        {
            try
            {
                Globals.DgvCurrent = dgvCurrent;
                AppSettings.Instance.LoadFromFile(Constants.AppSettingsPath, verbose);
                //var debugMe = AppSettings.Instance.ArrangementAnalyzerFilter;

                // These element values should be set when the tab page is selected
                // => no need to load them if another process is just trying to load application settings values
                cueRsDir.Text = AppSettings.Instance.RSInstalledDir;
                chkIncludeRS1CompSongs.Checked = AppSettings.Instance.IncludeRS1CompSongs;
                chkIncludeRS2BaseSongs.Checked = AppSettings.Instance.IncludeRS2BaseSongs;
                chkIncludeCustomPacks.Checked = AppSettings.Instance.IncludeCustomPacks;
                chkIncludeArrangementData.Checked = AppSettings.Instance.IncludeArrangementData;
                chkEnableAutoUpdate.Checked = AppSettings.Instance.EnableAutoUpdate;
                chkEnableNotifications.Checked = AppSettings.Instance.EnableNotifications;
                chkEnableQuarantine.Checked = AppSettings.Instance.EnableQuarantine;
                chkValidateD3D.Checked = AppSettings.Instance.ValidateD3D;
                chkMacMode.Checked = AppSettings.Instance.MacMode;
                chkCleanOnClosing.Checked = AppSettings.Instance.CleanOnClosing;

                // check validation only on startup
                if (dgvCurrent == null)
                {
                    ValidateRsDir();
                    ValidateD3D();

                    if (!AppSettings.Instance.EnableQuarantine)
                        SMLog.Log("<WARNING> 'Auto Quarantine' is disabled ...");
                }
            }
            catch (Exception ex)
            {
                SMLog.Log(String.Format("<Error> LoadSettingsFromFile: {0}", ex.Message));
            }
        }*/

        /// <summary>
        /// Save the current AppSettings instance to file.
        /// If a DataGridView is passed in, save the DGV settings to file.
        /// </summary>
        /// <param name="dgvCurrent"></param>
        /// <param name="verbose"></param>
        public void SaveSettingsToFile(DataGridView dgvCurrent, bool verbose = false)
        {
            Globals.DgvCurrent = dgvCurrent;
            Debug.WriteLine("Save DataGridView Settings: " + dgvCurrent.Name);

            try
            {
                using (var fs = new FileStream(Constants.AppSettingsPath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    AppSettings.Instance.SerializeXml(fs);
                    if (verbose)
                        SMLog.Log("Saved File: " + Path.GetFileName(Constants.AppSettingsPath));
                }

                if (String.IsNullOrEmpty(dgvCurrent.Name)) // || dgvCurrent.RowCount == 0)
                    return;

                if (!Directory.Exists(Constants.GridSettingsFolder))
                    Directory.CreateDirectory(Constants.GridSettingsFolder);

                SerialExtensions.SaveToFile(Constants.GridSettingsPath, RAExtensions.SaveColumnOrder(dgvCurrent));
                SMLog.Log("Saved File: " + Path.GetFileName(Constants.GridSettingsPath));
            }
            catch (Exception ex)
            {
                SMLog.Log(String.Format("<Error> SaveSettingsToFile: {0}", ex.Message));
            }
        }

        public static void ToogleRescan(bool rescan)
        {
            // speed up load process
            if (AppSettings.Instance.FirstRun)
                return;

            Globals.RescanSongManager = rescan;
            Globals.RescanArrangements = rescan;
            Globals.RescanDuplicates = rescan;
            Globals.RescanSetlistManager = rescan;
            Globals.RescanRenamer = rescan;
            Globals.ReloadSongPacks = rescan;
        }

        public static void ToogleReload(bool reload)
        {
            // speed up load process
            if (AppSettings.Instance.FirstRun)
                return;

            Globals.ReloadSongManager = reload;
            Globals.ReloadArrangements = reload;
            Globals.ReloadDuplicates = reload;
            Globals.ReloadSetlistManager = reload;
            Globals.ReloadRenamer = reload;
            Globals.ReloadSongPacks = reload;
        }

        private bool ValidateD3D()
        {
            if (!AppSettings.Instance.ValidateD3D || Constants.OnMac)
            {
                if (Constants.OnMac)
                    SMLog.Log("<MAC MODE> 'Validate D3DX9_42.dll' checkbox is not applicable ...");
                else
                    SMLog.Log("<WARNING> 'Validate D3DX9_42.dll' checkbox is disabled ...");

                return false;
            }

            // validate remastered version of D3DX9_42.dll
            // discountinued support for legacy version of D3DX9_42.dll (commented out)
            var luaPath = Path.Combine(AppSettings.Instance.RSInstalledDir, "lua5.1.dll");
            var steamClientPath = Path.Combine(AppSettings.Instance.RSInstalledDir, "Steamclient.dll");
            var d3dPath = Path.Combine(AppSettings.Instance.RSInstalledDir, "D3DX9_42.dll");

            if (!File.Exists(d3dPath))
            {
                var diaMsg = "The 'D3DX9_42.dll' file could not be found. Would you like CFSM to install the dll file that is required to play CDLC files?";
                if (DialogResult.No == BetterDialog2.ShowDialog(GenExtensions.SplitString(diaMsg, 30), "Validating D3DX9_42.dll ...", null, "Yes", "No", Bitmap.FromHicon(SystemIcons.Warning.Handle), "Warning", 0, 150))
                {
                    SMLog.Log("<WARNING> User aborted installing 'D3DX9_42.dll' file ...");
                    return false;
                }

                // working directly with the file rather than an embedded resource 
                if (File.Exists(luaPath) || File.Exists(steamClientPath))
                {
                    //GenExtensions.CopyFile(Path.Combine(Constants.ApplicationFolder, "D3DX9_42.dll.old"), Path.Combine(AppSettings.Instance.RSInstalledDir, "D3DX9_42.dll"), true, false);
                    //SMLog.Log("Installed 'D3DX9_42.dll' file for Rocksmith 2014 ...");
                    SMLog.Log("<WARNING> Legacy 'D3DX9_42.dll' file installation for Rocksmith 2014 is not supported ...");
                }
                else
                {
                    GenExtensions.CopyFile(Path.Combine(Constants.ApplicationFolder, "D3DX9_42.dll.new"), Path.Combine(AppSettings.Instance.RSInstalledDir, "D3DX9_42.dll"), true, false);
                    SMLog.Log("Installed 'D3DX9_42.dll' file for Rocksmith 2014 Remastered ...");
                }
            }
            else
            {
                if (File.GetCreationTime(d3dPath) >= new DateTime(2020, 7, 1)) // If the user is using a RSMods new D3DX9_42.dll version
                {
                    AppSettings.Instance.ValidateD3D = false;
                    return true;
                }

                // verify correct dll is installed using MD5 Hash
                var d3dFileMD5 = GenExtensions.GetMD5Hash(d3dPath);
                var d3dNewMD5 = GenExtensions.GetMD5Hash(Path.Combine(Constants.ApplicationFolder, "D3DX9_42.dll.new"));
                var d3dOldMD5 = GenExtensions.GetMD5Hash(Path.Combine(Constants.ApplicationFolder, "D3DX9_42.dll.old"));
                var d3dBasicModsMD5 = GenExtensions.GetMD5Hash(Path.Combine(Constants.ApplicationFolder, "D3DX9_42.dll.basic_mods")); //for the basic (custom song lists only) version of the modded DLL
                //var d3dExtraModsMD5 = GenExtensions.GetMD5Hash(Constants.ApplicationFolder, "D3X9_42.dll.extra_mods"); //for a future modded DLL

                if (((File.Exists(luaPath) || File.Exists(steamClientPath)) && d3dFileMD5 != d3dOldMD5) || ((!File.Exists(luaPath) && !File.Exists(steamClientPath)) && d3dFileMD5 != d3dNewMD5 && d3dFileMD5 != d3dBasicModsMD5))
                {
                    var dlgMsg1 = "The installed 'D3DX9_42.dll' file MD5 hash value is invalid. Would you like CFSM to update the dll file that is required to play CDLC files?";
                    var dlgMsg2 = "Note: If your CDLC are working fine then answer 'No' and then disable future validation checks in the 'Settings' tab menu.";
                    var dlgMsg = GenExtensions.SplitString(dlgMsg1, 30) + Environment.NewLine + Environment.NewLine + GenExtensions.SplitString(dlgMsg2, 30);

                    if (DialogResult.No == BetterDialog2.ShowDialog(dlgMsg, "Validating D3DX9_42.dll ...", null, "Yes", "No", Bitmap.FromHicon(SystemIcons.Warning.Handle), "Warning", 0, 150))
                    {
                        SMLog.Log("<WARNING> User aborted updating the 'D3DX9_42.dll' file ...");
                        return false;
                    }

                    if (File.Exists(luaPath) || File.Exists(steamClientPath))
                    {
                        //GenExtensions.CopyFile(Path.Combine(Constants.ApplicationFolder, "D3DX9_42.dll.old"), Path.Combine(AppSettings.Instance.RSInstalledDir, "D3DX9_42.dll"), true, false);
                        //SMLog.Log("Updated 'D3DX9_42.dll' file for Rocksmith 2014 ...");
                        SMLog.Log("<WARNING> Legacy 'D3DX9_42.dll' file updating for Rocksmith 2014 is not supported ...");
                    }
                    else
                    {
                        GenExtensions.CopyFile(Path.Combine(Constants.ApplicationFolder, "D3DX9_42.dll.new"), Path.Combine(AppSettings.Instance.RSInstalledDir, "D3DX9_42.dll"), true, false);
                        SMLog.Log("Updated 'D3DX9_42.dll' file for Rocksmith 2014 Remastered ...");
                    }
                }
                else
                    SMLog.Log("Validated existing 'D3DX9_42.dll' file installation ...");
            }

            return true;
        }

        private void ValidateRsDir()
        {
            // validate Rocksmith installation directory
            var rsDir = AppSettings.Instance.RSInstalledDir;
            if (String.IsNullOrEmpty(rsDir) || !Directory.Exists(rsDir))
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Select Rocksmith 2014 Installation Directory";
                    fbd.SelectedPath = LocalExtensions.GetSteamDirectory();

                    if (fbd.ShowDialog() != DialogResult.OK)
                        return;

                    cueRsDir.Text = fbd.SelectedPath;
                }

                if (!Directory.Exists(Path.Combine(cueRsDir.Text, "dlc")))
                {
                    MessageBox.Show(new Form { TopMost = true }, String.Format("Please select a directory that  {0}contains a 'dlc' subdirectory.", Environment.NewLine), Constants.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ValidateRsDir(); // put the user into a loop (force selection)
                }

                AppSettings.Instance.RSInstalledDir = cueRsDir.Text;
                SaveSettingsToFile(Globals.DgvCurrent);
            }

            SMLog.Log("Validated RS2014 Installation Directory: " + AppSettings.Instance.RSInstalledDir);
        }

        private void Settings_Leave(object sender, EventArgs e)
        {
            // This method is made obsolete by the SettingsManager.ShutDown() call
            // during frmMain's FormClosing event
            // => Settings inside SettingsManager will always be saved when application is exited.
            /*
            if (String.IsNullOrEmpty(AppSettings.Instance.RSInstalledDir))
                ValidateRsDir();

            SaveSettingsToFile(Globals.DgvCurrent);
            */
        }

        private void btnEmptyLogs_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to empty the log files?", Constants.ApplicationName + " ... Warning", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            GenExtensions.DeleteFile(AppSettings.Instance.LogFilePath);
            SMLog.Logger.AddTargetFile(AppSettings.Instance.LogFilePath);
            GenExtensions.DeleteFile(Constants.RepairsErrorLogPath);
            Globals.TbLog.Clear();
            SMLog.Log("Log files have been emptied ...");
            SMLog.Log("Starting new log ...");
            //SMLog.Log(Constants.AppTitle);
        }

        private void btnResetDownloads_Click(object sender, EventArgs e)
        {
            AppSettings.Instance.DownloadsDir = String.Empty;
            SMLog.Log("CDLC downloads folder path was reset ...");
        }

        private void btnSettingsLoad_Click(object sender, EventArgs e)
        {
            /*
            if (!String.IsNullOrEmpty(cueDgvSettingsPath.Text))
                SetGlobalsDgvCurrent(GetDgvGridName(cueDgvSettingsPath.Text));

            if (String.IsNullOrEmpty(Globals.DgvCurrent.Name))
                return;
            */

            // Load the application settings and the current DGV settings
            //LoadSettingsFromFile(Globals.DgvCurrent, true);
            SettingsManager.TryLoadApplicationSettingsFromFile();
            // TODO: Load current DGV settings

            // Populate the Settings view with the new settings
            //PopulateSettings(Globals.DgvCurrent);
            populateView();
        }

        private void btnSettingsSave_Click(object sender, EventArgs e)
        {
            SaveSettingsToFile(Globals.DgvCurrent);

            if (!String.IsNullOrEmpty(cueDgvSettingsPath.Text))
            {
                // grid settings file naming convention: 'dgv[GridName][[CustomName]].xml'
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Grid Settings XML Files (dgv[GridName][[CustomName]].xml)|dgv*.xml";
                    sfd.InitialDirectory = Constants.GridSettingsFolder;
                    sfd.Title = "Save grid settings file ... 'dgv[GridName][[CustomName]].xml'";
                    sfd.FileName = cueDgvSettingsPath.Text;

                    if (sfd.ShowDialog() != DialogResult.OK)
                        return;

                    // validate the new custom grid file name
                    if (String.IsNullOrEmpty(GetDgvGridName(sfd.FileName)))
                        return;

                    // ((RADataGridView)Globals.DgvCurrent).ReLoadColumnOrder(RAExtensions.ManagerGridSettings.ColumnOrder);
                    //SerialExtensions.SaveToFile(sfd.FileName, RAExtensions.SaveColumnOrder(Globals.DgvCurrent));
                    SerialExtensions.SaveToFile(sfd.FileName, RAExtensions.SaveColumnOrder(columnOrderList));
                    cueDgvSettingsPath.Text = sfd.FileName;
                    SMLog.Log("Saved Custom Grid Settings XML File: " + sfd.FileName);
                }
            }
        }

        private void chkEnableAutoUpdate_Click(object sender, EventArgs e)
        {
            AppSettings.Instance.EnableAutoUpdate = chkEnableAutoUpdate.Checked;
        }

        private void chkEnableNotifications_Click(object sender, EventArgs e)
        {
            // Change the property value and let listeners handle the event
            AppSettings.Instance.EnableNotifications = chkEnableNotifications.Checked;
        }

        private void chkEnableQuarantine_Click(object sender, EventArgs e)
        {
            AppSettings.Instance.EnableQuarantine = chkEnableQuarantine.Checked;
        }

        private void chkIncludeArrangementData_Click(object sender, EventArgs e)
        {
            AppSettings.Instance.IncludeArrangementData = chkIncludeArrangementData.Checked;
            ToogleRescan(true);
        }

        private void chkIncludeCustomPacks_Click(object sender, EventArgs e)
        {
            AppSettings.Instance.IncludeCustomPacks = chkIncludeCustomPacks.Checked;
            ToogleReload(true);
        }

        private void chkIncludeRS1CompSongs_Click(object sender, EventArgs e)
        {
            AppSettings.Instance.IncludeRS1CompSongs = chkIncludeRS1CompSongs.Checked;
            ToogleReload(true);
        }

        private void chkIncludeRS2BaseSongs_Click(object sender, EventArgs e)
        {
            AppSettings.Instance.IncludeRS2BaseSongs = chkIncludeRS2BaseSongs.Checked;
            ToogleReload(true);
        }

        private void chkMacMode_Click(object sender, EventArgs e)
        {
            if (chkMacMode.Checked)
            {
                MessageBox.Show("Switching to Mac Compatibility Mode ...  " + Environment.NewLine +
                    "CFSM will automatically restart!" + Environment.NewLine + Environment.NewLine +
                    "Perform a full rescan on restart if any songs are missing.",
                    "Mac Mode Enabled ...", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                SMLog.Log("Switched to Mac Compatibility Mode ...");
            }
            else
            {
                MessageBox.Show("Switching to PC Compatibility Mode ...  " + Environment.NewLine +
                    "CFSM will automatically restart!" + Environment.NewLine + Environment.NewLine +
                    "Perform a full rescan on restart if any songs are missing.",
                    "PC Mode Enabled ...", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                SMLog.Log("Switched to PC Compatibility Mode ...");
            }

            GenExtensions.DeleteFile(Constants.SongsInfoPath);
            AppSettings.Instance.MacMode = chkMacMode.Checked;
            AppSettings.Instance.FirstRun = true;

            // restart new instance of application and shutdown original
            Application.Restart(); // this triggers frmMain_FormClosing method
            Environment.Exit(0);
        }

        private void chkValidateD3D_Click(object sender, EventArgs e)
        {
            AppSettings.Instance.ValidateD3D = chkValidateD3D.Checked;
            ValidateD3D();
        }

        private void cueRsDir_MouseClick(object sender, MouseEventArgs e)
        {
            if (Constants.OnMac)
            {
                var diaMsg = "The RS2014 Installation Directiory may be hidden.  It can be found at:" + Environment.NewLine + @"Z:\Users[username]\Library\Application Support\Steam\steamapps\common\Rocksmith2014";

                BetterDialog2.ShowDialog(diaMsg, "Mac User ...", null, null, "Ok", Bitmap.FromHicon(SystemIcons.Information.Handle), "ReadMe", 150, 150);
            }

            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = LocalExtensions.GetSteamDirectory();
                fbd.Description = "Select RS2014 Installation Directory ...";

                if (fbd.ShowDialog() != DialogResult.OK)
                    return;

                cueRsDir.Text = fbd.SelectedPath;
            }

            if (!Directory.Exists(Path.Combine(cueRsDir.Text, "dlc")))
            {
                MessageBox.Show(String.Format("Please select a directory that  {0}contains a 'dlc' subdirectory.", Environment.NewLine), Constants.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ToogleRescan(true);

            // update RSInstalledDir after above error check passes
            AppSettings.Instance.RSInstalledDir = cueRsDir.Text;
            SMLog.Log("Updated RS2014 Installation Directory: " + AppSettings.Instance.RSInstalledDir);

            if (Constants.OnMac)
            {
                SMLog.Log("<README> Send this entire Log output (copy/paste) to Cozy1 for analysis ...");
                SMLog.Log("AppSettings.Instance.OnMac = " + AppSettings.Instance.MacMode);
                SMLog.Log("AppSettings.Instance.RSInstalledDir = " + AppSettings.Instance.RSInstalledDir);
                SMLog.Log("Application.ExecutablePath = " + Application.ExecutablePath);
                SMLog.Log("Path.GetDirectoryName(Application.ExecutablePath) = " + Path.GetDirectoryName(Application.ExecutablePath));
                SMLog.Log("Constants.ApplicationFolder = " + Constants.ApplicationFolder);
                SMLog.Log("Found 'Application Support' folder: " + Constants.Rs2DlcFolder.Contains("Application Support"));
            }
        }

        private void chkCleanOnClosing_Click(object sender, EventArgs e)
        {
            AppSettings.Instance.CleanOnClosing = chkCleanOnClosing.Checked;
        }

        private void btnResetThreading_Click(object sender, EventArgs e)
        {
            AppSettings.Instance.MultiThread = -1;
            SMLog.Log("CFSM multi threading usage was reset ...");
        }

        /// <summary>
        /// Old method to populate the settings page before "TabEnter" is called.
        /// </summary>
        /// <param name="dgvCurrent">The DGV loaded for the page PRIOR to tabbing to the settings page.</param>
        public void PopulateSettings(DataGridView dgvCurrent)
        {
            // If the given DGV is one that has a name 
            if (!String.IsNullOrEmpty(dgvCurrent.Name))
            {
                SMLog.Log("Populating Settings GUI for " + dgvCurrent.Name + " ...");
                Globals.DgvCurrent = dgvCurrent;

                // each DataGridView has a tag which holds a friendly name
                var dgvTag = dgvCurrent.Tag.ToString();
                lblDgvColumns.Text = String.Format("Grid settings for {0} from file: {1}", dgvTag, Path.GetFileName(Constants.GridSettingsPath));
            }

            // If the DGV settings for the given DGV are not found, then create a new default settings file
            if (!File.Exists(Constants.GridSettingsPath))
            {
                if (!Directory.Exists(Constants.GridSettingsFolder))
                    Directory.CreateDirectory(Constants.GridSettingsFolder);

                SerialExtensions.SaveToFile(Constants.GridSettingsPath, RAExtensions.SaveColumnOrder(Globals.DgvCurrent));
                SMLog.Log("<WARNING> Did not find file so created new default file: " + Path.GetFileName(Constants.GridSettingsPath));
            }

            LoadDgvColumns(Constants.GridSettingsPath);
        }

        private void populateView()
        {
            // Handle loading the current settings 
            loadAppSettingsIntoView();

            // Get the DGV last loaded by the application
            RADataGridView dgvCurrent = SettingsManager.GetCurrentDataGridView();
            if (dgvCurrent != null)
            {
                SMLog.Log("Populating Settings GUI for " + dgvCurrent.Name + " ...");
                
                // each DataGridView has a tag which holds a friendly name
                var dgvTag = dgvCurrent.Tag.ToString();
                lblDgvColumns.Text = String.Format("Grid settings for {0} from file: {1}", dgvTag, Path.GetFileName(Constants.GridSettingsPath));

                RADataGridViewSettings settings = SettingsManager.GetSettingsForDataGridView(dgvCurrent);
                loadDgvDataIntoView(dgvCurrent, settings);
            }
        }


        public void LoadDgvColumns(string gridSettingsPath)
        {
            isDirty = false;

            try
            {
                RAExtensions.ManagerGridSettings = SerialExtensions.LoadFromFile<RADataGridViewSettings>(gridSettingsPath);
                SMLog.Log("Loaded File: " + Path.GetFileName(gridSettingsPath));

                // reset the grid data
                dgvColumns.DataSource = null;
                dgvColumns.AutoGenerateColumns = false;
                columnOrderList = RAExtensions.ManagerGridSettings.ColumnOrder;
                // allows bound grid to be sorted by clicking column headers
                FilteredBindingList<ColumnOrderItem> fbl = new FilteredBindingList<ColumnOrderItem>(columnOrderList);
                BindingSource bs = new BindingSource { DataSource = fbl };
                dgvColumns.DataSource = bs;

                // initial sort is ascending on DisplayIndex
                dgvColumns.Sort(dgvColumns.Columns["colDisplayIndex"], ListSortDirection.Ascending);
                dgvColumns.SelectionMode = DataGridViewSelectionMode.CellSelect;
                colDisplayIndex.ReadOnly = false;
                colDisplayIndex.ToolTipText = "Click to Sort (Read/Write)\r\nEither manually edit 'DisplayIndex', or\r\ndrag and drop a 'HeaderText' row\r\nto change grid column display order.\r\n\r\nNOTE: CFSM makes its best effort to\r\nauto correct any manual entry mistakes.";

                // initialize drag and drop event handlers
                dgvColumns.AllowDrop = true;
                dgvColumns.MouseDown += new MouseEventHandler(dgvColumns_MouseDown);
                dgvColumns.MouseMove += new MouseEventHandler(dgvColumns_MouseMove);
                dgvColumns.DragOver += new DragEventHandler(dgvColumns_DragOver);
                dgvColumns.DragDrop += new DragEventHandler(dgvColumns_DragDrop);
            }
            catch (Exception ex)
            {
                SMLog.Log("<ERROR> GridSettings could not be loaded ...");
                SMLog.Log("Windows 10 users must uninstall .Net 4.7 and manually install .Net 4.0 if this error persists ...");
                SMLog.Log(ex.Message);
                RAExtensions.ManagerGridSettings = null; // reset
            }
        }



        private void loadDgvDataIntoView(RADataGridView dgv, RADataGridViewSettings settings)
        {
            // Put the settings file in the cueDgvsettingsfile path
            if (dgv != null)
            {
                cueDgvSettingsPath.Text = DirectoryManager.GetGridSettingsPathForGridName(dgv.Name);
            }

            try
            {
                // reset the grid data
                dgvColumns.DataSource = null;
                dgvColumns.AutoGenerateColumns = false;
                columnOrderList = settings.ColumnOrder;

                // allows bound grid to be sorted by clicking column headers
                FilteredBindingList<ColumnOrderItem> fbl = new FilteredBindingList<ColumnOrderItem>(columnOrderList);
                BindingSource bs = new BindingSource { DataSource = fbl };
                dgvColumns.DataSource = bs;

                // initial sort is ascending on DisplayIndex
                dgvColumns.Sort(dgvColumns.Columns["colDisplayIndex"], ListSortDirection.Ascending);
                dgvColumns.SelectionMode = DataGridViewSelectionMode.CellSelect;
                colDisplayIndex.ReadOnly = false;
                colDisplayIndex.ToolTipText = "Click to Sort (Read/Write)\r\nEither manually edit 'DisplayIndex', or\r" +
                    "\ndrag and drop a 'HeaderText' row\r\nto change grid column display order.\r\n\r\n" +
                    "NOTE: CFSM makes its best effort to\r\nauto correct any manual entry mistakes.";

                // initialize drag and drop event handlers
                dgvColumns.AllowDrop = true;
                dgvColumns.MouseDown += new MouseEventHandler(dgvColumns_MouseDown);
                dgvColumns.MouseMove += new MouseEventHandler(dgvColumns_MouseMove);
                dgvColumns.DragOver += new DragEventHandler(dgvColumns_DragOver);
                dgvColumns.DragDrop += new DragEventHandler(dgvColumns_DragDrop);
            }
            catch (Exception ex)
            {
                SMLog.Log("<ERROR> GridSettings could not be loaded ...");
                SMLog.Log("Windows 10 users must uninstall .Net 4.7 and manually install .Net 4.0 if this error persists ...");
                SMLog.Log(ex.Message);
                //RAExtensions.ManagerGridSettings = null; // reset
            }
        }

        private void lnkSelectAll_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            bool deselect = Convert.ToBoolean(dgvColumns.Rows[0].Cells["colVisible"].Value);

            for (int i = 0; i < dgvColumns.Rows.Count; i++)
            {
                dgvColumns.Rows[i].Cells["colVisible"].Value = Convert.ToString(!deselect);
            }
        }

        private void dgvColumns_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            isDirty = true;
            // refresh grid on manual DisplayIndex change
            if (!colDisplayIndex.ReadOnly)
            {
                dgvColumns.InvalidateCell(e.ColumnIndex, e.RowIndex);
                dgvColumns.Refresh();
            }
        }



        private void cueDgvSettingsPath_MouseClick(object sender, MouseEventArgs e)
        {
            var currentDirectory = Environment.CurrentDirectory;

            using (var ofd = new OpenFileDialog())
            {
                // ensure proper usage of 'My Documents\CFSM' folder and reset initial directory if needed
                if (!currentDirectory.Contains(Constants.WorkFolder))
                    ofd.InitialDirectory = Constants.GridSettingsFolder;

                ofd.RestoreDirectory = false;
                ofd.Filter = "Grid Settings XML Files (dgv[GridName][[CustomName]].xml)|dgv*.xml";
                ofd.Title = "Select a grid settings file to edit ...";
                ofd.CheckFileExists = true;

                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                cueDgvSettingsPath.Text = ofd.FileName;
                btnSettingsSave.Text = "Save As       ";
            }

            if (!RAExtensions.ValidateGridSettingsVersion(cueDgvSettingsPath.Text))
                throw new DataException("<ERROR> Invalid grid settings version " + cueDgvSettingsPath.Text);

            SetGlobalsDgvCurrent(GetDgvGridName(cueDgvSettingsPath.Text));
            LoadDgvColumns(cueDgvSettingsPath.Text);
        }

        private void dgvColumns_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            // validate user input
            int newInteger;
            var colNdx = e.ColumnIndex;
            var rowNdx = e.RowIndex;

            if (dgvColumns.Rows[rowNdx].IsNewRow)
                return;

            // clear any previous error message glyph
            dgvColumns.Rows[rowNdx].ErrorText = "";

            if (colNdx == dgvColumns.Columns["colDisplayIndex"].Index)
            {
                if (!int.TryParse(e.FormattedValue.ToString(), out newInteger) || newInteger < 0 || newInteger >= dgvColumns.Rows.Count)
                {
                    e.Cancel = true;
                    dgvColumns.Rows[rowNdx].ErrorText = "DisplayIndex must be between 0 and " + (dgvColumns.Rows.Count - 1);
                }
            }
            else if (colNdx == dgvColumns.Columns["colWidth"].Index)
            {
                if (!int.TryParse(e.FormattedValue.ToString(), out newInteger) || newInteger < 1 || newInteger > 999)
                {
                    e.Cancel = true;
                    dgvColumns.Rows[rowNdx].ErrorText = "Width must be between 1 and 999";
                }
            }

            // dgvColumns.Rows[rowNdx].ErrorText = "Oops ... that's not right!";
        }

        private string GetDgvGridName(string dgvSettingsPath)
        {
            // validate dgvSettingsPath file name and gets dgvGridName
            var dgvGridName = String.Empty;
            var settingsFile = Path.GetFileName(dgvSettingsPath);
            Regex rgxDefaultName = new Regex("^dgv[a-zA-Z]+\\.xml$");
            Regex rgxCustomName = new Regex("^dgv[a-zA-Z]+\\[[a-zA-Z0-9]+]\\.xml$");

            // confirm grid settings file name matches convention 'dgv[GridName][[CustomName]].xml' 
            if (rgxCustomName.IsMatch(settingsFile) || rgxDefaultName.IsMatch(settingsFile))
            {
                dgvGridName = Path.GetFileNameWithoutExtension(dgvSettingsPath);
                var beginBracket = dgvGridName.IndexOf("[");
                var endBracket = dgvGridName.IndexOf("]");
                var dgvNameCustom = String.Empty;
                if (beginBracket != -1 && endBracket != -1)
                    dgvNameCustom = dgvGridName.Substring(beginBracket, endBracket - beginBracket + 1);

                if (!String.IsNullOrEmpty(dgvNameCustom))
                    dgvGridName = dgvGridName.Replace(dgvNameCustom, "");

                var splitName = Regex.Split(dgvGridName, @"(?<!^)(?=[A-Z])");
                var dgvTag = splitName[1];
                for (int i = 2; i < splitName.Length; i++)
                    dgvTag = dgvTag + " " + splitName[i];

                lblDgvColumns.Text = String.Format("Settings for {0} from file: {1}", dgvTag, Path.GetFileName(dgvSettingsPath));
            }
            else
            {
                var diaMsg = "<WARNING> Invalid grid settings file name: " + Path.GetFileName(dgvSettingsPath) + Environment.NewLine + "must be in the form: dgv[GridName][[CustomName]].xml";
                BetterDialog2.ShowDialog(diaMsg, "Settings ...", null, null, "Ok", Bitmap.FromHicon(SystemIcons.Warning.Handle), "File Naming Error ...", 0, 150);
            }

            return dgvGridName;
        }

        private void SetGlobalsDgvCurrent(string dgvGridName)
        {
            switch (dgvGridName)
            {
                case "dgvSongsMaster":
                    Globals.DgvCurrent = Globals.SongManager.GetGrid();
                    Globals.ReloadSongManager = true;
                    break;
                case "dgvArrangements":
                    Globals.DgvCurrent = Globals.ArrangementAnalyzer.GetGrid();
                    Globals.ReloadArrangements = true;
                    break;
                case "dgvDuplicates":
                    Globals.DgvCurrent = Globals.Duplicates.GetGrid();
                    Globals.ReloadDuplicates = true;
                    break;
                case "dgvSetlistMaster":
                    Globals.DgvCurrent = Globals.SetlistManager.GetGrid();
                    Globals.ReloadSetlistManager = true;
                    break;
                case "dgvSongListMaster":
                    Globals.DgvCurrent = Globals.ProfileSongLists.GetGrid();
                    Globals.ReloadProfileSongLists = true;
                    break;
                case "dgvSongPacks":
                    Globals.DgvCurrent = Globals.SongPacks.GetGrid();
                    Globals.ReloadSongPacks = true;
                    break;
                default:
                    throw new DataException("<ERROR> Can not get grid control for " + dgvGridName);
            }
        }


        #region Drag/Drop effect for dgv rows

        private Rectangle dragBoxFromMouseDown;
        private int rowIndexFromMouseDown;
        private int colIndexFromMouseDown;
        private int rowIndexOfItemUnderMouseToDrop;
        private int colIndexOfItemUnderMouseToDrop;

        private void dgvColumns_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                // If the mouse moves outside the rectangle, start the drag.
                if (dragBoxFromMouseDown != Rectangle.Empty && !dragBoxFromMouseDown.Contains(e.X, e.Y))
                {
                    // Proceed with the drag and drop, passing in the grid data.                   
                    DragDropEffects dropEffect = dgvColumns.DoDragDrop(dgvColumns.Rows[rowIndexFromMouseDown], DragDropEffects.Move);
                }
            }
        }

        private void dgvColumns_MouseDown(object sender, MouseEventArgs e)
        {
            // Get the row index of the item the mouse is below.
            rowIndexFromMouseDown = dgvColumns.HitTest(e.X, e.Y).RowIndex;
            // Get the column index of the item the mouse is below.
            colIndexFromMouseDown = dgvColumns.HitTest(e.X, e.Y).ColumnIndex;

            if (rowIndexFromMouseDown != -1)
            {
                // Remember the point where the mouse down occurred.
                // The DragSize indicates the size that the mouse can move
                // before a drag event should be started.               
                Size dragSize = SystemInformation.DragSize;

                // Create a rectangle using the DragSize, with the mouse position being
                // at the center of the rectangle.
                dragBoxFromMouseDown = new Rectangle(new Point(e.X - (dragSize.Width / 2), e.Y - (dragSize.Height / 2)), dragSize);
            }
            else
            {
                // Reset the rectangle if the mouse is not over an item in the grid.
                dragBoxFromMouseDown = Rectangle.Empty;
            }
        }

        private void dgvColumns_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void dgvColumns_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                // The mouse locations are relative to the screen, so they must be
                // converted to client coordinates.
                Point clientPoint = dgvColumns.PointToClient(new Point(e.X, e.Y));

                // Get the row index of the item the mouse is below.
                rowIndexOfItemUnderMouseToDrop = dgvColumns.HitTest(clientPoint.X, clientPoint.Y).RowIndex;
                // Get the column index of the item the mouse is below.
                colIndexOfItemUnderMouseToDrop = dgvColumns.HitTest(clientPoint.X, clientPoint.Y).ColumnIndex;

                // If the drag operation was a move then remove and insert the row.
                if (e.Effect == DragDropEffects.Move)
                {
                    // check if grid is properly sorted
                    if (dgvColumns.SortedColumn.Index != dgvColumns.Columns["colDisplayIndex"].Index ||
                        (dgvColumns.SortedColumn.Index == dgvColumns.Columns["colDisplayIndex"].Index && dgvColumns.SortOrder != SortOrder.Ascending))
                    {
                        var diaMsg = "Sort on DisplayIndex (ascending) before" + Environment.NewLine + "using the drag and drop feature ...";
                        BetterDialog2.ShowDialog(diaMsg, "Settings ...", null, null, "Ok", Bitmap.FromHicon(SystemIcons.Warning.Handle), "ReadMe", 0, 150);
                        return;
                    }

                    if (colIndexFromMouseDown == dgvColumns.Columns["colHeaderText"].Index)
                    {
                        // use this method for unbound grids
                        //DataGridViewRow rowToMove = e.Data.GetData(typeof(DataGridViewRow)) as DataGridViewRow;
                        //dgvColumns.Rows.RemoveAt(rowIndexFromMouseDown);
                        //dgvColumns.Rows.Insert(rowIndexOfItemUnderMouseToDrop, rowToMove);

                        // use this method for bound grids
                        dynamic list = dgvColumns.DataSource;
                        var item = list[rowIndexFromMouseDown];
                        list.RemoveAt(rowIndexFromMouseDown);

                        if (rowIndexOfItemUnderMouseToDrop == -1)
                            list.Add(item);
                        else
                            list.Insert(rowIndexOfItemUnderMouseToDrop, item);

                        // refresh row placement in grid
                        dgvColumns.Invalidate();
                        dgvColumns.Refresh();

                        // renumber display index according to the new row position
                        for (int i = 0; i < dgvColumns.Rows.Count; i++)
                            dgvColumns.Rows[i].Cells["colDisplayIndex"].Value = i;

                        // prevent user from making any manual changes to DisplayIndex                    
                        colDisplayIndex.ReadOnly = true;
                        colDisplayIndex.ToolTipText = "Click to Sort (Read Only)";
                        dgvColumns.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                        dgvColumns.ClearSelection();
                    }

                    // reset drag/drop variables
                    dragBoxFromMouseDown = new Rectangle();
                    rowIndexFromMouseDown = -1;
                    colIndexFromMouseDown = -1;
                    rowIndexOfItemUnderMouseToDrop = -1;
                    colIndexOfItemUnderMouseToDrop = -1;
                }
            }
            catch (Exception ex)
            {
                // DO NOTHING
                Debug.WriteLine("dgvColumns_DragDrop Exception: " + ex.Message);
            }
        }


        #endregion


    }
}