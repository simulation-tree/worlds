using Unmanaged;

namespace Game
{
    public class WorldTests
    {
        [Test]
        public void CreateAndDisposeWorld()
        {
            World world = new();
            world.Dispose();
        }

        [Test]
        public void DisposeTwiceError()
        {
            World world = new();
            world.Dispose();
            Assert.Throws<ObjectDisposedException>(() => world.Dispose());
        }

        [Test]
        public void NonCreatedWorldError()
        {
            World world = default;
            Assert.Throws<NullReferenceException>(() => world.Dispose());
        }

        [Test]
        public void GetAddedComponent()
        {
            using World world = new();
            EntityID entity = world.CreateEntity();
            SimpleComponent component = new("Hello World");
            world.AddComponent(entity, component);

            Assert.That(world.GetComponent<SimpleComponent>(entity), Is.EqualTo(component));
        }

        [Test]
        public void CreateAndDestroyEntity()
        {
            using World world = new();
            EntityID entity = world.CreateEntity();
            world.DestroyEntity(entity);
            Assert.That(world.ContainsEntity(entity), Is.False);
            Assert.Throws<NullReferenceException>(() => world.GetComponent<SimpleComponent>(entity));
        }

        public struct SimpleComponent
        {
            public FixedString data;

            public SimpleComponent(FixedString data)
            {
                this.data = data;
            }
        }
    }
}