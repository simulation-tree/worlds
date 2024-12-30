namespace Worlds.Tests
{
    [Component]
    public struct TestComponent
    {
        public int value;

        public TestComponent(int value)
        {
            this.value = value;
        }
    }
}