using Unmanaged.Tests;

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
            World world = new();
            SchemaRegistry.Load(world.Schema);
            return world;
        }

        public static Schema CreateSchema()
        {
            Schema schema = new();
            SchemaRegistry.Load(schema);
            return schema;
        }
    }
}