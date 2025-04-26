using CustomsForgeSongManager.DataObjects;
using CustomsForgeSongManager.LocalTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomsForgeSongManager.DataManager
{
    /// <summary>
    /// Singleton class to manage directories used by the application.
    /// </summary>
    public static class DirectoryManager
    {
        private static string _workFolder = "";
        /// <summary>
        /// The main working folder for the application.
        /// </summary>
        public static string WorkFolder { get { return _workFolder; } }

        private static string _tempWorkFolder = "";
        public static string TempWorkFolder { get { return _tempWorkFolder; } }

        private static string _themeFolder = "";
        public static string ThemeFolder { get { return _themeFolder; } }

        private static string _logFilePath = "";
        public static string LogFilePath { get { return _logFilePath; } }

        private static string _appSettingsPath = "";
        public static string AppSettingsPath { get { return _appSettingsPath; } }

        private static string _songsInfoPath = "";
        public static string SongsInfoPath { get { return _songsInfoPath; } }

        private static string _profileDataPath = "";
        public static string ProfileDataPath { get { return _profileDataPath; } }

        private static string _gridSettingsFolder = "";
        public static string GridSettingsFolder { get { return _gridSettingsFolder; } }

        private static string _audioCacheFolder = "";
        public static string AudioCacheFolder { get { return _audioCacheFolder; } }

        private static string _profileBackupsFolder = "";
        public static string ProfileBackupsFolder { get { return _profileBackupsFolder; } }

        private static string _taggerWorkingFolder = "";
        public static string TaggerWorkingFolder { get { return _taggerWorkingFolder; } }

        private static string _taggerTemplatesFolder = "";
        public static string TaggerTemplatesFolder { get { return _taggerTemplatesFolder; } }

        private static string _taggerExtractedFolder = "";
        public static string TaggerExtractedFolder { get { return _taggerExtractedFolder; } }

        private static string _taggerPreviewsFolder = "";
        public static string TaggerPreviewsFolder { get { return _taggerPreviewsFolder; } }

        private static string _applicationFolder = "";
        public static string ApplicationFolder { get { return _applicationFolder; } }

        private static string _appIdFilePath = "";
        public static string AppIdFilePath { get { return _appIdFilePath; } }

        private static string _tuningDefFilePath = "";
        public static string TuningDefFilePath { get { return _tuningDefFilePath; } }

        public static string EnabledExtension
        {
            get
            {
                if (AppSettings.Instance.MacMode)
                    return "_m.psarc";
                else
                    return "_p.psarc";
            }
        }

        public static string DisabledExtension
        {
            get
            {
                if (AppSettings.Instance.MacMode)
                    return "_m.disabled.psarc";
                else
                    return "_p.disabled.psarc";
            }
        }

        public static string Rs1DiscPsarcPath
        {
            get
            {
                var rs1PackName = "rs1compatibilitydisc" + EnabledExtension;
                var dlcPath = Rs2DlcFolder;

                // TODO: determine if GetFiles is case sensitive
                var files = Directory.GetFiles(dlcPath, rs1PackName, SearchOption.AllDirectories);
                if (files.Length > 0)
                    return files[0];
                return Path.Combine(dlcPath, rs1PackName);
            }
        }

        public static string Rs1DlcPsarcPath
        {
            get
            {
                var rs1DlcPackName = "rs1compatibilitydlc" + EnabledExtension;
                var dlcPath = Rs2DlcFolder;

                // TODO: determine if GetFiles is case sensitive
                var files = Directory.GetFiles(dlcPath, rs1DlcPackName, SearchOption.AllDirectories);
                if (files.Length > 0)
                    return files[0];
                return Path.Combine(dlcPath, rs1DlcPackName);
            }
        }

        private static string _rsInstalledDir = null;
        public static string RSInstalledDir { get { return _rsInstalledDir; } }
        // write access to the Steam RSInstallDir is provided by the code 
        public static string Rs2DlcFolder { get { return Path.Combine(_rsInstalledDir, "dlc"); } }
        public static string Rs2CdlcFolder { get { return Path.Combine(Rs2DlcFolder, "cdlc"); } }
        public static string CachePsarcPath { get { return Path.Combine(_rsInstalledDir, "cache.psarc"); } }
        public static string Rs2OriginalsFolder { get { return Path.Combine(_rsInstalledDir, "originals"); } }
        public static bool OnMac
        {
            get
            {
                if (Rs2DlcFolder.Contains("Application Support")) //Rather unreliable, but other methods have been proven not to work in all cases on Wineskin
                    return true;

                if (Environment.GetEnvironmentVariable("WINE_INSTALLED") == "1")
                    return true;

                // run Mac compatiblity mode even when on a PC
                if (AppSettings.Instance.MacMode)
                    return true;

                return false;
            }
        }
        public static string Rs1DiscPsarcBackupPath
        {
            get
            {
                if (OnMac)
                    return Path.Combine(Rs2OriginalsFolder, "rs1compatibilitydisc_m.org.psarc");

                return Path.Combine(Rs2OriginalsFolder, "rs1compatibilitydisc_p.org.psarc");
            }
        }
        public static string Rs1DlcPsarcBackupPath
        {
            get
            {
                if (OnMac)
                    return Path.Combine(Rs2OriginalsFolder, "rs1compatibilitydlc_m.org.psarc");

                return Path.Combine(Rs2OriginalsFolder, "rs1compatibilitydlc_p.org.psarc");
            }
        }
        public static string CachePsarcBackupPath { get { return Path.Combine(Rs2OriginalsFolder, "cache.org.psarc"); } }
        public static string ExtractedSongsHsanPath { get { return Path.Combine(SongPacksFolder, "songs.hsan"); } }
        public static string ExtractedRs1DiscHsanPath { get { return Path.Combine(SongPacksFolder, "songs_rs1disc.hsan"); } }
        public static string ExtractedRs1DlcHsanPath { get { return Path.Combine(SongPacksFolder, "songs_rs1dlc.hsan"); } }
        // TODO: address Mac unpacked directory, i.e., cache.psarc_RS2014_Mac
        public static string Cache7zPath { get { return Path.Combine(SongPacksFolder, "cache_psarc_RS2014_Pc", "cache7.7z"); } }
        public static string CachePcPath { get { return Path.Combine(SongPacksFolder, "cache_psarc_RS2014_Pc"); } }
        // cache7.7z internal paths uses back slashes (normal path mode)
        public static string SongsHsanInternalPath { get { return Path.Combine("manifests", "songs", "songs.hsan"); } }
        // this is not a mistake archive internal paths use forward slashes (internal path mode)
        public static string SongsRs1DiscInternalPath { get { return @"manifests/songs_rs1disc/songs_rs1disc.hsan"; } }
        public static string SongsRs1DlcInternalPath { get { return @"manifests/songs_rs1dlc/songs_rs1dlc.hsan"; } }

        // not a good practice to use Rocksmith 2014 root for CFSM content     
        public static string SongPacksFolder { get { return Path.Combine(WorkFolder, "SongPacks"); } }
        public static string BackupsFolder { get { return Path.Combine(WorkFolder, "Backups"); } }
        public static string DuplicatesFolder { get { return Path.Combine(WorkFolder, "Duplicates"); } }
        public static string RemasteredFolder { get { return Path.Combine(WorkFolder, "Remastered"); } }
        public static string RepairsErrorLogPath { get { return Path.Combine(RemasteredFolder, "remastered_error.log"); } }
        public static string RemasteredArcFolder { get { return Path.Combine(RemasteredFolder, "archives"); } }
        public static string RemasteredCorFolder { get { return Path.Combine(RemasteredFolder, "corrupt"); } }
        public static string RemasteredOrgFolder { get { return Path.Combine(RemasteredFolder, "original"); } }
        public static string RemasteredMaxFolder { get { return Path.Combine(RemasteredFolder, "maxfive"); } }
        public static string QuarantineFolder { get { return Path.Combine(Constants.WorkFolder, "Quarantine"); } }
        public static string FfmpegLogPath { get { return Path.Combine(WorkFolder, "ffmpeg.log"); } }






        /// <summary>
        /// Method that asks the DirectoryManager to initialize the default paths
        /// for the paths that do not depend on user selection.
        /// </summary>
        public static void InitializeDefaultPaths()
        {
            _tempWorkFolder = Path.Combine(Path.GetTempPath(), "CFSM");


            _workFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CFSM");
            _themeFolder = Path.Combine(_workFolder, "Themes");
            _logFilePath = Path.Combine(_workFolder, "debug.log");
            _appSettingsPath = Path.Combine(_workFolder, "appSettings.xml");
            _songsInfoPath = Path.Combine(_workFolder, "songsInfo.xml");
            _profileDataPath = Path.Combine(_workFolder, "profileData.xml");
            _gridSettingsFolder = Path.Combine(_workFolder, "DgvSettings");
            _audioCacheFolder = Path.Combine(_workFolder, "AudioCache");
            _profileBackupsFolder = Path.Combine(_workFolder, "ProfileBackups");
            _taggerWorkingFolder = Path.Combine(_workFolder, "Tagger");
            _taggerTemplatesFolder = Path.Combine(_taggerWorkingFolder, "templates");
            _taggerExtractedFolder = Path.Combine(_taggerWorkingFolder, "extracted");
            _taggerPreviewsFolder = Path.Combine(_taggerWorkingFolder, "previews");

            _applicationFolder = Path.GetDirectoryName(Application.ExecutablePath);
            _appIdFilePath = Path.Combine(_applicationFolder, "RocksmithToolkitLib.SongAppId.xml");
            _tuningDefFilePath = Path.Combine(_applicationFolder, "RocksmithToolkitLib.TuningDefinition.xml");
        }

        /// <summary>
        /// Returns the path to the grid settings file for the given grid name.
        /// Here as a method until a better place is found
        /// => we should not be using Globals.DgvCurrent
        /// => we should not have a dynamic property for the grid settings path
        /// that relies on updates to Globals.DgvCurrent
        /// </summary>
        /// <param name="gridName"></param>
        /// <returns></returns>
        public static string GetGridSettingsPathForGridName(string gridName)
        {
            //Globals.DgvCurrent.Name = gridName; // update current grid name
            return Path.Combine(GridSettingsFolder, String.Format("{0}{1}", gridName, ".xml"));
        }

        /// <summary>
        /// Sets the Rocksmith installation directory for the application.
        /// Also handles setting the directory in the AppSettings.
        /// Also saves the new settings to the application settings file.
        /// </summary>
        /// <param name="rsDir"></param>
        public static void SetRocksmithInstallationDirectory(string rsDir)
        {
            // Set it here
            _rsInstalledDir = rsDir;
            // and in the AppSettings
            AppSettings.Instance.RSInstalledDir = rsDir;
            // Save the new settings
            FileTools.SaveApplicationSettingsToFile();
        }
    } 
}
