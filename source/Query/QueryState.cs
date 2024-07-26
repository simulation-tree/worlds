using System;
using Unmanaged;

namespace Simulation
{
    internal readonly struct QueryState : IDisposable
    {
        private readonly Allocation state;

        public readonly bool IsUpdated
        {
            get
            {
                byte value = state.AsRef<byte>();
                return value == 1;
            }
        }

        public readonly bool IsDisposed => state.IsDisposed;

        private QueryState(Allocation state)
        {
            this.state = state;
        }

        public readonly void Dispose()
        {
            state.Dispose();
        }

        public readonly void HasUpdated()
        {
            ref byte stateValue = ref state.AsRef<byte>();
            stateValue = 1;
        }

        public readonly void ThrowIfNotUpdated()
        {
            if (!IsUpdated)
            {
                throw new InvalidOperationException($"{nameof(Query.Update)}() must be called at least once before accessing query results.");
            }
        }

        public static QueryState Create()
        {
            Allocation state = Allocation.Create((byte)0);
            QueryState query = new(state);
            return query;
        }
    }
}
