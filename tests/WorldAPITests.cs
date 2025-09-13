using Collections.Generic;
using System;
using Unmanaged;

namespace Worlds.Tests
{
    public class WorldAPITests : WorldTests
    {
        [Test]
        public void CreateAndDisposeWorld()
        {
            World world = CreateWorld();

            Assert.That(world.IsDisposed, Is.False);

            world.Dispose();

            Assert.That(world.IsDisposed, Is.True);
        }

#if DEBUG
        [Test]
        public void DisposeTwiceError()
        {
            World world = CreateWorld();
            world.Dispose();
            Assert.Throws<InvalidOperationException>(() => world.Dispose());
        }
#endif

        [TestCase(3)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(30)]
        [TestCase(100)]
        public void MaxEntityIDIsValid(int creationCount)
        {
            using World world = CreateWorld();
            Span<uint> entities = stackalloc uint[creationCount];
            world.CreateEntities(entities);
            Assert.That(world.MaxEntityValue, Is.EqualTo(entities[creationCount - 1]));
            Assert.That(world.MaxEntityValue, Is.EqualTo(entities.Length));

            using Array<bool> buffer = new(world.MaxEntityValue + 1);
            for (int i = 0; i < entities.Length; i++)
            {
                buffer[(int)entities[i]] = true;
            }
        }

        [Test]
        public void PreviewingEntityCreating()
        {
            using World world = CreateWorld();
            uint a = world.GetNextCreatedEntity(0);
            uint b = world.GetNextCreatedEntity(1);
            uint c = world.GetNextCreatedEntity(2);
            Assert.That(a, Is.EqualTo(world.CreateEntity()));
            Assert.That(b, Is.EqualTo(world.CreateEntity()));
            Assert.That(c, Is.EqualTo(world.CreateEntity()));
            world.DestroyEntity(b);
            uint d = world.GetNextCreatedEntity(0);
            Assert.That(d, Is.EqualTo(b));
            Assert.That(d, Is.EqualTo(world.CreateEntity()));
            uint e = world.GetNextCreatedEntity(0);
            Assert.That(e, Is.EqualTo(world.CreateEntity()));
            world.DestroyEntity(a);
            world.DestroyEntity(c);
            world.DestroyEntity(e);
            uint f = world.GetNextCreatedEntity(0);
            Assert.That(f, Is.EqualTo(world.CreateEntity()));
            uint g = world.GetNextCreatedEntity(0);
            Assert.That(g, Is.EqualTo(world.CreateEntity()));
            uint h = world.GetNextCreatedEntity(0);
            Assert.That(h, Is.EqualTo(world.CreateEntity()));
        }

        [Test]
        public void Clearing()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            world.AddComponent(a, new SimpleComponent("Hello World"));
            uint b = world.CreateEntity();
            world.AddComponent(b, new Another(42));
            uint c = world.CreateEntity();
            world.AddComponent(c, new SimpleComponent("Goodbye World"));
            world.AddComponent(c, new Another(84));

            Assert.That(world.Count, Is.EqualTo(3));
            Assert.That(world.ContainsEntity(a), Is.True);
            Assert.That(world.ContainsEntity(b), Is.True);
            Assert.That(world.ContainsEntity(c), Is.True);
            world.Clear();
            Assert.That(world.Count, Is.EqualTo(0));
            a = world.CreateEntity();
            Assert.That(world.ContainsEntity(a), Is.True);
            Assert.That(world.Count, Is.EqualTo(1));
            Assert.That(world.ContainsComponent<SimpleComponent>(a), Is.False);
            b = world.CreateEntity();
            Assert.That(world.ContainsEntity(b), Is.True);
            Assert.That(world.Count, Is.EqualTo(2));
            Assert.That(world.ContainsComponent<Another>(b), Is.False);
            c = world.CreateEntity();
            Assert.That(world.ContainsEntity(c), Is.True);
            Assert.That(world.Count, Is.EqualTo(3));
            Assert.That(world.ContainsComponent<SimpleComponent>(c), Is.False);
            Assert.That(world.ContainsComponent<Another>(c), Is.False);

            world.AddComponent(c, new SimpleComponent("Hello Again"));
            Assert.That(world.ContainsComponent<SimpleComponent>(c), Is.True);
        }

        [Test]
        public void CreateBulkEntities()
        {
            using World world = CreateWorld();
            Span<uint> entities = stackalloc uint[7];
            world.CreateEntities(entities);
            Assert.That(world.Count, Is.EqualTo(7));
            Assert.That(entities[0], Is.EqualTo(1));
            Assert.That(entities[1], Is.EqualTo(2));
            Assert.That(entities[2], Is.EqualTo(3));
            Assert.That(entities[3], Is.EqualTo(4));
            Assert.That(entities[4], Is.EqualTo(5));
            Assert.That(entities[5], Is.EqualTo(6));
            Assert.That(entities[6], Is.EqualTo(7));

            world.DestroyEntity(entities[0]);
            world.DestroyEntity(entities[1]);
            world.DestroyEntity(entities[2]);

            Assert.That(world.Count, Is.EqualTo(7 - 3));

            world.CreateEntities(entities);

            Assert.That(world.Count, Is.EqualTo(7 - 3 + 7));
            Assert.That(entities[0], Is.EqualTo(3));
            Assert.That(entities[1], Is.EqualTo(2));
            Assert.That(entities[2], Is.EqualTo(1));
            Assert.That(entities[3], Is.EqualTo(8));
            Assert.That(entities[4], Is.EqualTo(9));
            Assert.That(entities[5], Is.EqualTo(10));
            Assert.That(entities[6], Is.EqualTo(11));
        }

        [Test]
        public void CreateBulkWith1Component()
        {
            using World world = CreateWorld();
            Span<uint> entities = stackalloc uint[100];
            int componentType = world.Schema.GetComponentType<Another>();
            Definition definition = new(new BitMask(componentType));
            world.CreateEntities(entities, definition);
            for (int i = 0; i < entities.Length; i++)
            {
                world.SetComponent(entities[i], componentType, new Another((uint)i));
            }
        }

        [Test]
        public void CreateBulkWith3Components()
        {
            using World world = CreateWorld();
            Span<uint> entities = stackalloc uint[100];
            int componentType1 = world.Schema.GetComponentType<Another>();
            int componentType2 = world.Schema.GetComponentType<SimpleComponent>();
            int componentType3 = world.Schema.GetComponentType<TestComponent>();
            Definition definition = new(new BitMask(componentType1, componentType2, componentType3));
            world.CreateEntities(entities, definition);
            for (int i = 0; i < entities.Length; i++)
            {
                Chunk.Row row = world.GetChunkRow(entities[i]);
                row.SetComponent(componentType1, new Another((uint)i));
                row.SetComponent(componentType2, new SimpleComponent("aa"));
                row.SetComponent(componentType3, new TestComponent(i));
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(100)]
        public void CreateBulkEntitiesWithComponents(int creationCount)
        {
            using World world = CreateWorld();
            Span<uint> entities = stackalloc uint[creationCount];
            int component1 = world.Schema.GetComponentType<SimpleComponent>();
            int component2 = world.Schema.GetComponentType<TestComponent>();
            int component3 = world.Schema.GetComponentType<Another>();
            Definition definition = new(new BitMask(component1, component2, component3));
            world.CreateEntities(entities, definition);
            Assert.That(world.Count, Is.EqualTo(creationCount));
            for (int i = 0; i < entities.Length; i++)
            {
                uint entity = entities[i];
                Assert.That(world.ContainsEntity(entity), Is.True);
                Assert.That(world.GetDefinition(entity) == definition, Is.True);
                Chunk.Row row = world.GetChunkRow(entity);
                row.SetComponent(component1, new SimpleComponent("aa"));
                row.SetComponent(component2, new TestComponent(i));
                row.SetComponent(component3, new Another(32));
                Assert.That(world.GetComponent<SimpleComponent>(entity).data.ToString(), Is.EqualTo("aa"));
                Assert.That(world.GetComponent<TestComponent>(entity).value, Is.EqualTo(i));
                Assert.That(world.GetComponent<Another>(entity).data, Is.EqualTo(32));
            }
        }

        [Test]
        public void EnumerateRealEntities()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            uint d = world.CreateEntity();
            world.DestroyEntity(b);
            uint e = world.CreateEntity();

            using List<uint> entities = new();
            foreach (uint entity in world.Entities)
            {
                entities.Add(entity);
            }

            Assert.That(entities.Count, Is.EqualTo(4));
            Assert.That(entities, Has.Member(a));
            Assert.That(entities, Has.Member(c));
            Assert.That(entities, Has.Member(d));
            Assert.That(entities, Has.Member(e));
        }

        [Test]
        public void GetAddedComponent()
        {
            using World world = CreateWorld();
            uint entity = world.CreateEntity();
            SimpleComponent component = new("Hello World");
            world.AddComponent(entity, component);

            Assert.That(world.GetComponent<SimpleComponent>(entity), Is.EqualTo(component));
            world.RemoveComponentType<SimpleComponent>(entity);
            Assert.That(world.ContainsComponent<SimpleComponent>(entity), Is.False);
            Assert.That(world.ContainsAnyComponent<SimpleComponent>(), Is.False);
            world.AddComponent(entity, component);
            Assert.That(world.ContainsComponent<SimpleComponent>(entity), Is.True);
            Assert.That(world.ContainsAnyComponent<SimpleComponent>(), Is.True);
            world.DestroyEntity(entity);
            Assert.That(world.ContainsEntity(entity), Is.False);
        }

#if DEBUG
        [Test]
        public void ThrowIfEntityIsMissing()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            Assert.That(world.ContainsEntity(a), Is.True);
            world.DestroyEntity(a);
            Assert.That(world.ContainsEntity(a), Is.False);
            Assert.That(world.Count, Is.EqualTo(0));
            Assert.Throws<EntityIsMissingException>(() => world.GetComponent<SimpleComponent>(a));
            uint b = world.CreateEntity();
            Assert.That(world.ContainsEntity(b), Is.True);
            Assert.That(world.Count, Is.EqualTo(1));
            Assert.That(world.ContainsComponent<SimpleComponent>(b), Is.False);
        }

        [Test]
        public void ThrowIfComponentIsMissing()
        {
            using World world = CreateWorld();
            uint entity = world.CreateEntity();
            SimpleComponent component1 = new("Hello World");
            Another component2 = new(42);
            world.AddComponent(entity, component1);
            world.AddComponent(entity, component2);
            Assert.That(world.GetComponent<SimpleComponent>(entity).data.ToString(), Is.EqualTo(component1.data.ToString()));
            Assert.That(world.GetComponent<Another>(entity).data, Is.EqualTo(component2.data));
            world.RemoveComponentType<SimpleComponent>(entity);
            Assert.Throws<ComponentIsMissingException>(() => world.GetComponent<SimpleComponent>(entity));
            Assert.That(world.GetComponent<Another>(entity).data, Is.EqualTo(component2.data));
        }
#endif

        [Test]
        public void AddingAndGettingComponentReference()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            ref Another another = ref world.AddComponent<Another>(a);
            Assert.That(world.ContainsComponent<Another>(a), Is.True);
            Assert.That(another.data, Is.EqualTo(0));
            another.data = 1337;
            Assert.That(world.GetComponent<Another>(a).data, Is.EqualTo(1337));

            uint b = world.CreateEntity();
            ref SimpleComponent simple = ref world.AddComponent<SimpleComponent>(b);
            Assert.That(world.ContainsComponent<SimpleComponent>(b), Is.True);
            simple.data = "Hello World";
            Assert.That(world.GetComponent<SimpleComponent>(b).data.ToString(), Is.EqualTo("Hello World"));

            ref Another anotherAnother = ref world.AddComponent<Another>(b);
            anotherAnother.data = 333;
            Assert.That(world.ContainsComponent<Another>(b), Is.True);
            Assert.That(world.GetComponent<Another>(b).data, Is.EqualTo(333));
            Assert.That(world.GetComponent<SimpleComponent>(b).data.ToString(), Is.EqualTo("Hello World"));
        }

        [Test]
        public void TwoInitialComponents()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity(new Another(4));
            uint b = world.CreateEntity(new Another(32), new SimpleComponent("what is this?"));
            Assert.That(world.ContainsComponent<SimpleComponent>(b), Is.True);
            Assert.That(world.ContainsComponent<Another>(b), Is.True);
            uint c = world.CreateEntity(new SimpleComponent("what is this?"), new Another(32));
            Assert.That(world.ContainsComponent<SimpleComponent>(c), Is.True);
            Assert.That(world.ContainsComponent<Another>(c), Is.True);
            Assert.That(world.GetComponent<SimpleComponent>(b), Is.EqualTo(world.GetComponent<SimpleComponent>(c)));
            Assert.That(world.GetComponent<Another>(b), Is.EqualTo(world.GetComponent<Another>(c)));
            world.RemoveComponentType<SimpleComponent>(b);
            Assert.That(world.ContainsComponent<SimpleComponent>(b), Is.False);
            Assert.That(world.GetComponent<Another>(a).data, Is.EqualTo(4));
            Assert.That(world.GetFirstComponent<Another>(out uint found).data, Is.EqualTo(4));
            Assert.That(found, Is.EqualTo(a));
            world.AddComponent(b, new SimpleComponent("Hello World"));
            Assert.That(world.GetComponent<SimpleComponent>(b).data.ToString(), Is.EqualTo("Hello World"));
            world.SetComponent(b, new SimpleComponent("Goodbye World"));
            Assert.That(world.GetComponent<SimpleComponent>(b).data.ToString(), Is.EqualTo("Goodbye World"));
        }

        [Test]
        public void AddingMultipleComponents()
        {
            using World world = CreateWorld();
            int anotherType = world.Schema.GetComponentType<Another>();
            int simpleType = world.Schema.GetComponentType<SimpleComponent>();
            uint a = world.CreateEntity();
            Assert.That(world.ContainsEntity(a), Is.True);
            Assert.That(world.ContainsComponent<Another>(a), Is.False);
            Assert.That(world.ContainsComponent<SimpleComponent>(a), Is.False);
            world.AddComponentTypes(a, new BitMask(anotherType, simpleType));
            Assert.That(world.ContainsComponent<Another>(a), Is.True);
            Assert.That(world.ContainsComponent<SimpleComponent>(a), Is.True);
            Assert.That(world.GetComponent<Another>(a).data, Is.EqualTo(0));
            Assert.That(world.GetComponent<SimpleComponent>(a).data.ToString(), Is.EqualTo(string.Empty));
            world.RemoveComponentTypes(a, new BitMask(anotherType, simpleType));
            Assert.That(world.ContainsComponent<Another>(a), Is.False);
            Assert.That(world.ContainsComponent<SimpleComponent>(a), Is.False);
        }

        [Test]
        public void DestroyEntityTwice()
        {
            using World world = CreateWorld();
            uint entity = world.CreateEntity();
            Assert.That(world.ContainsEntity(entity), Is.True);
            world.DestroyEntity(entity);
            Assert.That(world.ContainsEntity(entity), Is.False);

            uint another = world.CreateEntity();
            Assert.That(world.ContainsEntity(another), Is.True);
            world.DestroyEntity(another);
            Assert.That(world.ContainsEntity(another), Is.False);
        }

        [Test]
        public void DisabledEntityWithReferences()
        {
            using World world = CreateWorld();
            uint entity = world.CreateEntity();
            uint other = world.CreateEntity();
            rint reference = world.AddReference(entity, other);
            world.AddComponent(entity, new ComponentThatReferences(reference));
            Assert.That(world.GetComponent<ComponentThatReferences>(entity).reference, Is.EqualTo(reference));
            world.SetEnabled(entity, false);
            Assert.That(world.GetComponent<ComponentThatReferences>(entity).reference, Is.EqualTo(reference));
        }

        [Test]
        public void EnablingAndDisabling()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            world.AddComponent(a, new SimpleComponent("Hello World"));
            Assert.That(world.IsEnabled(a), Is.True);
            Assert.That(world.IsEnabled(b), Is.True);
            world.SetEnabled(a, false);
            Assert.That(world.IsEnabled(a), Is.False);
            Assert.That(world.IsEnabled(b), Is.True);
            world.SetEnabled(a, true);
            Assert.That(world.IsEnabled(a), Is.True);
        }

        [Test]
        public void DisablingParentDisablesChildToo()
        {
            using World world = CreateWorld();
            uint parent = world.CreateEntity();
            uint child = world.CreateEntity();
            world.AddComponent(child, new Another(1234567890));
            world.SetParent(child, parent);
            world.SetEnabled(parent, false);
            Assert.That(world.GetComponent<Another>(child).data, Is.EqualTo(1234567890));
            Assert.That(world.IsEnabled(parent), Is.False);
            Assert.That(world.IsEnabled(child), Is.False);
            Assert.That(world.IsLocallyEnabled(child), Is.True);
            Chunk chunk = world.GetChunk(child);
            Assert.That(chunk.Definition.tagTypes.Contains(Schema.DisabledTagType), Is.True);
            world.SetEnabled(parent, true);
            Assert.That(world.IsEnabled(parent), Is.True);
            Assert.That(world.IsEnabled(child), Is.True);
            chunk = world.GetChunk(child);
            Assert.That(chunk.Definition.tagTypes.Contains(Schema.DisabledTagType), Is.False);

            world.SetEnabled(child, false);
            Assert.That(world.IsEnabled(child), Is.False);
            chunk = world.GetChunk(child);
            Assert.That(chunk.Definition.tagTypes.Contains(Schema.DisabledTagType), Is.True);

            world.SetEnabled(parent, false);
            world.SetEnabled(child, true);
            Assert.That(world.IsEnabled(child), Is.False);
            chunk = world.GetChunk(child);
            Assert.That(chunk.Definition.tagTypes.Contains(Schema.DisabledTagType), Is.True);
        }

        [Test]
        public void RecreateDisabledEntity()
        {
            using World world = CreateWorld();
            uint entity = world.CreateEntity();
            world.SetEnabled(entity, false);
            world.DestroyEntity(entity);
            uint another = world.CreateEntity();
            Assert.That(entity, Is.EqualTo(another));
            Assert.That(world.IsEnabled(entity), Is.True);
        }

        [Test]
        public void PopulateWorldThenClear()
        {
            using World world = CreateWorld();
            using RandomGenerator rng = RandomGenerator.Create();
            uint realEntities = 0;
            for (uint i = 0; i < 100; i++)
            {
                uint entity = world.CreateEntity();
                if (rng.NextBool())
                {
                    world.AddComponent(entity, new SimpleComponent("apple"));
                }

                if (rng.NextBool())
                {
                    world.AddComponent(entity, new Another(5));
                }

                if (rng.NextBool())
                {
                    int length = rng.NextInt(1, 10);
                    Values<Character> array = world.CreateArray<Character>(entity, length);
                    for (int j = 0; j < length; j++)
                    {
                        array[j] = (char)rng.NextInt('a', 'z');
                    }
                }

                if (rng.NextBool())
                {
                    world.DestroyEntity(entity);
                }
                else
                {
                    realEntities++;
                }
            }

            Assert.That(world.Count, Is.EqualTo(realEntities));
            world.Clear();
            Assert.That(world.Count, Is.EqualTo(0));
            for (uint e = 1; e <= 100; e++)
            {
                Assert.That(world.ContainsEntity(e), Is.False, $"Entity {e} should not exist");
            }
        }

        [Test]
        public void CloneEntity()
        {
            using World world = CreateWorld();
            uint entity = world.CreateEntity();
            world.AddComponent(entity, new SimpleComponent("apple"));
            world.AddComponent(entity, new Another(5));
            Values<Character> array = world.CreateArray<Character>(entity, 5);
            array[0] = 'a';
            array[1] = 'b';
            array[2] = 'c';
            array[3] = 'd';
            array[4] = 'e';
            Values<Integer> data = world.CreateArray<Integer>(entity, 3);
            data[0] = 1337;
            data[1] = 666;
            data[2] = 500513;

            uint clone = world.CloneEntity(entity);
            Assert.That(world.ContainsEntity(clone), Is.True);
            Assert.That(world.ContainsComponent<SimpleComponent>(clone), Is.True);
            Assert.That(world.ContainsComponent<Another>(clone), Is.True);
            Assert.That(world.GetComponent<SimpleComponent>(clone).data, Is.EqualTo(new ASCIIText256("apple")));
            Assert.That(world.GetComponent<Another>(clone).data, Is.EqualTo(5));
            Values<Character> cloneArray = world.GetArray<Character>(clone);
            Assert.That(cloneArray.Length, Is.EqualTo(5));
            Assert.That((char)cloneArray[0], Is.EqualTo('a'));
            Assert.That((char)cloneArray[1], Is.EqualTo('b'));
            Assert.That((char)cloneArray[2], Is.EqualTo('c'));
            Assert.That((char)cloneArray[3], Is.EqualTo('d'));
            Assert.That((char)cloneArray[4], Is.EqualTo('e'));
            Values<Integer> cloneData = world.GetArray<Integer>(clone);
            Assert.That(cloneData.Length, Is.EqualTo(3));
            Assert.That((int)cloneData[0], Is.EqualTo(1337));
            Assert.That((int)cloneData[1], Is.EqualTo(666));
            Assert.That((int)cloneData[2], Is.EqualTo(500513));
        }
    }
}