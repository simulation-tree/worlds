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
        public void CollectTimes()
        {
            Stopwatch stopwatch = new();
            List<int> times = new();
            for (int r = 0; r < 100; r++)
            {
                stopwatch.Start();
                for (int i = 0; i < 100000; i++)
                {
                    BitMask x = new();
                    x.Set(3);
                    x.Set(4);
                    x.Set(5);
                    x.Set(9);
                    x.Contains(200);

                    BitMask y = new();
                    y.Set(4);
                    y.Set(9);

                    x.Contains(4);
                    x.Contains(9);
                }

                stopwatch.Stop();
                times.Add((int)stopwatch.ElapsedTicks);
            }

            float min = int.MaxValue;
            float max = int.MinValue;
            float total = 0;
            foreach (int time in times)
            {
                if (time < min)
                {
                    min = time;
                }
                
                if (time > max)
                {
                    max = time;
                }

                total += time;
            }

            float avg = total / times.Count;
            Console.WriteLine($"Min: {min / Stopwatch.Frequency}");
            Console.WriteLine($"Max: {max / Stopwatch.Frequency}");
            Console.WriteLine($"Avg: {avg / Stopwatch.Frequency}");
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

        public class ClassBitMask
        {
            private BitMask value;

            public void Set(byte index)
            {
                value.Set(index);
            }

            public void Clear()
            {
                value = default;
            }

            public void Clear(byte index)
            {
                value.Clear(index);
            }

            public bool Contains(byte index)
            {
                return value.Contains(index);
            }
        }
    }
}