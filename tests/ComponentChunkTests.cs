using Collections;
using Unmanaged;

namespace Worlds.Tests
{
    public unsafe class ComponentChunkTests : WorldTests
    {
        [Test]
        public void AddEntityNoComponents()
        {
            ComponentChunk chunk = new();
            chunk.AddEntity(7);
            Assert.That(chunk.Entities, Has.Count.EqualTo(1));
            Assert.That(chunk.Entities[0], Is.EqualTo(7));
            chunk.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddEntityWithComponents()
        {
            ComponentChunk chunk = new([ComponentType.Get<Integer>(), ComponentType.Get<Float>()]);
            uint entity = 7;
            uint index = chunk.AddEntity(entity);
            ref Integer intComponent = ref chunk.GetComponentRef<Integer>(index);
            ref Float floatComponent = ref chunk.GetComponentRef<Float>(index);
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
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemovingEntity()
        {
            ComponentChunk chunk = new([ComponentType.Get<Integer>(), ComponentType.Get<Float>()]);
            uint entity = 7;
            uint index = chunk.AddEntity(entity);
            ref Integer intComponent = ref chunk.GetComponentRef<Integer>(index);
            ref Float floatComponent = ref chunk.GetComponentRef<Float>(index);
            intComponent = 42;
            floatComponent = 3.14f;
            chunk.RemoveEntity(entity);
            Assert.That(chunk.Entities, Is.Empty);
            List<Integer> intComponents = chunk.GetComponents<Integer>();
            List<Float> floatComponents = chunk.GetComponents<Float>();
            Assert.That(intComponents, Is.Empty);
            Assert.That(floatComponents, Is.Empty);
            chunk.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void MovingEntity()
        {
            ComponentChunk chunkA = new([]);
            uint entity = 7;
            uint oldIndex = chunkA.AddEntity(entity);

            ComponentChunk chunkB = new([ComponentType.Get<Integer>()]);
            uint newIndex = chunkA.MoveEntity(entity, chunkB);
            ref Integer intComponent = ref chunkB.GetComponentRef<Integer>(newIndex);
            intComponent = 42;

            Assert.That(chunkA.Entities, Is.Empty);
            Assert.That(chunkB.Entities, Has.Count.EqualTo(1));
            Assert.That(chunkB.Entities[0], Is.EqualTo(entity));
            List<Integer> intComponents = chunkB.GetComponents<Integer>();
            Assert.That(intComponents, Has.Count.EqualTo(1));
            Assert.That(intComponents[0], Is.EqualTo((Integer)42));

            ComponentChunk chunkC = new([ComponentType.Get<Float>(), ComponentType.Get<Integer>()]);
            uint newerIndex = chunkB.MoveEntity(entity, chunkC);
            ref Float floatComponent = ref chunkC.GetComponentRef<Float>(newerIndex);
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
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void HashTypes()
        {
            ComponentChunk chunkA = new([ComponentType.Get<Integer>(), ComponentType.Get<Float>()]);
            ComponentChunk chunkB = new([ComponentType.Get<Float>(), ComponentType.Get<Integer>()]);
            int hashA = chunkA.TypesMask.GetHashCode();
            int hashB = chunkB.TypesMask.GetHashCode();
            Assert.That(hashA, Is.EqualTo(hashB));
            chunkA.Dispose();
            chunkB.Dispose();
        }
    }
}