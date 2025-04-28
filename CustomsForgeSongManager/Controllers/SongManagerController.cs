/**
 *
 * No Copyright yet. 2025
 * Author: David K. Hwang
 * 
*/

using CustomsForgeSongManager.DataObjects;
using CustomsForgeSongManager.Forms;
using CustomsForgeSongManager.LocalTools;
using CustomsForgeSongManager.UControls;
using CustomsForgeSongManager.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;

namespace CustomsForgeSongManager.Controllers
{
    /// <summary>
    /// Controller for the SongManager Tab View.
    /// </summary>
    public class SongManagerController
    {
        /// <summary>
        /// Singleton instance of the SongManagerController.
        /// </summary>
        private static SongManagerController _instance;

        /// <summary>
        /// Lock object for thread safety when accessing the singleton instance.
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// The SongManager control that this controller manages.
        /// </summary>
        private SongManager _songManagerControl = null;

        /// <summary>
        /// The main form of the application.
        /// The main form currently contains the progress controls we need to use to update the user
        /// on the progress of the worker.
        /// </summary>
        private frmMain _mainForm = null;

        /// <summary>
        /// List of active workers spawned by this controller operations.
        /// </summary>
        private List<ControllableWorker> _activeWorkers = new List<ControllableWorker>();

        /// <summary>
        /// Private constructor to prevent instantiation from outside.
        /// </summary>
        private SongManagerController() { }

        /// <summary>
        /// Gets the singleton instance of the SongManagerController.
        /// </summary>
        public static SongManagerController Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new SongManagerController();
                    return _instance;
                }        
            }
        }

        /// <summary>
        /// Sets the SongManager control to be used by the controller.
        /// </summary>
        /// <param name="songManagerControl"></param>
        public void SetSongManagerControl(SongManager songManagerControl)
        {
            _songManagerControl = songManagerControl;
        }

        /// <summary>
        /// Sets the MainForm control to be used by the controller.
        /// </summary>
        /// <param name="mainForm"></param>
        public void SetMainFormControl(frmMain mainForm)
        {
            _mainForm = mainForm;
        }

        /// <summary>
        /// Repairs the selected songs in the SongManager tab.
        /// </summary>
        /// <param name="songsToRepair">The songs to repair.</param>
        /// <param name="options">The song repair options selected.</param>
        public void RepairSongs(List<SongData> songsToRepair, RepairOptions options)
        {
            // Create the worker
            RepairSongsWorker worker = new RepairSongsWorker(songsToRepair, options, 
                _songManagerControl, _mainForm);

            // Start it working
            worker.RunWorkerAsync();

            // Add the worker to the list of active workers
            _activeWorkers.Add(worker);
        }

        /// <summary>
        /// Searches for and parses the songs in the folders specified by the user's settings.
        /// </summary>
        public void ParseSongs()
        {
            // Create the worker
            ParseSongsWorker worker = new ParseSongsWorker();

            // Start it working
            worker.RunWorkerAsync();

            // Add the worker to the list of active workers
            _activeWorkers.Add(worker);
        }
    }
}
