using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Worlds.Tests
{
    public class BitMaskTests
    {
        [Test]
        public void SetThenCheckIfContains()
        {
            BitMask a = new();
            a.Set(3);
            Assert.That(a.Contains(0), Is.False);
            Assert.That(a.Contains(1), Is.False);
            Assert.That(a.Contains(2), Is.False);
            Assert.That(a.Contains(3), Is.True);
            Assert.That(a.ToString(), Is.EqualTo("3"));

            a.Clear(3);
            a.Set(0);
            a.Set(32);
            Assert.That(a.Contains(3), Is.False);
            Assert.That(a.Contains(0), Is.True);
            Assert.That(a.Contains(32), Is.True);
            Assert.That(a.ToString(), Is.EqualTo("0, 32"));
        }

        [Test]
        public void PerformAndOperation()
        {
            BitMask a = new();
            a.Set(3);
            a.Set(4);
            a.Set(5);

            BitMask b = new();
            b.Set(4);

            BitMask c = a & b;
            Assert.That(c.Contains(3), Is.False);
            Assert.That(c.Contains(4), Is.True);
            Assert.That(c.Contains(5), Is.False);
        }

        [Test]
        public void CheckIfIntersects()
        {
            BitMask a = new();
            a.Set(3);
            a.Set(4);
            a.Set(5);

            BitMask b = new();
            b.Set(4);

            Assert.That(a & b, Is.EqualTo(b));
        }

        [Test]
        public void CantContainAll()
        {
            BitMask a = new();
            a.Set(3);
            a.Set(4);
            a.Set(5);

            BitMask b = new();
            b.Set(4);
            b.Set(9);

            Assert.That(a & b, Is.Not.EqualTo(b));
        }

        [Test]
        public void CountIncrementsWhenSetting()
        {
            BitMask a = new();
            Assert.That(a.Count, Is.EqualTo(0));
            a.Set(3);
            Assert.That(a.Count, Is.EqualTo(1));
            a.Set(30);
            Assert.That(a.Count, Is.EqualTo(2));
            a.Set(200);
            Assert.That(a.Count, Is.EqualTo(3));
            a.Clear(200);
            Assert.That(a.Count, Is.EqualTo(2));
            a.Clear(30);
            Assert.That(a.Count, Is.EqualTo(1));
            a.Clear(3);
            Assert.That(a.Count, Is.EqualTo(0));
        }

        [Test]
        public void CheckIfContainsNothing()
        {
            BitMask a = new();
            a.Set(5);

            BitMask b = new();

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
                    BitMask a = new();
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
    }
}