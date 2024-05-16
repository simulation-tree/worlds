﻿using System;
using System.Collections.Generic;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public class SerializationTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAnyAllocation();
        }

        [Test]
        public void SaveWorld()
        {
            using World world = new();
            EntityID a = world.CreateEntity();
            world.AddComponent(a, new Fruit(42));
            world.AddComponent(a, new Apple("Hello, World!"));
            EntityID temporary = world.CreateEntity();
            EntityID b = world.CreateEntity();
            world.AddComponent(b, new Fruit(43));
            EntityID c = world.CreateEntity();
            world.AddComponent(c, new Apple("Goodbye, World!"));
            world.DestroyEntity(temporary);

            List<EntityID> oldEntities = new();
            List<(EntityID, Apple)> apples = new();
            world.QueryComponents((in EntityID entity) =>
            {
                oldEntities.Add(entity);
            });

            world.QueryComponents((in EntityID entity, ref Apple apple) =>
            {
                apples.Add((entity, apple));
            });

            using BinaryWriter writer = new();
            writer.WriteSerializable(world);
            ReadOnlySpan<byte> data = writer.AsSpan();
            using BinaryReader reader = new(data);

            using World loadedWorld = reader.ReadSerializable<World>();
            List<EntityID> newEntities = new();
            List<(EntityID, Apple)> newApples = new();
            loadedWorld.QueryComponents((in EntityID entity) =>
            {
                newEntities.Add(entity);
            });

            loadedWorld.QueryComponents((in EntityID entity, ref Apple apple) =>
            {
                newApples.Add((entity, apple));
            });

            Assert.That(newEntities, Is.EquivalentTo(oldEntities));
            Assert.That(newApples, Is.EquivalentTo(apples));
        }

        [Test]
        public void BinaryReadAndWrite()
        {
            Span<Fruit> fruits =
            [
                new(1),
                new(3),
                new(6),
                new(-10),
            ];

            Big big = new(32, new Apple("apple"), fruits);
            using BinaryWriter writer = new();
            writer.WriteValue(big);
            using BinaryReader reader = new(writer.AsSpan());
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

            using BinaryReader reader = new(writer.AsSpan());
            ReadOnlySpan<byte> bytes = reader.ReadSpan<byte>(5);
            ReadOnlySpan<int> ints = reader.ReadSpan<int>(5);
            ReadOnlySpan<FixedString> strings = reader.ReadSpan<FixedString>(3);

            Assert.That(bytes.ToArray(), Is.EquivalentTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(ints.ToArray(), Is.EquivalentTo(new int[] { 1, 2, 3, 4, 5 }));
            Assert.That(strings.ToArray(), Is.EquivalentTo(new FixedString[] { "Hello", "World", "Goodbye" }));
        }

        [Test]
        public void ReadTooMuch()
        {
            using BinaryWriter writer = new();
            writer.WriteSpan<char>("The snake that eats its own tail");
            using BinaryReader reader = new(writer.AsSpan());
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

            using BinaryWriter writer = new();
            writer.WriteSerializable(complicated);
            using BinaryReader reader = new(writer.AsSpan());
            using Complicated loadedComplicated = reader.ReadSerializable<Complicated>();

            Assert.That(loadedComplicated.List.Length, Is.EqualTo(complicated.List.Length));
            for (int i = 0; i < complicated.List.Length; i++)
            {
                Assert.That(loadedComplicated.List[i], Is.EqualTo(complicated.List[i]));
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
            using BinaryWriter writer = new();
        }

        public struct Types : IDisposable, ISerializable, IDeserializable
        {
            private UnmanagedList<RuntimeType> types;

            public readonly ReadOnlySpan<RuntimeType> List => types.AsSpan();

            public Types()
            {
                this.types = new();
            }

            public readonly void Add<T>() where T : unmanaged
            {
                types.Add(RuntimeType.Get<T>());
            }

            public void Dispose()
            {
                types.Dispose();
            }

            void IDeserializable.Deserialize(ref BinaryReader reader)
            {
                byte count = reader.ReadValue<byte>();
                types = new();
                for (int i = 0; i < count; i++)
                {
                    RuntimeType type = reader.ReadValue<RuntimeType>();
                    types.Add(type);
                }
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                for (uint i = 0; i < types.Count; i++)
                {
                    writer.WriteValue(types[i]);
                }
            }
        }

        public struct Complicated : IDisposable, ISerializable, IDeserializable
        {
            private UnmanagedList<Player> players;

            public readonly ReadOnlySpan<Player> List => players.AsSpan();

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

            void IDeserializable.Deserialize(ref BinaryReader reader)
            {
                byte count = reader.ReadValue<byte>();
                players = new();
                for (int i = 0; i < count; i++)
                {
                    Player player = reader.ReadSerializable<Player>();
                    players.Add(player);
                }
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                writer.WriteValue((byte)players.Count);
                foreach (Player player in players)
                {
                    writer.WriteSerializable(player);
                }
            }
        }

        public struct Player : IDisposable, ISerializable, IDeserializable, IEquatable<Player>
        {
            public uint hp;
            public uint damage;
            private UnmanagedList<Fruit> inventory;

            public ReadOnlySpan<Fruit> Inventory => inventory.AsSpan();

            public Player(uint hp, uint damage)
            {
                this.hp = hp;
                this.damage = damage;
                this.inventory = new();
            }

            public override string ToString()
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

            void IDeserializable.Deserialize(ref BinaryReader reader)
            {
                hp = reader.ReadValue<uint>();
                damage = reader.ReadValue<uint>();
                inventory = new();
                uint count = reader.ReadValue<uint>();
                for (int i = 0; i < count; i++)
                {
                    inventory.Add(reader.ReadValue<Fruit>());
                }
            }

            void ISerializable.Serialize(BinaryWriter writer)
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

        public readonly struct Big(int a, Apple apple, ReadOnlySpan<Fruit> fruits) : IDisposable, IEquatable<Big>
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