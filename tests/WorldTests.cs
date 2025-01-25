using Types;
using Unmanaged.Tests;

namespace Worlds.Tests
{
    public abstract class WorldTests : UnmanagedTests
    {
        static WorldTests()
        {
            Schema.OnRegister = Schema.RegisterType;
            TypeRegistry.Load<Worlds.Tests.TypeBank>();
        }

        public static World CreateWorld()
        {
            World world = new(CreateSchema());
            return world;
        }

        public static Schema CreateSchema()
        {
            Schema schema = new();
            schema.Load<SchemaBank>();
            return schema;
        }
    }
}