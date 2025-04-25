using CustomControls;
using CustomsForgeSongManager.DataObjects;
using GenTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
            if (!AppSettings.Instance.ValidateD3D || Constants.OnMac)
            {
                if (Constants.OnMac)
                    Globals.Log("<MAC MODE> 'Validate D3DX9_42.dll' checkbox is not applicable ...");
                else
                    Globals.Log("<WARNING> 'Validate D3DX9_42.dll' checkbox is disabled ...");

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
                    Globals.Log("<WARNING> User aborted installing 'D3DX9_42.dll' file ...");
                    return false;
                }

                // working directly with the file rather than an embedded resource 
                if (File.Exists(luaPath) || File.Exists(steamClientPath))
                {
                    //GenExtensions.CopyFile(Path.Combine(Constants.ApplicationFolder, "D3DX9_42.dll.old"), Path.Combine(AppSettings.Instance.RSInstalledDir, "D3DX9_42.dll"), true, false);
                    //Globals.Log("Installed 'D3DX9_42.dll' file for Rocksmith 2014 ...");
                    Globals.Log("<WARNING> Legacy 'D3DX9_42.dll' file installation for Rocksmith 2014 is not supported ...");
                }
                else
                {
                    GenExtensions.CopyFile(Path.Combine(Constants.ApplicationFolder, "D3DX9_42.dll.new"), Path.Combine(AppSettings.Instance.RSInstalledDir, "D3DX9_42.dll"), true, false);
                    Globals.Log("Installed 'D3DX9_42.dll' file for Rocksmith 2014 Remastered ...");
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
                        Globals.Log("<WARNING> User aborted updating the 'D3DX9_42.dll' file ...");
                        return false;
                    }

                    if (File.Exists(luaPath) || File.Exists(steamClientPath))
                    {
                        //GenExtensions.CopyFile(Path.Combine(Constants.ApplicationFolder, "D3DX9_42.dll.old"), Path.Combine(AppSettings.Instance.RSInstalledDir, "D3DX9_42.dll"), true, false);
                        //Globals.Log("Updated 'D3DX9_42.dll' file for Rocksmith 2014 ...");
                        Globals.Log("<WARNING> Legacy 'D3DX9_42.dll' file updating for Rocksmith 2014 is not supported ...");
                    }
                    else
                    {
                        GenExtensions.CopyFile(Path.Combine(Constants.ApplicationFolder, "D3DX9_42.dll.new"), Path.Combine(AppSettings.Instance.RSInstalledDir, "D3DX9_42.dll"), true, false);
                        Globals.Log("Updated 'D3DX9_42.dll' file for Rocksmith 2014 Remastered ...");
                    }
                }
                else
                    Globals.Log("Validated existing 'D3DX9_42.dll' file installation ...");
            }

            return true;
        }
    }
}
