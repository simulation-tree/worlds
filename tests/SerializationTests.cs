using Collections;
using System;
using System.Linq;
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
            world.AddComponent(a, new Apple("Hello, World!"));
            uint temporary = world.CreateEntity();
            uint b = world.CreateEntity();
            world.AddComponent(b, new Fruit(43));
            uint c = world.CreateEntity();
            world.AddComponent(c, new Apple("Goodbye, World!"));
            world.DestroyEntity(temporary);
            uint list = world.CreateEntity();
            world.CreateArray<char>(list, "Well hello there list".AsUSpan());

            System.Collections.Generic.List<uint> oldEntities = world.Entities.ToList();
            System.Collections.Generic.List<(uint, Apple)> apples = new();
            world.ForEach((in uint entity, ref Apple apple) =>
            {
                apples.Add((entity, apple));
            });

            using BinaryWriter writer = new();
            writer.WriteObject(world);
            world.Dispose();

            USpan<byte> data = writer.GetBytes();
            using BinaryReader reader = new(data);

            World.SerializationContext.GetComponentType = (fullTypeName) =>
            {
                if (fullTypeName.SequenceEqual(typeof(Fruit).FullName.AsSpan()))
                {
                    return ComponentType.Get<Fruit>();
                }
                else if (fullTypeName.SequenceEqual(typeof(Apple).FullName.AsSpan()))
                {
                    return ComponentType.Get<Apple>();
                }

                throw new Exception($"Unknown type {fullTypeName.ToString()}");
            };

            World.SerializationContext.GetArrayType = (fullTypeName) =>
            {
                if (fullTypeName.SequenceEqual(typeof(char).FullName.AsSpan()))
                {
                    return ArrayType.Get<char>();
                }

                throw new Exception($"Unknown type {fullTypeName.ToString()}");
            };

            using World loadedWorld = reader.ReadObject<World>();
            System.Collections.Generic.List<uint> newEntities = loadedWorld.Entities.ToList();
            System.Collections.Generic.List<(uint, Apple)> newApples = new();
            loadedWorld.ForEach((in uint entity, ref Apple apple) =>
            {
                newApples.Add((entity, apple));
            });

            Assert.That(newEntities, Is.EquivalentTo(oldEntities));
            Assert.That(newApples, Is.EquivalentTo(apples));
            Assert.That(loadedWorld.ContainsArray<char>(list), Is.True);
            Assert.That(loadedWorld.GetArray<char>(list).ToArray(), Is.EqualTo("Well hello there list"));
        }

        [Test]
        public void AppendSavedWorld()
        {
            using World prefabWorld = new();
            uint a = prefabWorld.CreateEntity();
            prefabWorld.AddComponent(a, new Fruit(42));
            prefabWorld.AddComponent(a, new Apple("Hello, World!"));
            prefabWorld.AddComponent(a, new Prefab());

            using BinaryWriter writer = new();
            writer.WriteObject(prefabWorld);

            using World world = new();
            uint b = world.CreateEntity();
            world.AddComponent(b, new Fruit(43));

            uint c = world.CreateEntity();
            world.AddComponent(c, new Apple("Goodbye, World!"));

            World.SerializationContext.GetComponentType = (fullTypeName) =>
            {
                if (fullTypeName.SequenceEqual(typeof(Prefab).FullName.AsSpan()))
                {
                    return ComponentType.Get<Prefab>();
                }
                else if (fullTypeName.SequenceEqual(typeof(Fruit).FullName.AsSpan()))
                {
                    return ComponentType.Get<Fruit>();
                }
                else if (fullTypeName.SequenceEqual(typeof(Apple).FullName.AsSpan()))
                {
                    return ComponentType.Get<Apple>();
                }

                throw new Exception($"Unknown type {fullTypeName.ToString()}");
            };

            using BinaryReader reader = new(writer);
            using World loadedWorld = reader.ReadObject<World>();
            world.Append(loadedWorld);

            world.TryGetFirstComponent<Prefab>(out uint prefabEntity, out _);
            Assert.That(prefabEntity, Is.Not.EqualTo((uint)0));
            Assert.That(world.ContainsEntity(prefabEntity), Is.True);
            Assert.That(world.GetComponent<Fruit>(prefabEntity).data, Is.EqualTo(42));
            Assert.That(world.GetComponent<Apple>(prefabEntity).data.ToString(), Is.EqualTo("Hello, World!"));
        }

        public struct Prefab
        {

        }

        [Test]
        public void BinaryReadAndWrite()
        {
            USpan<Fruit> fruits =
            [
                new(1),
                new(3),
                new(6),
                new(-10),
            ];

            Big big = new(32, new Apple("apple"), fruits);
            using BinaryWriter writer = new();
            writer.WriteValue(big);
            using BinaryReader reader = new(writer.GetBytes());
            using Big loadedBig = reader.ReadValue<Big>();
            Assert.That(loadedBig, Is.EqualTo(big));
        }

        [Test]
        public void SaveAndLoadSpans()
        {
            using BinaryWriter writer = new();
            writer.WriteSpan<byte>([1, 2, 3, 4, 5]);
            writer.WriteSpan<int>([1, 2, 3, 4, 5]);
            writer.WriteSpan<FixedString>(["Hello", "World", "Goodbye"]);

            using BinaryReader reader = new(writer.GetBytes());
            USpan<byte> bytes = reader.ReadSpan<byte>(5);
            USpan<int> ints = reader.ReadSpan<int>(5);
            USpan<FixedString> strings = reader.ReadSpan<FixedString>(3);

            Assert.That(bytes.ToArray(), Is.EquivalentTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(ints.ToArray(), Is.EquivalentTo(new int[] { 1, 2, 3, 4, 5 }));
            Assert.That(strings.ToArray(), Is.EquivalentTo(new FixedString[] { "Hello", "World", "Goodbye" }));
        }

        [Test]
        public void CheckSerializable()
        {
            using Complicated complicated = new();
            Player player1 = new(100, 10);
            player1.Add(new Fruit(32));
            player1.Add(new Fruit(6123231));
            Player player2 = new(200, 20);
            player2.Add(new Fruit(32));
            Player player3 = new(300, 30);
            player3.Add(new Fruit(123123213));
            complicated.Add(player1);
            complicated.Add(player2);
            complicated.Add(player3);

            using BinaryWriter writer = new();
            writer.WriteObject(complicated);
            using BinaryReader reader = new(writer.GetBytes());
            using Complicated loadedComplicated = reader.ReadObject<Complicated>();

            Assert.That(loadedComplicated.List.Length, Is.EqualTo(complicated.List.Length));
            for (uint i = 0; i < complicated.List.Length; i++)
            {
                Player actual = loadedComplicated.List[i];
                Player expected = complicated.List[i];
                Assert.That(actual.Inventory.Length, Is.EqualTo(expected.Inventory.Length));
                for (uint j = 0; j < actual.Inventory.Length; j++)
                {
                    Fruit actualFruit = actual.Inventory[j];
                    Fruit expectedFruit = expected.Inventory[j];
                    Assert.That(actualFruit, Is.EqualTo(expectedFruit));
                }

                Assert.That(actual, Is.EqualTo(expected));
            }
        }

        public struct Types : IDisposable, ISerializable
        {
            private List<ComponentType> types;

            public readonly USpan<ComponentType> List => types.AsSpan();

            public Types()
            {
                this.types = new();
            }

            public readonly void Add<T>() where T : unmanaged
            {
                types.Add(ComponentType.Get<T>());
            }

            public void Dispose()
            {
                types.Dispose();
            }

            void ISerializable.Read(BinaryReader reader)
            {
                byte count = reader.ReadValue<byte>();
                types = new();
                for (uint i = 0; i < count; i++)
                {
                    ComponentType type = reader.ReadValue<ComponentType>();
                    types.Add(type);
                }
            }

            void ISerializable.Write(BinaryWriter writer)
            {
                for (uint i = 0; i < types.Count; i++)
                {
                    writer.WriteValue(types[i]);
                }
            }
        }

        public struct Complicated : IDisposable, ISerializable
        {
            private List<Player> players;

            public readonly USpan<Player> List => players.AsSpan();

            public Complicated()
            {
                players = new();
            }

            public readonly void Add(Player player)
            {
                players.Add(player);
            }

            public readonly void Dispose()
            {
                foreach (Player player in players)
                {
                    player.Dispose();
                }

                players.Dispose();
            }

            void ISerializable.Read(BinaryReader reader)
            {
                byte count = reader.ReadValue<byte>();
                players = new();
                for (uint i = 0; i < count; i++)
                {
                    Player player = reader.ReadObject<Player>();
                    players.Add(player);
                }
            }

            void ISerializable.Write(BinaryWriter writer)
            {
                writer.WriteValue((byte)players.Count);
                foreach (Player player in players)
                {
                    writer.WriteObject(player);
                }
            }
        }

        public struct Player : IDisposable, ISerializable, IEquatable<Player>
        {
            public uint hp;
            public uint damage;
            private List<Fruit> inventory;

            public readonly USpan<Fruit> Inventory => inventory.AsSpan();

            public Player(uint hp, uint damage)
            {
                this.hp = hp;
                this.damage = damage;
                this.inventory = new();
            }

            public readonly override string ToString()
            {
                return $"Player: HP={hp}, Damage={damage}";
            }

            public void Dispose()
            {
                inventory.Dispose();
            }

            public void Add(Fruit fruit)
            {
                inventory.Add(fruit);
            }

            void ISerializable.Read(BinaryReader reader)
            {
                hp = reader.ReadValue<uint>();
                damage = reader.ReadValue<uint>();
                inventory = new();
                uint count = reader.ReadValue<uint>();
                for (uint i = 0; i < count; i++)
                {
                    inventory.Add(reader.ReadValue<Fruit>());
                }
            }

            void ISerializable.Write(BinaryWriter writer)
            {
                writer.WriteValue(hp);
                writer.WriteValue(damage);
                writer.WriteValue(inventory.Count);
                foreach (Fruit fruit in inventory)
                {
                    writer.WriteValue(fruit);
                }
            }

            public readonly override bool Equals(object? obj)
            {
                return obj is Player player && Equals(player);
            }

            public readonly bool Equals(Player other)
            {
                return hp == other.hp && damage == other.damage && InventoryContentsEqual(other);
            }

            public readonly bool InventoryContentsEqual(Player other)
            {
                if (inventory.Count != other.inventory.Count)
                {
                    return false;
                }

                for (uint i = 0; i < inventory.Count; i++)
                {
                    if (inventory[i] != other.inventory[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public readonly override int GetHashCode()
            {
                return HashCode.Combine(hp, damage, inventory);
            }

            public static bool operator ==(Player left, Player right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Player left, Player right)
            {
                return !(left == right);
            }
        }

        public readonly struct Big(int a, Apple apple, USpan<Fruit> fruits) : IDisposable, IEquatable<Big>
        {
            public readonly int a = a;
            public readonly Apple apple = apple;
            public readonly List<Fruit> fruits = new(fruits);

            public void Dispose()
            {
                fruits.Dispose();
            }

            public override bool Equals(object? obj)
            {
                return obj is Big big && Equals(big);
            }

            public bool Equals(Big other)
            {
                return a == other.a && fruits.Equals(other.fruits) && apple.data == other.apple.data;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(a, apple, fruits);
            }

            public static bool operator ==(Big left, Big right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Big left, Big right)
            {
                return !(left == right);
            }
        }

        public readonly struct Fruit : IEquatable<Fruit>
        {
            public readonly int data;

            public Fruit(int data)
            {
                this.data = data;
            }

            public override bool Equals(object? obj)
            {
                return obj is Fruit fruit && Equals(fruit);
            }

            public bool Equals(Fruit other)
            {
                return data == other.data;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(data);
            }

            public static bool operator ==(Fruit left, Fruit right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Fruit left, Fruit right)
            {
                return !(left == right);
            }
        }

        public readonly struct Apple : IEquatable<Apple>
        {
            public readonly FixedString data;

            public Apple(FixedString data)
            {
                this.data = data;
            }

            public override bool Equals(object? obj)
            {
                return obj is Apple apple && Equals(apple);
            }

            public bool Equals(Apple other)
            {
                return data.Equals(other.data);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(data);
            }

            public static bool operator ==(Apple left, Apple right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Apple left, Apple right)
            {
                return !(left == right);
            }
        }
    }
}