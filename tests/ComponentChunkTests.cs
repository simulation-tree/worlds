using Collections;
using Unmanaged;

namespace Worlds.Tests
{
    public unsafe class ComponentChunkTests : WorldTests
    {
        [Test]
        public void AddEntityNoComponents()
        {
            Schema schema = CreateSchema();
            ComponentChunk chunk = new(schema);
            chunk.AddEntity(7);
            Assert.That(chunk.Entities, Has.Count.EqualTo(1));
            Assert.That(chunk.Entities[0], Is.EqualTo(7));
            chunk.Dispose();
            schema.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddEntityWithComponents()
        {
            Schema schema = CreateSchema();
            ComponentChunk chunk = new([schema.GetComponent<Integer>(), schema.GetComponent<Float>()], schema);
            uint entity = 7;
            uint index = chunk.AddEntity(entity);
            ref Integer intComponent = ref chunk.GetComponent<Integer>(index);
            ref Float floatComponent = ref chunk.GetComponent<Float>(index);
            intComponent = 42;
            floatComponent = 3.14f;
            Assert.That(chunk.Entities, Has.Count.EqualTo(1));
            Assert.That(chunk.Entities[0], Is.EqualTo(entity));
            List<Integer> intComponents = chunk.GetComponents<Integer>();
            List<Float> floatComponents = chunk.GetComponents<Float>();
            Assert.That(intComponents, Has.Count.EqualTo(1));
            Assert.That(intComponents[0], Is.EqualTo((Integer)42));
            Assert.That(floatComponents, Has.Count.EqualTo(1));
            Assert.That(floatComponents[0], Is.EqualTo((Float)3.14f));
            chunk.Dispose();
            schema.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemovingEntity()
        {
            Schema schema = CreateSchema();
            ComponentChunk chunk = new([schema.GetComponent<Integer>(), schema.GetComponent<Float>()], schema);
            uint entity = 7;
            uint index = chunk.AddEntity(entity);
            ref Integer intComponent = ref chunk.GetComponent<Integer>(index);
            ref Float floatComponent = ref chunk.GetComponent<Float>(index);
            intComponent = 42;
            floatComponent = 3.14f;
            chunk.RemoveEntity(entity);
            Assert.That(chunk.Entities, Is.Empty);
            List<Integer> intComponents = chunk.GetComponents<Integer>();
            List<Float> floatComponents = chunk.GetComponents<Float>();
            Assert.That(intComponents, Is.Empty);
            Assert.That(floatComponents, Is.Empty);
            chunk.Dispose();
            schema.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void MovingEntity()
        {
            Schema schema = CreateSchema();
            ComponentChunk chunkA = new([], schema);
            uint entity = 7;
            uint oldIndex = chunkA.AddEntity(entity);

            ComponentChunk chunkB = new([schema.GetComponent<Integer>()], schema);
            uint newIndex = chunkA.MoveEntity(entity, chunkB);
            ref Integer intComponent = ref chunkB.GetComponent<Integer>(newIndex);
            intComponent = 42;

            Assert.That(chunkA.Entities, Is.Empty);
            Assert.That(chunkB.Entities, Has.Count.EqualTo(1));
            Assert.That(chunkB.Entities[0], Is.EqualTo(entity));
            List<Integer> intComponents = chunkB.GetComponents<Integer>();
            Assert.That(intComponents, Has.Count.EqualTo(1));
            Assert.That(intComponents[0], Is.EqualTo((Integer)42));

            ComponentChunk chunkC = new([schema.GetComponent<Float>(), schema.GetComponent<Integer>()], schema);
            uint newerIndex = chunkB.MoveEntity(entity, chunkC);
            ref Float floatComponent = ref chunkC.GetComponent<Float>(newerIndex);
            floatComponent = 3.14f;

            Assert.That(chunkB.GetComponents<Integer>(), Is.Empty);
            Assert.That(chunkB.Entities, Is.Empty);
            Assert.That(chunkC.Entities, Has.Count.EqualTo(1));
            Assert.That(chunkC.Entities[0], Is.EqualTo(entity));
            List<Float> floatComponents = chunkC.GetComponents<Float>();
            Assert.That(floatComponents, Has.Count.EqualTo(1));
            Assert.That(floatComponents[0], Is.EqualTo((Float)3.14f));

            chunkA.Dispose();
            chunkB.Dispose();
            chunkC.Dispose();
            schema.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void HashTypes()
        {
            using Schema schema = CreateSchema();
            ComponentChunk chunkA = new([schema.GetComponent<Integer>(), schema.GetComponent<Float>()], schema);
            ComponentChunk chunkB = new([schema.GetComponent<Float>(), schema.GetComponent<Integer>()], schema);
            int hashA = chunkA.GetHashCode();
            int hashB = chunkB.GetHashCode();
            Assert.That(hashA, Is.EqualTo(hashB));
            chunkA.Dispose();
            chunkB.Dispose();
        }
    }
}