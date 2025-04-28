using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomsForgeSongManager.AppEvents;

namespace CustomsForgeSongManager.AppEvents
{
    /// <summary>
    /// Abstract superclass for classes wishing to produce events for other classes to consume.
    /// </summary>
    public abstract class EventProducer
    {
        /// <summary>
        /// List of event consumers that are registered to receive events.
        /// </summary>
        private List<EventConsumerTunnel> _eventConsumers = new List<EventConsumerTunnel>();

        /// <summary>
        /// Constructor for the EventProducer class.
        /// </summary>
        protected EventProducer() { }

        protected void RegisterConsumer(EventConsumerTunnel consumer)
        {
            if (consumer == null)
                throw new ArgumentNullException(nameof(consumer));
            _eventConsumers.Add(consumer);
        }

        /// <summary>
        /// Unregisters a consumer from receiving events.
        /// </summary>
        /// <param name="consumer"></param>
        /// <exception cref="ArgumentNullException"></exception>
        protected void UnregisterConsumer(EventConsumerTunnel consumer)
        {
            if (consumer == null)
                throw new ArgumentNullException(nameof(consumer));
            _eventConsumers.Remove(consumer);
        }

        /// <summary>
        /// Notifies all registered consumers of an event.
        /// </summary>
        /// <param name="appEvent">The event to send to registered consumers.</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected void NotifyConsumers(AppEvent appEvent)
        {
            if (appEvent == null)
                throw new ArgumentNullException(nameof(appEvent));
            foreach (var consumer in _eventConsumers)
            {
                consumer.EnqueueEvent(appEvent);
            }
        }
    }
}
