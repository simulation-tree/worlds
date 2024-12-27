using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Worlds.Tests
{
    public class BitSetTests : WorldTests
    {
        [Test]
        public void SetThenCheckIfContains()
        {
            BitSet a = new();
            a |= 3;
            Assert.That(a == 0, Is.False);
            Assert.That(a == 1, Is.False);
            Assert.That(a == 2, Is.False);
            Assert.That(a == 3, Is.True);
            Assert.That(a.ToString(), Is.EqualTo("000100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"));

            a &= 3;
            a |= 32;
            Assert.That(a == 3, Is.False);
            Assert.That(a == 32, Is.True);
            Assert.That(a.ToString(), Is.EqualTo("000000000000000000000000000000001000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"));
        }

        [Test]
        public void PerformAndOperation()
        {
            BitSet a = new();
            a |= 3;
            a |= 4;
            a |= 5;

            BitSet b = new();
            b |= 4;

            BitSet c = a & b;
            Assert.That(c == 3, Is.False);
            Assert.That(c == 4, Is.True);
            Assert.That(c == 5, Is.False);
        }

        [Test]
        public void CheckIfIntersects()
        {
            BitSet a = new();
            a |= 3;
            a |= 4;
            a |= 5;

            BitSet b = new();
            b |= 4;

            Assert.That(a & b, Is.EqualTo(b));
        }

        [Test]
        public void CantContainAll()
        {
            BitSet a = new();
            a |= 3;
            a |= 4;
            a |= 5;

            BitSet b = new();
            b |= 4;
            b |= 9;

            Assert.That(a & b, Is.Not.EqualTo(b));
        }

        [Test]
        public void CountIncrementsWhenSetting()
        {
            BitSet a = new();
            Assert.That(a.Count, Is.EqualTo(0));
            a |= 3;
            Assert.That(a.Count, Is.EqualTo(1));
            a |= 30;
            Assert.That(a.Count, Is.EqualTo(2));
            a |= 200;
            Assert.That(a.Count, Is.EqualTo(3));
            a &= 200;
            Assert.That(a.Count, Is.EqualTo(2));
            a &= 30;
            Assert.That(a.Count, Is.EqualTo(1));
            a &= 3;
            Assert.That(a.Count, Is.EqualTo(0));
        }

        [Test]
        public void CheckIfContainsNothing()
        {
            BitSet a = new();
            a |= 5;

            BitSet b = new();

            Assert.That(a | b, Is.EqualTo(a));
        }

        [Test]
        public void BenchmarkHashCode()
        {
            Stopwatch stopwatch = new();
            List<long> elapsedTicks = new();
            Perform(32, () =>
            {
                stopwatch.Restart();
                for (int i = 0; i < 1000; i++)
                {
                    BitSet a = new();
                    a |= 3;
                    a |= 4;
                    a |= 5;
                    a |= 6;
                    int hashCode = a.GetHashCode();
                }

                stopwatch.Stop();
                elapsedTicks.Add(stopwatch.ElapsedTicks);
            });

            Console.WriteLine($"GetHashCode(): {GetElapsedTicksAverage()}");

            void Perform(int times, Action action)
            {
                for (int i = 0; i < times; i++)
                {
                    action();
                }
            }

            long GetElapsedTicksAverage()
            {
                long totalTicks = 0;
                foreach (long ticks in elapsedTicks)
                {
                    totalTicks += ticks;
                }

                return totalTicks / elapsedTicks.Count;
            }
        }

        [Test]
        public void CompareHashCodeCase1()
        {
            using Schema schema = CreateSchema();
            BitSet a = new([schema.GetComponent<Double>(), schema.GetComponent<Byte>(), schema.GetComponent<Float>()]);
            BitSet b = new([schema.GetComponent<Byte>(), schema.GetComponent<Float>()]);
            Assert.That(a, Is.Not.EqualTo(b));
        }
    }
}