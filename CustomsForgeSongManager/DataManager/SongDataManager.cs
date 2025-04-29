using CustomsForgeSongManager.DataObjects;
using CustomsForgeSongManager.LocalTools;
using CustomsForgeSongManager.Workers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomsForgeSongManager.AppEvents;
using CustomsForgeSongManager.AppEvents.Event;
using GenTools;
using System.IO;
using System.Xml;
using System.Threading;

namespace CustomsForgeSongManager.DataManager
{
    public static class SongDataManager
    {
        /// <remarks>
        /// This list is populated by the ParseSongsWorker when it completes its work.
        /// </remarks> 
        private static List<SongData> _songsFromScannedFiles = new List<SongData>();

        /// <summary>
        /// Master list of all songs. This is the current list of songs that are being used by the application
        /// prior to any filtering done by the user.
        /// </summary>
        private static List<SongData> _songsMasterList = new List<SongData>();

        /// <summary>
        /// List of songs that are parsed from the songInfo.xml file.
        /// </summary>
        private static List<SongData> _songsFromSongInfoFile = new List<SongData>();

        private static ReaderWriterLockSlim _allSongsLock = new ReaderWriterLockSlim();

        /// <summary>
        /// List of background workers that have been created and run.
        /// </summary>
        private static List<BackgroundWorker> _workers = new List<BackgroundWorker>();

        /// <summary>
        /// Flag to indicate whether a full scan is currently being performed.
        /// </summary>
        public static bool IsPerformingFullScan { get; private set; } = false;

        /// <summary>
        /// Flag to indicate whether the SongDataManager has been initialized.
        /// Use this to know whether or not to trust the data in AllSongs.
        /// </summary>
        public static bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// An object to provide cross-thread safety when initializing the SongDataManager.
        /// </summary>
        private static object _initLock = new object();

        /// <summary>
        /// Flag to signal whether this class has started initialization.
        /// Used so the Initialize() method only gets called once.
        /// </summary>
        private static bool _hasStartedInitialization = false;

        /// <summary>
        /// Flag to indicate whether the SongDataManager has any SongData loaded.
        /// </summary>
        public static bool HasSongMasterList { get; private set; }

        #region Public Methods

        /// <summary>
        /// Initializes the SongDataManager by attempting to get song info from the
        /// songInfo.xml at its default location.
        /// </summary>
        public static void Initialize()
        {
            bool canInit = false;
            lock (_initLock)
            {
                if (!_hasStartedInitialization)
                {
                    _hasStartedInitialization = true;
                    canInit = true;
                }
            }

            if (canInit)
            {
                // If we can successfully load the songInfo file
                if (TryLoadSongInfoFromFile(out List<SongData> songInfo))
                {
                    // Set the song info to the AllSongs list
                    updateSongMasterList(songInfo);

                    // Set the initialized flag to true
                    IsInitialized = true;
                }
                else // If we can't load it from file.
                {
                    // If we have the rocksmith install directory
                    if (DirectoryManager.RSInstalledDir != null)
                    {
                        // Run a full scan for the songs
                        RunFullScan();
                    }
                }
            }
        }

        /// <summary>
        /// Runs a full scan of the directories for song data.
        /// </summary>
        public static void RunFullScan(bool saveSongInfo = true)
        {
            // If we are already performing a full scan, return
            if (IsPerformingFullScan)
            {
                // Return => If you are here, you just need to wait to
                // receive the SongMasterListChangedEvent AppEvent
                return;
            }
            else
            {
                // Raise flag for scan event
                IsPerformingFullScan = true;

                // Create the worker with the action parameter set to populate the Songs list after it is complete
                ParseSongsWorker worker = new ParseSongsWorker(() =>
                {
                    // Finalize by updating the song list
                    finalizeFullScan();

                    // Handle if this was called from the Initialize method
                    if (!IsInitialized)
                    {
                        // Set the initialized flag to true
                        IsInitialized = true;
                    }

                    // If saveSongInfo is true, save the song data to the database
                    if (saveSongInfo)
                    {
                        FileTools.SaveSongCollectionToFile(_songsMasterList);
                    }
                });

                _workers.Add(worker);

                // Start it working
                worker.RunWorkerAsync();

                // Send a notification about the start of a Song Scan
                SongScanEvent songScanEvent = new SongScanEvent(true);
                AppEventManager.RaiseEvent(songScanEvent);
            }

        }

        /// <summary>
        /// Gets the songs parsed from the Full Scan operation.
        /// </summary>
        /// <returns></returns>
        public static List<SongData> GetSongMasterList()
        {
            // Get the read lock for the AllSongs list
            _allSongsLock.EnterReadLock();
            try
            {
                return new List<SongData>(_songsMasterList);
            }
            finally
            {
                // Release the read lock
                _allSongsLock.ExitReadLock();
            }
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
                        // Set the song info to the _songsFromScannedFiles
                        _songsFromScannedFiles = new List<SongData>(songInfo);

                        return true;
                    }
                }
            }
            return false;
        }


        #endregion Public Methods

        /// <summary>
        /// Private method to update the Song Master List and
        /// send the SongMasterListChangedEvent to the consumers.
        /// </summary>
        /// <param name="newSongData"></param>
        private static void updateSongMasterList(List<SongData> newSongData)
        {
            // Update the list as a NEW list with the same objects
            _songsMasterList = new List<SongData>(newSongData);

            // Notify the consumers that the song list has been updated
            AppEventManager.RaiseEvent(new SongScanEvent(false, true, newSongData = new List<SongData>(newSongData)));
        }

        /// <summary>
        /// Method called after the worker has completed its work.
        /// </summary>
        private static void finalizeFullScan()
        {
            // Get the worker from the list of workers
            ParseSongsWorker workerDone = _workers.FirstOrDefault(w => w is ParseSongsWorker) as ParseSongsWorker;
            _workers.Remove(workerDone);
            List<SongData> newSongInfo = workerDone.SongData;

            // Get the write lock for the AllSongs list
            _allSongsLock.EnterWriteLock();

            // Set flag for rescan event
            IsPerformingFullScan = false;

            // Set the parsed data from the worker to the specific list for parsed songs
            _songsFromScannedFiles = workerDone.SongData;

            // Populate the Songs master list with the parsed data
            updateSongMasterList(workerDone.SongData);

            // Release the write lock
            _allSongsLock.ExitWriteLock();
        }
    }
}
