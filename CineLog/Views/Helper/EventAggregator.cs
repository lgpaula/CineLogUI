using System;
using System.Collections.Generic;
using System.Linq;

namespace CineLog.Views.Helper
{
    public class EventAggregator
    {
        private static EventAggregator? _instance;
        public static EventAggregator Instance => _instance ??= new EventAggregator();

        private readonly Dictionary<Type, List<Delegate>> _subscribers = [];

        public void Subscribe<T>(Action<T> callback)
        {
            var eventType = typeof(T);
            if (!_subscribers.ContainsKey(eventType))
                _subscribers[eventType] = [];

            _subscribers[eventType].Add(callback);
        }

        public void Publish<T>(T eventData)
        {
            var eventType = typeof(T);
            if (_subscribers.TryGetValue(eventType, out var callbacks))
            {
                foreach (var callback in callbacks.Cast<Action<T>>())
                    callback(eventData);
            }
        }
    }
}