using System;

namespace Worlds.Tests
{
    public class ParentingTests : WorldTests
    {
        [Test]
        public void SetParent()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            world.SetParent(b, a);
            Assert.That(world.GetParent(b), Is.EqualTo(a));
        }

        [Test]
        public void NotParented()
        {
            using World world = CreateWorld();
            uint entity = world.CreateEntity();
            Assert.That(world.GetParent(entity), Is.EqualTo(default(uint)));
        }

        [Test]
        public void ParentOfRecreatedEntity()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            world.SetParent(b, a);
            Assert.That(world.GetParent(b), Is.EqualTo(a));
            Assert.That(world.GetDepth(b), Is.EqualTo(1));
            world.DestroyEntity(b);
            uint c = world.CreateEntity();
            Assert.That(b, Is.EqualTo(c));
            Assert.That(world.GetParent(c), Is.EqualTo(default(uint)));
            Assert.That(world.GetDepth(c), Is.EqualTo(0));
        }

        [Test]
        public void CountChildren()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();

            Assert.That(world.GetChildCount(a), Is.EqualTo(0));

            world.SetParent(c, a);

            Assert.That(world.GetDepth(a), Is.EqualTo(0));
            Assert.That(world.GetDepth(c), Is.EqualTo(1));
            Assert.That(world.GetChildCount(a), Is.EqualTo(1));

            world.SetParent(c, b);

            Assert.That(world.GetChildCount(a), Is.EqualTo(0));
            Assert.That(world.GetChildCount(b), Is.EqualTo(1));
            Assert.That(world.GetDepth(c), Is.EqualTo(1));

            world.DestroyEntity(c);

            Assert.That(world.GetChildCount(b), Is.EqualTo(0));

            world.SetParent(b, a);

            Assert.That(world.GetChildCount(a), Is.EqualTo(1));

            world.DestroyEntity(a);
            uint d = world.CreateEntity();
            uint e = world.CreateEntity();

            Assert.That(world.GetChildCount(d), Is.EqualTo(0));

            world.SetParent(e, d);

            Assert.That(world.GetChildCount(d), Is.EqualTo(1));
            Assert.That(world.GetDepth(d), Is.EqualTo(0));
            Assert.That(world.GetDepth(e), Is.EqualTo(1));
        }

        [Test]
        public void ChildrenUpdateAfterChildGetsDestroyed()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();

            Assert.That(world.GetChildCount(a), Is.EqualTo(0));

            world.SetParent(b, a);

            Assert.That(world.GetChildCount(a), Is.EqualTo(1));

            world.DestroyEntity(b);

            Assert.That(world.GetChildCount(a), Is.EqualTo(0));
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
            Assert.That(world.GetDepth(a), Is.EqualTo(0));
            Assert.That(world.GetDepth(b), Is.EqualTo(1));
            Assert.That(world.GetDepth(c), Is.EqualTo(2));
            Assert.That(world.GetDepth(d), Is.EqualTo(3));
            Assert.That(world.GetDepth(e), Is.EqualTo(1));

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
        public void MaxDepthOfWorld()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            uint d = world.CreateEntity();
            uint e = world.CreateEntity();
            uint f = world.CreateEntity();
            world.SetParent(b, a);
            world.SetParent(c, b);
            world.SetParent(d, c);
            world.SetParent(e, d);
            world.SetParent(f, e);
            Assert.That(world.GetDepth(a), Is.EqualTo(0));
            Assert.That(world.GetDepth(b), Is.EqualTo(1));
            Assert.That(world.GetDepth(c), Is.EqualTo(2));
            Assert.That(world.GetDepth(d), Is.EqualTo(3));
            Assert.That(world.GetDepth(e), Is.EqualTo(4));
            Assert.That(world.GetDepth(f), Is.EqualTo(5));
            Assert.That(world.MaxDepth, Is.EqualTo(5));

            world.DestroyEntity(d);

            Assert.That(world.GetDepth(a), Is.EqualTo(0));
            Assert.That(world.GetDepth(b), Is.EqualTo(1));
            Assert.That(world.GetDepth(c), Is.EqualTo(2));
            Assert.That(world.MaxDepth, Is.GreaterThanOrEqualTo(2));
        }
    }
}