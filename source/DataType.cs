namespace Worlds
{
    /// <summary>
    /// Describes the type of data found on an entity.
    /// </summary>
    public enum DataType : byte
    {
        Unknown,
        Component,
        ArrayElement,
        Tag
    }
}