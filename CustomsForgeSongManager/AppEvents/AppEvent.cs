using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomsForgeSongManager.AppEvents
{
    /// <summary>
    /// Abstract superclass for classes that define application events.
    /// </summary>
    public abstract class AppEvent
    {
        /// <summary>
        /// The time when the event occurred.
        /// </summary>
        public DateTime EventTime { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected AppEvent()
        {
            EventTime = DateTime.Now;
        }
    }   
}
