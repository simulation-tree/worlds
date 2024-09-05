using System;
using System.Collections.Generic;
using System.Linq;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation.Tests
{
    public class SerializationTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAny();
        }

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
            world.CreateArray<char>(list, "Well hello there list".AsSpan());

            List<uint> oldEntities = world.Entities.ToList();
            List<(uint, Apple)> apples = new();
            world.ForEach((in uint entity, ref Apple apple) =>
            {
                apples.Add((entity, apple));
            });

            using BinaryWriter writer = BinaryWriter.Create();
            writer.WriteObject(world);
            world.Dispose();

            USpan<byte> data = writer.GetBytes();
            using BinaryReader reader = new(data);

            using World loadedWorld = reader.ReadObject<World>();
            List<uint> newEntities = loadedWorld.Entities.ToList();
            List<(uint, Apple)> newApples = new();
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

            using BinaryWriter writer = BinaryWriter.Create();
            writer.WriteObject(prefabWorld);

            using World world = new();
            uint b = world.CreateEntity();
            world.AddComponent(b, new Fruit(43));

            uint c = world.CreateEntity();
            world.AddComponent(c, new Apple("Goodbye, World!"));

            using BinaryReader reader = new(writer);
            using World loadedWorld = reader.ReadObject<World>();
            world.Append(loadedWorld);

            world.TryGetFirstComponent<Prefab>(out uint prefabEntity, out _);
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
            using BinaryWriter writer = BinaryWriter.Create();
            writer.WriteValue(big);
            using BinaryReader reader = new(writer.GetBytes());
            using Big loadedBig = reader.ReadValue<Big>();
            Assert.That(loadedBig, Is.EqualTo(big));
        }

        [Test]
        public void SaveAndLoadSpans()
        {
            using BinaryWriter writer = BinaryWriter.Create();
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
        public void ReadTooMuch()
        {
            using BinaryWriter writer = BinaryWriter.Create();
            writer.WriteSpan<char>("The snake that eats its own tail".AsSpan());
            using BinaryReader reader = new(writer.GetBytes());
            Assert.Throws<InvalidOperationException>(() => reader.ReadSpan<char>(100));
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

            using BinaryWriter writer = BinaryWriter.Create();
            writer.WriteObject(complicated);
            using BinaryReader reader = new(writer.GetBytes());
            using Complicated loadedComplicated = reader.ReadObject<Complicated>();

            Assert.That(loadedComplicated.List.length, Is.EqualTo(complicated.List.length));
            for (uint i = 0; i < complicated.List.length; i++)
            {
                Player actual = loadedComplicated.List[i];
                Player expected = complicated.List[i];
                Assert.That(actual.Inventory.length, Is.EqualTo(expected.Inventory.length));
                for (uint j = 0; j < actual.Inventory.length; j++)
                {
                    Fruit actualFruit = actual.Inventory[j];
                    Fruit expectedFruit = expected.Inventory[j];
                    Assert.That(actualFruit, Is.EqualTo(expectedFruit));
                }

                Assert.That(actual, Is.EqualTo(expected));
            }
        }

        [Test]
        public void SerializeWithObjects()
        {
            using Types types = new();
            types.Add<int>();
            types.Add<float>();
            types.Add<double>();

            Dictionary<uint, object> objects = new();
            using BinaryWriter writer = BinaryWriter.Create();
        }

        public struct Types : IDisposable, ISerializable
        {
            private UnmanagedList<RuntimeType> types;

            public readonly USpan<RuntimeType> List => types.AsSpan();

            public Types()
            {
                this.types = UnmanagedList<RuntimeType>.Create();
            }

            public readonly void Add<T>() where T : unmanaged
            {
                types.Add(RuntimeType.Get<T>());
            }

            public void Dispose()
            {
                types.Dispose();
            }

            void ISerializable.Read(BinaryReader reader)
            {
                byte count = reader.ReadValue<byte>();
                types = UnmanagedList<RuntimeType>.Create();
                for (uint i = 0; i < count; i++)
                {
                    RuntimeType type = reader.ReadValue<RuntimeType>();
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
            private UnmanagedList<Player> players;

            public readonly USpan<Player> List => players.AsSpan();

            public Complicated()
            {
                players = UnmanagedList<Player>.Create();
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
                players = UnmanagedList<Player>.Create();
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
            private UnmanagedList<Fruit> inventory;

            public readonly USpan<Fruit> Inventory => inventory.AsSpan();

            public Player(uint hp, uint damage)
            {
                this.hp = hp;
                this.damage = damage;
                this.inventory = UnmanagedList<Fruit>.Create();
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
                inventory = UnmanagedList<Fruit>.Create();
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

            public override bool Equals(object? obj)
            {
                return obj is Player player && Equals(player);
            }

            public bool Equals(Player other)
            {
                return hp == other.hp && damage == other.damage && inventory.GetContentHashCode() == other.inventory.GetContentHashCode();
            }

            public override int GetHashCode()
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
            public readonly UnmanagedList<Fruit> fruits = new(fruits);

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