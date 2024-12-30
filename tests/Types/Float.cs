namespace Worlds.Tests
{
    [Component]
    [ArrayElement]
    public struct Float
    {
        public float value;

        public Float(float value)
        {
            this.value = value;
        }

        public static implicit operator Float(float value)
        {
            return new Float(value);
        }

        public static implicit operator float(Float value)
        {
            return value.value;
        }
    }
}