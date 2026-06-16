using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedMan.Core
{
    /// <summary>
    /// Central event system. Static class — globally accessible without an Instance.
    /// Decouples publishers from subscribers. No system needs to know another directly.
    ///
    /// Usage:
    ///   EventBus.Publish(new OnGameStateChangedEvent(...));
    ///   EventBus.Subscribe[OnGameStateChangedEvent](HandleStateChanged);
    ///   EventBus.Unsubscribe[OnGameStateChangedEvent](HandleStateChanged);  // always in OnDestroy!
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, HashSet<Delegate>> _handlers
            = new Dictionary<Type, HashSet<Delegate>>();

        /// <summary>
        /// Subscribes a handler to the given event type.
        /// Duplicate handlers are automatically ignored.
        /// Call in Awake or OnEnable.
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Type type = typeof(T);

            if (!_handlers.ContainsKey(type))
                _handlers[type] = new HashSet<Delegate>();

            _handlers[type].Add(handler);
        }

        /// <summary>
        /// Unsubscribes a handler.
        /// ALWAYS call in OnDestroy — prevents memory leaks and calls on destroyed objects.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Type type = typeof(T);

            if (_handlers.ContainsKey(type))
                _handlers[type].Remove(handler);
        }

        /// <summary>
        /// Publishes an event to all subscribers.
        /// The payload is a struct (no boxing). A defensive copy of the handler set is
        /// allocated per publish to allow safe (un)subscription during iteration —
        /// acceptable for the current low-frequency events.
        /// </summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            Type type = typeof(T);

            if (!_handlers.ContainsKey(type) || _handlers[type].Count == 0)
                return;

            // Copy to array — guards against modification during iteration
            var handlers = new Delegate[_handlers[type].Count];
            _handlers[type].CopyTo(handlers);

            foreach (Delegate handler in handlers)
            {
                try
                {
                    ((Action<T>)handler)?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] Error in handler {handler.Method.Name}: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Clears all subscribers for a specific event type.
        /// </summary>
        public static void Clear<T>() where T : struct
        {
            Type type = typeof(T);
            if (_handlers.ContainsKey(type))
                _handlers[type].Clear();
        }

        /// <summary>
        /// Clears all subscribers for all event types.
        /// Use only on full game reset.
        /// </summary>
        public static void ClearAll()
        {
            _handlers.Clear();
        }
    }
}
