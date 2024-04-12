using Game.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Game
{
    /// <summary>
    /// An object for simulating machines that operate on events.
    /// </summary>
    public class VirtualMachine : IDisposable
    {
        private World world;
        private bool stopped;
        private bool disposed;
        private readonly HashSet<object> listenerKeys = [];
        private readonly Dictionary<object, List<object>> listeners = [];

        public ref World World => ref world;

        /// <summary>
        /// Has this instance been disposed?
        /// </summary>
        public bool IsDisposed => disposed;

        public VirtualMachine()
        {
            world = new();
            world.AddListener<Shutdown>(Shutdown);
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            disposed = true;

            world.RemoveListener<Shutdown>(Shutdown);
            world.Dispose();
            GC.SuppressFinalize(this);
        }

        [Conditional("DEBUG")]
        private void ThrowIfAlreadyAdded(object obj)
        {
            if (listenerKeys.Contains(obj))
            {
                throw new InvalidOperationException("Listener already added.");
            }
        }

        [Conditional("DEBUG")]
        private void ThrowIfNotAdded(object obj)
        {
            if (!listenerKeys.Contains(obj))
            {
                throw new InvalidOperationException("Listener not added.");
            }
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(VirtualMachine));
            }
        }

        [Conditional("DEBUG")]
        private void ThrowIfStopped()
        {
            if (stopped)
            {
                throw new InvalidOperationException("Virtual machine has stopped.");
            }
        }

        /// <summary>
        /// Broadcasts a <see cref="Events.Update"/> event, and polls all submitted
        /// events to added listeners.
        /// </summary>
        /// <returns><c>true</c> if the machine should keep updating, until the
        /// <see cref="Events.Shutdown"/> event is submitted.</returns>
        public bool Update()
        {
            ThrowIfStopped();
            ThrowIfDisposed();
            world.SubmitEvent(new Update());
            world.PollListeners();
            return !stopped;
        }

        public void SubmitEvent<T>(T message) where T : unmanaged
        {
            ThrowIfStopped();
            ThrowIfDisposed();
            world.SubmitEvent(message);
        }

        private void Shutdown(ref Shutdown shutdown)
        {
            stopped = true;
        }

        public void Add(object obj)
        {
            ThrowIfDisposed();
            ThrowIfAlreadyAdded(obj);

            listeners.Add(obj, ListenerUtils.AddImplementations(world, obj));
            listenerKeys.Add(obj);
        }

        public void Remove(object obj)
        {
            ThrowIfDisposed();
            ThrowIfNotAdded(obj);

            ListenerUtils.RemoveImplementations(world, listeners[obj]);
            listeners.Remove(obj);
            listenerKeys.Remove(obj);
        }

        public bool Contains<T>()
        {
            ThrowIfDisposed();

            if (listenerKeys.Count == 0)
            {
                return false;
            }

            object[] keys = [listenerKeys];
            for (int i = keys.Length - 1; i >= 0; i--)
            {
                object key = keys[i];
                if (key is T)
                {
                    return true;
                }
            }

            GC.SuppressFinalize(keys);
            return false;
        }
    }
}
