using Collections;
using Collections.Generic;
using System;
using Unmanaged;

namespace Worlds.Tests
{
    public class SchemaTests : WorldTests
    {
        [Test]
        public void AddToSchema()
        {
            using Schema schema = new();
            Assert.That(schema.ContainsComponentType<Stress>(), Is.False);
            schema.RegisterComponent<Stress>();
            Assert.That(schema.ContainsComponentType<Stress>(), Is.True);
        }

        [Test]
        public void Copying()
        {
            using Schema schema = new();
            Assert.That(schema.ContainsComponentType<Stress>(), Is.False);
            schema.RegisterComponent<Stress>();
            TagType thingTag = schema.RegisterTag<IsThing>();
            Assert.That(schema.ContainsComponentType<Stress>(), Is.True);
            Assert.That(schema.ContainsTagType<IsThing>(), Is.True);

            using Schema copy = new();
            copy.CopyFrom(schema);

            Assert.That(copy.ContainsComponentType<Stress>(), Is.True);
            Assert.That(copy.ContainsTagType<IsThing>(), Is.True);
            Assert.That(copy.ContainsTagType(thingTag), Is.True);
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
            TagType thingTag = prefabSchema.RegisterTag<IsThing>();

            using ByteWriter writer = new();
            writer.WriteObject(prefabSchema);

            using ByteReader reader = new(writer);
            using Schema loadedSchema = reader.ReadObject<Schema>();

            Assert.That(loadedSchema.ContainsComponentType<float>(), Is.True);
            Assert.That(loadedSchema.ContainsComponentType<char>(), Is.True);
            Assert.That(loadedSchema.ContainsTagType<IsThing>(), Is.True);
            Assert.That(loadedSchema.ContainsTagType(thingTag), Is.True);
        }

        [Test]
        public void VerifyTagsGetSaved()
        {
            using Schema schema = new();
            schema.RegisterTag<bool>();
            schema.RegisterTag<byte>();
            schema.RegisterComponent<bool>();
            schema.RegisterArrayElement<byte>();

            using ByteWriter writer = new();
            writer.WriteObject(schema);

            using ByteReader reader = new(writer);
            using Schema loadedSchema = reader.ReadObject<Schema>();

            Assert.That(loadedSchema.ContainsTagType<bool>(), Is.True);
            Assert.That(loadedSchema.ContainsTagType<byte>(), Is.True);
            Assert.That(loadedSchema.ContainsComponentType<bool>(), Is.True);
            Assert.That(loadedSchema.ContainsArrayType<byte>(), Is.True);
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
            DataType charType = schema.GetArrayDataType<char>();
            DataType intType = schema.GetTagDataType<int>();

            Assert.That(boolType.size, Is.EqualTo(sizeof(bool)));
            Assert.That(boolType.kind, Is.EqualTo(DataType.Kind.Component));
            Assert.That(byteType.size, Is.EqualTo(sizeof(byte)));
            Assert.That(byteType.kind, Is.EqualTo(DataType.Kind.Component));
            Assert.That(shortType.size, Is.EqualTo(sizeof(short)));
            Assert.That(shortType.kind, Is.EqualTo(DataType.Kind.Component));
            Assert.That(charType.size, Is.EqualTo(sizeof(char)));
            Assert.That(charType.kind, Is.EqualTo(DataType.Kind.ArrayElement));
            Assert.That(intType.size, Is.EqualTo(0));
            Assert.That(intType.kind, Is.EqualTo(DataType.Kind.Tag));

            Assert.That(schema.GetComponentTypeSize(boolType.index), Is.EqualTo(sizeof(bool)));
            Assert.That(schema.GetComponentTypeSize(byteType.index), Is.EqualTo(sizeof(byte)));
            Assert.That(schema.GetComponentTypeSize(shortType.index), Is.EqualTo(sizeof(short)));
            Assert.That(schema.GetArrayTypeSize(charType.index), Is.EqualTo(sizeof(char)));
        }

        [Test]
        public void LoadCustomBank()
        {
            using Schema schema = new();
            Assert.That(schema.ContainsComponentType<DateTime>(), Is.False);
            schema.Load<CustomSchemaBank>();
            Assert.That(schema.ContainsComponentType<DateTime>(), Is.True);
        }
    }
}