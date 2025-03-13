using System;

namespace Worlds
{
    public readonly struct ChunkKey : IEquatable<ChunkKey>
    {
        public readonly Definition definition;
        public readonly Chunk chunk;

        public ChunkKey(Chunk chunk)
        {
            this.definition = chunk.Definition;
            this.chunk = chunk;
        }

        public ChunkKey(Definition definition)
        {
            this.definition = definition;
            chunk = default;
        }

        public ChunkKey(Definition definition, Schema schema)
        {
            this.definition = definition;
            chunk = new(definition, schema);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is ChunkKey key && Equals(key);
        }

        public readonly bool Equals(ChunkKey other)
        {
            return definition.Equals(other.definition);
        }

        public readonly override int GetHashCode()
        {
            return definition.GetHashCode();
        }

        public static bool operator ==(ChunkKey left, ChunkKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChunkKey left, ChunkKey right)
        {
            return !(left == right);
        }
    }
}