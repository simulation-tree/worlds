using Unmanaged;

namespace Worlds.Tests
{
    public struct SimpleComponent
    {
        public ASCIIText256 data;

        public SimpleComponent(ASCIIText256 data)
        {
            this.data = data;
        }
    }
}