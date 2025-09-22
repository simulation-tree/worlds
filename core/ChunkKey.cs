namespace Worlds
{
    internal readonly struct ChunkKey
    {
        public readonly long definitionHash;
        public readonly Chunk chunk;

        public ChunkKey(Chunk chunk)
        {
            definitionHash = Definition.GetLongHashCode(chunk.ComponentTypes, chunk.ArrayTypes, chunk.TagTypes);
            this.chunk = chunk;
        }
    }
}