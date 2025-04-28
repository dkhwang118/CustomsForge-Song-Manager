using CustomsForgeSongManager.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomsForgeSongManager.AppEvents.Event
{
    public class SongMasterListChangedEvent : AppEvent
    {

        /// <summary>
        /// The new settings that have been applied.
        /// </summary>
        public List<SongData> SongData { get; }

        /// <summary>
        /// Constructor for the SongMasterListChangedEvent class.
        /// </summary>
        public SongMasterListChangedEvent(List<SongData> newSongData)
        {
            SongData = newSongData;
        }
    }
}
