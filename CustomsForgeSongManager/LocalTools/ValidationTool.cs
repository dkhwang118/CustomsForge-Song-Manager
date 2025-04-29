using CustomControls;
using CustomsForgeSongManager.DataManager;
using CustomsForgeSongManager.DataObjects;
using DLogNet;
using GenTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomsForgeSongManager.LocalTools
{
    /// <summary>
    /// ValidationTool is a static class that provides methods for validating data from the UI 
    /// prior to executing the processes selected by the user.
    /// </summary>
    public static class ValidationTool
    {
        /// <summary>
        /// Validates the given Rocksmith installation directory.
        /// </summary>
        /// <param name="installDir">The directory to validate</param>
        /// <returns>True if the given directory is a Rocksmith installation directory.</returns>
        public static bool ValidateRocksmithInstallDirectory(string installDir)
        {
            // If the given directory exists
            if (Directory.Exists(installDir))
            {
                // TODO: Validate that this is indeed the RS2014 installation directory

                // If the selected directory does contain a "dlc" subdirectory
                if (Directory.Exists(Path.Combine(installDir, "dlc")))
                {
                    // We consider this a valid install directory
                    // Set the selected path as the Rocksmith installation directory
                    DirectoryManager.SetRocksmithInstallationDirectory(installDir);
                    return true;
                }
            }
            // If we get here, the directory is invalid
            return false;
        }

        /// <summary>
        /// Validates the D3DX9_42.dll file.
        /// </summary>
        /// <returns></returns>
        public static bool ValidateD3D()
        {
            if (!AppSettings.Instance.ValidateD3D || DirectoryManager.OnMac)
            {
                if (DirectoryManager.OnMac)
                    SMLog.Log("<MAC MODE> 'Validate D3DX9_42.dll' checkbox is not applicable ...");
                else
                    SMLog.Log("<WARNING> 'Validate D3DX9_42.dll' checkbox is disabled ...");

                return false;
            }

            // validate remastered version of D3DX9_42.dll
            // discountinued support for legacy version of D3DX9_42.dll (commented out)
            var luaPath = Path.Combine(DirectoryManager.RSInstalledDir, "lua5.1.dll");
            var steamClientPath = Path.Combine(DirectoryManager.RSInstalledDir, "Steamclient.dll");
            var d3dPath = Path.Combine(DirectoryManager.RSInstalledDir, "D3DX9_42.dll");

            if (!File.Exists(d3dPath))
            {
                var diaMsg = "The 'D3DX9_42.dll' file could not be found. Would you like CFSM to install the dll file that is required to play CDLC files?";
                if (DialogResult.No == BetterDialog2.ShowDialog(GenExtensions.SplitString(diaMsg, 30),
                    "Validating D3DX9_42.dll ...", null, "Yes", "No", Bitmap.FromHicon(SystemIcons.Warning.Handle), "Warning", 0, 150))
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
                    GenExtensions.CopyFile(Path.Combine(DirectoryManager.ApplicationFolder, "D3DX9_42.dll.new"), 
                        Path.Combine(DirectoryManager.RSInstalledDir, "D3DX9_42.dll"), true, false);
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
                var d3dNewMD5 = GenExtensions.GetMD5Hash(Path.Combine(DirectoryManager.ApplicationFolder, "D3DX9_42.dll.new"));
                var d3dOldMD5 = GenExtensions.GetMD5Hash(Path.Combine(DirectoryManager.ApplicationFolder, "D3DX9_42.dll.old"));
                var d3dBasicModsMD5 = GenExtensions.GetMD5Hash(Path.Combine(DirectoryManager.ApplicationFolder, "D3DX9_42.dll.basic_mods")); //for the basic (custom song lists only) version of the modded DLL
                //var d3dExtraModsMD5 = GenExtensions.GetMD5Hash(Constants.ApplicationFolder, "D3X9_42.dll.extra_mods"); //for a future modded DLL

                if (((File.Exists(luaPath) || File.Exists(steamClientPath)) && d3dFileMD5 != d3dOldMD5) || ((!File.Exists(luaPath) && !File.Exists(steamClientPath)) && d3dFileMD5 != d3dNewMD5 && d3dFileMD5 != d3dBasicModsMD5))
                {
                    var dlgMsg1 = "The installed 'D3DX9_42.dll' file MD5 hash value is invalid. Would you like CFSM to update the dll file that is required to play CDLC files?";
                    var dlgMsg2 = "Note: If your CDLC are working fine then answer 'No' and then disable future validation checks in the 'Settings' tab menu.";
                    var dlgMsg = GenExtensions.SplitString(dlgMsg1, 30) + Environment.NewLine + Environment.NewLine 
                        + GenExtensions.SplitString(dlgMsg2, 30);

                    if (DialogResult.No == BetterDialog2.ShowDialog(dlgMsg, "Validating D3DX9_42.dll ...", null, "Yes", "No", 
                        Bitmap.FromHicon(SystemIcons.Warning.Handle), "Warning", 0, 150))
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
                        GenExtensions.CopyFile(Path.Combine(DirectoryManager.ApplicationFolder, "D3DX9_42.dll.new"), 
                            Path.Combine(DirectoryManager.RSInstalledDir, "D3DX9_42.dll"), true, false);
                        SMLog.Log("Updated 'D3DX9_42.dll' file for Rocksmith 2014 Remastered ...");
                    }
                }
                else
                    SMLog.Log("Validated existing 'D3DX9_42.dll' file installation ...");
            }

            return true;
        }

        #region ValidateDisplaySettings
        // Code copied from RocksmithToolkitLib.Extensions.GeneralExtension class


        /// <summary>
        /// Validates the display settings of the given form and control.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="control"></param>
        /// <param name="forceAdjustment"></param>
        /// <param name="verbose"></param>
        /// <returns></returns>
        public static bool ValidateDisplaySettings(Form form, Control control, bool forceAdjustment = false, bool verbose = true)
        {
            float displayDpi = GetDisplayDpi(control);
            float displayScalingFactor = GetDisplayScalingFactor(control);
            if (displayDpi != 96f || (double)displayScalingFactor != 1.0 || forceAdjustment)
            {
                if (verbose)
                {
                    MessageBox.Show(" - System Display DPI Setting (" + displayDpi + ")" + Environment.NewLine + " - System Display Screen Scale Factor (" + displayScalingFactor * 100f + "%)" + Environment.NewLine + " - Adjusted AutoScaleDimensions, AutoScaleMode, and AutoSize" + Environment.NewLine + Environment.NewLine + "If application does not display correctly then change system setting to:  " + Environment.NewLine + "Control Panel>Appearance and Personalization>Display>Smaller - 100%  ", "Validate Display Settings ...", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                }

                form.SuspendLayout();
                form.AutoScaleDimensions = new SizeF(6f, 13f);
                form.AutoScaleMode = AutoScaleMode.Font;
                control.AutoSize = true;
                form.ResumeLayout();
                return false;
            }

            return true;
        }

        public static float GetDisplayDpi(Control control)
        {
            return control.CreateGraphics().DpiX;
        }

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        public static float GetDisplayScalingFactor(Control control)
        {
            float displayDpi = GetDisplayDpi(control);
            if (displayDpi > 96f)
            {
                return displayDpi / 96f;
            }

            Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr hdc = graphics.GetHdc();
            int deviceCaps = GetDeviceCaps(hdc, 10);
            int deviceCaps2 = GetDeviceCaps(hdc, 117);
            return (float)deviceCaps2 / (float)deviceCaps;
        }

        #endregion ValidateDisplaySettings

        /// <summary>
        /// Validates the given directory as a Rocksmith installation directory,
        /// OR prompts the user until a valid directory is selected.
        /// </summary>
        /// <param name="installDir">The directory to be validated.</param>
        /// <returns>True if the directory is a valid Rocksmith installation directory.</returns>
        public static void ForceValidateRocksmithInstallDirectory(string installDir)
        {
            // If the directory is not set or does not exist, prompt the user to select it
            if (String.IsNullOrEmpty(installDir) || !Directory.Exists(installDir) || !Directory.Exists(Path.Combine(installDir, "dlc")))
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
                            if (!Directory.Exists(Path.Combine(fbd.SelectedPath, "dlc")))
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

            SMLog.Log("Validated RS2014 Installation Directory: " + installDir);
        }

    

        public static void ValidateSongManagerFolders()
        {
            try
            {
                createWorkFolders();

                validateWriteAccessToRocksmithInstallDirectory();

                // Copy the duplicates folder from the RSinstallDir to the CFSM Duplicates folder in My Documents
                GenExtensions.CopyDir(Path.Combine(DirectoryManager.RSInstalledDir, "duplicates"), DirectoryManager.DuplicatesFolder);
                
                cleanUpCfsmTempFolders();
            }
            catch (Exception ex)
            {
                // We'll let this slide for now... but we should never just throw an exception
                // that would "force app to stop here" as this is not a good practice and looks like a random crash to the user. 
                SMLog.Log("<ERROR> Could not verify CFSM work folders ...");
                SMLog.Log(ex.Message);
                throw new Exception(); // force app to stop here
            }
        }

        private static void cleanUpCfsmTempFolders()
        {
            try
            {

                // Delete CFSM temp folders if they exist
                GenExtensions.DeleteDirectory(Path.Combine(DirectoryManager.RSInstalledDir, "cdlc_quarantined"));
                GenExtensions.DeleteDirectory(Path.Combine(DirectoryManager.RSInstalledDir, "cdlc_duplicates"));
                GenExtensions.DeleteDirectory(Path.Combine(DirectoryManager.RSInstalledDir, "duplicates"));
            }
            catch (Exception ex)
            {
                SMLog.Log("<ERROR> Could not clean-up CFSM temp folders ... ex.Message: " + ex.Message);
            }
        }

        private static void createWorkFolders()
        {
            try
            {
                // use 'My Documents/CFSM' to avoid future OS Permission and AV issues
                // validate/create CFSM subfolders            
                GenExtensions.MakeDir(DirectoryManager.TempWorkFolder);
                GenExtensions.MakeDir(DirectoryManager.BackupsFolder);
                GenExtensions.MakeDir(DirectoryManager.DuplicatesFolder);
                GenExtensions.MakeDir(DirectoryManager.RemasteredArcFolder);
                GenExtensions.MakeDir(DirectoryManager.RemasteredOrgFolder);
                GenExtensions.MakeDir(DirectoryManager.RemasteredMaxFolder);
                GenExtensions.MakeDir(DirectoryManager.RemasteredCorFolder);
                GenExtensions.MakeDir(DirectoryManager.QuarantineFolder);
                GenExtensions.MakeDir(DirectoryManager.SongPacksFolder);
            }
            catch (Exception ex)
            {
                SMLog.Log("<ERROR> Could not create CFSM work folders ... ex.Message: " + ex.Message);
            }
        }

        private static bool validateWriteAccessToRocksmithInstallDirectory()
        {
            bool hasWriteAccess = false;
            try
            {
                // make sure we have write access to Rocksmith2014 folders
                if (Directory.Exists(DirectoryManager.RSInstalledDir))
                {
                    // make sure we have write access to the RSInstallDir
                    if (!ZipUtilities.EnsureWritableDirectory(DirectoryManager.RSInstalledDir))
                        ZipUtilities.RemoveReadOnlyAttribute(DirectoryManager.RSInstalledDir);

                    // make sure we have write access to all files in 'dlc' folder
                    ZipUtilities.RemoveReadOnlyAttribute(DirectoryManager.Rs2DlcFolder);

                    hasWriteAccess = true;
                }
            }
            catch (Exception ex)
            {
                SMLog.Log("<ERROR> We do not have write access to the Rocksmith Installation directory! ex.Message: " + ex.Message);
            }
            return hasWriteAccess;
        }
    }
}
