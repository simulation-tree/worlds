using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    /// <summary>
    /// A type that can subscribe to events, with instanced callbacks
    /// instead of static unmanaged ones.
    /// </summary>
    public abstract class SystemBase : IDisposable
    {
        private static readonly Dictionary<RuntimeType, List<Action<Container>>> staticCallbacks = [];

        private World world;

        /// <summary>
        /// The world that this system belongs to.
        /// <para>
        /// Will be <see cref="default"/> when the system
        /// is in the disposed state.
        /// </para>
        /// </summary>
        public World World => world;

        private readonly UnmanagedList<Listener> listeners;
        private readonly Dictionary<RuntimeType, Action<Container>> callbacks;

        public SystemBase(World world)
        {
            this.world = world;
            listeners = new();
            callbacks = new();
        }

        /// <summary>
        /// Cleans up the resources of this system.
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
                    bool removed = callbacks.Remove(callback);
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
            world = default;
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
                callbacks = new();
                staticCallbacks.Add(eventType, callbacks);

                Listener listener = World.Listen(eventType, &StaticEvent);
                listeners.Add(listener);
            }

            Action<Container> staticCallback = (message) => callback(message.AsRef<T>());
            callbacks.Add(staticCallback);
            this.callbacks.Add(eventType, staticCallback);
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
