using Unmanaged.Tests;
using Types;

namespace Worlds.Tests
{
    public abstract class WorldTests : UnmanagedTests
    {
        static WorldTests()
        {
            TypeLayoutRegistry.RegisterAll();
        }

        public static World CreateWorld()
        {
            World world = new(CreateSchema());
            return world;
        }

        public static Schema CreateSchema()
        {
            return SchemaRegistry.Get();
        }
    }
}