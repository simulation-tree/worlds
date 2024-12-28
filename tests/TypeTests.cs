using Unmanaged;

namespace Worlds.Tests
{
    public class TypeTests : WorldTests
    {
        [Test]
        public void VerifyLayoutOfRegisteredTypes()
        {
            TypeLayout layout = TypeLayout.Get<Stress>();
            Assert.That(layout.Name.ToString(), Is.EqualTo("Stress"));
            Assert.That(layout.Size, Is.EqualTo(TypeInfo<Stress>.size));
            Assert.That(layout.Variables.Length, Is.EqualTo(5));
            Assert.That(layout.Variables[0].Size, Is.EqualTo(1));
            Assert.That(layout.Variables[0].Name.ToString(), Is.EqualTo("first"));
            Assert.That(layout.Variables[1].Size, Is.EqualTo(2));
            Assert.That(layout.Variables[1].Name.ToString(), Is.EqualTo("second"));
            Assert.That(layout.Variables[2].Size, Is.EqualTo(4));
            Assert.That(layout.Variables[2].Name.ToString(), Is.EqualTo("third"));
            Assert.That(layout.Variables[3].Size, Is.EqualTo(4));
            Assert.That(layout.Variables[3].Name.ToString(), Is.EqualTo("fourth"));
            Assert.That(layout.Variables[4].Size, Is.EqualTo(TypeInfo<Cherry>.size));
            Assert.That(layout.Variables[4].Name.ToString(), Is.EqualTo("cherry"));
        }

        [Test]
        public void SerializeTypes()
        {
            TypeLayout a = TypeLayout.Get<Stress>();
            using BinaryWriter writer = new();
            writer.WriteObject(a);

            using BinaryReader reader = new(writer);
            TypeLayout b = reader.ReadObject<TypeLayout>();

            Assert.That(a.Name.ToString(), Is.EqualTo(b.Name.ToString()));
            Assert.That(a.Variables.Length, Is.EqualTo(b.Variables.Length));
            Assert.That(a.Variables[4].Name.ToString(), Is.EqualTo(b.Variables[4].Name.ToString()));
            Assert.That(a.Variables[4].TypeLayout.Variables[0], Is.EqualTo(b.Variables[4].TypeLayout.Variables[0]));
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void CheckLayouts()
        {
            Assert.That(TypeLayout.IsRegistered<bool>(), Is.True);
            Assert.That(TypeLayout.IsRegistered<byte>(), Is.True);
            TypeLayout boolean = TypeLayout.Get<bool>();
            TypeLayout byteType = TypeLayout.Get<byte>();
            Assert.That(boolean.Size, Is.EqualTo(1));
            Assert.That(byteType.Size, Is.EqualTo(1));
            Assert.That(boolean.GetHashCode(), Is.EqualTo(TypeLayout.Get<bool>().GetHashCode()));
            Assert.That(byteType.GetHashCode(), Is.EqualTo(TypeLayout.Get<byte>().GetHashCode()));
        }

        [Test]
        public void AddToSchema()
        {
            using Schema schema = new();
            schema.RegisterComponent<Stress>();
            Assert.That(schema.ContainsComponent<Stress>(), Is.True);

            using Schema copy = new();
            copy.CopyFrom(schema);

            Assert.That(copy.ContainsComponent<Stress>(), Is.True);
        }

        [Test]
        public void CheckIfLayoutIs()
        {
            TypeLayout layout = TypeLayout.Get<Stress>();

            Assert.That(layout.Is<Stress>(), Is.True);
            Assert.That(layout.Is<Cherry>(), Is.False);
        }

        [Test]
        public void SerializeSchema()
        {
            using Schema prefabSchema = new();
            prefabSchema.RegisterComponent<float>();
            prefabSchema.RegisterComponent<char>();

            using BinaryWriter writer = new();
            writer.WriteObject(prefabSchema);

            using BinaryReader reader = new(writer);
            using Schema loadedSchema = reader.ReadObject<Schema>();

            Assert.That(loadedSchema.ContainsComponent<float>(), Is.True);
            Assert.That(loadedSchema.ContainsComponent<char>(), Is.True);
        }
    }
}