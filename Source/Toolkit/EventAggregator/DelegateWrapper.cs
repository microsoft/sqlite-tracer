// -----------------------------------------------------------------------
// <copyright file="DelegateWrapper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Toolkit
{
    using System;
    using System.Reflection;

    internal class DelegateWrapper
    {
        private WeakReference target;
        private MethodInfo method;
        private Type type;

        private ThreadAffinity affinity;

        public DelegateWrapper(Delegate wrappedDelegate, ThreadAffinity affinity)
        {
            this.target = new WeakReference(wrappedDelegate.Target);
            this.method = wrappedDelegate.GetMethodInfo();
            this.type = wrappedDelegate.GetType();

            this.affinity = affinity;
        }

        public bool IsDead
        {
            get { return this.target.Target == null; }
        }

        public bool Wraps(Delegate wrappedDelegate)
        {
            return wrappedDelegate.Equals(this.TryGetDelegate());
        }

        public Invoker<TMessage> GetInvoker<TMessage>()
        {
            var action = this.TryGetDelegate();
            if (action != null)
            {
                return new Invoker<TMessage>((Action<TMessage>)action, this.affinity);
            }

            return null;
        }

        public class Invoker<TMessage>
        {
            private Action<TMessage> action;
            private ThreadAffinity affinity;

            public Invoker(Action<TMessage> action, ThreadAffinity affinity)
            {
                this.action = action;
                this.affinity = affinity;
            }

            public void Invoke(TMessage message, IDispatcher dispatcher)
            {
                if (this.affinity == ThreadAffinity.UIThread)
                {
                    dispatcher.Invoke(() => this.action(message));
                }
                else
                {
                    this.action(message);
                }
            }
        }

        private Delegate TryGetDelegate()
        {
            if (this.method.IsStatic)
            {
                return this.method.CreateDelegate(this.type, null);
            }

            var receiver = this.target.Target;
            if (this.target != null)
            {
                return this.method.CreateDelegate(this.type, receiver);
            }

            return null;
        }
    }
}