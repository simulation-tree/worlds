namespace Simulation
{
    public static class QueryFunctions
    {
        /// <summary>
        /// Retrieves the index of the entity in the query results.
        /// </summary>
        public unsafe static bool TryIndexOf<T>(this T query, uint entity, out uint resultIndex) where T : unmanaged, IQuery
        {
            nint results = query.Results;
            uint resultSize = query.ResultSize;
            uint count = query.Count;
            for (uint i = 0; i < count; i++)
            {
                uint currentEntity = *(uint*)(results + i * resultSize);
                if (currentEntity == entity)
                {
                    resultIndex = i;
                    return true;
                }
            }

            resultIndex = default;
            return false;
        }

        public unsafe static bool Contains<T>(this T query, uint entity) where T : unmanaged, IQuery
        {
            nint results = query.Results;
            uint resultSize = query.ResultSize;
            uint count = query.Count;
            for (uint i = 0; i < count; i++)
            {
                uint currentEntity = *(uint*)(results + i * resultSize);
                if (currentEntity == entity)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
