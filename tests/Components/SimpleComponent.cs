using Unmanaged;

namespace Simulation.Tests
{
    public struct SimpleComponent
    {
        public FixedString data;

        public SimpleComponent(FixedString data)
        {
            this.data = data;
        }
    }
}