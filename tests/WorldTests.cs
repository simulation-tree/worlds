using Types;
using Unmanaged.Tests;

namespace Worlds.Tests
{
    public abstract class WorldTests : UnmanagedTests
    {
        static WorldTests()
        {
            TypeRegistry.Load<WorldsTestsTypeBank>();
        }

        protected World CreateWorld()
        {
            World world = new(CreateSchema());
            return world;
        }

        protected virtual Schema CreateSchema()
        {
            Schema schema = new();
            schema.Load<WorldsTestsSchemaBank>();
            return schema;
        }
    }
}