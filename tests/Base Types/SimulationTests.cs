using Programs;
using Programs.Components;
using Unmanaged;
using Unmanaged.Tests;

namespace Simulation.Tests
{
    public abstract class SimulationTests : UnmanagedTests
    {
        protected override void SetUp()
        {
            base.SetUp();
            ComponentType.Register<float>();
            ComponentType.Register<int>();
            ComponentType.Register<double>();
            ComponentType.Register<char>();
            ComponentType.Register<World>();
            ComponentType.Register<IsProgram>();
            ComponentType.Register<ProgramState>();
            ComponentType.Register<byte>();
            ComponentType.Register<SimpleComponent>();
            ComponentType.Register<Another>();
            ComponentType.Register<bool>();
            ComponentType.Register<uint>();
            ComponentType.Register<FixedString>();
            ComponentType.Register<TestComponent>();
            ComponentType.Register<QueryTests.Apple>();
            ComponentType.Register<QueryTests.Berry>();
            ComponentType.Register<QueryTests.Cherry>();
            ComponentType.Register<SerializationTests.Fruit>();
            ComponentType.Register<SerializationTests.Apple>();
            ComponentType.Register<SerializationTests.Prefab>();
            ComponentType.Register<EntityReferenceTests.ComponentThatReferences>();
            ComponentType.Register<EntityReferenceTests.ReferencedEntity>();
            ComponentType.Register<short>();
            ComponentType.Register<ushort>();
            ComponentType.Register<ProgramAllocation>();
            ArrayType.Register<byte>();
            ArrayType.Register<float>();
            ArrayType.Register<double>();
            ArrayType.Register<char>();
            ArrayType.Register<SimpleComponent>();
            ArrayType.Register<uint>();
        }
    }
}