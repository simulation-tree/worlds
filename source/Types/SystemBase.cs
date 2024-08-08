using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation
{
    /// <summary>
    /// Base type that can subscribe to events with local methods.
    /// </summary>
    public abstract class SystemBase : IDisposable
    {
        private static readonly Dictionary<RuntimeType, List<Action<Container>>> staticCallbacks = new();

        public readonly World world;

        private readonly UnmanagedList<Listener> listeners;
        private readonly Dictionary<RuntimeType, Action<Container>> callbacks;

        public SystemBase(World world)
        {
            this.world = world;
            listeners = new(1);
            callbacks = new(1);
        }

        /// <summary>
        /// Cleans up the resources of this system.
        /// <para>
        /// When overriding, make sure to call the <c>base</c> method last.
        /// </para>
        /// <para>
        /// Will throw <see cref="ObjectDisposedException"/> when 
        /// invoking it more than once.
        /// </para>
        /// </summary>
        public virtual void Dispose()
        {
            if (listeners.IsDisposed)
            {
                throw new ObjectDisposedException(ToString());
            }

            foreach (RuntimeType type in callbacks.Keys)
            {
                Action<Container> callback = this.callbacks[type];
                if (staticCallbacks.TryGetValue(type, out List<Action<Container>>? callbacks))
                {
                    callbacks.Remove(callback);
                    if (callbacks.Count == 0)
                    {
                        for (uint i = 0; i < listeners.Count; i++)
                        {
                            Listener listener = listeners[i];
                            if (listener.eventType == type)
                            {
                                listener.Dispose();
                                listeners.RemoveAt(i);
                                break;
                            }
                        }

                        staticCallbacks.Remove(type);
                    }
                }
            }

            listeners.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Registers the given action to receive callbacks whenever
        /// an event of type <typeparamref name="T"/> is polled through
        /// a <see cref="World"/>.
        /// <para>
        /// Automatically unregistered when this system is disposed.
        /// </para>
        /// </summary>
        public unsafe void Subscribe<T>(Action<T> callback) where T : unmanaged
        {
            RuntimeType eventType = RuntimeType.Get<T>();
            if (this.callbacks.ContainsKey(eventType))
            {
                throw new InvalidOperationException($"Already subscribed to event type `{eventType}`.");
            }

            if (!staticCallbacks.TryGetValue(eventType, out List<Action<Container>>? callbacks))
            {
                callbacks = new();
                staticCallbacks.Add(eventType, callbacks);

                Listener listener = world.CreateListener(eventType, &StaticEvent);
                listeners.Add(listener);
            }

            void StaticCallback(Container message) => callback(message.Read<T>());
            callbacks.Add(StaticCallback);
            this.callbacks.Add(eventType, StaticCallback);
        }

#if NET5_0_OR_GREATER
        [UnmanagedCallersOnly]
#endif
        private static void StaticEvent(World world, Container message)
        {
            foreach (Action<Container> callback in staticCallbacks[message.type])
            {
                callback(message);
            }
        }
    }
}
