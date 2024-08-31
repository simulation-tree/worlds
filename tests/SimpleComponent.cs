using System;
using Unmanaged;

namespace Simulation.Tests
{
    public struct SimpleComponent
    {
        public FixedString data;

        public SimpleComponent(ReadOnlySpan<char> data)
        {
            this.data = new(data);
        }
    }
}