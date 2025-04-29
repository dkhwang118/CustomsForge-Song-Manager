using CustomsForgeSongManager.Forms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomsForgeSongManager.Workers
{
    /// <summary>
    /// Worker that shows the progress panel UI window while it runs its task.
    /// </summary>
    public class ProgressPanelWorker : BackgroundWorker
    {
        private static int _workerIDCounter = 0;

        // Bool and lock to initialize the form
        private static bool _isInitialized = false;
        private static object _globalLock = new object();

        private static frmProgressPanel _progressPanel = null;
        private static bool _panelIsHidden = true;

        /// <summary>
        /// The queue of currently running workers.
        /// </summary>
        private static List<ProgressPanelWorker> _workers = new List<ProgressPanelWorker>();
        private static ReaderWriterLockSlim _workerListLock = new ReaderWriterLockSlim();

        public int WorkerID { get; private set; } = -1;

        public ProgressPanelWorker()
        {
            WorkerSupportsCancellation = true;
            WorkerReportsProgress = true;
            WorkerID = _workerIDCounter++;

            initIfNecessary();

            this.DoWork += initWindowIfNeeded_DoWork;
            this.RunWorkerCompleted += closeFormIfNeeded_OnWorkComplete;

            // Put this background worker into the queue of workers in RAM
            _workerListLock.EnterWriteLock();
            _workers.Add(this);
            _workerListLock.ExitWriteLock();
        }

        private void initWindowIfNeeded_DoWork(object sender, DoWorkEventArgs e)
        {
            if (_panelIsHidden)
            {
                lock (_globalLock)
                {
                    if (_panelIsHidden)
                    {
                        _progressPanel.Show();
                        _panelIsHidden = false;
                    }
                }
            }
        }

        private void closeFormIfNeeded_OnWorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            // If hidden and I'm *potentially* the last worker
            if (_panelIsHidden && _workers.Count == 1)
            {
                // Get the lock and check again
                lock (_globalLock)
                {
                    // Get the lock for the worker list
                    _workerListLock.EnterWriteLock();

                    if (_panelIsHidden && _workers.Count == 1)
                    {
                        _progressPanel.Hide();
                        _panelIsHidden = true;

                        // Remove myself from queue

                        _workers.Remove(this);
                    }

                    _workerListLock.ExitWriteLock();
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                if (obj is ProgressPanelWorker)
                {
                    if ((obj as ProgressPanelWorker).WorkerID.Equals(this.WorkerID))
                    {
                        return true;
                    }
                }
            }
            return base.Equals(obj);
        }

        private static void initIfNecessary()
        {
            if (!_isInitialized)
            {
                lock (_globalLock)
                {
                    if (!_isInitialized)
                    {
                        // Initialize the ProgressPanel form
                        _progressPanel = new frmProgressPanel();
                        _progressPanel.Hide();
                        _isInitialized = true;
                    }
                }
            }
        }

        public static void InitializeForm()
        {
            initIfNecessary();
        }
    }
    

    
}
