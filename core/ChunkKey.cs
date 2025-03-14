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

        public readonly override bool Equals(object? obj)
        {
            return obj is ChunkKey key && Equals(key);
        }

        public readonly bool Equals(Definition definition)
        {
            return this.definition.Equals(definition);
        }

        public readonly bool Equals(ChunkKey other)
        {
            return definition.Equals(other.definition);
        }

        public readonly long GetLongHashCode()
        {
            return definition.GetLongHashCode();
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