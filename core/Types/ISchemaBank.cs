namespace Worlds
{
    /// <summary>
    /// Describes the component, array element and tag types found in the declaring project.
    /// </summary>
    public interface ISchemaBank
    {
        void Load(Schema schema);
    }
}