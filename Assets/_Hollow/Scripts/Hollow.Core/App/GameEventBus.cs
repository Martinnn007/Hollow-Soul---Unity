using System;
using System.Collections.Generic;

namespace Hollow.Core.App
{
    public sealed class GameEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> subscribers = new();

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var eventType = typeof(TEvent);
            if (!subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                subscribers.Add(eventType, handlers);
            }

            handlers.Add(handler);
            return new EventSubscription(() => handlers.Remove(handler));
        }

        public void Publish<TEvent>(TEvent gameEvent)
        {
            if (!subscribers.TryGetValue(typeof(TEvent), out var handlers))
            {
                return;
            }

            var snapshot = handlers.ToArray();
            foreach (var handler in snapshot)
            {
                ((Action<TEvent>)handler).Invoke(gameEvent);
            }
        }

        private sealed class EventSubscription : IDisposable
        {
            private Action unsubscribe;

            public EventSubscription(Action unsubscribe)
            {
                this.unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                unsubscribe?.Invoke();
                unsubscribe = null;
            }
        }
    }
}
