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

        [Test]
        public void MaxEntityIDIsValid()
        {
            using World world = CreateWorld();
            Span<uint> entities = stackalloc uint[4];
            world.CreateEntities(entities);
            Assert.That(world.MaxEntityValue, Is.EqualTo(entities[3]));
            Assert.That(world.MaxEntityValue, Is.EqualTo(entities.Length));

            using Array<bool> buffer = new(world.MaxEntityValue + 1);
            for (int i = 0; i < entities.Length; i++)
            {
                buffer[(int)entities[i]] = true;
            }
        }

        [Test]
        public void DestroyParentEntity()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            uint d = world.CreateEntity();
            uint e = world.CreateEntity();
            world.SetParent(b, a);
            world.SetParent(c, b);
            world.SetParent(d, c);
            world.SetParent(e, a);
            Assert.That(world.ContainsEntity(a), Is.True);
            Assert.That(world.ContainsEntity(b), Is.True);
            Assert.That(world.ContainsEntity(c), Is.True);
            Assert.That(world.ContainsEntity(d), Is.True);
            Assert.That(world.ContainsEntity(e), Is.True);
            Assert.That(world.GetParent(b), Is.EqualTo(a));
            Assert.That(world.GetParent(c), Is.EqualTo(b));
            Assert.That(world.GetParent(d), Is.EqualTo(c));

            Span<uint> children = stackalloc uint[4];
            int childCount = world.CopyChildrenTo(a, children);
            Assert.That(childCount, Is.EqualTo(2));
            Assert.That(children.ToArray(), Has.Member(b));
            Assert.That(children.ToArray(), Has.Member(e));

            childCount = world.CopyChildrenTo(b, children);
            Assert.That(childCount, Is.EqualTo(1));
            Assert.That(children.ToArray(), Has.Member(c));

            childCount = world.CopyChildrenTo(c, children);
            Assert.That(childCount, Is.EqualTo(1));
            Assert.That(children.ToArray(), Has.Member(d));

            world.DestroyEntity(a);
            Assert.That(world.ContainsEntity(a), Is.False);
            Assert.That(world.ContainsEntity(b), Is.False);
            Assert.That(world.ContainsEntity(c), Is.False);
            Assert.That(world.ContainsEntity(d), Is.False);
            Assert.That(world.ContainsEntity(e), Is.False);
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
            world.RemoveComponent<SimpleComponent>(entity);
            Assert.That(world.ContainsComponent<SimpleComponent>(entity), Is.False);
            Assert.That(world.ContainsAnyComponent<SimpleComponent>(), Is.False);
            world.AddComponent(entity, component);
            Assert.That(world.ContainsComponent<SimpleComponent>(entity), Is.True);
            Assert.That(world.ContainsAnyComponent<SimpleComponent>(), Is.True);
            world.DestroyEntity(entity);
            Assert.That(world.ContainsEntity(entity), Is.False);
        }

        [Test]
        public void CreateAndDestroyEntity()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            Assert.That(world.ContainsEntity(a), Is.True);
            world.DestroyEntity(a);
            Assert.That(world.ContainsEntity(a), Is.False);
            Assert.That(world.Count, Is.EqualTo(0));
            Assert.Throws<NullReferenceException>(() => world.GetComponent<SimpleComponent>(a));
            uint b = world.CreateEntity();
            Assert.That(world.ContainsEntity(b), Is.True);
            Assert.That(world.Count, Is.EqualTo(1));
            Assert.That(world.ContainsComponent<SimpleComponent>(b), Is.False);
        }

        [Test]
        public void AddingTwoComponents()
        {
            using World world = CreateWorld();
            uint entity = world.CreateEntity();
            SimpleComponent component1 = new("Hello World");
            Another component2 = new(42);
            world.AddComponent(entity, component1);
            world.AddComponent(entity, component2);
            Assert.That(world.GetComponent<SimpleComponent>(entity).data.ToString(), Is.EqualTo(component1.data.ToString()));
            Assert.That(world.GetComponent<Another>(entity).data, Is.EqualTo(component2.data));
            world.RemoveComponent<SimpleComponent>(entity);
            Assert.Throws<NullReferenceException>(() => world.GetComponent<SimpleComponent>(entity));
            Assert.That(world.GetComponent<Another>(entity).data, Is.EqualTo(component2.data));
        }

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
            world.RemoveComponent<SimpleComponent>(b);
            Assert.That(world.ContainsComponent<SimpleComponent>(b), Is.False);
            Assert.That(world.GetComponent<Another>(a).data, Is.EqualTo(4));
            Assert.That(world.GetFirstComponent<Another>(out uint found).data, Is.EqualTo(4));
            Assert.That(found, Is.EqualTo(a));
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
            Assert.That(chunk.Definition.tagTypes.Contains(TagType.Disabled), Is.True);
            world.SetEnabled(parent, true);
            Assert.That(world.IsEnabled(parent), Is.True);
            Assert.That(world.IsEnabled(child), Is.True);
            chunk = world.GetChunk(child);
            Assert.That(chunk.Definition.tagTypes.Contains(TagType.Disabled), Is.False);

            world.SetEnabled(child, false);
            Assert.That(world.IsEnabled(child), Is.False);
            chunk = world.GetChunk(child);
            Assert.That(chunk.Definition.tagTypes.Contains(TagType.Disabled), Is.True);

            world.SetEnabled(parent, false);
            world.SetEnabled(child, true);
            Assert.That(world.IsEnabled(child), Is.False);
            chunk = world.GetChunk(child);
            Assert.That(chunk.Definition.tagTypes.Contains(TagType.Disabled), Is.True);
        }

        [Test]
        public void ParentingToDisabledEntity()
        {
            using World world = CreateWorld();
            uint parent = world.CreateEntity();
            uint child = world.CreateEntity();
            world.SetEnabled(parent, false);
            world.SetParent(child, parent);
            Assert.That(world.GetParent(child), Is.EqualTo(parent));
            Assert.That(world.IsEnabled(child), Is.EqualTo(false));
            Assert.That(world.IsLocallyEnabled(child), Is.EqualTo(true));
            uint grandChild = world.CreateEntity();
            world.SetParent(grandChild, child);
            Assert.That(world.IsEnabled(grandChild), Is.EqualTo(false));
            Assert.That(world.IsLocallyEnabled(grandChild), Is.EqualTo(true));
            world.SetEnabled(parent, true);
            Assert.That(world.IsEnabled(child), Is.EqualTo(true));
            Assert.That(world.IsEnabled(grandChild), Is.EqualTo(true));
            world.SetEnabled(parent, false);
            Assert.That(world.IsEnabled(child), Is.EqualTo(false));
            Assert.That(world.IsEnabled(grandChild), Is.EqualTo(false));
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
        public void MoveChildToAnotherParent()
        {
            World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            world.SetParent(a, b);
            world.SetParent(a, c);
            Assert.That(world.GetParent(a), Is.EqualTo(c));
            Assert.That(world.GetChildCount(b), Is.EqualTo(0));
            Assert.That(world.GetChildCount(c), Is.EqualTo(1));
            world.Dispose();
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