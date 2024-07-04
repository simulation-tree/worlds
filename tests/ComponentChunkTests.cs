using Unmanaged;
using Unmanaged.Collections;

namespace Simulation
{
    public unsafe class ComponentChunkTests
    {
        [Test]
        public void AddEntityNoComponents()
        {
            ComponentChunk chunk = new([]);
            EntityID entity = new(7);
            chunk.Add(entity);
            Assert.That(chunk.Entities, Has.Count.EqualTo(1));
            Assert.That(chunk.Entities[0], Is.EqualTo(entity));
            chunk.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddEntityWithComponents()
        {
            ComponentChunk chunk = new([RuntimeType.Get<int>(), RuntimeType.Get<float>()]);
            EntityID entity = new(7);
            chunk.Add(entity);
            ref int intComponent = ref chunk.GetComponentRef<int>(entity);
            ref float floatComponent = ref chunk.GetComponentRef<float>(entity);
            intComponent = 42;
            floatComponent = 3.14f;
            Assert.That(chunk.Entities, Has.Count.EqualTo(1));
            Assert.That(chunk.Entities[0], Is.EqualTo(entity));
            UnmanagedList<int> intComponents = chunk.GetComponents<int>();
            UnmanagedList<float> floatComponents = chunk.GetComponents<float>();
            Assert.That(intComponents, Has.Count.EqualTo(1));
            Assert.That(intComponents[0], Is.EqualTo(42));
            Assert.That(floatComponents, Has.Count.EqualTo(1));
            Assert.That(floatComponents[0], Is.EqualTo(3.14f));
            chunk.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemovingEntity()
        {
            ComponentChunk chunk = new([RuntimeType.Get<int>(), RuntimeType.Get<float>()]);
            EntityID entity = new(7);
            chunk.Add(entity);
            ref int intComponent = ref chunk.GetComponentRef<int>(entity);
            ref float floatComponent = ref chunk.GetComponentRef<float>(entity);
            intComponent = 42;
            floatComponent = 3.14f;
            chunk.Remove(entity);
            Assert.That(chunk.Entities, Is.Empty);
            UnmanagedList<int> intComponents = chunk.GetComponents<int>();
            UnmanagedList<float> floatComponents = chunk.GetComponents<float>();
            Assert.That(intComponents, Is.Empty);
            Assert.That(floatComponents, Is.Empty);
            chunk.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void MovingEntity()
        {
            ComponentChunk chunkA = new([]);
            EntityID entity = new(7);
            chunkA.Add(entity);

            ComponentChunk chunkB = new([RuntimeType.Get<int>()]);
            chunkA.Move(entity, chunkB);
            ref int intComponent = ref chunkB.GetComponentRef<int>(entity);
            intComponent = 42;

            Assert.That(chunkA.Entities, Is.Empty);
            Assert.That(chunkB.Entities, Has.Count.EqualTo(1));
            Assert.That(chunkB.Entities[0], Is.EqualTo(entity));
            UnmanagedList<int> intComponents = chunkB.GetComponents<int>();
            Assert.That(intComponents, Has.Count.EqualTo(1));
            Assert.That(intComponents[0], Is.EqualTo(42));

            ComponentChunk chunkC = new([RuntimeType.Get<float>(), RuntimeType.Get<int>()]);
            chunkB.Move(entity, chunkC);
            ref float floatComponent = ref chunkC.GetComponentRef<float>(entity);
            floatComponent = 3.14f;

            Assert.That(chunkB.Entities, Is.Empty);
            Assert.That(chunkC.Entities, Has.Count.EqualTo(1));
            Assert.That(chunkC.Entities[0], Is.EqualTo(entity));
            UnmanagedList<float> floatComponents = chunkC.GetComponents<float>();
            Assert.That(floatComponents, Has.Count.EqualTo(1));
            Assert.That(floatComponents[0], Is.EqualTo(3.14f));

            chunkA.Dispose();
            chunkB.Dispose();
            chunkC.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void HashTypes()
        {
            ComponentChunk chunkA = new([RuntimeType.Get<int>(), RuntimeType.Get<float>()]);
            ComponentChunk chunkB = new([RuntimeType.Get<float>(), RuntimeType.Get<int>()]);
            uint hashA = chunkA.Key;
            uint hashB = chunkB.Key;
            Assert.That(hashA, Is.EqualTo(hashB));
            chunkA.Dispose();
            chunkB.Dispose();
        }
    }
}