namespace Worlds.Tests
{
    [Component]
    public struct Another
    {
        public uint data;

        public Another(uint data)
        {
            this.data = data;
        }
    }
}