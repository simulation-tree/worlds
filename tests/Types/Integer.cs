namespace Worlds.Tests
{
    public struct Integer
    {
        public int value;

        public Integer(int value)
        {
            this.value = value;
        }

        public static implicit operator Integer(int value)
        {
            return new Integer(value);
        }

        public static implicit operator int(Integer value)
        {
            return value.value;
        }
    }
}