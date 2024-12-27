using Unmanaged;

namespace Worlds.Tests
{
    public class DefinitionTests : WorldTests
    {
        [Test]
        public void CompareEquality()
        {
            using Schema schema = CreateSchema();
            Definition a = new([schema.GetComponent<Float>()], [schema.GetArrayElement<Byte>()]);
            Definition b = new([schema.GetComponent<Float>()], [schema.GetArrayElement<Byte>()]);

            Assert.That(a, Is.EqualTo(b));

            a = a.AddComponentType<Integer>(schema);
            a = a.AddComponentType<Double>(schema);
            a = a.AddComponentType<Character>(schema);

            Assert.That(a, Is.Not.EqualTo(b));

            b = b.AddComponentType<Double>(schema);
            b = b.AddComponentType<Character>(schema);
            b = b.AddComponentType<Integer>(schema);

            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void NotEqual()
        {
            using Schema schema = CreateSchema();
            Definition a = new([schema.GetComponent<Float>()], [schema.GetArrayElement<Byte>()]);
            Definition b = new([schema.GetComponent<Float>()], [schema.GetArrayElement<Float>()]);

            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void AddingAndIndexing()
        {
            using Schema schema = CreateSchema();
            Definition a = new();
            a = a.AddComponentType<Integer>(schema);
            a = a.AddArrayType<Double>(schema);
            a = a.AddComponentType<Character>(schema);
            a = a.AddArrayType<Float>(schema);

            Assert.That(a.ComponentTypesMask.Count, Is.EqualTo(2));
            Assert.That(a.ArrayTypesMask.Count, Is.EqualTo(2));
            USpan<ComponentType> componentTypes = stackalloc ComponentType[BitSet.Capacity];
            USpan<ArrayType> arrayTypes = stackalloc ArrayType[BitSet.Capacity];
            a.CopyComponentTypesTo(componentTypes);
            a.CopyArrayTypesTo(arrayTypes);
            Assert.That(componentTypes.Contains(schema.GetComponent<Integer>()), Is.True);
            Assert.That(componentTypes.Contains(schema.GetComponent<Character>()), Is.True);
            Assert.That(arrayTypes.Contains(schema.GetArrayElement<Double>()), Is.True);
            Assert.That(arrayTypes.Contains(schema.GetArrayElement<Float>()), Is.True);
        }

        [Test]
        public void CreateDefinitionFromMany()
        {
            using Schema schema = CreateSchema();
            Definition definition = new Definition().AddComponentTypes<Integer, Character, Double, Float>(schema);
            Assert.That(definition.ComponentTypesMask.Count, Is.EqualTo(4));
            Assert.That(definition.ArrayTypesMask.Count, Is.EqualTo(0));
            Assert.That(definition.ContainsComponent<Integer>(schema), Is.True);
            Assert.That(definition.ContainsComponent<Character>(schema), Is.True);
            Assert.That(definition.ContainsComponent<Double>(schema), Is.True);
            Assert.That(definition.ContainsComponent<Float>(schema), Is.True);
        }

        [Test]
        public void ContainsComponentTypes()
        {
            using Schema schema = CreateSchema();
            Definition definition = new([schema.GetComponent<Byte>(), schema.GetComponent<Float>()], [schema.GetArrayElement<Character>()]);
            Assert.That(definition.ContainsComponent<Byte>(schema), Is.True);
            Assert.That(definition.ContainsComponent<Float>(schema), Is.True);
            Assert.That(definition.ContainsComponent<Character>(schema), Is.False);
            Assert.That(definition.ContainsArray<Character>(schema), Is.True);
        }

        [Test]
        public void CreateFromDefinition()
        {
            using World world = CreateWorld();

            Definition definition = new([world.Schema.GetComponent<Byte>(), world.Schema.GetComponent<Float>()], [world.Schema.GetArrayElement<Character>()]);
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
            using World world = CreateWorld();

            Definition definitionA = new([world.Schema.GetComponent<Byte>(), world.Schema.GetComponent<Float>()], [world.Schema.GetArrayElement<Character>()]);

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

            Definition definitionB = new([], [world.Schema.GetArrayElement<Byte>()]);

            entity.Become(definitionB);

            Assert.That(entity.ContainsArray<Byte>(), Is.True);
        }

        [Test]
        public void CheckIfIsDefinition()
        {
            using World world = CreateWorld();

            Entity entityA = new(world);
            entityA.AddComponent<Byte>();
            entityA.AddComponent<Float>();

            Entity entityB = new(world);
            entityB.AddComponent<Byte>();
            entityB.AddComponent<Float>();
            entityB.CreateArray<Character>();

            Definition definition = new([world.Schema.GetComponent<Byte>(), world.Schema.GetComponent<Float>()], [world.Schema.GetArrayElement<Character>()]);

            Assert.That(entityA.Is(definition), Is.False);
            Assert.That(entityB.Is(definition), Is.True);
        }
    }
}