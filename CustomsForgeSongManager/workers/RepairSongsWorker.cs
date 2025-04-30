using CustomsForgeSongManager.DataObjects;
using CustomsForgeSongManager.Forms;
using CustomsForgeSongManager.LocalTools;
using CustomsForgeSongManager.UControls;
using GenTools;
using RocksmithToolkitLib.DLCPackage.Manifest.Functions;
using RocksmithToolkitLib.DLCPackage;
using RocksmithToolkitLib.PSARC;
using RocksmithToolkitLib.Sng;
using RocksmithToolkitLib.XML;
using RocksmithToolkitLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Arrangement = RocksmithToolkitLib.DLCPackage.Arrangement;
using RocksmithToolkitLib.Extensions;
using RocksmithToolkitLib.Sng2014HSL;
using CustomsForgeSongManager.Properties;
using System.Collections.Concurrent;
using System.Reflection;
using DLogNet;

namespace CustomsForgeSongManager.Workers
{
    /// <summary>
    /// Class that handles the logic of repairing songs. 
    /// </summary>
    public class RepairSongsWorker : ProgressPanelWorker
    {
        [Obfuscation(Exclude = false, Feature = "-rename")]
        internal enum ErrorType : byte
        {
            None = 0,
            RestoreOriginalFileFailed = 1,
            CreateBackupFailed = 2,
            MaximumPlayableArrangementLimitExceeded = 3,
            RepackageFailed = 4
        }

        /// <summary>
        /// List of songs to be repaired.
        /// </summary>
        private readonly List<SongData> _songsToRepair;

        /// <summary>
        /// Options for repairing songs.
        /// </summary>
        private readonly RepairOptions _repairOptions;

        /// <summary>
        /// Parent control for reporting progress and cancellation.
        /// </summary>
        private readonly SongManager _parentControl;

        /// <summary>
        /// Main form of the application for reporting progress and errors.
        /// </summary>
        private readonly frmMain _mainForm;

        /// <summary>
        /// StringBuilder to store error messages during the repair process.
        /// </summary>
        private StringBuilder _repairErrors = new StringBuilder();

        /// <summary>
        /// Lock object for thread-safe access to the number of repaired songs.
        /// </summary>
        private object _songsRepairedLock = new object();

        private int _numSongsRepairedBackingField = 0;

        /// <summary>
        /// Queue to store the paths of songs to be processed.
        /// </summary>
        ConcurrentQueue<string> _songPathsToProcess = null;

        private List<string> _messageLog = new List<string>();

        /// <summary>
        /// Queue to store Log messages from worker threads for reporting to the UI.
        /// </summary>
        private ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// Queue to store error messages from worker threads for reporting to the UI.
        /// </summary>
        private ConcurrentQueue<string> _errorMsgQueue = new ConcurrentQueue<string>();


        /// <summary>
        /// Number of songs repaired so far.
        /// </summary>
        private int _numSongsRepaired
        {
            get
            {
                lock (_songsRepairedLock) { return _numSongsRepairedBackingField; }
            }
            set
            {
                lock (_songsRepairedLock) { _numSongsRepairedBackingField = value; }
            }
        }

        /// <summary>
        /// Lock object for thread-safe access to the total number of songs.
        /// </summary>
        private object _numSongsLock = new object();


        private int _numTotalSongsBackingField;

        /// <summary>
        /// Total number of songs to be repaired.
        /// </summary>
        private int _numTotalSongs
        {
            get
            {
                lock (_numSongsLock) { return _numTotalSongsBackingField; }
            }
            set
            {
                lock (_numSongsLock) { _numTotalSongsBackingField = value; }
            }
        }

        /// <summary>
        /// Lock object for thread-safe access to the total number of songs skipped during repair.
        /// </summary>
        private object _numSongsSkippedLock = new object();

        private int _numSongsSkippedBackingField = 0;

        /// <summary>
        /// Total number of songs that were skipped during the repair process.
        /// </summary>
        private int _numSongsSkipped
        {
            get
            {
                lock (_numSongsSkippedLock) { return _numSongsSkippedBackingField; }
            }
            set
            {
                lock (_numSongsSkippedLock) { _numSongsSkippedBackingField = value; }
            }
        }

        /// <summary>
        /// Lock object for thread-safe access to the total number of songs skipped during repair.
        /// </summary>
        private object _numSongsFailedLock = new object();

        private int _numSongsFailedBackingField = 0;

        /// <summary>
        /// Total number of songs that were skipped during the repair process.
        /// </summary>
        private int _numSongsFailed
        {
            get
            {
                lock (_numSongsFailedLock) { return _numSongsFailedBackingField; }
            }
            set
            {
                lock (_numSongsFailedLock) { _numSongsFailedBackingField = value; }
            }
        }

        /// <summary>
        /// Lock object for thread-safe access to the download folder process handled flag.
        /// </summary>
        private object _downloadFolderProcessHandledLock = new object();

        /// <summary>
        /// Flag indicating whether the download folder processing has been handled.
        /// </summary>
        private bool _downloadFolderProcessHandled = false;

        /// <summary>
        /// Lock object for thread-safe access to the package writer.
        /// </summary>
        private object _packageWriterLock = new object();

        private bool _updateDataFlag = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepairSongsWorker"/> class.
        /// </summary>
        public RepairSongsWorker(List<SongData> songsToRepair, RepairOptions options, 
            SongManager parentControl, frmMain mainControl)
        {
            // Set default params
            WorkerSupportsCancellation = true;
            WorkerReportsProgress = true;
            _songsToRepair = songsToRepair;
            _repairOptions = options;
            _parentControl = parentControl;
            _mainForm = mainControl;
            _numTotalSongs = songsToRepair.Count;
            _numSongsRepaired = 0;
            _numSongsSkipped = 0;
            _numSongsFailed = 0;

            // Hook up the event handlers
            this.DoWork += repairSongsWorker_DoWork;
            this.ProgressChanged += repairSongsWorker_ProgressChanged;
            this.RunWorkerCompleted += repairSongsWorker_RunWorkComplete;

        }

        /// <summary>
        /// Method called when the worker reports on its progress.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void repairSongsWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Report progress to the parent control

            // Set the number of files processed
            _mainForm.SetToolStripMainMessage(String.Format("Files Processed: {0} of {1}", _numSongsRepaired, _numTotalSongs));

            // Set the status message
            _mainForm.SetToolStripStatusMessage(String.Format("Skipped: {0}  Failed: {1}", _numSongsSkipped, _numSongsFailed));

            // Set the main progress bar value
            int progressValue = (int)((float)(_numSongsRepaired + _numSongsSkipped + _numSongsFailed) / _numTotalSongs * 100);
            _mainForm.SetToolStripMainProgressBarValue(progressValue);
        }


        /// <summary>
        /// Method to Repair selected songs in the SongManager tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void repairSongsWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // First, we make sure 'dlc' folder is clean        
            FileTools.CleanDlcFolder();

            // Start the repair process by announcing the start and reporting initial progress numbers
            SMLog.Log("Applying selected repair options ...");
            this.ReportProgress(0, null);

            if (!CancellationPending)
            {
                string workerResults = String.Empty;

                // Enable multicore processing by default
                int coreCount = SysExtensions.GetCoreCount();
                AppSettings.Instance.MultiThread = (coreCount > 1) ? 1 : 0;
                Globals.Settings.SaveSettingsToFile(Globals.DgvCurrent);

                // Try to parse the song paths from the provided list of songs
                // This also initializes the _songPathsToProcess queue for workers to pull from
                if (!tryInitializeSongPaths(_songsToRepair))
                {
                    // TODO: Generate way to handle error messages better
                    // TODO: Generate error message for no songs found
                }
                // Else we continue 
                else
                {
                    // Initialize the worker threads
                    Thread[] threads = initializeRepairSongThreads(coreCount);

                    // Start and monitor the repairing of the songs
                    repairSongs(threads);

                    // Finalize the repair process
                    finalizeRepairSongs();

                    // If we have any errors, open the error log
                    if (_repairErrors.Length > 0)
                    {
                        RepairTools.ViewErrorLog();
                    }
                }                
            }
        }

        /// <summary>
        /// Method to initialize the threads for repairing songs.
        /// </summary>
        /// <param name="numThreads">Number of worker threads to be created for song repair.</param>
        /// <returns>The array of song repair worker threads.</returns>
        private Thread[] initializeRepairSongThreads(int numThreads)
        {

            Thread[] threads = new Thread[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                int currentCoreNum = i;
                threads[currentCoreNum] = new Thread(() =>
                {
                    // While there are song paths to process
                    while (_songPathsToProcess.Count > 0)
                    {
                        // Try to dequeue a song file path from the queue
                        if (_songPathsToProcess.TryDequeue(out var songFilePath))
                        {
                            // Process the song file path
                            repairSong(songFilePath, currentCoreNum);
                        }
                    }
                });
            }
            return threads;
        }

        /// <summary>
        /// Method that starts the repair worker threads and waits for them to complete.
        /// </summary>
        /// <param name="threads"></param>
        private void repairSongs(Thread[] threads)
        {
            // Start each thread
            foreach (var thread in threads)
            {
                thread.Start();
            }

            // Loop until all threads are complete
            bool isRunning = true;
            int numThreadsComplete = 0;
            while (isRunning)
            {
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

                // If we have new data to report, send it to the UI
                // This is a flag that is set by the worker threads spawned by this worker.
                if (_updateDataFlag)
                {
                    this.ReportProgress(0);
                    _updateDataFlag = false;
                }

                // Check if all tasks are completed
                numThreadsComplete = 0; // Reset the counter
                foreach (Thread t in threads)
                {
                    if (!t.IsAlive)
                    {
                        // Incerement the completed threads
                        numThreadsComplete++;

                        // Check if all threads are complete
                        if (numThreadsComplete == threads.Length)
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
            }
        }


        /// <summary>
        /// Method run when the worker has completed its work.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void repairSongsWorker_RunWorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            GenExtensions.InvokeIfRequired(_mainForm, delegate { Globals.TsLabel_Cancel.Visible = false; });

            /*
            if (e.Cancelled || Globals.TsLabel_Cancel.Text == "Canceling" || Globals.CancelBackgroundScan)
            {
                // bWorker.Abort(); // don't use abort
                SMLog.Log(Resources.UserCancelledProcess);
                Globals.TsLabel_MainMsg.Text = Resources.UserCancelled;
                Globals.TsLabel_StatusMsg.Text = "";
                Globals.WorkerFinished = Globals.Tristate.Cancelled;
            }
            else
            {
            */

            ReportProgress(100);
            //counterStopwatch.Stop();
            //SMLog.Log(String.Format("Finished " + WorkDescription.ToLower() + " took: {0}", counterStopwatch.Elapsed));
            Globals.WorkerFinished = Globals.Tristate.True;
            
            

            Globals.RescanProfileSongLists = false;
            Globals.RescanSetlistManager = false;
            Globals.RescanDuplicates = false;
            Globals.RescanSongManager = false;
            Globals.RescanRenamer = false;
            Globals.ReloadSetlistManager = true;
            Globals.ReloadDuplicates = true;
            Globals.ReloadRenamer = true;
            Globals.ReloadSongManager = true;
            Globals.ReloadProfileSongLists = true;
        }

        /// <summary>
        /// Tries to get the file paths of the songs to be repaired.
        /// </summary>
        /// <param name="songs">The songs to be repaired.</param>
        /// <returns>True if Song Paths were successfully retrieved.</returns>
        private bool tryInitializeSongPaths(List<SongData> songs)
        {
            List<string> srcFilePaths = new List<string>();

            if (_repairOptions.UsingOrgFiles)
            {
                SMLog.Log("Using [.org] files for all selected repairs ...");
                srcFilePaths = Directory.EnumerateFiles(Constants.RemasteredOrgFolder, "*" + Constants.EXT_ORG + "*").ToList();
                // only repair selected files (not all of them, doh!)
                List<string> selectedFileNames = FileTools.SongFilePaths(songs).Select(x => Path.GetFileNameWithoutExtension(x)).ToList();
                srcFilePaths = srcFilePaths.Where(x => selectedFileNames.Any(y => x.Contains(y))).ToList();
                if (!srcFilePaths.Any())
                {
                    SMLog.Log("<ERROR> Did not find any [.org] files ...");
                }
            }
            else if (_repairOptions.DLFolderProcess)
            {
                // TODO: maybe make sure new CDLC have been unzipped/unrar'd first
                // AppSettings.Instance.DownloadsDir is (must be) validated before being used by the bWorker
                foreach (var dlDirPath in AppSettings.Instance.MonitoredFolders)
                {
                    SMLog.Log("Repairing CDLC files from: " + dlDirPath + " ...");
                    if (Directory.Exists(dlDirPath))
                        srcFilePaths.AddRange(FileTools.GetSongListForAFolder(dlDirPath));
                    else
                        SMLog.Log("Folder: " + dlDirPath + " does not currently exist.");
                }              
            }
            else
            {
                srcFilePaths = FileTools.SongFilePaths(songs);
            }

            // If we have any valid song paths
            if (srcFilePaths.Any())
            {
                // Put all the songs to process into a concurrent queue
                _songPathsToProcess = new ConcurrentQueue<string>();
                foreach (var songFilePath in srcFilePaths)
                {
                    _songPathsToProcess.Enqueue(songFilePath);
                }
                return true;
            }
            else
            {
                return false;
            }          
        }

        private void queueLogMessage(string message)
        {
            _messageQueue.Enqueue(message);
        }

        private void queueLogMessage(int threadID, string message)
        {
            string formattedMsg = String.Format("[Thread ({0})]: {1}", threadID, message);
            _messageQueue.Enqueue(formattedMsg);
        }

        /// <summary>
        /// Checks if the current file should be skipped based on the repair options.
        /// </summary>
        /// <param name="srcFilePath">Song file path.</param>
        /// <param name="msg">Out message specifying why the song was skipped.</param>
        /// <returns>True if the song given has been skipped due to repair options.</returns>
        private bool trySkipProcessingSong(string srcFilePath, out string msg)
        {
            // Check if we need to skip the current file
            var isSkipped = false;
            msg = "";

            var isOfficialRepairedDisabled = FileTools.IsOfficialRepairedDisabled(srcFilePath);
            if (!String.IsNullOrEmpty(isOfficialRepairedDisabled))
            {
                if (isOfficialRepairedDisabled.Contains("Official"))
                {
                    msg = " - Skipped ODLC File";
                    isSkipped = true;
                }
                else if (isOfficialRepairedDisabled.Contains("Remastered") && _repairOptions.SkipRemastered)
                {
                    msg = " - Skipped Remastered File";
                    isSkipped = true;
                }
                else if (isOfficialRepairedDisabled.Contains("Disabled"))
                {
                    msg = " - Skipped Disabled File";
                    isSkipped = true;
                }
            }

            return isSkipped;
        }

        /// <summary>
        /// Method to repair the selected song.
        /// Contians the logic for calling specific repair methods and reporting their progress to the RepairSongsWorker.
        /// </summary>
        /// <param name="srcFilePaths"></param>
        /// <param name="threadID"></param>
        /// <returns></returns>
        private void repairSong(string srcFilePath, int threadID)
        {
            // Get the file name for logging purposes
            var fileName = Path.GetFileName(srcFilePath);

            // If we need to skip the current file
            if (trySkipProcessingSong(srcFilePath, out var msg))
            {
                // Log the message and increment the skipped songs count
                queueLogMessage(threadID, "Skipping file: " + fileName + " due to reason: " + msg);
                _numSongsSkipped++;

                // Raise flag to update data that isn't part of the Global.Log
                _updateDataFlag = true;

                return;
            }
            // Else we continue with the repair process
            else
            {
                // Announce the processing of the file
                queueLogMessage(threadID, "Processing: " + fileName);

                // REMASTER the CDLC file
                // If the remastering was successful
                if (remasterSong(srcFilePath, threadID, out ErrorType error))
                {
                    // Increment the number of repaired songs
                    _numSongsRepaired++;
                }
                else
                {
                    // Increment the number of songs that failed to repair
                    _numSongsFailed++;


                    var lines = _repairErrors.ToString().Split(new string[] { Environment.NewLine },
                        StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (lines.Last().ToLower().Contains("maximum"))
                        queueLogMessage(String.Format(" - CDLC exceeds playable arrangements limit ..."));
                    else
                        queueLogMessage(String.Format(" - CDLC is not repairable ..."));


                    // remove corrupt CDLC from SongCollection
                    var song = Globals.MasterCollection.FirstOrDefault(s => s.FilePath == srcFilePath);
                    int index = Globals.MasterCollection.IndexOf(song);
                    Globals.MasterCollection.AllowRemove = true;
                    Globals.MasterCollection.RemoveAt(index);
                    Globals.ReloadSongManager = true; // set quick reload flag
                }


                // move new CDLC from the downloads folder to the 'dlc/downloads' folder
                if (_repairOptions.DLFolderProcess)
                {
                    string downloadsDestFolder = AppSettings.Instance.DLMonitorDesinationFolder;

                    if (String.IsNullOrEmpty(downloadsDestFolder))
                        downloadsDestFolder = Path.Combine(Constants.Rs2CdlcFolder, "downloads");

                    if (!Directory.Exists(downloadsDestFolder))
                        Directory.CreateDirectory(downloadsDestFolder);

                    var destFilePath = Path.Combine(downloadsDestFolder, Path.GetFileName(srcFilePath));
                    GenExtensions.MoveFile(srcFilePath, destFilePath, false, true, _repairOptions.SkipDupes);
                    queueLogMessage(threadID, " - Moved new CDLC to: " + downloadsDestFolder);
                    Globals.ReloadSongManager = true; // set quick reload flag

                    // add new repaired downloads CDLC to the SongCollection
                    using (var browser = new PsarcBrowser(destFilePath))
                    {
                        var songInfo = browser.GetSongData();
                        Globals.MasterCollection.Add(songInfo.First());
                    }
                }

                // Raise the new data flag
                _updateDataFlag = true;

                return;
            }
               
        }

        /// <summary>
        /// Method to finalize the repair process after all songs have been processed.
        /// </summary>
        private void finalizeRepairSongs()
        {
            if (!String.IsNullOrEmpty(_repairErrors.ToString())) //failed > 0)
            {
                // error log can be turned into CSV file
                _repairErrors.Insert(0, "File Path, Error Message" + Environment.NewLine);
                _repairErrors.Insert(0, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + Environment.NewLine);
                using (TextWriter tw = new StreamWriter(Constants.RepairsErrorLogPath, true))
                {
                    tw.WriteLine(_repairErrors + Environment.NewLine);
                    tw.Close();
                }

                SMLog.Log(" - For file and error details, see: " + Constants.RepairsErrorLogPath);
            }

            if (_numSongsRepaired > 0)
            {
                SMLog.Log("CDLC repairs completed ...");
                Globals.ReloadSongManager = true;

                if (!Constants.DebugMode)
                    GenExtensions.CleanLocalTemp();
            }
            else
            {
                SMLog.Log("No CDLC were repaired ...");
            }
        }

        /// <summary>
        /// Remasters the CDLC by extracting, repairing, and repackaging it.
        /// </summary>
        /// <param name="srcFilePath">The file to remaster.</param>
        /// <returns>True if the remastering was successful.</returns>
        private bool remasterSong(string srcFilePath, int threadID, out ErrorType error)
        {
            error = ErrorType.None;

            // If the repair specifies to use original files
            if (_repairOptions.UsingOrgFiles)
            {
                // restore the original file
                srcFilePath = FileTools.RestoreOriginal(srcFilePath);

                // If the restore fails
                if (String.IsNullOrEmpty(srcFilePath))
                {
                    error = ErrorType.RestoreOriginalFileFailed;
                    return false;
                }
            }
            else
            {
                // If the repair fails to create a backup of the original file
                if (!FileTools.CreateBackupOfType(srcFilePath, Constants.RemasteredOrgFolder, Constants.EXT_ORG))
                {
                    error = ErrorType.CreateBackupFailed;
                    return false;
                }
            }

            try
            {
                // XML, JSON and SNG's must be regenerated
                // ArrangementIDs are stored in multiple place and all need to be updated
                // therefore we are going to unpack, apply repair, and repack
                queueLogMessage(threadID, " - Extracting CDLC Artifacts");

                // repair status variables
                bool addedDD = false;
                bool ddError = false;
                bool fixedMax5 = false;

                // Package the file into a standard format
                DLCPackageData packageData;
                using (var psarcOld = new ThreadSafePsarcPackager())
                {
                    packageData = psarcOld.ReadPackage(srcFilePath, _repairOptions.IgnoreMultitone, _repairOptions.FixLowBass);
                }

                // If MaxFiveArangements is selected, remove arrangements before remastering
                if (_repairOptions.RepairMaxFive)
                {
                    fixedMax5 = maxFiveArrangements(ref packageData);
                }

                // Check the max playable arrangement count
                var playableArrCount = packageData.Arrangements.Count(arr => arr.ArrangementType == ArrangementType.Guitar 
                                                                        || arr.ArrangementType == ArrangementType.Bass);
                if (playableArrCount > 5)
                {
                    error = ErrorType.MaximumPlayableArrangementLimitExceeded;
                    throw new CustomException("Maximum playable arrangement limit exceeded");
                }

                /// NOTE: This is about where I stop on the evening of 2025/4/17


                // update arrangement song info, i.e. always Remaster the CDLC (default)
                foreach (Arrangement arr in packageData.Arrangements)
                {
                    if (!_repairOptions.PreserveStats)
                    {
                        // generate new AggregateGraph
                        arr.SongFile = new RocksmithToolkitLib.DLCPackage.AggregateGraph.SongFile { File = "" };

                        // generate new Arrangement IDs
                        arr.Id = IdGenerator.Guid();
                        arr.MasterId = RandomGenerator.NextInt();
                    }

                    // skip vocal and showlight arrangements
                    if (arr.ArrangementType == ArrangementType.Vocal || arr.ArrangementType == ArrangementType.ShowLight)
                        continue;

                    // validate SongInfo
                    var songXml = Song2014.LoadFromFile(arr.SongXml.File);
                    songXml.ArtistName = packageData.SongInfo.Artist.GetValidAtaSpaceName();
                    songXml.Title = packageData.SongInfo.SongDisplayName.GetValidAtaSpaceName();
                    songXml.AlbumName = packageData.SongInfo.Album.GetValidAtaSpaceName();
                    songXml.ArtistNameSort = packageData.SongInfo.ArtistSort.GetValidSortableName();
                    songXml.SongNameSort = packageData.SongInfo.SongDisplayNameSort.GetValidSortableName();
                    songXml.AlbumNameSort = packageData.SongInfo.AlbumSort.GetValidSortableName();
                    songXml.AverageTempo = Convert.ToSingle(packageData.SongInfo.AverageTempo.ToString().GetValidTempo());
                    songXml.AlbumYear = packageData.SongInfo.SongYear.ToString().GetValidYear();

                    // update packageData with validated SongInfo
                    packageData.SongInfo.Artist = songXml.ArtistName;
                    packageData.SongInfo.SongDisplayName = songXml.Title;
                    packageData.SongInfo.Album = songXml.AlbumName;
                    packageData.SongInfo.ArtistSort = songXml.ArtistNameSort;
                    packageData.SongInfo.SongDisplayNameSort = songXml.SongNameSort;
                    packageData.SongInfo.AlbumSort = songXml.AlbumNameSort;
                    packageData.SongInfo.AverageTempo = (int)songXml.AverageTempo;
                    packageData.SongInfo.SongYear = Convert.ToInt32(songXml.AlbumYear);

                    // write updated xml arrangement
                    using (var stream = File.Open(arr.SongXml.File, FileMode.Create))
                        songXml.Serialize(stream, true);

                    // add comments back to xml arrangement   
                    Song2014.WriteXmlComments(arr.SongXml.File, arr.XmlComments);

                    // only add DD to NDD arrangements (unless user specifies otherwise)             
                    var mf = new ManifestFunctions(GameVersion.RS2014);
                    var maxDD = mf.GetMaxDifficulty(songXml);

                    // If the user has selected to add DD
                    // and the max DD value is zero OR the user has selected to overwrite DD
                    if (_repairOptions.AddDD && (maxDD == 0 || _repairOptions.OverwriteDD))
                    {
                        // phrase length should be at least 8 to fix chord density bug
                        if (_repairOptions.PhraseLength < 8) throw new Exception("DD Phrase Length less than eight."); // belt and suspenders code

                        var consoleOutput = String.Empty;
                        var result = DynamicDifficulty.ApplyDD(arr.SongXml.File, _repairOptions.PhraseLength, 
                            _repairOptions.RemoveSustain, _repairOptions.RampUpPath, _repairOptions.CfgPath, out consoleOutput, true);
                        if (result == -1)
                            throw new CustomException("ddc.exe is missing"); // may want to exit the application if this happens

                        if (String.IsNullOrEmpty(consoleOutput))
                        {
                            queueLogMessage(threadID, " - Added DD to " + arr);
                            addedDD = true;
                        }
                        else
                        {
                            queueLogMessage(threadID, " - " + arr + " DDC console output: " + consoleOutput);
                            _repairErrors.AppendLine(String.Format("{0}, Could not apply DD to: {1}", srcFilePath, arr));
                            ddError = true;
                        }
                    }

                    if (_repairOptions.AdjustScrollSpeed)
                        arr.ScrollSpeed = (int)(_repairOptions.ScrollSpeed * 10);

                    // put arrangement comments in correct order
                    Song2014.WriteXmlComments(arr.SongXml.File);
                }

                // add comments to ToolkitInfo to identify Remastered CDLC
                packageData = packageData.AddPackageComment(Constants.TKI_REMASTER);

                // add comments to ToolkitInfo to identify repairs made by CFSM
                if (!_repairOptions.PreserveStats)
                    packageData = packageData.AddPackageComment(Constants.TKI_ARRID);

                if (_repairOptions.AddDD && addedDD)
                    packageData = packageData.AddPackageComment(Constants.TKI_DDC);

                if (_repairOptions.RepairMaxFive && fixedMax5)
                {
                    packageData = packageData.AddPackageComment(Constants.TKI_MAX5);
                    queueLogMessage(threadID, " - Applied MaxFive Repairs");
                }

                // add default package version if missing
                if (String.IsNullOrEmpty(packageData.ToolkitInfo.PackageVersion))
                {
                    packageData.ToolkitInfo.PackageVersion = "1";
                    queueLogMessage(threadID, " - Fixed Missing PackageVersion");
                }
                else
                    packageData.ToolkitInfo.PackageVersion = packageData.ToolkitInfo.PackageVersion.GetValidVersion();

                // apply default Cherub Rock AppId
                if (_repairOptions.FixAppId)
                {
                    packageData.AppId = "248750";
                    SMLog.Log(threadID, " - Applied Default AppId");
                }

                // validate packageData.Name (important)
                packageData.Name = packageData.Name.GetValidKey(); // DLC Key                 

                // log repair status
                queueLogMessage(threadID, String.Format(" - {0}", _repairOptions.PreserveStats ? "Preserved Song Stats" : "Reset Song Stats"));

                if (_repairOptions.AdjustScrollSpeed)
                    queueLogMessage(threadID, " - Adjusted Scroll Speed: " + _repairOptions.ScrollSpeed);

                queueLogMessage(threadID, " - Repackaging Remastered CDLC");

                // use main audio as the preview audio (quick fix old school method if preview is missing)
                if (packageData.OggPreviewPath == null || !File.Exists(packageData.OggPreviewPath))
                {
                    var previewPath = String.Format(Path.Combine(Path.GetDirectoryName(packageData.OggPath), Path.GetFileNameWithoutExtension(packageData.OggPath)) + "_preview.wem");
                    File.Copy(packageData.OggPath, previewPath);
                    packageData.OggPreviewPath = previewPath;
                    queueLogMessage(threadID, " - Fixed missing preview audio");
                }

                try
                {
                    // regenerates repaired XML, JSON, and SNG files and repackages               
                    using (var psarcNew = new ThreadSafePsarcPackager(true)) 
                    {
                        psarcNew.WritePackage(srcFilePath, packageData);
                    }
                }
                catch (Exception ex)
                {
                    error = ErrorType.RepackageFailed;
                    throw new Exception("<ERROR> Writing Package: " + ex.Message);
                }

                if (_repairOptions.UsingOrgFiles)
                    queueLogMessage(threadID, " - Used [" + Constants.EXT_ORG + "] File");

                if (!ddError)
                    queueLogMessage(threadID, " - Repair was successful ...");
                else
                    queueLogMessage(threadID, " - Repair was successful, but DD could not be applied ...");

                if (!_repairOptions.DLFolderProcess)
                {
                    // update/rescan just one CDLC in the bound SongCollection
                    // gets contents of archive after it has been repaired
                    using (var browser = new PsarcBrowser(srcFilePath))
                    {
                        var songInfo = browser.GetSongData();
                        var song = Globals.MasterCollection.FirstOrDefault(s => s.FilePath == srcFilePath);
                        int index = Globals.MasterCollection.IndexOf(song);
                        Globals.MasterCollection[index] = songInfo.First();
                    }
                }
            }
            catch (CustomException ex) // (e1)
            {
                string errorMsg = "<ERROR> (e1) " + ex.Message.Replace("\r\n", "");
                queueLogMessage(errorMsg);

                if (ex.Message.Contains("Maximum"))
                {
                    //  copy (org) to maximum (max), delete backup (org), delete original
                    var properExt = Path.GetExtension(srcFilePath);
                    var orgFilePath = String.Format(@"{0}{1}{2}", Path.Combine(Constants.RemasteredOrgFolder, Path.GetFileNameWithoutExtension(srcFilePath)), Constants.EXT_ORG, properExt).Trim();
                    var maxFilePath = String.Format(@"{0}{1}{2}", Path.Combine(Constants.RemasteredMaxFolder, Path.GetFileNameWithoutExtension(srcFilePath)), Constants.EXT_MAX, properExt).Trim();
                    File.SetAttributes(orgFilePath, FileAttributes.Normal);
                    File.SetAttributes(srcFilePath, FileAttributes.Normal);
                    File.Copy(orgFilePath, maxFilePath, true);
                    File.Delete(orgFilePath);
                    File.Delete(srcFilePath);
                    _repairErrors.AppendLine(String.Format("{0}, Maximum playable arrangement limit exceeded", maxFilePath));

                    return false;
                }

                _repairErrors.AppendLine("CustomException thrown: " + errorMsg);
            }
            catch (Exception ex) // (e2)
            {
                queueLogMessage("<ERROR> (e2) " + ex.Message.Replace("\r\n", ""));

                //  copy (org) to corrupt (cor), delete backup (org), delete original
                var properExt = Path.GetExtension(srcFilePath);
                var orgFilePath = String.Format(@"{0}{1}{2}", Path.Combine(Constants.RemasteredOrgFolder, Path.GetFileNameWithoutExtension(srcFilePath)), Constants.EXT_ORG, properExt).Trim();
                var corFilePath = String.Format(@"{0}{1}{2}", Path.Combine(Constants.RemasteredCorFolder, Path.GetFileNameWithoutExtension(srcFilePath)), Constants.EXT_COR, properExt).Trim();
                File.SetAttributes(orgFilePath, FileAttributes.Normal);
                File.SetAttributes(srcFilePath, FileAttributes.Normal);
                File.Copy(orgFilePath, corFilePath, true);
                File.Delete(orgFilePath);
                File.Delete(srcFilePath);
                _repairErrors.AppendLine(String.Format("{0}, Corrupt CDLC", corFilePath));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes arrangements from the package data to limit the number of playable arrangements to a maximum of 5.
        /// </summary>
        /// <param name="packageData">The package data to process.</param>
        /// <returns>The package data with a maximum of 5 arrangements.</returns>
        private bool maxFiveArrangements(ref DLCPackageData packageData)
        {
            const int playableArrLimit = 5; // one based limit
            var playableArrCount = packageData.Arrangements.Count(arr => arr.ArrangementType == ArrangementType.Guitar 
                                                                    || arr.ArrangementType == ArrangementType.Bass);

            // if the package has less than 5 playable arrangements, no need to repair
            if (!_repairOptions.IgnoreStopLimit && playableArrCount <= playableArrLimit)
            {
                return true;

            }
            else
            {
                var removalNdx = playableArrCount - playableArrLimit; // zero based index
                var packageDataKept = new DLCPackageData();
                packageDataKept.Arrangements = new List<RocksmithToolkitLib.DLCPackage.Arrangement>();

                foreach (var arr in packageData.Arrangements)
                {
                    // skip vocal and showlight arrangements
                    if (arr.ArrangementType == ArrangementType.Vocal || arr.ArrangementType == ArrangementType.ShowLight)
                        continue;

                    var isKept = true;
                    var songXml = Song2014.LoadFromFile(arr.SongXml.File);
                    var mf = new ManifestFunctions(GameVersion.RS2014);

                    if (mf.GetMaxDifficulty(songXml) == 0 && _repairOptions.RemoveNDD) isKept = false;
                    if (arr.ArrangementType == ArrangementType.Bass && _repairOptions.RemoveBass) isKept = false;
                    if (arr.ArrangementType == ArrangementType.Guitar && _repairOptions.RemoveGuitar) isKept = false;
                    if (arr.BonusArr && _repairOptions.RemoveBonus) isKept = false;
                    if (arr.Metronome == Metronome.Generate && _repairOptions.RemoveMetronome) isKept = false;

                    if (isKept || removalNdx == 0)
                    {
                        SMLog.Log(" - Kept Arrangement: " + arr);
                        packageDataKept.Arrangements.Add(arr);

                        if (packageDataKept.Arrangements.Count == playableArrLimit)
                        {
                            SMLog.Log(" - Kept first [" + playableArrLimit + "] arrangements matching the repair criteria");
                            break;
                        }
                    }
                    else
                    {
                        SMLog.Log(" - Removed Arrangement: " + arr);
                        if (!_repairOptions.IgnoreStopLimit)
                            removalNdx--;
                    }
                }

                // add back vocals and showlights arrangements
                foreach (var arr in packageData.Arrangements)
                    if (arr.ArrangementType == ArrangementType.Vocal || arr.ArrangementType == ArrangementType.ShowLight)
                        packageDataKept.Arrangements.Add(arr);

                // replace original arrangements with kept arrangements
                packageData.Arrangements = packageDataKept.Arrangements;
                return true;
            }
            
            
        }
    }
}
