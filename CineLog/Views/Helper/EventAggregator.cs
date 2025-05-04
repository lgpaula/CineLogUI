using System;
using System.Collections.Generic;

namespace CineLog.Views.Helper
{
    public class EventAggregator
    {
        private static EventAggregator? _instance;
        public static EventAggregator Instance => _instance ??= new EventAggregator();

        private readonly Dictionary<string, List<Action<DatabaseHandler.CustomList>>> _subscribers = new();

        public void Subscribe(string eventName, Action<DatabaseHandler.CustomList> callback)
        {
            if (!_subscribers.ContainsKey(eventName))
                _subscribers[eventName] = [];

            _subscribers[eventName].Add(callback);
        }

        public void Publish(string eventName, DatabaseHandler.CustomList customList)
        {
            if (_subscribers.ContainsKey(eventName))
            {
                foreach (var callback in _subscribers[eventName])
                    callback(customList);
            }
        }
    }
}