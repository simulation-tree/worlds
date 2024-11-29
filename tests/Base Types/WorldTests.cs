using System.Runtime.CompilerServices;
using Unmanaged.Tests;

namespace Worlds.Tests
{
    public abstract class WorldTests : UnmanagedTests
    {
        protected override void SetUp()
        {
            base.SetUp();
            RuntimeHelpers.RunClassConstructor(typeof(TypeTable).TypeHandle);
        }
    }
}