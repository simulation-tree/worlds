namespace Simulation.Tests
{
    public class BitSetTests
    {
        [Test]
        public void SetThenCheckIfContains()
        {
            BitSet a = new();
            a.Set(3);
            Assert.That(a.Contains(0), Is.False);
            Assert.That(a.Contains(1), Is.False);
            Assert.That(a.Contains(2), Is.False);
            Assert.That(a.Contains(3), Is.True);
        }

        [Test]
        public void CheckIfIntersects()
        {
            BitSet a = new();
            a.Set(3);
            a.Set(4);
            a.Set(5);

            BitSet b = new();
            b.Set(4);

            Assert.That(a.ContainsAll(b), Is.True);
        }

        [Test]
        public void MustContainAll()
        {
            BitSet a = new();
            a.Set(3);
            a.Set(4);
            a.Set(5);

            BitSet b = new();
            b.Set(4);
            b.Set(9);

            Assert.That(a.ContainsAll(b), Is.False);
        }

        [Test]
        public void CountIncrementsWhenSetting()
        {
            BitSet a = new();
            Assert.That(a.Count, Is.EqualTo(0));
            a.Set(3);
            Assert.That(a.Count, Is.EqualTo(1));
            a.Set(4);
            Assert.That(a.Count, Is.EqualTo(2));
            a.Set(5);
            Assert.That(a.Count, Is.EqualTo(3));
            a.Clear(4);
            Assert.That(a.Count, Is.EqualTo(2));
            a.Clear(5);
            Assert.That(a.Count, Is.EqualTo(1));
            a.Clear(3);
            Assert.That(a.Count, Is.EqualTo(0));
        }

        [Test]
        public void CheckIfContainsNothing()
        {
            BitSet a = new();
            a.Set(5);

            BitSet b = new();

            Assert.That(a.ContainsAll(b), Is.True);
        }
    }
}