using Unmanaged;

namespace Worlds.Tests
{
    public class DefinitionTests : WorldTests
    {
        [Test]
        public void CompareEquality()
        {
            Definition a = new([ComponentType.Get<Float>()], [ArrayType.Get<Byte>()]);
            Definition b = new([ComponentType.Get<Float>()], [ArrayType.Get<Byte>()]);

            Assert.That(a, Is.EqualTo(b));

            a = a.AddComponentType<Integer>();
            a = a.AddComponentType<Double>();
            a = a.AddComponentType<Character>();

            Assert.That(a, Is.Not.EqualTo(b));

            b = b.AddComponentType<Double>();
            b = b.AddComponentType<Character>();
            b = b.AddComponentType<Integer>();

            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void NotEqual()
        {
            Definition a = new([ComponentType.Get<Float>()], [ArrayType.Get<Byte>()]);
            Definition b = new([ComponentType.Get<Float>()], [ArrayType.Get<Float>()]);

            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void AddingAndIndexing()
        {
            Definition a = new();
            a = a.AddComponentType<Integer>();
            a = a.AddArrayType<Double>();
            a = a.AddComponentType<Character>();
            a = a.AddArrayType<Float>();

            Assert.That(a.componentTypeCount, Is.EqualTo(2));
            Assert.That(a.arrayTypeCount, Is.EqualTo(2));
            USpan<ComponentType> componentTypes = stackalloc ComponentType[a.componentTypeCount];
            USpan<ArrayType> arrayTypes = stackalloc ArrayType[a.arrayTypeCount];
            a.CopyComponentTypesTo(componentTypes);
            a.CopyArrayTypesTo(arrayTypes);
            Assert.That(componentTypes.Contains(ComponentType.Get<Integer>()), Is.True);
            Assert.That(componentTypes.Contains(ComponentType.Get<Character>()), Is.True);
            Assert.That(arrayTypes.Contains(ArrayType.Get<Double>()), Is.True);
            Assert.That(arrayTypes.Contains(ArrayType.Get<Float>()), Is.True);
        }

        [Test]
        public void CreateDefinitionFromMany()
        {
            Definition definition = new Definition().AddComponentTypes<Integer, Character, Double, Float>();
            Assert.That(definition.componentTypeCount, Is.EqualTo(4));
            Assert.That(definition.arrayTypeCount, Is.EqualTo(0));
            Assert.That(definition.ContainsComponent<Integer>(), Is.True);
            Assert.That(definition.ContainsComponent<Character>(), Is.True);
            Assert.That(definition.ContainsComponent<Double>(), Is.True);
            Assert.That(definition.ContainsComponent<Float>(), Is.True);
        }

        [Test]
        public void QueryUsingDefinition()
        {
            using World world = new();

            uint entityA = world.CreateEntity();
            world.AddComponent(entityA, (Byte)1);
            world.AddComponent(entityA, (Float)1);
            world.CreateArray(entityA, "Hello".AsUSpan().As<Character>());

            uint entityB = world.CreateEntity();
            world.AddComponent(entityB, (Byte)2);

            Definition ByteDataComponent = new([ComponentType.Get<Byte>()], []);
            Definition charArray = new([], [ArrayType.Get<Character>()]);

            using DefinitionQuery ByteDataQuery = new(ByteDataComponent);
            ByteDataQuery.Update(world);
            Assert.That(ByteDataQuery.Count, Is.EqualTo(2));
            Assert.That(ByteDataQuery[0], Is.EqualTo(entityB));
            Assert.That(ByteDataQuery[1], Is.EqualTo(entityA));

            using DefinitionQuery charQuery = new(charArray);
            charQuery.Update(world);
            Assert.That(charQuery.Count, Is.EqualTo(1));
            Assert.That(charQuery[0], Is.EqualTo(entityA));
        }

        [Test]
        public void ContainsComponentTypes()
        {
            Definition definition = new([ComponentType.Get<Byte>(), ComponentType.Get<Float>()], [ArrayType.Get<Character>()]);
            Assert.That(definition.ContainsComponent<Byte>(), Is.True);
            Assert.That(definition.ContainsComponent<Float>(), Is.True);
            Assert.That(definition.ContainsComponent<Character>(), Is.False);
            Assert.That(definition.ContainsArray<Character>(), Is.True);
        }

        [Test]
        public void CreateFromDefinition()
        {
            using World world = new();

            Definition definition = new([ComponentType.Get<Byte>(), ComponentType.Get<Float>()], [ArrayType.Get<Character>()]);
            uint entity = world.CreateEntity(definition);
            Byte defaultByte = world.GetComponent<Byte>(entity);
            Float defaultFloat = world.GetComponent<Float>(entity);
            USpan<Character> defaultCharArray = world.GetArray<Character>(entity);

            Assert.That(defaultByte, Is.EqualTo(default(Byte)));
            Assert.That(defaultFloat, Is.EqualTo(default(Float)));
            Assert.That(defaultCharArray.Length, Is.EqualTo(0));
        }

        [Test]
        public void BecomeADefinition()
        {
            using World world = new();

            Definition definitionA = new([ComponentType.Get<Byte>(), ComponentType.Get<Float>()], [ArrayType.Get<Character>()]);

            Entity entity = new(world);
            entity.Become(definitionA);

            Assert.That(entity.ContainsComponent<Byte>(), Is.True);
            Assert.That(entity.ContainsComponent<Float>(), Is.True);
            Assert.That(entity.ContainsArray<Character>(), Is.True);
            Assert.That(entity.ContainsArray<Byte>(), Is.False);
            Assert.That(entity.ContainsComponent<Character>(), Is.False);
            Assert.That(entity.GetComponent<Byte>(), Is.EqualTo(default(Byte)));
            Assert.That(entity.GetComponent<Float>(), Is.EqualTo(default(Float)));
            Assert.That(entity.GetArray<Character>().Length, Is.EqualTo(0));

            Definition definitionB = new([], [ArrayType.Get<Byte>()]);

            entity.Become(definitionB);

            Assert.That(entity.ContainsArray<Byte>(), Is.True);
        }

        [Test]
        public void CheckIfIsDefinition()
        {
            using World world = new();

            Entity entityA = new(world);
            entityA.AddComponent<Byte>();
            entityA.AddComponent<Float>();

            Entity entityB = new(world);
            entityB.AddComponent<Byte>();
            entityB.AddComponent<Float>();
            entityB.CreateArray<Character>();

            Definition definition = new([ComponentType.Get<Byte>(), ComponentType.Get<Float>()], [ArrayType.Get<Character>()]);

            Assert.That(entityA.Is(definition), Is.False);
            Assert.That(entityB.Is(definition), Is.True);
        }
    }
}