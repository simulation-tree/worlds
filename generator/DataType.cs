using System;

namespace Worlds
{
    internal readonly struct DataType : IEquatable<DataType>
    {
        public readonly DataKind kind;
        public readonly string fullTypeName;

        public readonly string Name
        {
            get
            {
                int index = fullTypeName.LastIndexOf('.');
                if (index == -1)
                {
                    return fullTypeName;
                }

                return fullTypeName.Substring(index + 1);
            }
        }

        public DataType(DataKind kind, string fullTypeName)
        {
            this.kind = kind;
            this.fullTypeName = fullTypeName;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is DataType type && Equals(type);
        }

        public readonly bool Equals(DataType other)
        {
            return kind == other.kind && fullTypeName == other.fullTypeName;
        }

        public readonly override int GetHashCode()
        {
            int hashCode = -1702990006;
            hashCode = hashCode * -1521134295 + kind.GetHashCode();
            for (int i = 0; i < fullTypeName.Length; i++)
            {
                hashCode = hashCode * -1521134295 + fullTypeName[i];
            }

            return hashCode;
        }

        public static bool operator ==(DataType left, DataType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DataType left, DataType right)
        {
            return !(left == right);
        }
    }
}