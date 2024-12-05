namespace Worlds.Tests
{
    [Component]
    public struct Apple
    {
        public byte bites;

        public Apple(byte bites)
        {
            this.bites = bites;
        }
    }
}