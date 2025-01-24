using Worlds.Functions;

namespace Worlds
{
    /// <summary>
    /// Describes the component, array element and tag types found in the declaring project.
    /// </summary>
    public interface ISchemaBank
    {
        /// <summary>
        /// Loads the types into the schema.
        /// </summary>
        void Load(RegisterDataType function);
    }
}