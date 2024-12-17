namespace Worlds.Tests
{
    public class TypeTests : WorldTests
    {
        [Test]
        public void VerifyLayoutOfRegisteredTypes()
        {
            TypeLayout layout = TypeLayout.Get<Stress>();
            Assert.That(layout.name.ToString(), Is.EqualTo("Stress"));
            Assert.That(layout.Size, Is.EqualTo(1 + 2 + 4 + 4 + 1));
            Assert.That(layout.Variables.Length, Is.EqualTo(5));
            Assert.That(layout.Variables[0].size, Is.EqualTo(1));
            Assert.That(layout.Variables[0].name.ToString(), Is.EqualTo("first"));
            Assert.That(layout.Variables[1].size, Is.EqualTo(2));
            Assert.That(layout.Variables[1].name.ToString(), Is.EqualTo("second"));
            Assert.That(layout.Variables[2].size, Is.EqualTo(4));
            Assert.That(layout.Variables[2].name.ToString(), Is.EqualTo("third"));
            Assert.That(layout.Variables[3].size, Is.EqualTo(4));
            Assert.That(layout.Variables[3].name.ToString(), Is.EqualTo("fourth"));
            Assert.That(layout.Variables[4].size, Is.EqualTo(1));
            Assert.That(layout.Variables[4].name.ToString(), Is.EqualTo("yes"));
        }
    }
}