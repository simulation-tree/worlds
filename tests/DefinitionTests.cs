using Unmanaged;

namespace Simulation.Tests
{
    public class DefinitionTests : SimulationTests
    {
        [Test]
        public void CompareEquality()
        {
            Definition a = new([ComponentType.Get<float>()], [ArrayType.Get<byte>()]);
            Definition b = new([ComponentType.Get<float>()], [ArrayType.Get<byte>()]);

            Assert.That(a, Is.EqualTo(b));

            a = a.AddComponentType<int>();
            a = a.AddComponentType<double>();
            a = a.AddComponentType<char>();

            Assert.That(a, Is.Not.EqualTo(b));

            b = b.AddComponentType<double>();
            b = b.AddComponentType<char>();
            b = b.AddComponentType<int>();

            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void NotEqual()
        {
            Definition a = new([ComponentType.Get<float>()], [ArrayType.Get<byte>()]);
            Definition b = new([ComponentType.Get<float>()], [ArrayType.Get<float>()]);

            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void AddingAndIndexing()
        {
            Definition a = new();
            a = a.AddComponentType<int>();
            a = a.AddArrayType<double>();
            a = a.AddComponentType<char>();
            a = a.AddArrayType<float>();

            Assert.That(a.componentTypeCount, Is.EqualTo(2));
            Assert.That(a.arrayTypeCount, Is.EqualTo(2));
            Assert.That(a.TotalTypeCount, Is.EqualTo(4));
            USpan<ComponentType> componentTypes = stackalloc ComponentType[a.componentTypeCount];
            USpan<ArrayType> arrayTypes = stackalloc ArrayType[a.arrayTypeCount];
            a.CopyComponentTypes(componentTypes);
            a.CopyArrayTypes(arrayTypes);
            Assert.That(componentTypes.Contains(ComponentType.Get<int>()), Is.True);
            Assert.That(componentTypes.Contains(ComponentType.Get<char>()), Is.True);
            Assert.That(arrayTypes.Contains(ArrayType.Get<double>()), Is.True);
            Assert.That(arrayTypes.Contains(ArrayType.Get<float>()), Is.True);
        }

        [Test]
        public void CreateDefinitionFromMany()
        {
            Definition definition = new Definition().AddComponentTypes<int, char, double, float>();
            Assert.That(definition.componentTypeCount, Is.EqualTo(4));
            Assert.That(definition.arrayTypeCount, Is.EqualTo(0));
            Assert.That(definition.TotalTypeCount, Is.EqualTo(4));
            Assert.That(definition.ContainsComponent<int>(), Is.True);
            Assert.That(definition.ContainsComponent<char>(), Is.True);
            Assert.That(definition.ContainsComponent<double>(), Is.True);
            Assert.That(definition.ContainsComponent<float>(), Is.True);
        }

        [Test]
        public void QueryUsingDefinition()
        {
            using World world = new();

            uint entityA = world.CreateEntity();
            world.AddComponent(entityA, (byte)1);
            world.AddComponent(entityA, (float)1);
            world.CreateArray(entityA, "Hello".AsUSpan());

            uint entityB = world.CreateEntity();
            world.AddComponent(entityB, (byte)2);

            Definition byteComponent = new([ComponentType.Get<byte>()], []);
            Definition charArray = new([], [ArrayType.Get<char>()]);

            using DefinitionQuery byteQuery = new(byteComponent);
            byteQuery.Update(world);
            Assert.That(byteQuery.Count, Is.EqualTo(2));
            Assert.That(byteQuery[0], Is.EqualTo(entityB));
            Assert.That(byteQuery[1], Is.EqualTo(entityA));

            using DefinitionQuery charQuery = new(charArray);
            charQuery.Update(world);
            Assert.That(charQuery.Count, Is.EqualTo(1));
            Assert.That(charQuery[0], Is.EqualTo(entityA));
        }

        [Test]
        public void ContainsComponentTypes()
        {
            Definition definition = new([ComponentType.Get<byte>(), ComponentType.Get<float>()], [ArrayType.Get<char>()]);
            Assert.That(definition.ContainsComponent<byte>(), Is.True);
            Assert.That(definition.ContainsComponent<float>(), Is.True);
            Assert.That(definition.ContainsComponent<char>(), Is.False);
            Assert.That(definition.ContainsArray<char>(), Is.True);
        }

        [Test]
        public void CreateFromDefinition()
        {
            using World world = new();

            Definition definition = new([ComponentType.Get<byte>(), ComponentType.Get<float>()], [ArrayType.Get<char>()]);
            uint entity = world.CreateEntity(definition);
            byte defaultByte = world.GetComponent<byte>(entity);
            float defaultFloat = world.GetComponent<float>(entity);
            USpan<char> defaultCharArray = world.GetArray<char>(entity);

            Assert.That(defaultByte, Is.EqualTo(default(byte)));
            Assert.That(defaultFloat, Is.EqualTo(default(float)));
            Assert.That(defaultCharArray.Length, Is.EqualTo(0));
        }

        [Test]
        public void BecomeADefinition()
        {
            using World world = new();

            Definition definitionA = new([ComponentType.Get<byte>(), ComponentType.Get<float>()], [ArrayType.Get<char>()]);

            Entity entity = new(world);
            entity.Become(definitionA);

            Assert.That(entity.ContainsComponent<byte>(), Is.True);
            Assert.That(entity.ContainsComponent<float>(), Is.True);
            Assert.That(entity.ContainsArray<char>(), Is.True);
            Assert.That(entity.ContainsArray<byte>(), Is.False);
            Assert.That(entity.ContainsComponent<char>(), Is.False);
            Assert.That(entity.GetComponent<byte>(), Is.EqualTo(default(byte)));
            Assert.That(entity.GetComponent<float>(), Is.EqualTo(default(float)));
            Assert.That(entity.GetArray<char>().Length, Is.EqualTo(0));

            Definition definitionB = new([], [ArrayType.Get<byte>()]);

            entity.Become(definitionB);

            Assert.That(entity.ContainsArray<byte>(), Is.True);
        }

        [Test]
        public void CheckIfIsDefinition()
        {
            using World world = new();

            Entity entityA = new(world);
            entityA.AddComponent<byte>();
            entityA.AddComponent<float>();

            Entity entityB = new(world);
            entityB.AddComponent<byte>();
            entityB.AddComponent<float>();
            entityB.CreateArray<char>();

            Definition definition = new([ComponentType.Get<byte>(), ComponentType.Get<float>()], [ArrayType.Get<char>()]);

            Assert.That(entityA.Is(definition), Is.False);
            Assert.That(entityB.Is(definition), Is.True);
        }
    }
}