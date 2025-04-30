using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CustomsForgeSongManager.DataObjects;
using GenTools;
using RocksmithToolkitLib.DLCPackage;
using RocksmithToolkitLib.Extensions;
using System.Drawing;
using CustomsForgeSongManager.Forms;
using DataGridViewTools;
using CustomsForgeSongManager.UControls;
using RocksmithToolkitLib.PSARC;
using System.Xml;
using System.Diagnostics;
using DLogNet;
using CustomsForgeSongManager.DataManager;

namespace CustomsForgeSongManager.LocalTools
{
    public static class FileTools
    {
        public static void ArchiveFiles(string srcExt, string srcFolder, bool srcDelete = false)
        {
            SMLog.Log("Archiving  [" + srcExt + "] files ...");

            var srcFilePaths = Directory.EnumerateFiles(srcFolder, "*" + srcExt + "*").ToList();
            if (!srcFilePaths.Any())
            {
                SMLog.Log("No files to archive: " + srcFolder);
                return;
            }

            var fileName = String.Format("{0}{1}.zip", DateTime.Now.ToString("yyyyMMddTHHmmss"), srcExt).GetValidFileName();
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = ".zip files (*.zip)|*.zip";
                sfd.FilterIndex = 0;
                sfd.InitialDirectory = Constants.RemasteredArcFolder;
                sfd.FileName = fileName;

                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                fileName = sfd.FileName;
            }

            // save zip file to 'remastered' folder so that it is not accidently deleted
            try
            {
                if (ZipUtilities.ZipDirectory(srcFolder, Path.Combine(Constants.RemasteredArcFolder, fileName)))
                    SMLog.Log("Archive saved to: " + Path.Combine(Constants.RemasteredArcFolder, fileName));
                else
                    throw new IOException();

                if (srcDelete)
                {
                    GenExtensions.DeleteDirectory(srcFolder);
                    GenExtensions.MakeDir(srcFolder);
                }
            }
            catch (IOException ex)
            {
                SMLog.Log("<ERROR> Archiving failed ...");
                SMLog.Log(ex.Message);
            }
        }

        public static void ArchiveFilesWorker(object sender)
        {
            // run new generic worker
            using (var gWorker = new GenericWorker())
            {
                gWorker.WorkDescription = Constants.GWORKER_ACHRIVE;
                gWorker.BackgroundProcess(sender);
                while (Globals.WorkerFinished == Globals.Tristate.False)
                    Application.DoEvents();
            }
        }

        public static void ArtistFolders(string dlcDir, List<SongData> selectedSongs, bool isUndo)
        {
            // check for duplicates that will cause auto file renaming problems
            // intentionally less restrictive than the Duplicates tabmenu check
            var dups = selectedSongs.GroupBy(x => new { Song = x.Title, x.Artist, x.PackageVersion }).Where(group => group.Count() > 1).SelectMany(group => group).ToList();
            if (dups.Any())
            {
                var diaMsg = "Can not organize song collection quite yet ..." + Environment.NewLine +
                             "Please resolve duplicate song conflicts" + Environment.NewLine +
                             "using the Duplicates tabmenu ..." + Environment.NewLine + Environment.NewLine +
                             "HINT: For a quick workaround, enter unique package" + Environment.NewLine +
                             "version numbers directly into the Duplicates grid," + Environment.NewLine +
                             "or use the Duplicates linkbutton 'Select All Older" + Environment.NewLine +
                             "By ToolkitVersion' to put songs into the correct" + Environment.NewLine +
                             "order to be moved or deleted ..." + Environment.NewLine + Environment.NewLine +
                             "NOTE: if the game runs fine with your current song set and the Duplicates tab doesn't show any entries " +
                             Environment.NewLine +
                             "go through the song list and check if you have multiple versions of the same song that are marked with same PackageVersion"
                             + Environment.NewLine + "and change those! (Right click->Edit Song Information)";

                BetterDialog2.ShowDialog(diaMsg, "Organize Songs ...", null, null, "Ok", Bitmap.FromHicon(SystemIcons.Warning.Handle), "Warning", 0, 150);
                //Globals.CancelBackgroundScan = true; TODO: determine whether this should actually be disabled - if it's not, it messes up with Duplicates, resulting in those not showing while they are actually present
                return;
            }

            if (isUndo)
            {
                SMLog.Log("Restoring CDLC files to 'dlc/cdlc' folder ...");
                if (!Directory.Exists(Constants.Rs2CdlcFolder))
                    Directory.CreateDirectory(Constants.Rs2CdlcFolder);
            }
            else
                SMLog.Log("Organizing CDLC into artist name folders ...");

            var total = selectedSongs.Count();
            int processed = 0, failed = 0, skipped = 0;
            GenericWorker.InitReportProgress();

            foreach (var songInfo in selectedSongs)
            {
                var srcFilePath = songInfo.FilePath;
                SMLog.Log(" - Processing: " + Path.GetFileName(srcFilePath));
                processed++;
                GenericWorker.ReportProgress(processed, total, skipped, failed);

                string destFilePath;
                if (isUndo)
                {
                    if (songInfo.PackageAuthor == "Ubisoft")
                        destFilePath = Path.Combine(Constants.Rs2DlcFolder, Path.GetFileName(srcFilePath));
                    else
                        destFilePath = Path.Combine(Constants.Rs2CdlcFolder, Path.GetFileName(srcFilePath));
                }
                else
                {
                    var version = songInfo.PackageVersion;

                    // workaround for old toolkit behavior
                    if (String.IsNullOrEmpty(version) || version == "Null")
                        version = "1";

                    // workaround to identify ODLC
                    if (songInfo.PackageAuthor == "Ubisoft")
                        version = "0";

                    var artistName = songInfo.Artist;
                    var titleName = songInfo.Title;
                    // validate file and directory names
                    var destFileName = GenExtensions.MakeValidFileName(String.Format("{0}_{1}_v{2}{3}", artistName, titleName, version, Constants.EnabledExtension));
                    var destDir = GenExtensions.MakeValidDirName(Path.Combine(dlcDir, artistName));
                    destFilePath = Path.Combine(destDir, destFileName);

                    // create new artist name folder for song files
                    if (!Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);
                }

                try
                {
                    // no point moving what is already in the correct folder
                    if (srcFilePath != destFilePath)
                    {
                        // update Global SongCollection
                        var song = Globals.MasterCollection.FirstOrDefault(s => s.FilePath == srcFilePath);
                        int index = Globals.MasterCollection.IndexOf(song);
                        Globals.MasterCollection[index].FilePath = destFilePath;
                        GenExtensions.MoveFile(srcFilePath, destFilePath, false, true);
                    }
                    else
                        skipped++;
                }
                catch (Exception ex)
                {
                    if (isUndo)
                        SMLog.Log("<ERROR> Failed to restore CDLC: " + srcFilePath);
                    else
                        SMLog.Log("<ERROR> Failed to organized CDLC: " + srcFilePath);

                    SMLog.Log(" - " + ex.Message);
                    failed++;
                }
            }

            GenericWorker.ReportProgress(processed, total, skipped, failed);

            if (processed > 0)
            {
                // remove empty directories from inside the 'dlc' folder
                new DirectoryInfo(dlcDir).DeleteEmptyDirs();

                if (isUndo)
                    SMLog.Log("Sucessully restored CDLC files to 'dlc/cdlc' folder and removed empty artist name folders ...");
                else
                    SMLog.Log("Sucessully organized and renamed CDLC into artist name folders ...");
            }
        }

        public static void CleanDlcFolder()
        {
            // remove any .bak, .org, .max and .cor files from dlc folder and subfolders
            SMLog.Log("Cleaning 'dlc' folder and subfolders ...");
            string[] extensions = { Constants.EXT_BAK, Constants.EXT_COR, Constants.EXT_DUP, Constants.EXT_MAX, Constants.EXT_ORG };
            var extFilePaths = Directory.EnumerateFiles(Constants.Rs2DlcFolder, "*.*", SearchOption.AllDirectories).Where(fi => extensions.Any(fi.ToLower().Contains)).ToList();

            var total = extFilePaths.Count;
            int processed = 0, failed = 0, skipped = 0;
            GenericWorker.InitReportProgress();

            foreach (var extFilePath in extFilePaths)
            {
                processed++;
                GenericWorker.ReportProgress(processed, total, skipped, failed);

                var destFilePath = extFilePath;
                if (extFilePath.Contains(Constants.EXT_BAK))
                    destFilePath = Path.Combine(Constants.BackupsFolder, Path.GetFileName(extFilePath));
                else if (extFilePath.Contains(Constants.EXT_COR))
                    destFilePath = Path.Combine(Constants.RemasteredCorFolder, Path.GetFileName(extFilePath));
                else if (extFilePath.Contains(Constants.EXT_DUP))
                    destFilePath = Path.Combine(Constants.DuplicatesFolder, Path.GetFileName(extFilePath));
                else if (extFilePath.Contains(Constants.EXT_MAX))
                    destFilePath = Path.Combine(Constants.RemasteredMaxFolder, Path.GetFileName(extFilePath));
                else if (extFilePath.Contains(Constants.EXT_ORG))
                    destFilePath = Path.Combine(Constants.RemasteredOrgFolder, Path.GetFileName(extFilePath));
                else
                {
                    // JIC ... this should never happen
                    SMLog.Log("<WARNING> Unexpected file type: " + extFilePath);
                    skipped++;
                    continue;
                }

                try
                {
                    // TODO: confirm this action
                    if (!File.Exists(destFilePath))
                    {
                        GenExtensions.CopyFile(extFilePath, destFilePath, true, false);
                        SMLog.Log("Moved file to: " + destFilePath);
                    }
                    else
                    {
                        SMLog.Log("Deleted duplicate file: " + extFilePath);
                        skipped++;
                    }

                    // this could throw an error if file is "Read-Only" or does not exist
                    GenExtensions.DeleteFile(extFilePath);
                }
                catch (IOException ex)
                {
                    SMLog.Log("<ERROR> Move File Failed: " + extFilePath + " ...");
                    SMLog.Log(ex.Message);
                    failed++;
                }
            }

            // Commented out ... so devs don't hear, "I deleted all my cdlc files" 
            // Remove originals from Remastered_backup/orignals folder
            //DirectoryInfo backupDir = new DirectoryInfo(Constants.RemasteredCLI_OrgCDLCFolder);
            //backupDir.CleanDir();

            GenericWorker.ReportProgress(processed, total, skipped, failed);

            if (processed > 0)
            {
                Globals.RescanSongManager = true;
                SMLog.Log("Finished cleaning 'dlc' folder and subfolders ...");
            }
            else
                SMLog.Log("The 'dlc' folder and subfolders didn't need cleaning ...");
        }

        public static bool CreateBackupOfType(string srcFilePath, string destFolder, string backupExt)
        {
            try
            {
                var properExt = Path.GetExtension(srcFilePath);
                var destFilePath = String.Format(@"{0}{1}{2}", Path.Combine(destFolder, Path.GetFileNameWithoutExtension(srcFilePath)), backupExt, properExt).Trim();

                if (srcFilePath.Contains(Constants.RS1COMP))
                {
                    SMLog.Log(" - Can not backup individual RS1 Compatiblity DLC ...");
                    return false;
                }

                if (!File.Exists(destFilePath))
                {
                    GenExtensions.CopyFile(srcFilePath, destFilePath, false);
                    SMLog.Log(" - Successfully created backup ..."); // a good thing
                }
                else
                    SMLog.Log(" - Backup already exists ..."); // also a good thing
            }
            catch (Exception ex)
            {
                // it is critical that backup of originals was successful before proceeding
                SMLog.Log(" - <ERROR> Backup failed ..."); // a bad thing
                SMLog.Log(ex.Message);
                return false;
            }

            return true;
        }

        public static bool CreateBackupOfType(List<SongData> songs, string destFolder, string backupExt, bool isBackup = true)
        {
            if (isBackup)
                SMLog.Log("Backing up selected CDLC files ...");
            else
                SMLog.Log("Moving selected CDLC files ...");

            VerifyCfsmFolders();
            var srcFilePaths = SongFilePaths(songs);
            var total = srcFilePaths.Count;
            int processed = 0, failed = 0, skipped = 0;
            GenericWorker.InitReportProgress();

            foreach (var srcFilePath in srcFilePaths)
            {
                SMLog.Log("Processing File: " + Path.GetFileName(srcFilePath));
                processed++;
                GenericWorker.ReportProgress(processed, total, skipped, failed);

                try
                {
                    var properExt = Path.GetExtension(srcFilePath);
                    var destFilePath = String.Format(@"{0}{1}{2}", Path.Combine(destFolder, Path.GetFileNameWithoutExtension(srcFilePath)), backupExt, properExt).Trim();

                    if (srcFilePath.Contains(Constants.RS1COMP))
                    {
                        SMLog.Log(" - Can not process individual RS1 Compatiblity DLC");
                        ++skipped;
                    }
                    else if (!File.Exists(destFilePath))
                    {
                        GenExtensions.CopyFile(srcFilePath, destFilePath, false);
                        SMLog.Log(" - Successfully processed file"); // a good thing
                    }
                    else
                    {
                        SMLog.Log(" - File already exists"); // also a good thing
                        skipped++;
                    }
                }
                catch (Exception ex)
                {
                    // it is critical that backup of originals was successful before proceeding
                    SMLog.Log(" - <ERROR> CreateBackupOfType method failed ..."); // a bad thing
                    SMLog.Log(ex.Message);
                    failed++;
                }

                if (failed > 0)
                    return false;
            }

            GenericWorker.ReportProgress(processed, total, skipped, failed);

            if (processed > 0)
            {
                if (isBackup)
                    SMLog.Log("Finished backing up selected files ...");
                else
                    SMLog.Log("Finished moving selected files ...");

                SMLog.Log("Files saved to: " + destFolder);
                return true;
            }

            SMLog.Log("No files processed ...");
            return false;
        }

        public static void DeleteFiles(List<SongData> songs, bool verbose = true)
        {
            if (verbose)
                SMLog.Log("Deleting selected CDLC files ...");

            var srcFilePaths = SongFilePaths(songs);
            var total = srcFilePaths.Count;
            int processed = 0, failed = 0, skipped = 0;
            GenericWorker.InitReportProgress();

            foreach (var srcFilePath in srcFilePaths)
            {
                if (verbose)
                    SMLog.Log("Processing File: " + Path.GetFileName(srcFilePath));

                processed++;
                GenericWorker.ReportProgress(processed, total, skipped, failed);

                try
                {
                    GenExtensions.DeleteFile(srcFilePath);

                    if (verbose)
                        SMLog.Log(" - Successfully deleted file"); // a good thing
                }
                catch (IOException ex)
                {
                    SMLog.Log(" - <ERROR> Deletion failed ..."); // a bad thing
                    SMLog.Log(ex.Message);
                    failed++;
                }
            }

            GenericWorker.ReportProgress(processed, total, skipped, failed);

            if (verbose)
            {
                if (processed > 0)
                    SMLog.Log("Finished deleting files ...");
                else
                    SMLog.Log("No files deleted ...");
            }
        }

        public static string RestoreOriginal(string srcFilePath)
        {
            var srcFileName = Path.GetFileName(srcFilePath).Replace(Constants.EXT_ORG, "");
            var destFilePath = Path.Combine(Constants.Rs2DlcFolder, srcFileName);
            try
            {
                // make sure [.org] file gets put back into the correct 'dlc' subfolder
                // if CDLC is not found then [.org] file is renamed and put into default 'dlc' folder
                var remasteredFilePath = Globals.MasterCollection.FirstOrDefault(s => s.FilePath.Contains(srcFileName)).FilePath;
                if (remasteredFilePath.Any())
                    destFilePath = Path.Combine(Path.GetDirectoryName(remasteredFilePath), srcFileName);

                // copy but don't delete [.org]
                if (GenExtensions.CopyFile(srcFilePath, destFilePath, true, false))
                    SMLog.Log(" - Successfully restored file: " + Path.GetFileName(srcFilePath));
                else
                {
                    SMLog.Log(" - <ERROR> Could not restore file: " + Path.GetFileName(srcFilePath));
                    destFilePath = String.Empty;
                }

                return destFilePath;
            }
            catch (Exception ex)
            {
                // this should never happen but just in case
                SMLog.Log(" - <ERROR> Restore [" + Constants.EXT_ORG + "] failed ...");
                SMLog.Log(ex.Message);
                return String.Empty;
            }
        }

        public static bool IsDirectory(string path)
        {
            bool isDirectory = false;

            try
            {
                FileAttributes attr = File.GetAttributes(path);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    isDirectory = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Invalid directory." + Environment.NewLine + ex.Message);
            }

            return isDirectory;
        }

        public static string IsOfficialRepairedDisabled(string filePath)
        {
            if (filePath.ToLower().Contains("disable"))
                return "Disabled";

            ToolkitInfo entryTkInfo;
            using (var browser = new PsarcLoader(filePath, true))
                entryTkInfo = browser.ExtractToolkitInfo();

            if (entryTkInfo == null)
                return "Official";

            string msg = "";
            if (entryTkInfo != null)
            {
                if (entryTkInfo.PackageAuthor?.Equals("Ubisoft") == true)
                    return "Official";

                if (entryTkInfo.PackageComment?.Contains("Remastered") == true)
                    msg += "Remastered";

                if (entryTkInfo.ToolkitVersion?.Contains("DLC Builder") == true)
                    msg += "DLC Builder";

                return msg;
            }

            return null;
        }

        public static void RestoreBackups(string backupExt, string backupFolder)
        {
            SMLog.Log("Restoring [" + backupExt + "] CDLC ...");
            // get contents of backup folder
            var bakFilePaths = Directory.EnumerateFiles(backupFolder, "*" + backupExt + "*").ToList();
            // get contents of 'dlc' folder for comparison
            var dlcFilePaths = Directory.EnumerateFiles(Constants.Rs2DlcFolder, "*.psarc", SearchOption.AllDirectories)
                .Where(fi => !fi.ToLower().Contains(Constants.RS1COMP) && // ignore compatibility packs
                             !fi.ToLower().Contains(Constants.SONGPACK) && // ignore songpacks
                             !fi.ToLower().Contains(Constants.ABVSONGPACK) && // ignore _sp_
                             !fi.ToLower().Contains("inlay")) // ignore inlays
                .ToList();

            var dlcFilePath = String.Empty;
            var total = bakFilePaths.Count;
            int processed = 0, failed = 0, skipped = 0;
            GenericWorker.InitReportProgress();

            foreach (var bakFilePath in bakFilePaths)
            {
                processed++;
                GenericWorker.ReportProgress(processed, total, skipped, failed);

                try
                {
                    var dlcFileName = Path.GetFileName(bakFilePath).Replace(backupExt, "");
                    dlcFilePath = Path.Combine(Constants.Rs2DlcFolder, dlcFileName);

                    // make sure bakExt file gets put back into the correct 'dlc' subfolder
                    // if CDLC is not found then bakExt file is put into default 'dlc' folder
                    var remasteredFilePath = dlcFilePaths.FirstOrDefault(x => x.Contains(dlcFileName));
                    if (remasteredFilePath != null)
                        dlcFilePath = Path.Combine(Path.GetDirectoryName(remasteredFilePath), dlcFileName);

                    // copy but don't delete bakExt
                    GenExtensions.CopyFile(bakFilePath, dlcFilePath, true, false);
                    SMLog.Log("Successfully Restored: " + Path.GetFileName(dlcFilePath));
                }
                catch (IOException ex)
                {
                    SMLog.Log(ex.Message);
                    SMLog.Log("<ERROR> Could Not Restore: " + Path.GetFileName(dlcFilePath));
                    failed++;
                }
            }

            GenericWorker.ReportProgress(processed, total, skipped, failed);

            if (processed > 0)
            {
                SMLog.Log("CDLC backups with extension [" + backupExt + "] were restored to original location in 'dlc' folder ...");
                Globals.RescanSongManager = true;
            }
            else
                SMLog.Log("No CDLC were restored from: " + backupFolder);
        }

        public static List<string> SongFilePaths(List<SongData> songs)
        {

            var srcFilePaths = new List<string>();
            songs = songs.Where(s => !s.FilePath.ToLower().Contains(Constants.RS1COMP) &&
                                     !s.FilePath.ToLower().Contains(Constants.SONGPACK) &&
                                     !s.FilePath.ToLower().Contains(Constants.ABVSONGPACK) &&
                                     !s.FilePath.ToLower().Contains("inlay"))
                .ToList();

            songs.ForEach(s => srcFilePaths.Add(s.FilePath));

            return srcFilePaths;
        }

        public static void SetDLDestinationFolder()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = Constants.Rs2DlcFolder;
                fbd.Description = "Select the folder where new CDLC downloads will be moved to.";
                if (fbd.ShowDialog() != DialogResult.OK)
                    return;

                AppSettings.Instance.DLMonitorDesinationFolder = fbd.SelectedPath;
                SMLog.Log("- Downloads destination folder set to: " + fbd.SelectedPath);
                Globals.Settings.SaveSettingsToFile(Globals.DgvCurrent);
            }
        }

        /// <summary>
        /// Method that checks whether the user has any monitored folders set, and if not, asks the user to set them with a form.
        /// </summary>
        /// <returns>Always true.</returns>
        public static bool ValidateDownloadsDirs()
        {
            var dlDirectories = AppSettings.Instance.MonitoredFolders;

            if (dlDirectories.Count() == 0)
            {
                frmMonitoredFolders frmMonitoredFolders = new frmMonitoredFolders();
                frmMonitoredFolders.ShowDialog();
            }

            return true;
        }

        public static void VerifyCfsmFolders()
        {
            try
            {
                // use 'My Documents/CFSM' to avoid future OS Permission and AV issues
                // validate/create CFSM subfolders            
                GenExtensions.MakeDir(Constants.TempWorkFolder);
                GenExtensions.MakeDir(Constants.BackupsFolder);
                GenExtensions.MakeDir(Constants.DuplicatesFolder);
                GenExtensions.MakeDir(Constants.RemasteredArcFolder);
                GenExtensions.MakeDir(Constants.RemasteredOrgFolder);
                GenExtensions.MakeDir(Constants.RemasteredMaxFolder);
                GenExtensions.MakeDir(Constants.RemasteredCorFolder);
                GenExtensions.MakeDir(Constants.QuarantineFolder);
                GenExtensions.MakeDir(Constants.SongPacksFolder);

                // make sure we have write access to Rocksmith2014 folders
                var rsDir = AppSettings.Instance.RSInstalledDir;
                if (Directory.Exists(rsDir))
                {
                    // make sure we have write access to the RSInstallDir
                    if (!ZipUtilities.EnsureWritableDirectory(rsDir))
                        ZipUtilities.RemoveReadOnlyAttribute(rsDir);

                    // make sure we have write access to all files in 'dlc' folder
                    ZipUtilities.RemoveReadOnlyAttribute(Constants.Rs2DlcFolder);
                }

                // TODO: eventually this conditional check can be depricated
                // if old CFSM remenants exist then move them to 'My Documents/CFSM' 
                if (Directory.Exists(Constants.Rs2CfsmFolder))
                {
                    // leave these important orginal files in RS root (file attribute flags are unchanged)
                    GenExtensions.CopyDir(Path.Combine(Constants.Rs2CfsmFolder, "songpacks", "originals"), Constants.Rs2OriginalsFolder);
                    //             
                    GenExtensions.CopyDir(Path.Combine(Constants.Rs2CfsmFolder, "archives"), Constants.RemasteredArcFolder);
                    GenExtensions.CopyDir(Path.Combine(Constants.Rs2CfsmFolder, "backups"), Constants.BackupsFolder);
                    GenExtensions.CopyDir(Path.Combine(Constants.Rs2CfsmFolder, "duplicates"), Constants.DuplicatesFolder);
                    GenExtensions.CopyDir(Path.Combine(Constants.Rs2CfsmFolder, "remastered"), Constants.RemasteredFolder);
                    GenExtensions.CopyDir(Path.Combine(Constants.Rs2CfsmFolder, "songpacks"), Constants.SongPacksFolder, false);

                    // make sure we have write access to all files in 'cfsm' folder
                    ZipUtilities.RemoveReadOnlyAttribute(Constants.Rs2CfsmFolder);
                    GenExtensions.DeleteDirectory(Constants.Rs2CfsmFolder);
                }

                GenExtensions.CopyDir(Path.Combine(AppSettings.Instance.RSInstalledDir, "duplicates"), Constants.DuplicatesFolder);
                GenExtensions.DeleteDirectory(Path.Combine(AppSettings.Instance.RSInstalledDir, "cdlc_quarantined"));
                GenExtensions.DeleteDirectory(Path.Combine(AppSettings.Instance.RSInstalledDir, "cdlc_duplicates"));
                GenExtensions.DeleteDirectory(Path.Combine(AppSettings.Instance.RSInstalledDir, "duplicates"));
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

        public static void VerifyCfsmFiles()
        {
            // attempting to populate grid settings XML files on initial run
            // this method seems to be a dead end
            //Globals.DgvCurrent = new SongManager().dgvSongsMaster as DataGridView;
            //if (!File.Exists(Constants.GridSettingsPath))
            //    SerialExtensions.SaveToFile(Constants.GridSettingsPath, RAExtensions.ManagerGridSettings.ColumnOrder);
        }

        /// <summary>
        /// Get a list of all songs in a folder.
        /// </summary>
        /// <param name="dirPath">The full directory path.</param>
        /// <returns>The list of full paths for the songs in the given directory.</returns>
        public static List<string> GetSongListForAFolder(string dirPath)
        {
            var songListInFolder = Directory.EnumerateFiles(dirPath, "*.psarc", SearchOption.TopDirectoryOnly)
                .Where(fi => !fi.ToLower().Contains(Constants.RS1COMP) && // ignore compatibility packs
                             !fi.ToLower().Contains(Constants.SONGPACK) && // ignore songpacks
                             !fi.ToLower().Contains(Constants.ABVSONGPACK) && // ignore _sp_
                             !fi.ToLower().Contains("inlay")).ToList(); // ignore inlays

            SMLog.Log("Number of song .psarcs found in " + dirPath + " folder: " + songListInFolder.Count().ToString());

            return songListInFolder;
        }

        /// <summary>
        /// Gets a list of all songs file paths given the user's specifications.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="includeRS1Pack"></param>
        /// <param name="includeRS2014BaseSongs"></param>
        /// <param name="includeCustomPacks"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<string> FilesList(string filePath, bool includeRS1Pack = false, bool includeRS2014BaseSongs = false, bool includeCustomPacks = false)
        {
            if (String.IsNullOrEmpty(filePath))
                throw new Exception("<ERROR> No path provided for file scanning");

            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);

            var files = Directory.EnumerateFiles(filePath, "*" + Constants.EnabledExtension, SearchOption.AllDirectories).ToList();
            files.AddRange(Directory.EnumerateFiles(filePath, "*" + Constants.DisabledExtension, SearchOption.AllDirectories).ToList());

            // removes enabled/disabled RS1Packs
            if (!includeRS1Pack)
                files = files.Where(file => !file.ToLower().Contains(Constants.RS1COMP)).ToList();

            // removes enabled/disabled CustomPacks
            if (!includeCustomPacks)
                files = files.Where(file => !file.ToLower().Contains(Constants.SONGPACK) && !file.ToLower().Contains(Constants.ABVSONGPACK)).ToList();

            if (includeRS2014BaseSongs)
            {
                var baseSongs = Directory.EnumerateFiles(AppSettings.Instance.RSInstalledDir, Constants.BASESONGS, SearchOption.TopDirectoryOnly).ToList();
                baseSongs.AddRange(Directory.EnumerateFiles(AppSettings.Instance.RSInstalledDir, Constants.BASESONGSDISABLED, SearchOption.TopDirectoryOnly).ToList());

                // Check for duplicate enable/disabled files and remove disabled file
                //if (baseSongs.Count > 1)
                //{
                //    SMLog.Log("<WARNING> Invalid songs*.psarc file count ...");

                //    for (int i = 1; i < baseSongs.Count; i++)
                //    {
                //        var baseSong = Path.Combine(AppSettings.Instance.RSInstalledDir, baseSongs[i]);
                //        File.Delete(baseSong);
                //        baseSongs.RemoveAt(i);
                //        SMLog.Log("- Deleted file: " + baseSong + " ...");
                //    }
                //}

                files.AddRange(baseSongs);
            }

            // Check for duplicate enable/disabled files in same directory and move the disabled file to CFSM/Duplicates folder
            var dups = files.Select(fullPath => new { Name = Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fullPath))), FullPath = fullPath })
                .GroupBy(file => file.Name).Where(fileGroup => fileGroup.Count() > 1).ToList();

            foreach (var dup in dups)
            {
                foreach (var item in dup)
                {
                    var dupPath = item.FullPath;
                    // if (dupPath.Contains(Constants.DisabledExtension)) // does not detect songs.disabled.psarc
                    if (dupPath.Contains(".disabled."))
                    {
                        // File.Delete(dupPath); // a bit too harsh
                        var destFilePath = Path.Combine(Constants.DuplicatesFolder, Path.GetFileName(dupPath));
                        if (!GenExtensions.MoveFile(dupPath, destFilePath, true, true))
                            continue;

                        files.Remove(dupPath);
                        SMLog.Log("- Moved disabled duplicate file: " + dupPath);
                        SMLog.Log("- To: " + destFilePath);
                    }
                }
            }

            return files;
        }

        /// <summary>
        /// Saves the given song collection to the SongsInfo.xml file.
        /// </summary>
        public static void SaveSongCollectionToFile(List<SongData> songInfoToSave)
        {
            var dom = songInfoToSave.XmlSerializeToDom();
            XmlElement versionNode = dom.CreateElement("SongDataList");
            versionNode.SetAttribute("version", SongData.SongDataVersion);
            versionNode.SetAttribute("AppVersion", Constants.CustomVersion());
            dom.DocumentElement.AppendChild(versionNode);

            foreach (XmlElement songData in dom.GetElementsByTagName("ArrayOfSongData")[0].ChildNodes)
            {
                // reduce songInfo.xml file size by removing extraneous/null/empty elements
                var arrangementsNode = songData.GetElementsByTagName("Arrangements")[0];
                if (arrangementsNode != null)
                {
                    var arrNodes = arrangementsNode.ChildNodes.OfType<XmlNode>().ToList();

                    foreach (var arrNode in arrNodes)
                    {
                        var isVocals = arrNode.InnerXml.Contains("<Name>Vocals</Name>");
                        var innerNodes = arrNode.ChildNodes.OfType<XmlNode>().ToList();

                        foreach (var n in innerNodes)
                        {
                            // remove analyzer data from vocals
                            if (n.InnerText == "0" && isVocals)
                                arrNode.RemoveChild(n);

                            // remove null/empty data
                            if (String.IsNullOrEmpty(n.InnerText))
                                arrNode.RemoveChild(n);
                        }
                    }
                }
            }

            dom.Save(DirectoryManager.SongsInfoPath);
            SMLog.Log("Saved File: " + Path.GetFileName(DirectoryManager.SongsInfoPath));
        }

        /// <summary>
        /// Saves the application settings to the settings file.
        /// </summary>
        /// <param name="verbose"></param>
        public static void SaveApplicationSettingsToFile(AppSettings settingsToSave = null, bool verbose = false)
        {
            try
            {
                using (var fs = new FileStream(DirectoryManager.AppSettingsPath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    AppSettings.Instance.SerializeXml(fs);
                    if (verbose)
                        SMLog.Log("Saved File: " + Path.GetFileName(DirectoryManager.AppSettingsPath));
                }
            }
            catch (Exception ex)
            {
                SMLog.Log(String.Format("<Error> SaveSettingsToFile: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Loads the application settings from the settings file.
        /// </summary>
        /// <param name="verbose"></param>
        public static bool TryLoadApplicationSettingsFromFile(string settingsFilePath, out AppSettings settings,
                                                                bool verbose = false)
        {
            settings = null;
            try
            {
                // Try to read the settings from the file 
                if (AppSettings.TryGetSettingsFromFile(settingsFilePath, out settings))
                {
                    // If the settings were successfully loaded, set the settings instance
                    SettingsManager.SetApplicationSettings(settings);

                }
            }
            catch (Exception ex)
            {
                SMLog.Log(String.Format("<Error> LoadSettingsFromFile: {0}", ex.Message));
            }
            return false;
        }

        /// <summary>
        /// Save the given DGV settings for the given DGV object to a file.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="dgvToSave"></param>
        public static void SaveDataGridViewSettingsToFile(RADataGridViewSettings settings, DataGridView dgvToSave)
        {
            try
            {
                // If the directory for the grid settings folder does not exist, create it
                if (!Directory.Exists(DirectoryManager.GridSettingsFolder))
                {
                    Directory.CreateDirectory(DirectoryManager.GridSettingsFolder);
                }

                // Save the current DataGridView column order to the specified file
                string gridSettingsPath = DirectoryManager.GetGridSettingsPathForGridName(dgvToSave.Name);
                SerialExtensions.SaveToFile(gridSettingsPath, settings);
                SMLog.Log("Saved DataGridView Settings File: " + Path.GetFileName(gridSettingsPath));             
            }
            catch (Exception ex)
            {
                SMLog.Log(String.Format("<Error> SaveDataGridViewSettingsToFile: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Save the given DGV settings for the given DGV name to a file.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="dgvName">Name of the DataGridView</param>
        public static void SaveDataGridViewSettingsToFile(RADataGridViewSettings settings, string dgvName)
        {
            try
            {
                // If the directory for the grid settings folder does not exist, create it
                if (!Directory.Exists(DirectoryManager.GridSettingsFolder))
                {
                    Directory.CreateDirectory(DirectoryManager.GridSettingsFolder);
                }

                // Save the current DataGridView column order to the specified file
                string gridSettingsPath = DirectoryManager.GetGridSettingsPathForGridName(dgvName);
                SerialExtensions.SaveToFile(gridSettingsPath, settings);
                SMLog.Log("Saved DataGridView Settings File: " + Path.GetFileName(gridSettingsPath));
            }
            catch (Exception ex)
            {
                SMLog.Log(String.Format("<Error> SaveDataGridViewSettingsToFile: {0}", ex.Message));
            }
        }


        public static void SaveDataGridViewSettingsToFile(DataGridView dgvToSave)
        {   
            // Get the settings
            RADataGridViewSettings settings = RAExtensions.SaveColumnOrder(dgvToSave);
            SaveDataGridViewSettingsToFile(settings, dgvToSave);
        }


        public static void DeleteApplicationSettingsFiles()
        {
            // DO NOT use the bulldozer here
            // 'My Documents/CFSM' may contain some original files
            ZipUtilities.RemoveReadOnlyAttribute(DirectoryManager.WorkFolder);
            GenExtensions.DeleteFile(DirectoryManager.SongsInfoPath);
            GenExtensions.DeleteFile(DirectoryManager.AppSettingsPath);
            GenExtensions.DeleteDirectory(DirectoryManager.GridSettingsFolder);
            GenExtensions.DeleteDirectory(DirectoryManager.TaggerWorkingFolder);
        }


        /// <summary>
        /// Tries to load the song info from the file.
        /// Will return false if the file does not exist or if the SongDataVersion is incorrect.
        /// </summary>
        /// <param name="songInfo">The song info loaded from the file.</param>
        /// <returns>True if the song info was successfully loaded.</returns>
        public static bool TryLoadSongInfoFromFile(out List<SongData> songInfo)
        {
            songInfo = null;
            // load songsInfo.xml if it exists 
            if (File.Exists(DirectoryManager.SongsInfoPath))
            {
                XmlDocument dom = new XmlDocument();
                dom.Load(DirectoryManager.SongsInfoPath);
                SMLog.Log("Loaded File: " + Path.GetFileName(DirectoryManager.SongsInfoPath));

                // remove version info node
                var listNode = dom["ArrayOfSongData"];
                if (listNode != null)
                {
                    var versionNode = listNode["SongDataList"];
                    if (versionNode != null)
                    {
                        if (versionNode.HasAttribute("version"))
                        {
                            // If this collection has old version info
                            if (versionNode.GetAttribute("version") != SongData.SongDataVersion)
                            {
                                // Log it and return false to get songs with the new version
                                SMLog.Log("<WARNING> Incorrect song collection version found ...");
                                return false;
                            }
                        }

                        listNode.RemoveChild(versionNode);
                    }

                    songInfo = SerialExtensions.XmlDeserialize<List<SongData>>(listNode.OuterXml);

                    if (songInfo == null || songInfo.Count == 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool TryLoadDataGridViewSettingsFromFile(DataGridView dgv, out RADataGridViewSettings settings)
        {
            settings = null;
            string settingsPath = DirectoryManager.GetGridSettingsPathForGridName(dgv.Name);
            // If a file exists at the given path
            if (File.Exists(settingsPath))
            {
                try
                {
                    // load the settings from the file
                    settings = SerialExtensions.LoadFromFile<RADataGridViewSettings>(settingsPath);
                    SMLog.Log("Loaded File: " + Path.GetFileName(settingsPath));
                    return true;
                }
                catch (Exception ex)
                {
                    SMLog.Log("<ERROR> GridSettings could not be loaded ...");
                    SMLog.Log("Windows 10 users must uninstall .Net 4.7 and manually install .Net 4.0 if this error persists ...");
                    SMLog.Log(ex.Message);
                    return false;
                }
            }

            return false;
        }
    }
}

