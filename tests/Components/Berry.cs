namespace Worlds.Tests
{
    [Component]
    public struct Berry
    {
        public byte hearts;

        public Berry(byte hearts)
        {
            this.hearts = hearts;
        }
    }
}