using System;

namespace Worlds.Tests
{
    public unsafe class ComponentChunkTests : WorldTests
    {
        [Test]
        public void AddEntityNoComponents()
        {
            using Schema schema = CreateSchema();
            Chunk chunk = new(schema);
            int index = 0;
            chunk.AddEntity(7, ref index);
            Assert.That(chunk.Entities.Length, Is.EqualTo(1));
            Assert.That(chunk.Entities[0], Is.EqualTo(7));
            chunk.Dispose();
        }

        [Test]
        public void AddEntityWithComponents()
        {
            Schema schema = CreateSchema();
            int integerType = schema.GetComponentType<Integer>();
            int floatType = schema.GetComponentType<Float>();

            Definition definition = new();
            definition.AddComponentTypes<Integer, Float>(schema);
            Chunk chunk = new(schema, definition);
            uint entity = 7;
            int index = 0;
            chunk.AddEntity(entity, ref index);
            ref Integer intComponent = ref chunk.GetComponent<Integer>(index, integerType);
            ref Float floatComponent = ref chunk.GetComponent<Float>(index, floatType);
            intComponent = 42;
            floatComponent = 3.14f;
            Assert.That(chunk.Entities.Length, Is.EqualTo(1));
            Assert.That(chunk.Entities[0], Is.EqualTo(entity));
            ComponentEnumerator<Integer> intComponents = chunk.GetComponents<Integer>(integerType);
            ComponentEnumerator<Float> floatComponents = chunk.GetComponents<Float>(floatType);
            Assert.That(intComponents.length, Is.EqualTo(1));
            Assert.That(intComponents[0], Is.EqualTo((Integer)42));
            Assert.That(floatComponents.length, Is.EqualTo(1));
            Assert.That(floatComponents[0], Is.EqualTo((Float)3.14f));
            chunk.Dispose();
            schema.Dispose();
        }

        [Test]
        public void RemovingEntity()
        {
            Schema schema = CreateSchema();
            int integerType = schema.GetComponentType<Integer>();
            int floatType = schema.GetComponentType<Float>();

            Definition definition = new();
            definition.AddComponentTypes<Integer, Float>(schema);
            Chunk chunk = new(schema, definition);
            uint entity = 7;
            int index = 0;
            chunk.AddEntity(entity, ref index);
            ref Integer intComponent = ref chunk.GetComponent<Integer>(index, integerType);
            ref Float floatComponent = ref chunk.GetComponent<Float>(index, floatType);
            intComponent = 42;
            floatComponent = 3.14f;
            chunk.RemoveEntityAt(index);
            Assert.That(chunk.Entities.Length, Is.EqualTo(0));
            ComponentEnumerator<Integer> intComponents = chunk.GetComponents<Integer>(integerType);
            ComponentEnumerator<Float> floatComponents = chunk.GetComponents<Float>(floatType);
            Assert.That(intComponents.length, Is.EqualTo(0));
            Assert.That(floatComponents.length, Is.EqualTo(0));
            chunk.Dispose();
            schema.Dispose();
        }

        [Test]
        public void MovingEntity()
        {
            Schema schema = CreateSchema();
            int integerType = schema.GetComponentType<Integer>();
            int floatType = schema.GetComponentType<Float>();
            int fruitType = schema.GetComponentType<Fruit>();

            Chunk chunkA = new(schema);
            Chunk currentChunk = chunkA;
            uint entity = 7;
            int index = 0;
            chunkA.AddEntity(entity, ref index);

            Definition definitionB = new();
            definitionB.AddComponentType(integerType);
            Chunk chunkB = new(schema, definitionB);
            Chunk.MoveEntityAt(entity, ref index, ref currentChunk, chunkB);
            ref Integer intComponent = ref chunkB.GetComponent<Integer>(index, integerType);
            intComponent = 42;

            Assert.That(chunkA.Entities.Length, Is.EqualTo(0));
            Assert.That(chunkB.Entities.Length, Is.EqualTo(1));
            Assert.That(chunkB.Entities[0], Is.EqualTo(entity));
            ComponentEnumerator<Integer> intComponents = chunkB.GetComponents<Integer>(integerType);
            Assert.That(intComponents.length, Is.EqualTo(1));
            Assert.That(intComponents[0], Is.EqualTo((Integer)42));

            Definition definitionC = new();
            definitionC.AddComponentTypes(new BitMask(floatType, integerType));
            Chunk chunkC = new(schema, definitionC);
            Chunk.MoveEntityAt(entity, ref index, ref currentChunk, chunkC);
            ref Float floatComponent = ref chunkC.GetComponent<Float>(index, floatType);
            floatComponent = 3.14f;

            intComponents = chunkB.GetComponents<Integer>(integerType);
            Assert.That(intComponents.length, Is.EqualTo(0));

            Assert.That(chunkB.Entities.Length, Is.EqualTo(0));
            Assert.That(chunkC.Entities.Length, Is.EqualTo(1));
            Assert.That(chunkC.Entities[0], Is.EqualTo(entity));

            ComponentEnumerator<Float> floatComponents = chunkC.GetComponents<Float>(floatType);
            Assert.That(floatComponents.length, Is.EqualTo(1));
            Assert.That(floatComponents[0], Is.EqualTo((Float)3.14f));

            Definition definitionD = new();
            definitionD.AddComponentType(fruitType);
            Chunk chunkD = new(schema, definitionD);
            Chunk.MoveEntityAt(entity, ref index, ref currentChunk, chunkD);

            Assert.That(chunkC.Entities.Length, Is.EqualTo(0));
            Assert.That(chunkD.Entities.Length, Is.EqualTo(1));
            Assert.That(chunkD.Entities[0], Is.EqualTo(entity));

            chunkA.Dispose();
            chunkB.Dispose();
            chunkC.Dispose();
            chunkD.Dispose();
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
            Chunk chunkA = new(schema, definitionA);
            Chunk chunkB = new(schema, definitionB);
            int hashA = chunkA.GetHashCode();
            int hashB = chunkB.GetHashCode();
            Assert.That(hashA, Is.EqualTo(hashB));
            chunkA.Dispose();
            chunkB.Dispose();
        }

        [Test]
        public void CopyIntoSpan()
        {
            using Schema schema = CreateSchema();
            Definition definition = new();
            int intType = schema.GetComponentType<Integer>();
            int floatType = schema.GetComponentType<Float>();
            definition.AddComponentType(intType);
            definition.AddComponentType(floatType);
            using Chunk chunk = new(schema, definition);
            uint a = 7;
            uint b = 8;
            uint c = 9;
            int aIndex = chunk.AddEntity(a);
            int bIndex = chunk.AddEntity(b);
            int cIndex = chunk.AddEntity(c);
            chunk.SetComponent(aIndex, intType, new Integer(32));
            chunk.SetComponent(bIndex, intType, new Integer(64));
            chunk.SetComponent(cIndex, intType, new Integer(128));

            ComponentEnumerator<Integer> intComponents = chunk.GetComponents<Integer>(intType);
            Assert.That(intComponents.length, Is.EqualTo(3));
            Span<Integer> destination = stackalloc Integer[intComponents.length];
            intComponents.CopyTo(destination);
            Assert.That(destination[0].value, Is.EqualTo(32));
            Assert.That(destination[1].value, Is.EqualTo(64));
            Assert.That(destination[2].value, Is.EqualTo(128));
        }

        [Test]
        public void Indexing()
        {
            using Schema schema = CreateSchema();
            Definition definition = new();
            int intType = schema.GetComponentType<Integer>();
            int floatType = schema.GetComponentType<Float>();
            definition.AddComponentType(intType);
            definition.AddComponentType(floatType);
            using Chunk chunk = new(schema, definition);
            uint a = 7;
            uint b = 8;
            uint c = 9;
            int aIndex = chunk.AddEntity(a);
            int bIndex = chunk.AddEntity(b);
            int cIndex = chunk.AddEntity(c);
            chunk.SetComponent(aIndex, intType, new Integer(32));
            chunk.SetComponent(bIndex, intType, new Integer(64));
            chunk.SetComponent(cIndex, intType, new Integer(128));

            ComponentEnumerator<Integer> intComponents = chunk.GetComponents<Integer>(intType);
            Assert.That(intComponents.length, Is.EqualTo(3));
            Assert.That(intComponents[0].value, Is.EqualTo(32));
            Assert.That(intComponents[1].value, Is.EqualTo(64));
            Assert.That(intComponents[2].value, Is.EqualTo(128));
        }
    }
}