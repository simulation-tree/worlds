using System.Runtime.CompilerServices;
using Unmanaged.Tests;

namespace Simulation.Tests
{
    public abstract class SimulationTests : UnmanagedTests
    {
        protected override void SetUp()
        {
            base.SetUp();
            RuntimeHelpers.RunClassConstructor(typeof(TypeTable).TypeHandle);
        }
    }
}