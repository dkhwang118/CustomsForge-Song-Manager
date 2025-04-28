using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomsForgeSongManager.AppEvents
{
    /// <summary>
    /// Interface for classes that consume application events.
    /// </summary>
    public interface IEventConsumer
    {
        void HandleEvent(AppEvent appEvent);
    }
}
