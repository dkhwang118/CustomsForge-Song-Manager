using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomsForgeSongManager.DataObjects;

namespace CustomsForgeSongManager.AppEvents.Event
{
    /// <summary>
    /// Event class that represents a change in application settings.
    /// </summary>
    public class SettingsChangedEvent : AppEvent
    {
        /// <summary>
        /// The new settings that have been applied.
        /// </summary>
        public AppSettings Settings { get; }

        /// <summary>
        /// Constructor for the SettingsChangedEvent class.
        /// </summary>
        /// <param name="settings"></param>
        public SettingsChangedEvent(AppSettings settings)
        {
            Settings = settings;
        }

    }
}
