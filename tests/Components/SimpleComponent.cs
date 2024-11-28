using Unmanaged;

namespace Worlds.Tests
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