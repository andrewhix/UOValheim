using System;
using System.Collections.Generic;

namespace UltimaValheim.Core
{
    /// <summary>
    /// Global event bus for inter-module communication using publish/subscribe pattern.
    /// Allows modules to communicate without direct dependencies.
    /// </summary>
    public class EventBus
    {
        private readonly Dictionary<string, List<Delegate>> _eventHandlers = new Dictionary<string, List<Delegate>>();

        /// <summary>
        /// Subscribe to an event by name with a typed handler.
        /// </summary>
        /// <typeparam name="T">Delegate type for the event handler</typeparam>
        /// <param name="eventName">Name of the event to subscribe to</param>
        /// <param name="handler">Handler to invoke when event is published</param>
        public void Subscribe<T>(string eventName, T handler) where T : Delegate
        {
            if (string.IsNullOrEmpty(eventName))
            {
                CoreAPI.Log.LogWarning("[EventBus] Attempted to subscribe with null/empty event name!");
                return;
            }

            if (handler == null)
            {
                CoreAPI.Log.LogWarning($"[EventBus] Attempted to subscribe null handler to event '{eventName}'!");
                return;
            }

            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = new List<Delegate>();
            }

            _eventHandlers[eventName].Add(handler);
            CoreAPI.Log.LogDebug($"[EventBus] Subscribed to event '{eventName}'");
        }

        /// <summary>
        /// Unsubscribe from an event.
        /// </summary>
        public void Unsubscribe<T>(string eventName, T handler) where T : Delegate
        {
            if (string.IsNullOrEmpty(eventName) || handler == null)
                return;

            if (_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName].Remove(handler);
                
                if (_eventHandlers[eventName].Count == 0)
                {
                    _eventHandlers.Remove(eventName);
                }

                CoreAPI.Log.LogDebug($"[EventBus] Unsubscribed from event '{eventName}'");
            }
        }

        /// <summary>
        /// Publish an event with no arguments.
        /// </summary>
        public void Publish(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                return;

            if (!_eventHandlers.ContainsKey(eventName))
                return;

            var handlers = _eventHandlers[eventName].ToArray(); // Copy to avoid modification during iteration

            foreach (var handler in handlers)
            {
                try
                {
                    handler.DynamicInvoke();
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[EventBus] Error invoking handler for event '{eventName}': {ex}");
                }
            }
        }

        /// <summary>
        /// Publish an event with one argument.
        /// </summary>
        public void Publish<T1>(string eventName, T1 arg1)
        {
            if (string.IsNullOrEmpty(eventName))
                return;

            if (!_eventHandlers.ContainsKey(eventName))
                return;

            var handlers = _eventHandlers[eventName].ToArray();

            foreach (var handler in handlers)
            {
                try
                {
                    handler.DynamicInvoke(arg1);
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[EventBus] Error invoking handler for event '{eventName}': {ex}");
                }
            }
        }

        /// <summary>
        /// Publish an event with two arguments.
        /// </summary>
        public void Publish<T1, T2>(string eventName, T1 arg1, T2 arg2)
        {
            if (string.IsNullOrEmpty(eventName))
                return;

            if (!_eventHandlers.ContainsKey(eventName))
                return;

            var handlers = _eventHandlers[eventName].ToArray();

            foreach (var handler in handlers)
            {
                try
                {
                    handler.DynamicInvoke(arg1, arg2);
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[EventBus] Error invoking handler for event '{eventName}': {ex}");
                }
            }
        }

        /// <summary>
        /// Publish an event with three arguments.
        /// </summary>
        public void Publish<T1, T2, T3>(string eventName, T1 arg1, T2 arg2, T3 arg3)
        {
            if (string.IsNullOrEmpty(eventName))
                return;

            if (!_eventHandlers.ContainsKey(eventName))
                return;

            var handlers = _eventHandlers[eventName].ToArray();

            foreach (var handler in handlers)
            {
                try
                {
                    handler.DynamicInvoke(arg1, arg2, arg3);
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[EventBus] Error invoking handler for event '{eventName}': {ex}");
                }
            }
        }

        /// <summary>
        /// Publish an event with four arguments.
        /// </summary>
        public void Publish<T1, T2, T3, T4>(string eventName, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (string.IsNullOrEmpty(eventName))
                return;

            if (!_eventHandlers.ContainsKey(eventName))
                return;

            var handlers = _eventHandlers[eventName].ToArray();

            foreach (var handler in handlers)
            {
                try
                {
                    handler.DynamicInvoke(arg1, arg2, arg3, arg4);
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[EventBus] Error invoking handler for event '{eventName}': {ex}");
                }
            }
        }

        /// <summary>
        /// Check if there are any subscribers to an event.
        /// </summary>
        public bool HasSubscribers(string eventName)
        {
            return _eventHandlers.ContainsKey(eventName) && _eventHandlers[eventName].Count > 0;
        }

        /// <summary>
        /// Get the number of subscribers for an event.
        /// </summary>
        public int GetSubscriberCount(string eventName)
        {
            return _eventHandlers.ContainsKey(eventName) ? _eventHandlers[eventName].Count : 0;
        }

        /// <summary>
        /// Clear all event subscriptions. Use with caution!
        /// </summary>
        public void ClearAll()
        {
            _eventHandlers.Clear();
            CoreAPI.Log.LogWarning("[EventBus] Cleared all event subscriptions!");
        }
    }
}
