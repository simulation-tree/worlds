using Simulation.Functions;
using Unmanaged;

namespace Simulation
{
    public interface ISystem
    {
        InitializeFunction Initialize { get; }
        IterateFunction Update { get; }
        FinalizeFunction Finalize { get; }

        public uint GetMessageHandlers(USpan<MessageHandler> buffer)
        {
            return 0;
        }
    }
}