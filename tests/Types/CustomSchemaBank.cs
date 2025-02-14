using System;

namespace Worlds.Tests
{
    public readonly struct CustomSchemaBank : ISchemaBank
    {
        void ISchemaBank.Load(Schema schema)
        {
            schema.RegisterComponent<DateTime>();
        }
    }
}