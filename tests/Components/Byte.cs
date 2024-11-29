namespace Worlds.Tests
{
    [Component]
    [Array]
    public struct Byte
    {
        public byte value;

        public Byte(byte value)
        {
            this.value = value;
        }

        public static implicit operator Byte(byte value)
        {
            return new Byte(value);
        }

        public static implicit operator byte(Byte value)
        {
            return value.value;
        }
    }
}