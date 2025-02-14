namespace Worlds
{
    public static class SchemaBankExtensions
    {
        public static void Load<T>(this T schemaBank, Schema schema) where T : unmanaged, ISchemaBank
        {
            schemaBank.Load(schema);
        }
    }
}