using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomsForgeSongManager.LocalTools
{
    /// <summary>
    /// Class that attempts to automatically control and balance the usage of its instance and others 
    /// across the current CPU.
    /// </summary>
    public class LoadBalancingThread
    {

        private static ConcurrentBag<LoadBalancingThread> _activeThreads = new ConcurrentBag<LoadBalancingThread>();

        private Func<bool> _mainMethod;

        private Thread _thread;

        public LoadBalancingThread(Func<bool> mainMethod)
        {
            this._mainMethod = mainMethod;
        }

        public void Start()
        {
            // Create the thread
            //_thread = 
        }
    }
}
