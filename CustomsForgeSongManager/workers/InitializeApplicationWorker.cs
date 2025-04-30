using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomsForgeSongManager.Workers
{
    /// <summary>
    /// Background worker class used to initialize application data 
    /// so that frmMain can fully construct without having to do too much processing.
    /// </summary>
    public class InitializeApplicationWorker : ProgressPanelWorker
    {


        public InitializeApplicationWorker()
        {
            // Hook event handlers
            this.DoWork += initAppWorker_DoWork;
            this.RunWorkerCompleted += initAppWorker_RunWorkComplete;
        }

        /// <summary>
        /// Method that runs when the background worker is started.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void initAppWorker_DoWork(object sender, DoWorkEventArgs e)
        {

        }

        private void initAppWorker_RunWorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
        }
    }
}
