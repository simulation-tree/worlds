using Unmanaged;

namespace Worlds.Tests
{
    [Component]
    public struct Cherry
    {
        public FixedString stones;

        public Cherry(FixedString stones)
        {
            this.stones = stones;
        }
    }
}