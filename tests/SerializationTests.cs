using Collections;
using System;
using Unmanaged;

namespace Worlds.Tests
{
    public class SerializationTests : WorldTests
    {
        [Test]
        public void SaveWorld()
        {
            World world = new();
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
            world.CreateArray(list, "Well hello there list".AsUSpan().As<Character>());

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

            World.SerializationContext.GetComponentType = (type) =>
            {
                if (type.Name == typeof(Fruit).Name)
                {
                    return ComponentType.Get<Fruit>();
                }
                else if (type.Name == typeof(Apple).Name)
                {
                    return ComponentType.Get<Apple>();
                }
                else if (type.Name == typeof(Cherry).Name)
                {
                    return ComponentType.Get<Cherry>();
                }

                throw new Exception($"Unknown type {type}");
            };

            World.SerializationContext.GetArrayType = (type) =>
            {
                if (type.Name == typeof(Character).Name)
                {
                    return ArrayType.Get<Character>();
                }

                throw new Exception($"Unknown type {type}");
            };

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
            using World prefabWorld = new();
            uint a = prefabWorld.CreateEntity();
            prefabWorld.AddComponent(a, new Fruit(42));
            prefabWorld.AddComponent(a, new Cherry("Hello, World!"));
            prefabWorld.AddComponent(a, new Prefab());

            using BinaryWriter writer = new();
            writer.WriteObject(prefabWorld);

            using World world = new();
            uint b = world.CreateEntity();
            world.AddComponent(b, new Fruit(43));

            uint c = world.CreateEntity();
            world.AddComponent(c, new Cherry("Goodbye, World!"));

            World.SerializationContext.GetComponentType = (type) =>
            {
                if (type.Name == typeof(Fruit).Name)
                {
                    return ComponentType.Get<Fruit>();
                }
                else if (type.Name == typeof(Apple).Name)
                {
                    return ComponentType.Get<Apple>();
                }
                else if (type.Name == typeof(Cherry).Name)
                {
                    return ComponentType.Get<Cherry>();
                }
                else if (type.Name == typeof(Prefab).Name)
                {
                    return ComponentType.Get<Prefab>();
                }

                throw new Exception($"Unknown serialized component type {type}");
            };

            using BinaryReader reader = new (writer);
            using World loadedWorld = reader.ReadObject<World>();
            world.Append(loadedWorld);

            world.TryGetFirstEntityContainingComponent<Prefab>(out uint prefabEntity, out _);
            Assert.That(prefabEntity, Is.Not.EqualTo((uint)0));
            Assert.That(world.ContainsEntity(prefabEntity), Is.True);
            Assert.That(world.GetComponent<Fruit>(prefabEntity).data, Is.EqualTo(42));
            Assert.That(world.GetComponent<Cherry>(prefabEntity).stones.ToString(), Is.EqualTo("Hello, World!"));
        }
    }
}