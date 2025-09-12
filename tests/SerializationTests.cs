using Collections.Generic;
using System;
using Types;
using Unmanaged;

namespace Worlds.Tests
{
    public class SerializationTests : WorldTests
    {
        [Test]
        public void SaveWorld()
        {
            World world = CreateWorld();
            uint a = world.CreateEntity();
            world.AddComponent(a, new Fruit(42));
            world.AddComponent(a, new Cherry("Hello, World!"));
            uint temporary = world.CreateEntity();
            uint b = world.CreateEntity();
            world.SetParent(b, a);
            world.AddComponent(b, new Fruit(43));
            uint c = world.CreateEntity();
            world.AddComponent(c, new Cherry("Goodbye, World!"));
            world.DestroyEntity(temporary);
            uint list = world.CreateEntity();
            world.CreateArray(list, "Well hello there list".AsSpan().As<char, Character>());

            uint d = world.CreateEntity();
            world.AddReference(d, c);
            world.AddReference(d, a);
            world.AddReference(d, b);

            Assert.That(world.GetReferenceCount(d), Is.EqualTo(3));

            using List<uint> oldEntities = new(world.Entities);
            using List<(uint, Fruit)> apples = new();
            foreach (uint entity in world.GetAllContaining<Fruit>())
            {
                apples.Add((entity, world.GetComponent<Fruit>(entity)));
            }

            using ByteWriter writer = new();
            writer.WriteObject(world);
            world.Dispose();

            Span<byte> data = writer.AsSpan();
            using ByteReader reader = new(data);
            using World loadedWorld = reader.ReadObject<World>();
            using List<uint> newEntities = new(loadedWorld.Entities);
            using List<(uint, Fruit)> newApples = new();
            foreach (uint entity in loadedWorld.GetAllContaining<Fruit>())
            {
                newApples.Add((entity, loadedWorld.GetComponent<Fruit>(entity)));
            }

            Assert.That(newEntities, Is.EquivalentTo(oldEntities));
            Assert.That(newApples, Is.EquivalentTo(apples));
            Assert.That(loadedWorld.ContainsEntity(a), Is.True);
            Assert.That(loadedWorld.ContainsEntity(b), Is.True);
            Assert.That(loadedWorld.GetParent(b), Is.EqualTo(a));

            list = default;
            Query arrayQuery = new(loadedWorld);
            arrayQuery.RequireArrayElement<Character>();
            foreach (uint entity in arrayQuery)
            {
                list = entity;
                break;
            }

            Assert.That(loadedWorld.ContainsEntity(list), Is.True);
            Assert.That(loadedWorld.ContainsArray<Character>(list), Is.True);
            Assert.That(loadedWorld.GetArray<Character>(list).AsSpan<char>().ToString(), Is.EqualTo("Well hello there list"));

            Assert.That(loadedWorld.ContainsEntity(d), Is.True);
            Assert.That(loadedWorld.GetReferenceCount(d), Is.EqualTo(3));
            Assert.That(loadedWorld.GetReference(d, (rint)1), Is.EqualTo(c));
            Assert.That(loadedWorld.GetReference(d, (rint)2), Is.EqualTo(a));
            Assert.That(loadedWorld.GetReference(d, (rint)3), Is.EqualTo(b));
        }

        [Test]
        public void AppendSavedWorld()
        {
            using World prefabWorld = CreateWorld();
            uint a = prefabWorld.CreateEntity();
            prefabWorld.AddComponent(a, new Fruit(42));
            prefabWorld.AddComponent(a, new Cherry("Hello, World!"));
            prefabWorld.AddTag<IsPrefab>(a);

            uint aa = prefabWorld.CreateEntity();
            prefabWorld.AddComponent(aa, new Fruit(43));
            prefabWorld.AddTag<IsThing>(aa);

            using ByteWriter writer = new();
            writer.WriteObject(prefabWorld);

            using World world = CreateWorld();
            uint b = world.CreateEntity();
            world.AddComponent(b, new Fruit(43));

            uint c = world.CreateEntity();
            world.AddComponent(c, new Cherry("Goodbye, World!"));

            using ByteReader reader = new(writer);
            using World loadedWorld = reader.ReadObject<World>();
            world.Append(loadedWorld);

            Query prefabQuery = new(world);
            prefabQuery.RequireTag<IsPrefab>();
            prefabQuery.TryGetFirst(out uint prefabEntity);

            Query thingQuery = new(world);
            thingQuery.RequireTag<IsThing>();
            thingQuery.TryGetFirst(out uint thingEntity);

            Assert.That(prefabEntity, Is.Not.EqualTo(0u));
            Assert.That(world.ContainsEntity(prefabEntity), Is.True);
            Assert.That(world.ContainsEntity(thingEntity), Is.True);
            Assert.That(world.GetComponent<Fruit>(prefabEntity).data, Is.EqualTo(42));
            Assert.That(world.GetComponent<Cherry>(prefabEntity).stones.ToString(), Is.EqualTo("Hello, World!"));
            Assert.That(world.GetComponent<Fruit>(thingEntity).data, Is.EqualTo(43));
            Assert.That(world.ContainsTag<IsThing>(thingEntity), Is.True);
        }

        [Test]
        public void CheckSchemaOfLoadedWorld()
        {
            World prefabWorld = CreateWorld();
            int fruitType = prefabWorld.Schema.GetComponentType<Fruit>();
            int cherryType = prefabWorld.Schema.GetComponentType<Cherry>();
            uint a = prefabWorld.CreateEntity();
            prefabWorld.AddComponent(a, new Fruit(42));
            prefabWorld.AddComponent(a, new Cherry("Hello, World!"));
            prefabWorld.AddTag<IsPrefab>(a);

            using ByteWriter writer = new();
            writer.WriteObject(prefabWorld);
            prefabWorld.Dispose();

            using ByteReader reader = new(writer);
            using World loadedWorld = reader.ReadObject<World>();

            Schema loadedSchema = loadedWorld.Schema;
            Assert.That(loadedSchema.TryGetComponentType(MetadataRegistry.GetType<Fruit>(), out int loadedFruitType), Is.True);
            Assert.That(loadedSchema.TryGetComponentType(MetadataRegistry.GetType<Cherry>(), out int loadedCherryType), Is.True);
            Assert.That(loadedSchema.ContainsTagType<IsPrefab>(), Is.True);
            Assert.That(loadedFruitType, Is.EqualTo(fruitType));
            Assert.That(loadedCherryType, Is.EqualTo(cherryType));
        }

        [Test]
        public void ProcessSchema()
        {
            Schema prefabSchema = new();
            prefabSchema.RegisterComponent<Fruit>();
            prefabSchema.RegisterComponent<Cherry>();
            prefabSchema.RegisterTag<IsPrefab>();
            using World prefabWorld = new(prefabSchema);
            uint a = prefabWorld.CreateEntity();
            prefabWorld.AddComponent(a, new Fruit(42));
            prefabWorld.AddComponent(a, new Cherry("Hello, World!"));
            prefabWorld.AddTag<IsPrefab>(a);

            using ByteWriter writer = new();
            writer.WriteObject(prefabWorld);

            using ByteReader reader = new(writer);
            using World loadedWorld = World.Deserialize(reader, Process);

            static TypeMetadata Process(TypeMetadata type, DataType.Kind dataType)
            {
                if (type.Is<Fruit>())
                {
                    //replace fruit with another
                    return MetadataRegistry.GetType<Another>();
                }
                else
                {
                    return type;
                }
            }

            Schema loadedSchema = loadedWorld.Schema;
            Assert.That(loadedSchema.ContainsComponentType<Another>(), Is.True);
            Assert.That(loadedSchema.ContainsComponentType<Fruit>(), Is.False);
            Assert.That(loadedSchema.ContainsComponentType<Cherry>(), Is.True);
            Assert.That(loadedSchema.ContainsTagType<IsPrefab>(), Is.True);
            Assert.That(loadedSchema.GetTagType<IsPrefab>(), Is.EqualTo(0));
            Assert.That(loadedWorld.ContainsEntity(a), Is.True);
            Assert.That(loadedWorld.GetComponent<Another>(a).data, Is.EqualTo(42));
            Assert.That(loadedWorld.GetComponent<Cherry>(a).stones.ToString(), Is.EqualTo("Hello, World!"));
            Assert.That(loadedWorld.ContainsTag<IsPrefab>(a), Is.True);
        }
    }
}