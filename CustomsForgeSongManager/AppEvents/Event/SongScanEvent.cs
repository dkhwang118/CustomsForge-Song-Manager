using CustomsForgeSongManager.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomsForgeSongManager.AppEvents.Event
{
    public class SongScanEvent : AppEvent
    {
        /// <summary>
        /// The new settings that have been applied.
        /// </summary>
        public List<SongData> SongData { get; }

        /// <summary>
        /// True if a Scan has been initiated to create a master list of songs.
        /// </summary>
        public bool SongScanStarting { get; } = false;

        /// <summary>
        /// True if the Scan is complete. 
        /// When true, SongData then has the list of scanned Songs.
        /// </summary>
        public bool SongScanComplete { get; } = false;

        /// <summary>
        /// Constructor for the SongMasterListChangedEvent class.
        /// </summary>
        public SongScanEvent(bool scanStarting = false, bool scanComplete = false, List<SongData> newSongData = null)
        {
            SongScanStarting = scanStarting;
            SongScanComplete = scanComplete;
            if (newSongData != null)
            {
                SongData = newSongData;
                SongScanComplete = true;
            }
        }
    }
}
