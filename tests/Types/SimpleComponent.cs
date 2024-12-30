using Unmanaged;

namespace Worlds.Tests
{
    [Component]
    [ArrayElement]
    public struct SimpleComponent
    {
        public FixedString data;

        public SimpleComponent(FixedString data)
        {
            this.data = data;
        }
    }
}