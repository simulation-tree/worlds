using Collections;
using Programs;
using Programs.Components;
using Unmanaged;

namespace Simulation.Tests
{
    public class RegisteredTypesTest : SimulationTests
    {
        [Test]
        public void CheckIfAllAreRegistered()
        {
            using List<ComponentType> componentTypes = new();
            componentTypes.Add(ComponentType.Get<float>());
            componentTypes.Add(ComponentType.Get<int>());
            componentTypes.Add(ComponentType.Get<double>());
            componentTypes.Add(ComponentType.Get<char>());
            componentTypes.Add(ComponentType.Get<World>());
            componentTypes.Add(ComponentType.Get<IsProgram>());
            componentTypes.Add(ComponentType.Get<ProgramState>());
            componentTypes.Add(ComponentType.Get<byte>());
            componentTypes.Add(ComponentType.Get<SimpleComponent>());
            componentTypes.Add(ComponentType.Get<Another>());
            componentTypes.Add(ComponentType.Get<bool>());
            componentTypes.Add(ComponentType.Get<uint>());
            componentTypes.Add(ComponentType.Get<FixedString>());
            componentTypes.Add(ComponentType.Get<TestComponent>());
            componentTypes.Add(ComponentType.Get<QueryTests.Apple>());
            componentTypes.Add(ComponentType.Get<QueryTests.Berry>());
            componentTypes.Add(ComponentType.Get<QueryTests.Cherry>());
            componentTypes.Add(ComponentType.Get<SerializationTests.Fruit>());
            componentTypes.Add(ComponentType.Get<SerializationTests.Apple>());
            componentTypes.Add(ComponentType.Get<SerializationTests.Prefab>());
            componentTypes.Add(ComponentType.Get<EntityReferenceTests.ComponentThatReferences>());
            componentTypes.Add(ComponentType.Get<EntityReferenceTests.ReferencedEntity>());
            componentTypes.Add(ComponentType.Get<short>());
            componentTypes.Add(ComponentType.Get<ushort>());
            componentTypes.Add(ComponentType.Get<ProgramAllocation>());

            using List<ArrayType> arrayTypes = new();
            arrayTypes.Add(ArrayType.Get<byte>());
            arrayTypes.Add(ArrayType.Get<float>());
            arrayTypes.Add(ArrayType.Get<double>());
            arrayTypes.Add(ArrayType.Get<char>());
            arrayTypes.Add(ArrayType.Get<SimpleComponent>());
            arrayTypes.Add(ArrayType.Get<uint>());

            foreach (ComponentType type in ComponentType.All)
            {
                Assert.That(componentTypes.Contains(type));
            }

            foreach (ArrayType type in ArrayType.All)
            {
                Assert.That(arrayTypes.Contains(type));
            }
        }
    }
}