namespace Worlds
{
    /// <summary>
    /// Extensions for <see cref="ISchemaBank"/> instances.
    /// </summary>
    public static class SchemaBankExtensions
    {
        /// <summary>
        /// Loads the schema bank of type <typeparamref name="T"/> into the <paramref name="schema"/>.
        /// </summary>
        public static void Load<T>(this T schemaBank, Schema schema) where T : unmanaged, ISchemaBank
        {
            schemaBank.Load(schema);
        }
    }
}