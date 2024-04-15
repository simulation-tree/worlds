using System;
using System.Collections.Generic;

namespace Game
{
    public unsafe static class ListenerUtils
    {
        public static List<object> AddImplementations(World world, object obj)
        {
            return AddImplementations(world.value, obj);
        }

        /// <summary>
        /// Adds all implementations of <see cref="IListener{T}"/>
        /// </summary>
        public static List<object> AddImplementations(UnmanagedWorld* world, object obj)
        {
            nint worldPointer = (nint)world;
            Type objType = obj.GetType();
#pragma warning disable IL2075
            Type[] types = objType.GetInterfaces();
#pragma warning restore IL2075
            List<object> listenerList = [];
            foreach (Type interfaceType in types)
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IListener<>))
                {
#pragma warning disable IL2055
#pragma warning disable IL3050
                    //todo: remove this warning suppression, together with the causing issue for aot
                    Type eventType = interfaceType.GetGenericArguments()[0];
                    Type genericListenerType = typeof(Listener<>).MakeGenericType(eventType);
                    IListener listener = (IListener)(Activator.CreateInstance(genericListenerType, [worldPointer, obj]) ?? throw new Exception());
                    listener.AddToWorld(world);
                    listenerList.Add(listener);
#pragma warning restore IL3050
#pragma warning restore IL2055
                }
            }

            return listenerList;
        }

        public static void RemoveImplementations(World world, List<object> listenerList)
        {
            RemoveImplementations(world.value, listenerList);
        }

        public static void RemoveImplementations(UnmanagedWorld* world, List<object> listenerList)
        {
            foreach (IListener listener in listenerList)
            {
                listener.RemoveFromWorld(world);
            }
        }

        private unsafe interface IListener
        {
            void AddToWorld(UnmanagedWorld* world);
            void RemoveFromWorld(UnmanagedWorld* world);
        }

        private unsafe sealed class Listener<T>(nint worldPointer, IListener<T> listener) : IListener where T : unmanaged
        {
            private readonly UnmanagedWorld* world = (UnmanagedWorld*)worldPointer;
            private readonly IListener<T> listener = listener;

            void IListener.AddToWorld(UnmanagedWorld* world)
            {
                UnmanagedWorld.AddListener<T>(world, Receive);
            }

            void IListener.RemoveFromWorld(UnmanagedWorld* world)
            {
                UnmanagedWorld.RemoveListener<T>(world, Receive);
            }

            private void Receive(ref T message)
            {
                listener.Receive(new(world), ref message);
            }
        }
    }
}
