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
            Assert.That(a.Variables[4].Name.ToString(), Is.EqualTo(b.Variables[4].Name.ToString()));
            Assert.That(a.Variables[4].TypeLayout.Variables[0], Is.EqualTo(b.Variables[4].TypeLayout.Variables[0]));
        }
    }
}