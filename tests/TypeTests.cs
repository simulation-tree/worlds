using Collections;
using Unmanaged;

namespace Worlds.Tests
{
    public class TypeTests : WorldTests
    {
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

        [Test]
        public void VerifyTagsGetSaved() 
        {
            using Schema schema = new();
            schema.RegisterTag<bool>();
            schema.RegisterTag<byte>();
            schema.RegisterComponent<bool>();
            schema.RegisterArrayElement<byte>();

            using BinaryWriter writer = new();
            writer.WriteObject(schema);

            using BinaryReader reader = new(writer);
            using Schema loadedSchema = reader.ReadObject<Schema>();

            Assert.That(loadedSchema.ContainsTag<bool>(), Is.True);
            Assert.That(loadedSchema.ContainsTag<byte>(), Is.True);
            Assert.That(loadedSchema.ContainsComponent<bool>(), Is.True);
            Assert.That(loadedSchema.ContainsArrayElement<byte>(), Is.True);
        }
    }
}