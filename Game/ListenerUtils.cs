using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unmanaged;

namespace Game
{
    public unsafe static class ListenerUtils
    {
        private static readonly Dictionary<(World, RuntimeType), HashSet<ListenerCallback>> eventKeyToListener = [];

        /// <summary>
        /// Adds all implementations of <see cref="IListener{T}"/>
        /// </summary>
        public static List<object> AddImplementations(World world, object obj)
        {
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
                    IListener listener = (IListener)(Activator.CreateInstance(genericListenerType, [world, obj]) ?? throw new Exception());
                    listener.AddListenerToWorld(world);
                    listenerList.Add(listener);
#pragma warning restore IL3050
#pragma warning restore IL2055
                }
            }

            return listenerList;
        }

        public static void RemoveImplementations(World world, List<object> listenerList)
        {
            foreach (IListener listener in listenerList)
            {
                listener.RemoveListenerFromWorld(world);
            }
        }

        private unsafe interface IListener
        {
            void AddListenerToWorld(World world);
            void RemoveListenerFromWorld(World world);
        }

        [UnmanagedCallersOnly]
        private static void Callback(World world, Container container)
        {
            if (eventKeyToListener.ContainsKey((world, container.type)))
            {
                foreach (ListenerCallback listener in eventKeyToListener[(world, container.type)])
                {
                    listener(ref container);
                }
            }
        }

        private unsafe sealed class Listener<T>(World world, IListener<T> listener) : IListener where T : unmanaged
        {
            private static bool listening; 
            private static readonly RuntimeType eventType = RuntimeType.Get<T>();

            private readonly World world = world;
            private readonly IListener<T> listener = listener;

            void IListener.AddListenerToWorld(World world)
            {
                if (!listening)
                {
                    world.Listen(eventType, &Callback);
                    listening = true;
                }

                if (!eventKeyToListener.ContainsKey((world, RuntimeType.Get<T>())))
                {
                    eventKeyToListener.Add((world, RuntimeType.Get<T>()), new());
                }

                eventKeyToListener[(world, RuntimeType.Get<T>())].Add(Receive);
            }

            void IListener.RemoveListenerFromWorld(World world)
            {
                eventKeyToListener[(world, RuntimeType.Get<T>())].Remove(Receive);
            }

            private void Receive(ref Container message)
            {
                listener.Receive(world, ref message.AsRef<T>());
            }
        }
    }
}
