using System;

namespace Worlds.Tests
{
    public class DefinitionTests : WorldTests
    {
        [Test]
        public void CompareEquality()
        {
            using Schema schema = CreateSchema();

            Definition a = new();
            a.AddComponentTypes<Float, Byte>(schema);
            a.AddArrayType<Character>(schema);
            a.AddTagType<IsThing>(schema);

            Definition b = new();
            b.AddComponentTypes<Float, Byte>(schema);
            b.AddArrayType<Character>(schema);
            b.AddTagType<IsThing>(schema);

            Assert.That(a, Is.EqualTo(b));

            a.AddComponentType<Integer>(schema);
            a.AddComponentType<Double>(schema);
            a.AddComponentType<Character>(schema);

            Assert.That(a, Is.Not.EqualTo(b));

            b.AddComponentType<Double>(schema);
            b.AddComponentType<Character>(schema);
            b.AddComponentType<Integer>(schema);

            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void NotEqual()
        {
            using Schema schema = CreateSchema();
            Definition a = new();
            a.AddComponentType<Float>(schema);
            a.AddArrayType<Byte>(schema);

            Definition b = new();
            b.AddComponentType<Float>(schema);
            b.AddArrayType<Float>(schema);

            Assert.That(a, Is.Not.EqualTo(b));

            Definition c = b;
            c.AddTagType<IsThing>(schema);

            Assert.That(b, Is.Not.EqualTo(c));
        }

        [Test]
        public void AddingAndIndexing()
        {
            using Schema schema = CreateSchema();
            Definition a = new();
            a.AddComponentType<Integer>(schema);
            a.AddArrayType<Double>(schema);
            a.AddComponentType<Character>(schema);
            a.AddArrayType<Float>(schema);

            Assert.That(a.componentTypes.Count, Is.EqualTo(2));
            Assert.That(a.arrayTypes.Count, Is.EqualTo(2));
            Span<ComponentType> componentTypes = stackalloc ComponentType[BitMask.Capacity];
            Span<ArrayType> arrayTypes = stackalloc ArrayType[BitMask.Capacity];
            a.CopyComponentTypesTo(componentTypes);
            a.CopyArrayTypesTo(arrayTypes);
            Assert.That(componentTypes.Contains(schema.GetComponentType<Integer>()), Is.True);
            Assert.That(componentTypes.Contains(schema.GetComponentType<Character>()), Is.True);
            Assert.That(arrayTypes.Contains(schema.GetArrayType<Double>()), Is.True);
            Assert.That(arrayTypes.Contains(schema.GetArrayType<Float>()), Is.True);
        }

        [Test]
        public void CreateDefinitionFromMany()
        {
            using Schema schema = CreateSchema();

            Definition definition = new();
            definition.AddComponentType<Integer>(schema);
            definition.AddComponentType<Character>(schema);
            definition.AddComponentType<Double>(schema);
            definition.AddComponentType<Float>(schema);

            Assert.That(definition.componentTypes.Count, Is.EqualTo(4));
            Assert.That(definition.arrayTypes.Count, Is.EqualTo(0));
            Assert.That(definition.ContainsComponent<Integer>(schema), Is.True);
            Assert.That(definition.ContainsComponent<Character>(schema), Is.True);
            Assert.That(definition.ContainsComponent<Double>(schema), Is.True);
            Assert.That(definition.ContainsComponent<Float>(schema), Is.True);
        }

        [Test]
        public void ContainsComponentTypes()
        {
            using Schema schema = CreateSchema();

            Definition definition = new();
            definition.AddComponentType<Byte>(schema);
            definition.AddComponentType<Float>(schema);
            definition.AddArrayType<Character>(schema);


            Assert.That(definition.ContainsComponent<Byte>(schema), Is.True);
            Assert.That(definition.ContainsComponent<Float>(schema), Is.True);
            Assert.That(definition.ContainsComponent<Character>(schema), Is.False);
            Assert.That(definition.ContainsArray<Character>(schema), Is.True);
        }

        [Test]
        public void CreateFromDefinition()
        {
            using World world = CreateWorld();

            Definition definition = new();
            definition.AddComponentType<Byte>(world.Schema);
            definition.AddComponentType<Float>(world.Schema);
            definition.AddArrayType<Character>(world.Schema);

            uint entity = world.CreateEntity(definition);
            Byte defaultByte = world.GetComponent<Byte>(entity);
            Float defaultFloat = world.GetComponent<Float>(entity);
            Values<Character> defaultCharArray = world.GetArray<Character>(entity);

            Assert.That(defaultByte, Is.EqualTo(default(Byte)));
            Assert.That(defaultFloat, Is.EqualTo(default(Float)));
            Assert.That(defaultCharArray.Length, Is.EqualTo(0));
        }

        [Test]
        public void BecomeADefinition()
        {
            using World world = CreateWorld();

            Definition definitionA = new();
            definitionA.AddComponentType<Byte>(world.Schema);
            definitionA.AddComponentType<Float>(world.Schema);
            definitionA.AddArrayType<Character>(world.Schema);

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

            Definition definitionB = new();
            definitionB.AddArrayType<Byte>(world.Schema);

            Assert.That(entity.ContainsArray<Byte>(), Is.False);

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

            Definition definition = new();
            definition.AddComponentType<Byte>(world.Schema);
            definition.AddComponentType<Float>(world.Schema);
            definition.AddArrayType<Character>(world.Schema);

            Assert.That(entityA.Is(definition), Is.False);
            Assert.That(entityB.Is(definition), Is.True);
        }

        [Test]
        public void VerifySizesOfTypesInArchetype()
        {
            using World world = CreateWorld();
            Archetype archetype = new(world.Schema);
            archetype.AddComponentType<Byte>();
            Assert.That(archetype.ComponentTypes.Count, Is.EqualTo(1));
            archetype.AddComponentType<Float>();
            Assert.That(archetype.ComponentTypes.Count, Is.EqualTo(2));
            archetype.AddArrayType<Character>();
            Assert.That(archetype.ArrayTypes.Count, Is.EqualTo(1));
            archetype.AddArrayType<Float>();
            Assert.That(archetype.ArrayTypes.Count, Is.EqualTo(2));
            archetype.AddArrayType<Byte>();
            Assert.That(archetype.ArrayTypes.Count, Is.EqualTo(3));

            Assert.That(archetype.ContainsComponent<Byte>(), Is.True);
            Assert.That(archetype.ContainsComponent<Float>(), Is.True);
            Assert.That(archetype.ContainsComponent<Character>(), Is.False);
            Assert.That(archetype.ContainsArray<Character>(), Is.True);
            Assert.That(archetype.ContainsArray<Float>(), Is.True);
            Assert.That(archetype.ContainsArray<Byte>(), Is.True);
            Assert.That(archetype.GetComponentSize<Byte>(), Is.EqualTo(sizeof(byte)));
            Assert.That(archetype.GetComponentSize<Float>(), Is.EqualTo(sizeof(float)));
            Assert.That(archetype.GetArrayElementSize<Character>(), Is.EqualTo(sizeof(char)));
            Assert.That(archetype.GetArrayElementSize<Float>(), Is.EqualTo(sizeof(float)));
            Assert.That(archetype.GetArrayElementSize<Byte>(), Is.EqualTo(sizeof(byte)));
        }

        [Test]
        public void BecomeAnotherEntity()
        {
            using World world = CreateWorld();

            Entity entity = new(world);

            Assert.That(entity.Is<AnotherEntity>(), Is.False);

            entity.Become<AnotherEntity>();

            Assert.That(entity.Is<AnotherEntity>(), Is.True);
            Assert.That(entity.ContainsComponent<Another>(), Is.True);
            Assert.That(entity.ContainsArray<Byte>(), Is.True);
        }
    }
}