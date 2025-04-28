using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomsForgeSongManager.AppEvents
{
    /// <summary>
    /// Class that is an intermediary for classes wishing to consume events produced by other classes.
    /// This class holds the queue of events to be processed and the thread that processes them.
    /// </summary>
    public class EventConsumerTunnel : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        private IEventConsumer _eventConsumer = null;

        /// <summary>
        /// Queue of events to be processed.
        /// </summary>
        private BlockingCollection<AppEvent> _eventQueue = new BlockingCollection<AppEvent>();

        /// <summary>
        /// The thread that processes events from the queue.
        /// </summary>
        private Thread _processingThread = null;

        /// <summary>
        /// Flag to indicate whether the processing thread is currently running.
        /// </summary>
        private bool _processingThreadRunning = false;

        /// <summary>
        /// Flag to indicate whether this object has been disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Constructor for the EventConsumer class.
        /// </summary>
        public EventConsumerTunnel(IEventConsumer consumer) 
        {   
            _eventConsumer = consumer;
            startProcessingThread();
        }

        /// <summary>
        /// Initializes and starts the thread that processes incoming log messages.
        /// </summary>
        private void startProcessingThread()
        {
            // Define the processing thread's behavior
            _processingThread = new Thread(() =>
            {
                while (_processingThreadRunning)
                {
                    // Reset the message object
                    AppEvent ae = null;

                    // Try to take a message from the queue
                    try
                    {
                        ae = _eventQueue.Take();
                    }
                    catch (InvalidOperationException)
                    {
                        // The collection has been marked as complete
                        // => Do nothing, as we are only going to mark this as complete when
                        // the application is closing.
                    }

                    // If we have a message, process it
                    if (ae != null)
                    {
                        // Process the message (e.g., write to target controls)
                        _eventConsumer.HandleEvent(ae);
                    }
                }
            });

            // Set the flag to indicate that the processing thread is running
            _processingThreadRunning = true;

            // Start the processing thread
            _processingThread.Start();
        }

        /// <summary>
        /// Enqueues an event to be processed by this consumer.
        /// </summary>
        /// <param name="appEvent">The event to be processed by the consumer.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void EnqueueEvent(AppEvent appEvent)
        {
            if (appEvent == null)
                throw new ArgumentNullException(nameof(appEvent));
            _eventQueue.Add(appEvent);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._disposed)
            {
                // If disposing equals true, dispose all managed resources
                if (disposing)
                {
                    // Stop the processing thread
                    _processingThreadRunning = false;

                    // Mark the collection as complete to unblock the processing thread
                    _eventQueue.CompleteAdding();
                    _processingThread = null;
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.



                // Note disposing has been done.
                _disposed = true;
            }
        }

        // Use C# finalizer syntax for finalization code.
        // This finalizer will run only if the Dispose method
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide finalizer in types derived from this class.
        ~EventConsumerTunnel()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(disposing: false) is optimal in terms of
            // readability and maintainability.
            Dispose(disposing: false);
        }
    }
}
