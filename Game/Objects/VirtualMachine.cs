using Game.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unmanaged;

namespace Game
{
    /// <summary>
    /// An object for simulating machines that operate on events.
    /// </summary>
    public class VirtualMachine : IDisposable
    {
        private static readonly Dictionary<World, VirtualMachine> vms = [];

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

        unsafe public VirtualMachine()
        {
            world = new();
            world.Listen<Shutdown>(&Shutdown);
            vms.Add(world, this);
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            vms.Remove(world);
            disposed = true;

            //world.RemoveListener<Shutdown>(Shutdown);
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
            world.Submit(new Update());
            world.Poll();
            return !stopped;
        }

        public void SubmitEvent<T>(T message) where T : unmanaged
        {
            ThrowIfStopped();
            ThrowIfDisposed();
            world.Submit(message);
        }

        [UnmanagedCallersOnly]
        private static void Shutdown(World world, Container container)
        {
            VirtualMachine vm = vms[world];
            vm.stopped = true;
        }

        public void Add(object obj)
        {
            ThrowIfDisposed();
            ThrowIfAlreadyAdded(obj);

            listeners.Add(obj, ListenerUtils.AddImplementations(world, obj));
            listenerKeys.Add(obj);
            world.Submit(new SystemAdded(0));
        }

        public void Remove(object obj)
        {
            ThrowIfDisposed();
            ThrowIfNotAdded(obj);

            ListenerUtils.RemoveImplementations(world, listeners[obj]);
            listeners.Remove(obj);
            listenerKeys.Remove(obj);
            world.Submit(new SystemRemoved(0));
        }

        public void MoveToEnd(object obj)
        {
            ThrowIfDisposed();
            ThrowIfNotAdded(obj);

            ListenerUtils.RemoveImplementations(world, listeners[obj]);
            listeners[obj] = ListenerUtils.AddImplementations(world, obj);
            listenerKeys.Remove(obj);
            listenerKeys.Add(obj);
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
