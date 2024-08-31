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
        private static readonly Dictionary<RuntimeType, List<Action<Allocation>>> staticCallbacks = new();

        public readonly World world;

        private readonly UnmanagedList<Listener> listeners;
        private readonly Dictionary<RuntimeType, Action<Allocation>> callbacks;

        public SystemBase(World world)
        {
            this.world = world;
            listeners = new(1);
            callbacks = new(1);
        }

        /// <summary>
        /// Cleans up the resources of this system.
        /// <para>
        /// When overriding, make sure to call the <c>base</c> implementation last.
        /// </para>
        /// <para>
        /// May throw <see cref="ObjectDisposedException"/> if already disposed.
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
                Action<Allocation> callback = this.callbacks[type];
                if (staticCallbacks.TryGetValue(type, out List<Action<Allocation>>? callbacks))
                {
                    callbacks.Remove(callback);
                    if (callbacks.Count == 0)
                    {
                        for (uint i = 0; i < listeners.Count; i++)
                        {
                            Listener listener = listeners[i];
                            if (listener.messageType == type)
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
            RuntimeType messageType = RuntimeType.Get<T>();
            if (this.callbacks.ContainsKey(messageType))
            {
                throw new InvalidOperationException($"This instance of {GetType()} is already subscribed to `{messageType}`.");
            }

            if (!staticCallbacks.TryGetValue(messageType, out List<Action<Allocation>>? callbacks))
            {
                callbacks = new();
                staticCallbacks.Add(messageType, callbacks);
                Listener listener = world.CreateListener(messageType, &StaticEvent);
                listeners.Add(listener);
            }

            void StaticCallback(Allocation message) => callback(message.Read<T>());
            callbacks.Add(StaticCallback);
            this.callbacks.Add(messageType, StaticCallback);
        }

#if NET5_0_OR_GREATER
        [UnmanagedCallersOnly]
#endif
        private static void StaticEvent(World world, Allocation message, RuntimeType messageType)
        {
            foreach (Action<Allocation> callback in staticCallbacks[messageType])
            {
                callback(message);
            }
        }
    }
}
