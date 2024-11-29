namespace Worlds.Tests
{
    [Component]
    [Array]
    public struct Character
    {
        public char value;

        public Character(char value)
        {
            this.value = value;
        }

        public static implicit operator Character(char value)
        {
            return new Character(value);
        }

        public static implicit operator char(Character value)
        {
            return value.value;
        }
    }
}