﻿using Collections;
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
            Definition definition = new();
            definition.AddComponentTypes<Integer, Float>(schema);
            ComponentChunk chunk = new(definition, schema);
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
            Definition definition = new();
            definition.AddComponentTypes<Integer, Float>(schema);
            ComponentChunk chunk = new(definition, schema);
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
            ComponentChunk chunkA = new(default, schema);
            uint entity = 7;
            uint oldIndex = chunkA.AddEntity(entity);

            Definition definitionB = new();
            definitionB.AddComponentType<Integer>(schema);
            ComponentChunk chunkB = new(definitionB, schema);
            uint newIndex = chunkA.MoveEntity(entity, chunkB);
            ref Integer intComponent = ref chunkB.GetComponent<Integer>(newIndex);
            intComponent = 42;

            Assert.That(chunkA.Entities, Is.Empty);
            Assert.That(chunkB.Entities, Has.Count.EqualTo(1));
            Assert.That(chunkB.Entities[0], Is.EqualTo(entity));
            List<Integer> intComponents = chunkB.GetComponents<Integer>();
            Assert.That(intComponents, Has.Count.EqualTo(1));
            Assert.That(intComponents[0], Is.EqualTo((Integer)42));

            Definition definitionC = new();
            definitionC.AddComponentTypes<Float, Integer>(schema);
            ComponentChunk chunkC = new(definitionC, schema);
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
            Definition definitionA = new();
            definitionA.AddComponentTypes<Integer, Float>(schema);
            Definition definitionB = new();
            definitionB.AddComponentTypes<Float, Integer>(schema);
            ComponentChunk chunkA = new(definitionA, schema);
            ComponentChunk chunkB = new(definitionB, schema);
            int hashA = chunkA.GetHashCode();
            int hashB = chunkB.GetHashCode();
            Assert.That(hashA, Is.EqualTo(hashB));
            chunkA.Dispose();
            chunkB.Dispose();
        }
    }
}