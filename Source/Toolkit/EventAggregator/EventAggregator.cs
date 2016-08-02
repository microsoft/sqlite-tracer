// -----------------------------------------------------------------------
// <copyright file="EventAggregator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Toolkit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    
    public enum ThreadAffinity
    {
        Unknown,

        UIThread,

        PublisherThread,
    }

    public class EventAggregator
    {
        private Dictionary<Type, Event> events = new Dictionary<Type, Event>();
        private IDispatcher dispatcher;

        public EventAggregator(IDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public void Subscribe<TMessage>(Action<TMessage> handler, ThreadAffinity affinity)
        {
            var type = typeof(TMessage);
            Event subscribers;
            lock (this.events)
            {
                if (!this.events.TryGetValue(type, out subscribers))
                {
                    subscribers = new Event();
                    this.events.Add(type, subscribers);
                }
            }

            subscribers.Subscribe(handler, affinity);
        }

        public void Unsubscribe<TMessage>(Action<TMessage> handler)
        {
            var type = typeof(TMessage);
            Event subscribers;
            lock (this.events)
            {
                if (!this.events.TryGetValue(type, out subscribers))
                {
                    return;
                }
            }

            subscribers.Unsubscribe<TMessage>(handler);
        }

        public void Publish<TMessage>(TMessage message)
        {
            var type = typeof(TMessage);
            Event subscribers;
            lock (this.events)
            {
                if (!this.events.TryGetValue(type, out subscribers))
                {
                    return;
                }
            }

            subscribers.Publish<TMessage>(message, this.dispatcher);
        }
    }
}
