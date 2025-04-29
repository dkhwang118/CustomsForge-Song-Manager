using CustomsForgeSongManager.AppEvents.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomsForgeSongManager.AppEvents
{
    public static class AppEventManager
    {
        /// <summary>
        /// Dictionary that maps events to their respective consumers.
        /// </summary>
        private static Dictionary<Type, List<EventConsumerTunnel>> _eventConsumersByEvent = new Dictionary<Type, List<EventConsumerTunnel>>();

        /// <summary>
        /// Lock object to ensure thread safety when adding/removing event consumers.
        /// </summary>
        private static ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        /// <summary>
        /// Adds an event consumer to the list of active consumers.
        /// </summary>
        /// <param name="consumer">The event consumer to add.</param>
        public static void RegisterEventConsumer(IEventConsumer consumer, Type eventType)
        {
            _lock.EnterWriteLock();

            // If we don't have a list started for this event type yet, create one
            if (!_eventConsumersByEvent.ContainsKey(eventType))
            {
                _eventConsumersByEvent[eventType] = new List<EventConsumerTunnel>();
            }

            // Create the EventConsumerTunnel object
            EventConsumerTunnel consumerTunnel = new EventConsumerTunnel(consumer);

            // Add the consumer tunnel to the list of consumers for this event type
            _eventConsumersByEvent[eventType].Add(consumerTunnel);

            _lock.ExitWriteLock();
            
        }

        public static void RaiseEvent(AppEvent appEvent)
        {
            _lock.EnterReadLock();

            // Check if the event type is registered
            if (_eventConsumersByEvent.ContainsKey(appEvent.GetType()))
            {
                // Notify all consumers of the event
                foreach (var consumer in _eventConsumersByEvent[appEvent.GetType()])
                {
                    consumer.EnqueueEvent(appEvent);
                }
            }

            _lock.ExitReadLock();
        }

        /// <summary>
        /// Disposes of all EventConsumer resources used by the application.
        /// </summary>
        public static void ShutDown()
        {
            // Get all the EventConsumerTunnels and dispose of them
            _lock.EnterWriteLock();

            foreach (List<EventConsumerTunnel> consumerList in _eventConsumersByEvent.Values)
            {
                foreach (EventConsumerTunnel consumerTunnel in consumerList)
                {
                    consumerTunnel.Dispose();
                }
            }

            _lock.ExitWriteLock();
        }
    }
}
