namespace Worlds
{
    internal readonly struct ChunkKey
    {
        public readonly long definitionHash;
        public readonly Chunk chunk;

        public ChunkKey(Chunk chunk)
        {
            definitionHash = Definition.GetLongHashCode(chunk.componentTypes, chunk.ArrayTypes, chunk.tagTypes);
            this.chunk = chunk;
        }
    }
}