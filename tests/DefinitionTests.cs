using Unmanaged;

namespace Simulation.Tests
{
    public class DefinitionTests : UnmanagedTests
    {
        [Test]
        public void CompareEquality()
        {
            Definition a = new([RuntimeType.Get<float>()], [RuntimeType.Get<byte>()]);
            Definition b = new([RuntimeType.Get<float>()], [RuntimeType.Get<byte>()]);

            Assert.That(a, Is.EqualTo(b));

            a = a.AddComponentType<int>();
            a = a.AddComponentType<double>();
            a = a.AddComponentType<char>();

            Assert.That(a, Is.Not.EqualTo(b));

            b = b.AddComponentType<double>();
            b = b.AddComponentType<char>();
            b = b.AddComponentType<int>();

            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void NotEqual()
        {
            Definition a = new([RuntimeType.Get<float>()], [RuntimeType.Get<byte>()]);
            Definition b = new([RuntimeType.Get<float>()], [RuntimeType.Get<float>()]);

            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void AddingAndIndexing()
        {
            Definition a = new();
            a = a.AddComponentType<int>();
            a = a.AddArrayType<double>();
            a = a.AddComponentType<char>();
            a = a.AddArrayType<float>();

            Assert.That(a.ComponentTypeCount, Is.EqualTo(2));
            Assert.That(a.ArrayTypeCount, Is.EqualTo(2));
            Assert.That(a.TotalTypeCount, Is.EqualTo(4));
            Assert.That(a[0].type, Is.EqualTo(RuntimeType.Get<int>()));
            Assert.That(a[0].isArray, Is.False);
            Assert.That(a[1].type, Is.EqualTo(RuntimeType.Get<double>()));
            Assert.That(a[1].isArray, Is.True);
            Assert.That(a[2].type, Is.EqualTo(RuntimeType.Get<char>()));
            Assert.That(a[2].isArray, Is.False);
            Assert.That(a[3].type, Is.EqualTo(RuntimeType.Get<float>()));
            Assert.That(a[3].isArray, Is.True);
        }

        [Test]
        public void ReadTypeValues()
        {
            Definition a = new();
            a = a.AddComponentType<int>();
            a = a.AddArrayType<double>();
            a = a.AddComponentType<char>();
            a = a.AddArrayType<float>();

            USpan<RuntimeType> buffer = stackalloc RuntimeType[8];
            uint count = a.CopyAllTypes(buffer);

            Assert.That(count, Is.EqualTo(4));
            Assert.That(buffer[0], Is.EqualTo(RuntimeType.Get<int>()));
            Assert.That(buffer[1], Is.EqualTo(RuntimeType.Get<double>()));
            Assert.That(buffer[2], Is.EqualTo(RuntimeType.Get<char>()));
            Assert.That(buffer[3], Is.EqualTo(RuntimeType.Get<float>()));

            count = a.CopyComponentTypes(buffer);
            Assert.That(count, Is.EqualTo(2));
            Assert.That(buffer[0], Is.EqualTo(RuntimeType.Get<int>()));
            Assert.That(buffer[1], Is.EqualTo(RuntimeType.Get<char>()));

            count = a.CopyArrayTypes(buffer);
            Assert.That(count, Is.EqualTo(2));
            Assert.That(buffer[0], Is.EqualTo(RuntimeType.Get<double>()));
            Assert.That(buffer[1], Is.EqualTo(RuntimeType.Get<float>()));
        }

        [Test]
        public void QueryUsingDefinition()
        {
            using World world = new();

            uint entityA = world.CreateEntity();
            world.AddComponent(entityA, (byte)1);
            world.AddComponent(entityA, (float)1);
            world.CreateArray(entityA, "Hello".AsUSpan());

            uint entityB = world.CreateEntity();
            world.AddComponent(entityB, (byte)2);

            Definition byteComponent = new([RuntimeType.Get<byte>()], []);
            Definition charArray = new([], [RuntimeType.Get<char>()]);

            using DefinitionQuery byteQuery = new(byteComponent);
            byteQuery.Update(world);
            Assert.That(byteQuery.Count, Is.EqualTo(2));
            Assert.That(byteQuery[0], Is.EqualTo(entityB));
            Assert.That(byteQuery[1], Is.EqualTo(entityA));

            using DefinitionQuery charQuery = new(charArray);
            charQuery.Update(world);
            Assert.That(charQuery.Count, Is.EqualTo(1));
            Assert.That(charQuery[0], Is.EqualTo(entityA));
        }

        [Test]
        public void CreateFromDefinition()
        {
            using World world = new();

            Definition definition = new([RuntimeType.Get<byte>(), RuntimeType.Get<float>()], [RuntimeType.Get<char>()]);
            uint entity = world.CreateEntity(definition);
            byte defaultByte = world.GetComponent<byte>(entity);
            float defaultFloat = world.GetComponent<float>(entity);
            USpan<char> defaultCharArray = world.GetArray<char>(entity);

            Assert.That(defaultByte, Is.EqualTo(default(byte)));
            Assert.That(defaultFloat, Is.EqualTo(default(float)));
            Assert.That(defaultCharArray.Length, Is.EqualTo(0));
        }

        [Test]
        public void BecomeADefinition()
        {
            using World world = new();

            Definition definitionA = new([RuntimeType.Get<byte>(), RuntimeType.Get<float>()], [RuntimeType.Get<char>()]);

            Entity entity = new(world);
            entity.Become(definitionA);

            Assert.That(entity.ContainsComponent<byte>(), Is.True);
            Assert.That(entity.ContainsComponent<float>(), Is.True);
            Assert.That(entity.ContainsArray<char>(), Is.True);
            Assert.That(entity.ContainsArray<byte>(), Is.False);
            Assert.That(entity.ContainsComponent<char>(), Is.False);
            Assert.That(entity.GetComponent<byte>(), Is.EqualTo(default(byte)));
            Assert.That(entity.GetComponent<float>(), Is.EqualTo(default(float)));
            Assert.That(entity.GetArray<char>().Length, Is.EqualTo(0));

            Definition definitionB = new([], [RuntimeType.Get<byte>()]);

            entity.Become(definitionB);

            Assert.That(entity.ContainsArray<byte>(), Is.True);
        }

        [Test]
        public void CheckIfIsDefinition()
        {
            using World world = new();

            Entity entityA = new(world);
            entityA.AddComponent<byte>();
            entityA.AddComponent<float>();

            Entity entityB = new(world);
            entityB.AddComponent<byte>();
            entityB.AddComponent<float>();
            entityB.CreateArray<char>();

            Definition definition = new([RuntimeType.Get<byte>(), RuntimeType.Get<float>()], [RuntimeType.Get<char>()]);

            Assert.That(entityA.Is(definition), Is.False);
            Assert.That(entityB.Is(definition), Is.True);
        }
    }
}