using Unmanaged;

namespace Worlds.Tests
{
    public unsafe class ComponentChunkTests : WorldTests
    {
        [Test]
        public void AddEntityNoComponents()
        {
            Chunk chunk = new();
            chunk.AddEntity(7);
            Assert.That(chunk.Entities.Length, Is.EqualTo(1));
            Assert.That(chunk.Entities[0], Is.EqualTo(7));
            chunk.Dispose();
        }

        [Test]
        public void AddEntityWithComponents()
        {
            Schema schema = CreateSchema();
            ComponentType integerType = schema.GetComponentType<Integer>();
            ComponentType floatType = schema.GetComponentType<Float>();

            Definition definition = new();
            definition.AddComponentTypes<Integer, Float>(schema);
            Chunk chunk = new(definition, schema);
            uint entity = 7;
            uint index = chunk.AddEntity(entity);
            ref Integer intComponent = ref chunk.GetComponent<Integer>(index, integerType);
            ref Float floatComponent = ref chunk.GetComponent<Float>(index, floatType);
            intComponent = 42;
            floatComponent = 3.14f;
            Assert.That(chunk.Entities.Length, Is.EqualTo(1));
            Assert.That(chunk.Entities[0], Is.EqualTo(entity));
            USpan<Integer> intComponents = chunk.GetComponents<Integer>(integerType);
            USpan<Float> floatComponents = chunk.GetComponents<Float>(floatType);
            Assert.That(intComponents.Length, Is.EqualTo(1));
            Assert.That(intComponents[0], Is.EqualTo((Integer)42));
            Assert.That(floatComponents.Length, Is.EqualTo(1));
            Assert.That(floatComponents[0], Is.EqualTo((Float)3.14f));
            chunk.Dispose();
            schema.Dispose();
        }

        [Test]
        public void RemovingEntity()
        {
            Schema schema = CreateSchema();
            ComponentType integerType = schema.GetComponentType<Integer>();
            ComponentType floatType = schema.GetComponentType<Float>();

            Definition definition = new();
            definition.AddComponentTypes<Integer, Float>(schema);
            Chunk chunk = new(definition, schema);
            uint entity = 7;
            uint index = chunk.AddEntity(entity);
            ref Integer intComponent = ref chunk.GetComponent<Integer>(index, integerType);
            ref Float floatComponent = ref chunk.GetComponent<Float>(index, floatType);
            intComponent = 42;
            floatComponent = 3.14f;
            chunk.RemoveEntity(entity);
            Assert.That(chunk.Entities.Length, Is.EqualTo(0));
            USpan<Integer> intComponents = chunk.GetComponents<Integer>(integerType);
            USpan<Float> floatComponents = chunk.GetComponents<Float>(floatType);
            Assert.That(intComponents.Length, Is.EqualTo(0));
            Assert.That(floatComponents.Length, Is.EqualTo(0));
            chunk.Dispose();
            schema.Dispose();
        }

        [Test]
        public void MovingEntity()
        {
            Schema schema = CreateSchema();
            ComponentType integerType = schema.GetComponentType<Integer>();
            ComponentType floatType = schema.GetComponentType<Float>();

            Chunk chunkA = new(default, schema);
            uint entity = 7;
            uint oldIndex = chunkA.AddEntity(entity);

            Definition definitionB = new();
            definitionB.AddComponentType<Integer>(schema);
            Chunk chunkB = new(definitionB, schema);
            uint newIndex = chunkA.MoveEntity(entity, chunkB);
            ref Integer intComponent = ref chunkB.GetComponent<Integer>(newIndex, integerType);
            intComponent = 42;

            Assert.That(chunkA.Entities.Length, Is.EqualTo(0));
            Assert.That(chunkB.Entities.Length, Is.EqualTo(1));
            Assert.That(chunkB.Entities[0], Is.EqualTo(entity));
            USpan<Integer> intComponents = chunkB.GetComponents<Integer>(integerType);
            Assert.That(intComponents.Length, Is.EqualTo(1));
            Assert.That(intComponents[0], Is.EqualTo((Integer)42));

            Definition definitionC = new();
            definitionC.AddComponentTypes<Float, Integer>(schema);
            Chunk chunkC = new(definitionC, schema);
            uint newerIndex = chunkB.MoveEntity(entity, chunkC);
            ref Float floatComponent = ref chunkC.GetComponent<Float>(newerIndex, floatType);
            floatComponent = 3.14f;

            Assert.That(chunkB.GetComponents<Integer>(integerType).Length, Is.EqualTo(0));
            Assert.That(chunkB.Entities.Length, Is.EqualTo(0));
            Assert.That(chunkC.Entities.Length, Is.EqualTo(1));
            Assert.That(chunkC.Entities[0], Is.EqualTo(entity));
            USpan<Float> floatComponents = chunkC.GetComponents<Float>(floatType);
            Assert.That(floatComponents.Length, Is.EqualTo(1));
            Assert.That(floatComponents[0], Is.EqualTo((Float)3.14f));

            chunkA.Dispose();
            chunkB.Dispose();
            chunkC.Dispose();
            schema.Dispose();
        }

        [Test]
        public void HashTypes()
        {
            using Schema schema = CreateSchema();
            Definition definitionA = new();
            definitionA.AddComponentTypes<Integer, Float>(schema);
            Definition definitionB = new();
            definitionB.AddComponentTypes<Float, Integer>(schema);
            Chunk chunkA = new(definitionA, schema);
            Chunk chunkB = new(definitionB, schema);
            int hashA = chunkA.GetHashCode();
            int hashB = chunkB.GetHashCode();
            Assert.That(hashA, Is.EqualTo(hashB));
            chunkA.Dispose();
            chunkB.Dispose();
        }
    }
}