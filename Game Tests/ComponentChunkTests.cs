using Game.ECS;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public unsafe class ComponentChunkTests
    {
        [Test]
        public void AddEntityNoComponents()
        {
            UnsafeComponentChunk* chunk = UnsafeComponentChunk.Allocate([]);
            EntityID entity = EntityID.Assume(7);
            UnsafeComponentChunk.Add(chunk, entity);
            Assert.That(UnsafeComponentChunk.GetEntities(chunk), Has.Count.EqualTo(1));
            Assert.That(UnsafeComponentChunk.GetEntities(chunk)[0], Is.EqualTo(entity));
            UnsafeComponentChunk.Free(ref chunk);
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddEntityWithComponents()
        {
            UnsafeComponentChunk* chunk = UnsafeComponentChunk.Allocate([RuntimeType.Get<int>(), RuntimeType.Get<float>()]);
            EntityID entity = EntityID.Assume(7);
            UnsafeComponentChunk.Add(chunk, entity);
            ref int intComponent = ref UnsafeComponentChunk.GetComponentRef<int>(chunk, entity);
            ref float floatComponent = ref UnsafeComponentChunk.GetComponentRef<float>(chunk, entity);
            intComponent = 42;
            floatComponent = 3.14f;
            Assert.That(UnsafeComponentChunk.GetEntities(chunk), Has.Count.EqualTo(1));
            Assert.That(UnsafeComponentChunk.GetEntities(chunk)[0], Is.EqualTo(entity));
            UnmanagedList<int> intComponents = UnsafeComponentChunk.GetComponents<int>(chunk);
            UnmanagedList<float> floatComponents = UnsafeComponentChunk.GetComponents<float>(chunk);
            Assert.That(intComponents, Has.Count.EqualTo(1));
            Assert.That(intComponents[0], Is.EqualTo(42));
            Assert.That(floatComponents, Has.Count.EqualTo(1));
            Assert.That(floatComponents[0], Is.EqualTo(3.14f));
            UnsafeComponentChunk.Free(ref chunk);
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemovingEntity()
        {
            UnsafeComponentChunk* chunk = UnsafeComponentChunk.Allocate([RuntimeType.Get<int>(), RuntimeType.Get<float>()]);
            EntityID entity = EntityID.Assume(7);
            UnsafeComponentChunk.Add(chunk, entity);
            ref int intComponent = ref UnsafeComponentChunk.GetComponentRef<int>(chunk, entity);
            ref float floatComponent = ref UnsafeComponentChunk.GetComponentRef<float>(chunk, entity);
            intComponent = 42;
            floatComponent = 3.14f;
            UnsafeComponentChunk.Remove(chunk, entity);
            Assert.That(UnsafeComponentChunk.GetEntities(chunk), Is.Empty);
            UnmanagedList<int> intComponents = UnsafeComponentChunk.GetComponents<int>(chunk);
            UnmanagedList<float> floatComponents = UnsafeComponentChunk.GetComponents<float>(chunk);
            Assert.That(intComponents, Is.Empty);
            Assert.That(floatComponents, Is.Empty);
            UnsafeComponentChunk.Free(ref chunk);
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void MovingEntity()
        {
            UnsafeComponentChunk* chunkA = UnsafeComponentChunk.Allocate([]);
            EntityID entity = EntityID.Assume(7);
            UnsafeComponentChunk.Add(chunkA, entity);

            UnsafeComponentChunk* chunkB = UnsafeComponentChunk.Allocate([RuntimeType.Get<int>()]);
            UnsafeComponentChunk.Move(chunkA, entity, chunkB);
            ref int intComponent = ref UnsafeComponentChunk.GetComponentRef<int>(chunkB, entity);
            intComponent = 42;

            Assert.That(UnsafeComponentChunk.GetEntities(chunkA), Is.Empty);
            Assert.That(UnsafeComponentChunk.GetEntities(chunkB), Has.Count.EqualTo(1));
            Assert.That(UnsafeComponentChunk.GetEntities(chunkB)[0], Is.EqualTo(entity));
            UnmanagedList<int> intComponents = UnsafeComponentChunk.GetComponents<int>(chunkB);
            Assert.That(intComponents, Has.Count.EqualTo(1));
            Assert.That(intComponents[0], Is.EqualTo(42));

            UnsafeComponentChunk* chunkC = UnsafeComponentChunk.Allocate([RuntimeType.Get<float>(), RuntimeType.Get<int>()]);
            UnsafeComponentChunk.Move(chunkB, entity, chunkC);
            ref float floatComponent = ref UnsafeComponentChunk.GetComponentRef<float>(chunkC, entity);
            floatComponent = 3.14f;

            Assert.That(UnsafeComponentChunk.GetEntities(chunkB), Is.Empty);
            Assert.That(UnsafeComponentChunk.GetEntities(chunkC), Has.Count.EqualTo(1));
            Assert.That(UnsafeComponentChunk.GetEntities(chunkC)[0], Is.EqualTo(entity));
            UnmanagedList<float> floatComponents = UnsafeComponentChunk.GetComponents<float>(chunkC);
            Assert.That(floatComponents, Has.Count.EqualTo(1));
            Assert.That(floatComponents[0], Is.EqualTo(3.14f));

            UnsafeComponentChunk.Free(ref chunkA);
            UnsafeComponentChunk.Free(ref chunkB);
            UnsafeComponentChunk.Free(ref chunkC);
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void HashTypes()
        {
            UnsafeComponentChunk* chunkA = UnsafeComponentChunk.Allocate([RuntimeType.Get<int>(), RuntimeType.Get<float>()]);
            UnsafeComponentChunk* chunkB = UnsafeComponentChunk.Allocate([RuntimeType.Get<float>(), RuntimeType.Get<int>()]);
            int hashA = UnsafeComponentChunk.GetKey(chunkA);
            int hashB = UnsafeComponentChunk.GetKey(chunkB);
            Assert.That(hashA, Is.EqualTo(hashB));
            UnsafeComponentChunk.Free(ref chunkA);
            UnsafeComponentChunk.Free(ref chunkB);
        }
    }
}