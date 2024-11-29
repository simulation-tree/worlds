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
                    a.Set(3);
                    a.Set(4);
                    a.Set(5);
                    a.Set(6);
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
            BitSet a = new([ComponentType.Get<Double>(), ComponentType.Get<Byte>(), ComponentType.Get<Float>()]);
            BitSet b = new([ComponentType.Get<Byte>(), ComponentType.Get<Float>()]);
            Assert.That(a, Is.Not.EqualTo(b));
        }
    }
}