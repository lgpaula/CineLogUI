using System;
using System.Collections.Generic;

namespace CineLog.Views.Helper
{
    public class EventAggregator
    {
        private static EventAggregator? _instance;
        public static EventAggregator Instance => _instance ??= new EventAggregator();

        private readonly Dictionary<string, List<Action<string, string>>> _subscribers = new();

        public void Subscribe(string eventName, Action<string, string> callback)
        {
            if (!_subscribers.ContainsKey(eventName))
                _subscribers[eventName] = new List<Action<string, string>>();

            _subscribers[eventName].Add(callback);
        }

        public void Publish(string eventName, string parameter, string parameter2)
        {
            if (_subscribers.ContainsKey(eventName))
            {
                foreach (var callback in _subscribers[eventName])
                    callback(parameter, parameter2);
            }
        }
    }
}