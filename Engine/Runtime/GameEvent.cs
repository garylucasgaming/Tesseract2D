using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Runtime
{
    // a blank interface marking any structure or class as a valid event payload. 
    public interface IGameEventData
    {
    }

    // a centralized typesafe highway handling global messaging across the runtime
    public static class GameEvent
    {

        // A dictionary mapping an Event Type to a list of action callbacks that handle that type.
        // We use object because different events have different generic argument types.
        private static readonly Dictionary<Type, List<object>> _listeners = new();

        /// <summary>
        /// Subscribes a method to listen for a specific type of GameEvent.
        /// </summary>
        /// <typeparam name="T">The type of event data to listen for.</typeparam>
        /// <param name="callback">The method to execute when the event fires.</param>
        public static void Subscribe<T>(Action<T> callback) where T : IGameEventData
        {
            Type eventType = typeof(T);

            if(!_listeners.ContainsKey(eventType))
            {
                _listeners[eventType] = new List<object>();
            }

            _listeners[eventType].Add(callback);
        }

        /// <summary>
        /// Unsubscribes a method so it stops listening to a specific type of GameEvent.
        /// Prevents memory leaks when scenes or managers are destroyed.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> callback) where T : IGameEventData
        {
            Type eventType = typeof(T);

            if(_listeners.TryGetValue(eventType, out var list))
            {
                list.Remove(callback);

                if(list.Count == 0)
                {
                    _listeners.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// Broadcasts an event payload instantly to every registered listener.
        /// </summary>
        /// <typeparam name="T">The type of event data being sent.</typeparam>
        /// <param name="eventData">The actual structural data package containing details about the event.</param>
        public static void Raise<T>(T eventData) where T : IGameEventData
        {
            Type eventType = typeof(T);

            if(_listeners.TryGetValue(eventType, out var list))
            {
                // We loop backwards so if a listener unsubscribes itself during the event callback, 
                // it won't crash our collection iteration.
                for(int i = list.Count - 1; i >= 0; i--)
                {
                    if(list[i] is Action<T> callback)
                    {
                        callback.Invoke(eventData);
                    }
                }
            }
        }

        /// <summary>
        /// Completely clears all event listeners. Essential for clean scene transitions via the SceneDirector.
        /// </summary>
        public static void ClearAllListeners()
        {
            _listeners.Clear();
            System.Diagnostics.Debug.WriteLine("[Runtime] GameEvent highway completely flushed cleanly.");
        }
    }
}
