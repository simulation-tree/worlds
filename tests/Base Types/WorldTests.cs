using System.Runtime.CompilerServices;
using Unmanaged.Tests;

namespace Worlds.Tests
{
    public abstract class WorldTests : UnmanagedTests
    {
        static WorldTests()
        {
            RuntimeHelpers.RunClassConstructor(typeof(TypeTable).TypeHandle);
        }
    }
}