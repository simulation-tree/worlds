using System;

namespace Worlds
{
    /// <summary>
    /// Exception for when a chunk is being iterated while it has also changed.
    /// </summary>
    public class ChunkModifiedWhileIteratingException : Exception
    {
        /// <inheritdoc/>
        public ChunkModifiedWhileIteratingException(Chunk chunk) : base(GetMessage(chunk))
        {
        }

        private unsafe static string GetMessage(Chunk chunk)
        {
            return $"Chunk `{chunk}` is being iterated while it has also changed.";
        }
    }
}