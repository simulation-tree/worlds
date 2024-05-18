namespace Game
{
    public class TextTests
    {
        [Test]
        public void CreateText()
        {
            using Text text = new("Hello There");
            Assert.That(text.Length, Is.EqualTo(11));
            Assert.That(text.ToString(), Is.EqualTo("Hello There"));
        }

        [Test]
        public void ConcatenateText()
        {
            using Text text1 = new("Hello ");
            using Text text2 = new("There");
            text1.Append(text2);
            Assert.That(text1.Length, Is.EqualTo(11));
            Assert.That(text1.ToString(), Is.EqualTo("Hello There"));
        }

        [Test]
        public void IndexOf()
        {
            using Text text = new("abacus");
            Assert.That(text.IndexOf('a'), Is.EqualTo(0));
            Assert.That(text.IndexOf('b'), Is.EqualTo(1));
            Assert.That(text.IndexOf('c'), Is.EqualTo(3));
            Assert.That(text.IndexOf('u'), Is.EqualTo(4));
            Assert.That(text.IndexOf('s'), Is.EqualTo(5));

            Assert.That(text.IndexOf("acus"), Is.EqualTo(2));
        }

        [Test]
        public void ChangeLength()
        {
            Text text = new("abc");
            text.Length = 5;
            Assert.That(text.Length, Is.EqualTo(5));
            Assert.That(text.ToString(), Is.EqualTo("abc\0\0"));
            text.Length = 2;
            Assert.That(text.Length, Is.EqualTo(2));
            Assert.That(text.ToString(), Is.EqualTo("ab"));
            text.Dispose();
        }
    }
}