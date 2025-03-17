namespace Worlds
{
    /// <summary>
    /// Describes the components, arrays and tag types found in the declaring project.
    /// </summary>
    public interface ISchemaBank
    {
        /// <summary>
        /// Loads all types described in this schema bank into the given <paramref name="schema"/>.
        /// </summary>
        void Load(Schema schema);
    }
}