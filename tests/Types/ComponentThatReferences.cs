namespace Worlds.Tests
{
    [Component]
    public struct ComponentThatReferences
    {
        public rint reference;

        public ComponentThatReferences(rint reference)
        {
            this.reference = reference;
        }
    }
}