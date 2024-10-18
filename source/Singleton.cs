using System;

namespace Simulation
{
    public readonly struct Singleton<T> where T : IDisposable, new()
    {
        private static readonly T instance;
        private static int references;

        static Singleton()
        {
            instance = new();
        }

        public static T Retrieve()
        {
            references++;
            return instance;
        }

        public static void Return()
        {
            references--;
            if (references < 0)
            {
                throw new InvalidOperationException("Singleton was returned more times than it was retrieved");
            }

            if (references == 0)
            {
                instance.Dispose();
            }
        }
    }
}