using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomsForgeSongManager.Workers
{
    public class ControllableWorker : BackgroundWorker
    {
        private static int _workerIDCounter = 0;

        public int WorkerID { get; private set; } = -1;

        public ControllableWorker()
        {
            WorkerSupportsCancellation = true;
            WorkerReportsProgress = true;
            WorkerID = _workerIDCounter++;
        }
    }
    

    
}
