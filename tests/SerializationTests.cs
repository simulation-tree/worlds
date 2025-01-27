using Collections;
using System;
using System.Runtime.InteropServices;
using Types;
using Unmanaged;
using Worlds.Functions;

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
            world.AddComponent(b, new Fruit(43));
            uint c = world.CreateEntity();
            world.AddComponent(c, new Cherry("Goodbye, World!"));
            world.DestroyEntity(temporary);
            uint list = world.CreateEntity();
            world.CreateArray(list, new USpan<char>("Well hello there list").As<Character>());

            using List<uint> oldEntities = new(world.Entities);
            using List<(uint, Apple)> apples = new();
            foreach (uint entity in world.GetAllContaining<Apple>())
            {
                apples.Add((entity, world.GetComponent<Apple>(entity)));
            }

            using BinaryWriter writer = new();
            writer.WriteObject(world);
            world.Dispose();

            USpan<byte> data = writer.GetBytes();
            using BinaryReader reader = new(data);
            using World loadedWorld = reader.ReadObject<World>();
            using List<uint> newEntities = new(loadedWorld.Entities);
            using List<(uint, Apple)> newApples = new();
            foreach (uint entity in loadedWorld.GetAllContaining<Apple>())
            {
                newApples.Add((entity, world.GetComponent<Apple>(entity)));
            }

            Assert.That(newEntities, Is.EquivalentTo(oldEntities));
            Assert.That(newApples, Is.EquivalentTo(apples));
            Assert.That(loadedWorld.ContainsArray<Character>(list), Is.True);
            Assert.That(loadedWorld.GetArray<Character>(list).As<char>().ToString(), Is.EqualTo("Well hello there list"));
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

            using BinaryWriter writer = new();
            writer.WriteObject(prefabWorld);

            using World world = CreateWorld();
            uint b = world.CreateEntity();
            world.AddComponent(b, new Fruit(43));

            uint c = world.CreateEntity();
            world.AddComponent(c, new Cherry("Goodbye, World!"));

            using BinaryReader reader = new(writer);
            using World loadedWorld = reader.ReadObject<World>();
            world.Append(loadedWorld);

            world.TryGetFirstComponent<IsPrefab>(out uint prefabEntity, out _);
            Assert.That(prefabEntity, Is.Not.EqualTo((uint)0));
            Assert.That(world.ContainsEntity(prefabEntity), Is.True);
            Assert.That(world.ContainsEntity(prefabEntity + 1), Is.True);
            Assert.That(world.GetComponent<Fruit>(prefabEntity).data, Is.EqualTo(42));
            Assert.That(world.GetComponent<Cherry>(prefabEntity).stones.ToString(), Is.EqualTo("Hello, World!"));
            Assert.That(world.GetComponent<Fruit>(prefabEntity + 1).data, Is.EqualTo(43));
            Assert.That(world.ContainsTag<IsThing>(prefabEntity + 1), Is.True);
        }

        [Test]
        public unsafe void CheckSchemaOfLoadedWorld()
        {
            World prefabWorld = CreateWorld();
            uint a = prefabWorld.CreateEntity();
            prefabWorld.AddComponent(a, new Fruit(42));
            prefabWorld.AddComponent(a, new Cherry("Hello, World!"));
            prefabWorld.AddTag<IsPrefab>(a);

            using BinaryWriter writer = new();
            writer.WriteObject(prefabWorld);
            prefabWorld.Dispose();

            using BinaryReader reader = new(writer);
            using World loadedWorld = reader.ReadObject<World>();

            Schema loadedSchema = loadedWorld.Schema;
            Assert.That(loadedSchema.TryGetComponentLayout(typeof(Fruit).FullName!, out TypeLayout fruitType), Is.True);
            Assert.That(loadedSchema.TryGetComponentLayout(typeof(Cherry).FullName!, out TypeLayout cherryType), Is.True);
            Assert.That(loadedSchema.TryGetComponentLayout(typeof(IsPrefab).FullName!, out TypeLayout prefabType), Is.True);
            Assert.That(fruitType.Size, Is.EqualTo(sizeof(Fruit)));
            Assert.That(cherryType.Size, Is.EqualTo(sizeof(Cherry)));
            Assert.That(prefabType.Size, Is.EqualTo(sizeof(IsPrefab)));
        }

        [Test]
        public unsafe void ProcessSchema()
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

            using BinaryWriter writer = new();
            writer.WriteObject(prefabWorld);

            using BinaryReader reader = new(writer);
            using World loadedWorld = World.Deserialize(reader, new(&Process));

            Schema loadedSchema = loadedWorld.Schema;

            [UnmanagedCallersOnly]
            static TypeLayout Process(ProcessSchema.Input input)
            {
                if (input.type.Is<Fruit>())
                {
                    //replace fruit with another
                    return TypeRegistry.Get<Another>();
                }
                else
                {
                    return input.type;
                }
            }

            Assert.That(loadedSchema.ContainsComponent<Another>(), Is.True);
            Assert.That(loadedSchema.ContainsComponent<Fruit>(), Is.False);
            Assert.That(loadedSchema.ContainsComponent<Cherry>(), Is.True);
            Assert.That(loadedSchema.ContainsTag<IsPrefab>(), Is.True);
            Assert.That(loadedWorld.ContainsEntity(a), Is.True);
            Assert.That(loadedWorld.GetComponent<Another>(a).data, Is.EqualTo(42));
            Assert.That(loadedWorld.GetComponent<Cherry>(a).stones.ToString(), Is.EqualTo("Hello, World!"));
            Assert.That(loadedWorld.ContainsTag<IsPrefab>(a), Is.True);
        }
    }
}