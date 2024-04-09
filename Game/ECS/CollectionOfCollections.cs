using Unmanaged.Collections;

namespace Game.ECS
{
    internal unsafe sealed class CollectionOfCollections
    {
        public UnsafeList*[] lists;

        public CollectionOfCollections()
        {
            lists = new UnsafeList*[CollectionType.MaxTypes];
        }
    }
}
