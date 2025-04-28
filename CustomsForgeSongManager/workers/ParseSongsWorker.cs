using CustomControls;
using CustomsForgeSongManager.DataObjects;
using CustomsForgeSongManager.LocalTools;
using GenTools;
using RocksmithToolkitLib.XmlRepository;
using RocksmithToolkitLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using CustomsForgeSongManager.UControls;
using CustomsForgeSongManager.Properties;
using System.Runtime.CompilerServices;

namespace CustomsForgeSongManager.Workers
{
    /// <summary>
    /// Background worker class for parsing songs.
    /// </summary>
    public class ParseSongsWorker : ControllableWorker
    {
        /// <summary>
        /// Queue to store Log messages from worker threads for reporting to the UI.
        /// </summary>
        private ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// Queue to store the paths of songs to be parsed.
        /// </summary>
        private ConcurrentQueue<string> _filesToParse = new ConcurrentQueue<string>();

        /// <summary>
        /// List to store the old song data from the old song collection file.
        /// </summary>
        private ConcurrentBag<SongData> _oldSongData = new ConcurrentBag<SongData>();

        /// <summary>
        /// Holds the song data after it has been parsed.
        /// </summary>
        private ConcurrentBag<SongData> _parsedSongData = new ConcurrentBag<SongData>();

        /// <summary>
        /// The current threads that are parsing songs.
        /// </summary>
        private List<Thread> _activeThreads = new List<Thread>();

        /// <summary>
        /// Stopwatch used to track the time taken to parse songs.
        /// </summary>
        private Stopwatch _counterStopwatch = null;

        /// <summary>
        /// Action to perform when the parsing is complete.
        /// </summary>
        private Action _actionOnComplete = null;

        /// <summary>
        /// List of song data that has been parsed.
        /// </summary>
        public List<SongData> SongData { get; private set; } = null;

        /// <summary>
        /// Constructor for the ParseSongsWorker class.
        /// </summary>
        /// <param name="actionOnComplete">Action to perform after the parsing work has been completed.</param>
        public ParseSongsWorker(Action actionOnComplete = null)
        {
            // Set default params
            WorkerSupportsCancellation = true;
            WorkerReportsProgress = true;

            // Hook the event handlers
            this.DoWork += parseSongs_DoWork;
            this.RunWorkerCompleted += parseSongs_OnWorkComplete;
            _actionOnComplete = actionOnComplete;
        }


        /// <summary>
        /// Method that runs when the background worker is started.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void parseSongs_DoWork(object sender, DoWorkEventArgs e)
        {
            // Initialize the data that the worker will use
            if (!tryInitializeWorkerData(out string errorMsg))
            {
                // log the error message and exit
                return;
            }
            else
            {
                // If the worker has been cancelled, stop processing
                // NOTE: handle later... cancellation as of now is a mess across all workloads
                /*
                if (bWorker.CancellationPending || Globals.TsLabel_Cancel.Text == "Canceling" || Globals.CancelBackgroundScan)
                {
                    bWorker.CancelAsync();
                    e.Cancel = true;
                    Globals.DebugLog(Resources.UserCancelledProcess);
                    return;
                }
                */

                Globals.DebugLog("Parsing files ...");

                _counterStopwatch = new Stopwatch();
                _counterStopwatch.Restart();

                // Initialize the threads for parsing songs
                int coreCount = SysExtensions.GetCoreCount();
                Thread[] workThreads = initializeParseSongThreads(coreCount);

                // Do the work
                DoParsingWork(workThreads);
            }
        }

        /// <summary>
        /// Method that runs the threads to parse the songs and handles the reporting of progress from said threads.
        /// </summary>
        private void DoParsingWork(Thread[] parseSongsThreads)
        {
            // Start each thread
            foreach (var thread in parseSongsThreads)
            {
                thread.Start();

                // Add the thread to the list of active threads
                _activeThreads.Add(thread);
            }

            // Loop until all threads are complete
            bool isRunning = true;
            int numThreadsComplete = 0;
            while (isRunning)
            {
                // Check if all tasks are completed
                numThreadsComplete = 0; // Reset the counter
                foreach (Thread t in parseSongsThreads)
                {
                    if (!t.IsAlive)
                    {
                        // Incerement the completed threads
                        numThreadsComplete++;

                        // Check if all threads are complete
                        if (numThreadsComplete == parseSongsThreads.Length)
                        {
                            isRunning = false;
                        }
                    }
                    else
                    {
                        // Else at least one thread is alive => break
                        break;
                    }
                }

                // Attempt to get any new messages that have been queued by worker threads to send to the UI
                if (_messageQueue.IsEmpty)
                {
                    // If no messages, wait 200ms and check again
                    Thread.Sleep(200);
                }
                else
                {
                    // If we have messages, dequeue them and send them to the UI
                    while (_messageQueue.TryDequeue(out var message))
                    {
                        SMLog.Log(message);
                    }
                }
            }
        }

        /// <summary>
        /// Method that runs when the background worker has completed its work.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void parseSongs_OnWorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            // Change the toolStrip cancel label to not visible
            // TODO: This needs to only be done on the song manager view itself
            //GenExtensions.InvokeIfRequired(_songManagerView, delegate { Globals.TsLabel_Cancel.Visible = false; });

            // Handle the Globals.Tristate
            // This eventually needs to go
            if (e.Cancelled || Globals.TsLabel_Cancel.Text == "Canceling" || Globals.CancelBackgroundScan)
            {
                // bWorker.Abort(); // don't use abort
                SMLog.Log(Resources.UserCancelledProcess);
                Globals.TsLabel_MainMsg.Text = Resources.UserCancelled;
                Globals.WorkerFinished = Globals.Tristate.Cancelled;
            }
            else
            {
                //WorkerProgress(100);

                SMLog.Log(String.Format("Finished multithread parsing took: {0}", _counterStopwatch.Elapsed));
                Globals.WorkerFinished = Globals.Tristate.True;
            }

            // Set the scanning flag
            Globals.IsScanning = false;


            // Create the finalized list
            List<SongData> parsedSongs = _parsedSongData.ToList();

            // Sort, if possible
            if (!String.IsNullOrEmpty(AppSettings.Instance.SortColumn))
            {
                var prop = typeof(SongData).GetProperty(AppSettings.Instance.SortColumn);
                if (prop != null)
                {
                    Type interfaceType = prop.PropertyType.GetInterface("IComparable");
                    if (interfaceType != null)
                    {
                        try
                        {
                            // Sort the parsed songs based on their CompareTo methods
                            parsedSongs.Sort((x1, x2) =>
                            {
                                var c1 = (prop.GetValue(x1, new object[] { }) as IComparable);
                                var c2 = (prop.GetValue(x2, new object[] { }) as IComparable);
                                if (c1 == null || c2 == null)
                                    return -1;

                                if (AppSettings.Instance.SortAscending)
                                    return c1.CompareTo(c2);
                                else
                                    return c2.CompareTo(c1);
                            });
                        }
                        catch (Exception)
                        {
                            // do nothing
                        }
                    }
                }
            }

            // -- CRITCAL -- this populates Arrangement DLCKey info in Arrangements2D
            parsedSongs.ForEach(a => a.Arrangements2D.ToList().ForEach(arr => arr.Parent = a));

            // Set the list of parsed songs to this object's property for outside access
            SongData = parsedSongs;

            // Bind the parsed songs to the global master list
            Globals.MasterCollection = new BindingList<SongData>(parsedSongs);

            // -- CRITCAL -- this populates Arrangement DLCKey info in Arrangements2D
            //Globals.MasterCollection.ToList().ForEach(a => a.Arrangements2D.ToList().ForEach(arr => arr.Parent = a));
            //counterStopwatch.Stop();

            // Save the parsed songs collection to the songsInfo file
            //FileTools.SaveSongCollectionToFile();

            // TODO: Update the UI
            //_songManagerView.PopulateLocalSongListMember();

            // If there is an action to perform on completion, do it
            if (_actionOnComplete != null)
            {
                _actionOnComplete.Invoke();
            }
        }

        private bool tryInitializeWorkerData(out string processMessage)
        {
            processMessage = String.Empty;

            // Announce that we're scanning current files
            Globals.IsScanning = true;

            // 2x speed hack ... preload the TuningDefinition and fix for tuning 'Other' issue           
            if (Globals.TuningXml == null || Globals.TuningXml.Count == 0)
                Globals.TuningXml = TuningDefinitionRepository.Instance.LoadTuningDefinitions(GameVersion.RS2014);

            // If we cannot get the list of files to parse, exit
            if (!tryGetValidSongFilePaths(out List<string> songFilePathsDiscovered))
            {
                // Return that we did not discover any song files within the paths determined by settings.
                processMessage = "No song files found in the specified paths.";
                return false;
            }
            else
            {
                // Place the files to be parsed into the queue
                foreach (string file in songFilePathsDiscovered)
                {
                    _filesToParse.Enqueue(file);
                }

                // "Raw" is good descriptor :)
                // Log the amount of songs found
                SMLog.Log(String.Format("Raw songs count: {0}", songFilePathsDiscovered.Count));

                // Try to get the current collection of song data
                if (!tryGetValidLocalSongData(out List<SongData> localSongData))
                {
                    // Currently only returns true
                    //processMessage = "No song files found in the specified paths.";
                    return false;
                }
                else
                {
                    // Add the processed list of songData to the current local song data
                    foreach (var songData in localSongData)
                    {
                        // Add the songData to the local song data
                        _oldSongData.Add(songData);
                    }

                    return true;
                }
            }         
        }

        /// <summary>
        /// Attempts to get a list of valid song file paths based on user settings.
        /// </summary>
        /// <param name="songFilePathsDiscovered">Out parameter for the list of valid song file paths.</param>
        /// <returns>True if any valid song file paths were discovered.</returns>
        private bool tryGetValidSongFilePaths(out List<string> songFilePathsDiscovered)
        {

            // Get the list of all the files to parse based on user settings
            songFilePathsDiscovered = FileTools.FilesList(Constants.Rs2DlcFolder, // All files in the Rocksmith2014 DLC folder
                AppSettings.Instance.IncludeRS1CompSongs,
                AppSettings.Instance.IncludeRS2BaseSongs,
                AppSettings.Instance.IncludeCustomPacks);

            // remove inlays
            if (!Globals.IncludeInlays)
                songFilePathsDiscovered = songFilePathsDiscovered.Where(fi => !fi.ToLower().Contains("inlay")).ToList();
            else
                SMLog.Log("Duplicates scan includes any custom inlays ...");

            // If after removing inlays, we have no files to parse, exits
            if (songFilePathsDiscovered.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Attempts to get the current local collection of song data.
        /// </summary>
        /// <returns></returns>
        private bool tryGetValidLocalSongData(out List<SongData> globalSongData)
        {
            // Get the current collection of songs
            globalSongData = Globals.MasterCollection.ToList();

            int oldCount = globalSongData.Count();

            // remove songs from collection when the FilePath does not exist
            globalSongData.RemoveAll(sd => !File.Exists(sd.FilePath));
            // remove duplicate songs from collection that have same FilePath and DLCKey (prevents multiple count of song pack songs)
            var duplicates = globalSongData.GroupBy(x => new { x.FilePath, x.DLCKey }).Where(group => group.Count() > 1).ToList();
            if (duplicates.Count() > 0)
            {
                foreach (var x in duplicates)
                {
                    var toDelete = x.Where(z => z != x.First());
                    globalSongData.RemoveAll(sd => toDelete.Contains(sd));
                }
            }

            int removed = Math.Abs(globalSongData.Count() - oldCount);
            if (removed > 0)
                SMLog.Log(String.Format("Removed ({0}) obsolete songs/inlays from songsInfo.xml ...", removed));

            return true;
        }

        /// <summary>
        /// Initializes the threads for parsing songs.
        /// </summary>
        /// <param name="threadCount">The number of threads to initialize.</param>
        /// <returns>The threads to be used to parse songs.</returns>
        private Thread[] initializeParseSongThreads(int threadCount)
        {
            // Create an array of threads
            Thread[] threads = new Thread[threadCount];

            // Create and start each thread
            for (int i = 0; i < threadCount; i++)
            {
                int currentThreadID = i;
                threads[i] = new Thread(() =>
                {
                    // While there are song paths to process
                    while (_filesToParse.Count > 0)
                    {
                        // Try to dequeue a song file path from the queue
                        if (_filesToParse.TryDequeue(out var songFilePath))
                        {
                            // Process the song file path
                            if (tryParseSong(songFilePath, currentThreadID, out SongData parsedSongData))
                            {
                                // If the song was parsed successfully, add it to the parsed list
                                _parsedSongData.Add(parsedSongData);
                                // Log the successful parsing of the song
                                queueLogMessage(currentThreadID, String.Format("Parsed song: {0}", songFilePath));
                            }
                            else
                            {
                                // Log an error message if parsing failed
                                queueLogMessage(currentThreadID, $"Failed to parse song at {songFilePath}");
                            }
                        }
                    }
                });
            }
            return threads;
        }

        /// <summary>
        /// Attempts to parse the song file at the given path.
        /// </summary>
        /// <param name="songFilePath">The song's file path.</param>
        /// <returns>True </returns>
        private bool tryParseSong(string songFilePath, int threadID, out SongData parsedSongData)
        {
            parsedSongData = null;

            // Handle the case where the file has already been parsed

            // Try to get the current songData for the song on the given file path 
            SongData sInfo = _oldSongData.FirstOrDefault(s => s.FilePath.Equals(songFilePath, StringComparison.OrdinalIgnoreCase));

            // If the songData exists
            if (sInfo != null)
            {
                FileInfo fInfo = new FileInfo(songFilePath);

                // If the file size and last write time are the same
                if ((int)fInfo.Length == sInfo.FileSize && fInfo.LastWriteTime == sInfo.FileDate)
                {
                    // The song at the given path has already parsed => return true and the song data
                    parsedSongData = sInfo;
                    return true;
                }
            }

            // If the song has not been parsed yet
            try
            {
                using (var browser = new PsarcBrowser(songFilePath))
                {
                    // Get the song data 
                    var songInfo = browser.GetSongData();

                    // For each piece of unique song data
                    foreach (SongData songData in songInfo.Distinct())
                    {
                        // If the song data's package version is "Null"
                        if (songData.PackageVersion == "Null")
                        {
                            // Get the version from the file name
                            var fileNameVersion = songData.GetVersionFromFileName();
                            if (fileNameVersion != "")
                                songData.PackageVersion = fileNameVersion;
                        }

                        // Add the song data to the parsed list
                        // Confirm that only one songData gets generated per songFilePath???
                        parsedSongData = songData;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                // corrupt CDLC move to Quarantine folder
                if (ex.Message.StartsWith("Error reading JObject"))
                    queueLogMessage(threadID, String.Format("<ERROR> CDLC is corrupt: {0}", songFilePath));
                else if (ex.Message.StartsWith("Object reference not set"))
                    queueLogMessage(threadID, String.Format("<ERROR> CDLC is missing data: {0}", songFilePath));
                else
                    queueLogMessage(threadID, String.Format("<ERROR> {1}: {0}", songFilePath, ex.Message));

                if (AppSettings.Instance.EnableQuarantine)
                {
                    if (ex.Message.Contains("xblock file"))
                    {
                        queueLogMessage(threadID, "Assuming the current file is not a song psarc, skipping the quarantine process...");
                        return false;
                    }

                    var corFileName = String.Format("{0}{1}", Path.GetFileName(songFilePath), ".cor");
                    var corFilePath = Path.Combine(Constants.QuarantineFolder, corFileName);

                    if (!Directory.Exists(Constants.QuarantineFolder))
                        Directory.CreateDirectory(Constants.QuarantineFolder);

                    File.Move(songFilePath, corFilePath);
                    queueLogMessage(threadID, String.Format("File was quarantined to: {0}", Constants.QuarantineFolder));
                }
                else
                {
                    queueLogMessage(threadID, String.Format("<WARNING> File was not quarantined ..."));
                    queueLogMessage(threadID, String.Format(" - Auto quarantine may be enabled in the 'Settings' tabmenu ..."));
                }

                return false;
            }
            
        }

        /// <summary>
        /// Queues the given message for logging by the background worker.
        /// </summary>
        /// <param name="threadID"></param>
        /// <param name="message"></param>
        private void queueLogMessage(int threadID, string message)
        {
            string formattedMsg = String.Format("[Thread ({0})]: {1}", threadID, message);
            _messageQueue.Enqueue(formattedMsg);
        }
    }
}
