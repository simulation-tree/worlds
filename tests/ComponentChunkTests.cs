using System.Runtime.InteropServices;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation.Tests
{
    public unsafe class ComponentChunkTests
    {
        [Test]
        public void AddEntityNoComponents()
        {
            ComponentChunk chunk = new([]);
            uint entity = new Union(7).entity;
            chunk.AddEntity(entity);
            Assert.That(chunk.Entities, Has.Count.EqualTo(1));
            Assert.That(chunk.Entities[0], Is.EqualTo(entity));
            chunk.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddEntityWithComponents()
        {
            ComponentChunk chunk = new([RuntimeType.Get<int>(), RuntimeType.Get<float>()]);
            uint entity = new Union(7).entity;
            uint index = chunk.AddEntity(entity);
            ref int intComponent = ref chunk.GetComponentRef<int>(index);
            ref float floatComponent = ref chunk.GetComponentRef<float>(index);
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
            uint entity = new Union(7).entity;
            uint index = chunk.AddEntity(entity);
            ref int intComponent = ref chunk.GetComponentRef<int>(index);
            ref float floatComponent = ref chunk.GetComponentRef<float>(index);
            intComponent = 42;
            floatComponent = 3.14f;
            chunk.RemoveEntity(entity);
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
            uint entity = new Union(7).entity;
            uint oldIndex = chunkA.AddEntity(entity);

            ComponentChunk chunkB = new([RuntimeType.Get<int>()]);
            uint newIndex = chunkA.MoveEntity(entity, chunkB);
            ref int intComponent = ref chunkB.GetComponentRef<int>(newIndex);
            intComponent = 42;

            Assert.That(chunkA.Entities, Is.Empty);
            Assert.That(chunkB.Entities, Has.Count.EqualTo(1));
            Assert.That(chunkB.Entities[0], Is.EqualTo(entity));
            UnmanagedList<int> intComponents = chunkB.GetComponents<int>();
            Assert.That(intComponents, Has.Count.EqualTo(1));
            Assert.That(intComponents[0], Is.EqualTo(42));

            ComponentChunk chunkC = new([RuntimeType.Get<float>(), RuntimeType.Get<int>()]);
            uint newerIndex = chunkB.MoveEntity(entity, chunkC);
            ref float floatComponent = ref chunkC.GetComponentRef<float>(newerIndex);
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

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct Union
        {
            [FieldOffset(0)]
            public readonly uint value;

            [FieldOffset(0)]
            public readonly uint entity;

            public Union(uint value)
            {
                this.value = value;
            }
        }
    }
}