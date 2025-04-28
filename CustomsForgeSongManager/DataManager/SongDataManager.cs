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

namespace CustomsForgeSongManager.DataManager
{
    public static class SongDataManager
    {
        /// <remarks>
        /// This list is populated by the ParseSongsWorker when it completes its work.
        /// </remarks> 
        public static List<SongData> AllSongs { get; private set; } = new List<SongData>();

        /// <summary>
        /// List of background workers that have been created and run.
        /// </summary>
        private static List<BackgroundWorker> _workers = new List<BackgroundWorker>();

        /// <summary>
        /// Runs a full scan of the directories for song data.
        /// </summary>
        public static void RunFullScan(bool saveSongInfo = true)
        {
            // Create the worker with the action parameter set to populate the Songs list after it is complete
            ParseSongsWorker worker = new ParseSongsWorker(() =>
            {
                // Get the worker from the list of workers
                ParseSongsWorker workerDone = _workers.FirstOrDefault(w => w is ParseSongsWorker) as ParseSongsWorker;

                // Populate the Songs list with the parsed data
                AllSongs = workerDone.SongData;

                // If saveSongInfo is true, save the song data to the database
                if (saveSongInfo)
                {
                    FileTools.SaveSongCollectionToFile(AllSongs);
                }

            });

            _workers.Add(worker);

            // Start it working
            worker.RunWorkerAsync();
        }


        public static void SetSongMasterList(List<SongData> songData)
        {
            updateSongList(songData);
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
                        updateSongList(songInfo);
                        return true;
                    }
                }
            }
            return false;
        }
        

        /// <summary>
        /// Private method to update the Song Master List and
        /// do any other processing necessary to get the update out to the UI
        /// </summary>
        /// <param name="newSongData"></param>
        private static void updateSongList(List<SongData> newSongData)
        {
            // Update the list as a NEW list with the same objects
            AllSongs = newSongData.ToList();

            // Notify the consumers that the song list has been updated
            AppEventManager.RaiseEvent(new SongMasterListChangedEvent(AllSongs));
        }
    }
}
