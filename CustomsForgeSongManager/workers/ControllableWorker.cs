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
        public ControllableWorker()
        {
            WorkerSupportsCancellation = true;
            WorkerReportsProgress = true;
        }
    }
    

    
}
