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
        private static readonly Dictionary<RuntimeType, List<Action<Container>>> staticCallbacks = [];

        public readonly World world;

        private readonly UnmanagedList<Listener> listeners;
        private readonly Dictionary<RuntimeType, Action<Container>> callbacks;

        public SystemBase(World world)
        {
            this.world = world;
            listeners = [];
            callbacks = [];
        }

        /// <summary>
        /// Cleans up the resources of this system.
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
        /// Subscribes to an event of type <typeparamref name="T"/>.
        /// Listener gets disposed together with the system.
        /// </summary>
        public unsafe void Subscribe<T>(Action<T> callback) where T : unmanaged
        {
            RuntimeType eventType = RuntimeType.Get<T>();
            if (!staticCallbacks.TryGetValue(eventType, out List<Action<Container>>? callbacks))
            {
                callbacks = [];
                staticCallbacks.Add(eventType, callbacks);

                Listener listener = world.CreateListener(eventType, &StaticEvent);
                listeners.Add(listener);
            }

            void StaticCallback(Container message) => callback(message.AsRef<T>());
            callbacks.Add(StaticCallback);
            this.callbacks.Add(eventType, StaticCallback);
        }

        [UnmanagedCallersOnly]
        private static void StaticEvent(World world, Container message)
        {
            foreach (Action<Container> callback in staticCallbacks[message.type])
            {
                callback(message);
            }
        }
    }
}
