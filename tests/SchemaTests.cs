﻿using Collections;
using Unmanaged;

namespace Worlds.Tests
{
    public class SchemaTests : WorldTests
    {
        [Test]
        public void AddToSchema()
        {
            using Schema schema = new();
            Assert.That(schema.ContainsComponent<Stress>(), Is.False);
            schema.RegisterComponent<Stress>();
            Assert.That(schema.ContainsComponent<Stress>(), Is.True);

            using Schema copy = new();
            copy.CopyFrom(schema);

            Assert.That(copy.ContainsComponent<Stress>(), Is.True);
        }

        [Test]
        public void IterateAndFetchLayouts()
        {
            using Schema schema = new();
            ComponentType c1 = schema.RegisterComponent<Stress>();
            ComponentType c2 = schema.RegisterComponent<float>();
            ComponentType c3 = schema.RegisterComponent<char>();

            using List<ComponentType> componentTypes = new();
            foreach (ComponentType componentType in schema.ComponentTypes)
            {
                componentTypes.Add(componentType);
            }

            Assert.That(componentTypes.Count, Is.EqualTo(3));
            Assert.That(componentTypes.Contains(c1), Is.True);
            Assert.That(componentTypes.Contains(c2), Is.True);
            Assert.That(componentTypes.Contains(c3), Is.True);
        }

        [Test]
        public void SerializeSchema()
        {
            using Schema prefabSchema = new();
            prefabSchema.RegisterComponent<float>();
            prefabSchema.RegisterComponent<char>();

            using BinaryWriter writer = new();
            writer.WriteObject(prefabSchema);

            using BinaryReader reader = new(writer);
            using Schema loadedSchema = reader.ReadObject<Schema>();

            Assert.That(loadedSchema.ContainsComponent<float>(), Is.True);
            Assert.That(loadedSchema.ContainsComponent<char>(), Is.True);
        }

        [Test]
        public void VerifyTagsGetSaved()
        {
            using Schema schema = new();
            schema.RegisterTag<bool>();
            schema.RegisterTag<byte>();
            schema.RegisterComponent<bool>();
            schema.RegisterArrayElement<byte>();

            using BinaryWriter writer = new();
            writer.WriteObject(schema);

            using BinaryReader reader = new(writer);
            using Schema loadedSchema = reader.ReadObject<Schema>();

            Assert.That(loadedSchema.ContainsTag<bool>(), Is.True);
            Assert.That(loadedSchema.ContainsTag<byte>(), Is.True);
            Assert.That(loadedSchema.ContainsComponent<bool>(), Is.True);
            Assert.That(loadedSchema.ContainsArrayElement<byte>(), Is.True);
        }

        [Test]
        public void VerifySizeOfDataTypes()
        {
            using Schema schema = new();
            schema.RegisterComponent<bool>();
            schema.RegisterComponent<byte>();
            schema.RegisterComponent<short>();
            schema.RegisterArrayElement<char>();
            schema.RegisterTag<int>();

            DataType boolType = schema.GetComponentDataType<bool>();
            DataType byteType = schema.GetComponentDataType<byte>();
            DataType shortType = schema.GetComponentDataType<short>();
            DataType charType = schema.GetArrayElementDataType<char>();
            DataType intType = schema.GetTagDataType<int>();

            Assert.That(boolType.Size, Is.EqualTo(sizeof(bool)));
            Assert.That(boolType.Type, Is.EqualTo(DataType.Kind.Component));
            Assert.That(byteType.Size, Is.EqualTo(sizeof(byte)));
            Assert.That(byteType.Type, Is.EqualTo(DataType.Kind.Component));
            Assert.That(shortType.Size, Is.EqualTo(sizeof(short)));
            Assert.That(shortType.Type, Is.EqualTo(DataType.Kind.Component));
            Assert.That(charType.Size, Is.EqualTo(sizeof(char)));
            Assert.That(charType.Type, Is.EqualTo(DataType.Kind.ArrayElement));
            Assert.That(intType.Size, Is.EqualTo(0));
            Assert.That(intType.Type, Is.EqualTo(DataType.Kind.Tag));
        }

        [Test]
        public void LoadCustomBank()
        {
            using Schema schema = new();
            schema.Load<CustomSchemaBank>();
            Assert.That(schema.ContainsComponent<bool>(), Is.True);
            Assert.That(schema.ContainsComponent<FixedString>(), Is.True);
            Assert.That(schema.ContainsComponent<uint>(), Is.True);
            Assert.That(schema.ContainsComponent<float>(), Is.True);
            Assert.That(schema.ContainsComponent<ulong>(), Is.True);
            Assert.That(schema.ContainsComponent<byte>(), Is.True);
            Assert.That(schema.ContainsComponent<int>(), Is.True);
        }
    }
}