using CustomsForgeSongManager.DataObjects;
using CustomsForgeSongManager.Forms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomsForgeSongManager.Workers
{
    /// <summary>
    /// Worker that shows the progress panel UI window while it runs its task.
    /// </summary>
    public abstract class ProgressPanelWorker : BackgroundWorker
    {
        private static int _workerIDCounter = 0;

        // Bool and lock to initialize the form
        private static bool _isInitialized = false;

        //
        private static frmProgressPanel _progressPanel = null;
        private static bool _panelIsHidden = true;
        private static object _panelVisibilityLock = new object();

        private static Control _mainForm = null;

        protected string _processName = string.Empty;
        public string ProcessName {  get { return _processName; } }

        /// <summary>
        /// The queue of currently running workers.
        /// </summary>
        private static BindingList<ProgressPanelWorker> _workers = new BindingList<ProgressPanelWorker>();
        private static ReaderWriterLockSlim _workerListLock = new ReaderWriterLockSlim();
        private static BindingSource _workersBindingSource = null;


        public int WorkerID { get; private set; } = -1;

        public ProgressPanelWorker()
        {
            WorkerSupportsCancellation = true;
            WorkerReportsProgress = true;
            WorkerID = _workerIDCounter++;

            this.DoWork += progressPanelWorker_DoWork;
            this.RunWorkerCompleted += progressPanelWorker_OnWorkComplete;

        }

        /// <summary>
        /// Handles the showing of the ProgressPanel form if needed, 
        /// then executes the OnDoWork function provided by implementing classes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void progressPanelWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool showWindow = false;

            // Get write lock for worker list
            _workerListLock.EnterWriteLock();

            // If I am the first worker to queue
            if (_workers.Count == 0)
            {
                // I am also the one who will call Show on the ProgressPanel form
                showWindow = true;
            }

            _workers.Add(this);

            _workerListLock.ExitWriteLock();

            if (showWindow)
            {
                _mainForm.Invoke(new Action(() =>
                {
                    _progressPanel.BringToFront();
                    _progressPanel.Show();
                }));
            }
        }

        private void progressPanelWorker_OnWorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            bool hidePanel = false;

            // Get the lock for the worker list
            _workerListLock.EnterWriteLock(); 

            // Remove myself from queue
            _workers.Remove(this);

            // If I am the last worker in the queue
            if (_workers.Count == 0)
            {
                // I will also hide the Progress Panel
                hidePanel = true;
            }

            _workerListLock.ExitWriteLock();

            if (hidePanel)
            {
                _mainForm.Invoke(new Action(() =>
                {
                    _progressPanel.Hide();
                }));
            }
        }

        /// <summary>
        /// Override method. Worker is the same if they have the same WorkerID.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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


        public override int GetHashCode()
        {
            return WorkerID.GetHashCode();
        }

        /// <summary>
        /// Initializes frmProgressPanel.
        /// </summary>
        /// <param name="mainFormControl"></param>
        public static void InitializeProgressPanelWindow(Control mainFormControl)
        {
            // Init the workers binding source
            _workersBindingSource = new BindingSource { DataSource = _workers };

            // Initialize the ProgressPanel form
            _progressPanel = new frmProgressPanel(mainFormControl, _workersBindingSource);
            _progressPanel.WindowState = FormWindowState.Normal;

            _mainForm = mainFormControl;

            
        }
    }
    

    
}
