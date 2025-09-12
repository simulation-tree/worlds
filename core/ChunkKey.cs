namespace Worlds
{
    internal readonly struct ChunkKey
    {
        public readonly Definition definition;
        public readonly Chunk chunk;

        public unsafe ChunkKey(Chunk chunk)
        {
            this.definition = chunk.chunk->Definition;
            this.chunk = chunk;
        }
    }
}