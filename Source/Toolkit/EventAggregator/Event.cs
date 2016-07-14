// -----------------------------------------------------------------------
// <copyright file="Event.cs" company="Microsoft">
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

    internal class Event
    {
        private List<DelegateWrapper> subscribers = new List<DelegateWrapper>();

        public void Subscribe<TMessage>(Action<TMessage> handler, ThreadAffinity affinity)
        {
            lock (this.subscribers)
            {
                this.subscribers.Add(new DelegateWrapper(handler, affinity));
            }
        }

        public void Unsubscribe<TMessage>(Action<TMessage> handler)
        {
            lock (this.subscribers)
            {
                this.subscribers.RemoveAll((subscriber) =>
                {
                    return subscriber.IsDead || subscriber.Wraps(handler);
                });
            }
        }

        public void Publish<TMessage>(TMessage message, IDispatcher dispatcher)
        {
            var invokers = new List<DelegateWrapper.Invoker<TMessage>>();
            lock (this.subscribers)
            {
                for (var i = this.subscribers.Count - 1; i >= 0; i--)
                {
                    var invoker = this.subscribers[i].GetInvoker<TMessage>();
                    if (invoker == null)
                    {
                        this.subscribers.RemoveAt(i);
                    }
                    else
                    {
                        invokers.Add(invoker);
                    }
                }
            }

            foreach (var invoker in invokers)
            {
                invoker.Invoke(message, dispatcher);
            }
        }
    }
}
