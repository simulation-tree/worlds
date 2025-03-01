using System.Collections;
using System.Collections.Generic;
using Worlds.Generator;

namespace Worlds
{
    public class DataTypeCollection : IEnumerable<DataType>
    {
        private readonly List<DataType> all;

        public int Count => all.Count;

        public DataTypeCollection()
        {
            all = new();
        }

        public List<DataType>.Enumerator GetEnumerator()
        {
            return all.GetEnumerator();
        }

        public bool TryAdd(DataKind kind, string fullTypeName)
        {
            //todo: the check against data types here is because of methods that accept both a generic, and the data type
            //need a smarter way to fetch the generic type out
            if (fullTypeName == "?" || fullTypeName == "Worlds.ComponentType" || fullTypeName == "Worlds.ArrayElementType" || fullTypeName == "Worlds.TagType")
            {
                return false;
            }

            DataType found = new(kind, fullTypeName);
            if (all.Contains(found))
            {
                return false;
            }

            all.Add(found);
            return true;
        }

        IEnumerator<DataType> IEnumerable<DataType>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}