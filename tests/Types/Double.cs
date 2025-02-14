namespace Worlds.Tests
{
    public struct Double
    {
        public double value;

        public Double(double value)
        {
            this.value = value;
        }

        public static implicit operator Double(double value)
        {
            return new Double(value);
        }

        public static implicit operator double(Double value)
        {
            return value.value;
        }
    }
}