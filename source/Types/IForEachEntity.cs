namespace Worlds
{
    public interface IForEach
    {
        /// <summary>
        /// Component types that must be present.
        /// </summary>
        BitSet ComponentTypes { get; }

        /// <summary>
        /// Component types that must not be present.
        /// </summary>
        public BitSet ExcludeComponentTypes => default;

        void ForEach(ComponentChunk chunk, uint index);
    }

    public interface IForEach<C1> : IForEach where C1 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1>();

        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            ForEach(ref chunk.GetComponent<C1>(index));
        }

        void ForEach(ref C1 c1);
    }

    public interface IForEachEntity<C1> : IForEach where C1 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1>();

        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            uint entity = chunk.Entities[index];
            ForEach(in entity, ref chunk.GetComponent<C1>(index));
        }

        void ForEach(in uint entity, ref C1 c1);
    }

    public interface IForEach<C1, C2> : IForEach where C1 : unmanaged where C2 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1, C2>();

        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            ForEach(ref chunk.GetComponent<C1>(index), ref chunk.GetComponent<C2>(index));
        }

        void ForEach(ref C1 c1, ref C2 c2);
    }

    public interface IForEachEntity<C1, C2> : IForEach where C1 : unmanaged where C2 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1, C2>();

        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            uint entity = chunk.Entities[index];
            ForEach(in entity, ref chunk.GetComponent<C1>(index), ref chunk.GetComponent<C2>(index));
        }

        void ForEach(in uint entity, ref C1 c1, ref C2 c2);
    }

    public interface IForEach<C1, C2, C3> : IForEach where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1, C2, C3>();

        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            ForEach(ref chunk.GetComponent<C1>(index), ref chunk.GetComponent<C2>(index), ref chunk.GetComponent<C3>(index));
        }

        void ForEach(ref C1 c1, ref C2 c2, ref C3 c3);
    }

    public interface IForEachEntity<C1, C2, C3> : IForEach where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1, C2, C3>();

        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            uint entity = chunk.Entities[index];
            ForEach(in entity, ref chunk.GetComponent<C1>(index), ref chunk.GetComponent<C2>(index), ref chunk.GetComponent<C3>(index));
        }

        void ForEach(in uint entity, ref C1 c1, ref C2 c2, ref C3 c3);
    }

    public interface IForEach<C1, C2, C3, C4> : IForEach where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1, C2, C3, C4>();

        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            ForEach(ref chunk.GetComponent<C1>(index), ref chunk.GetComponent<C2>(index), ref chunk.GetComponent<C3>(index), ref chunk.GetComponent<C4>(index));
        }

        void ForEach(ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4);
    }

    public interface IForEachEntity<C1, C2, C3, C4> : IForEach where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1, C2, C3, C4>();

        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            uint entity = chunk.Entities[index];
            ForEach(in entity, ref chunk.GetComponent<C1>(index), ref chunk.GetComponent<C2>(index), ref chunk.GetComponent<C3>(index), ref chunk.GetComponent<C4>(index));
        }

        void ForEach(in uint entity, ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4);
    }

    public interface IForEach<C1, C2, C3, C4, C5> : IForEach where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1, C2, C3, C4, C5>();

        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            ForEach(ref chunk.GetComponent<C1>(index), ref chunk.GetComponent<C2>(index), ref chunk.GetComponent<C3>(index), ref chunk.GetComponent<C4>(index), ref chunk.GetComponent<C5>(index));
        }

        void ForEach(ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, ref C5 c5);
    }

    public interface IForEachEntity<C1, C2, C3, C4, C5> : IForEach where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1, C2, C3, C4, C5>();

        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            uint entity = chunk.Entities[index];
            ForEach(in entity, ref chunk.GetComponent<C1>(index), ref chunk.GetComponent<C2>(index), ref chunk.GetComponent<C3>(index), ref chunk.GetComponent<C4>(index), ref chunk.GetComponent<C5>(index));
        }

        void ForEach(in uint entity, ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, ref C5 c5);
    }

    public interface IForEach<C1, C2, C3, C4, C5, C6> : IForEach where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6>();

        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            ForEach(ref chunk.GetComponent<C1>(index), ref chunk.GetComponent<C2>(index), ref chunk.GetComponent<C3>(index), ref chunk.GetComponent<C4>(index), ref chunk.GetComponent<C5>(index), ref chunk.GetComponent<C6>(index));
        }

        void ForEach(ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, ref C5 c5, ref C6 c6);
    }

    public interface IForEachEntity<C1, C2, C3, C4, C5, C6> : IForEach where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6>();

        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            uint entity = chunk.Entities[index];
            ForEach(in entity, ref chunk.GetComponent<C1>(index), ref chunk.GetComponent<C2>(index), ref chunk.GetComponent<C3>(index), ref chunk.GetComponent<C4>(index), ref chunk.GetComponent<C5>(index), ref chunk.GetComponent<C6>(index));
        }

        void ForEach(in uint entity, ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, ref C5 c5, ref C6 c6);
    }

    public interface IForEach<C1, C2, C3, C4, C5, C6, C7> : IForEach where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7>();
        
        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            ForEach(ref chunk.GetComponent<C1>(index), ref chunk.GetComponent<C2>(index), ref chunk.GetComponent<C3>(index), ref chunk.GetComponent<C4>(index), ref chunk.GetComponent<C5>(index), ref chunk.GetComponent<C6>(index), ref chunk.GetComponent<C7>(index));
        }
        
        void ForEach(ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, ref C5 c5, ref C6 c6, ref C7 c7);
    }

    public interface IForEachEntity<C1, C2, C3, C4, C5, C6, C7> : IForEach where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7>();
        
        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            uint entity = chunk.Entities[index];
            ForEach(in entity, ref chunk.GetComponent<C1>(index), ref chunk.GetComponent<C2>(index), ref chunk.GetComponent<C3>(index), ref chunk.GetComponent<C4>(index), ref chunk.GetComponent<C5>(index), ref chunk.GetComponent<C6>(index), ref chunk.GetComponent<C7>(index));
        }

        void ForEach(in uint entity, ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, ref C5 c5, ref C6 c6, ref C7 c7);
    }

    public interface IForEach<C1, C2, C3, C4, C5, C6, C7, C8> : IForEach where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8>();
        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            ForEach(ref chunk.GetComponent<C1>(index), ref chunk.GetComponent<C2>(index), ref chunk.GetComponent<C3>(index), ref chunk.GetComponent<C4>(index), ref chunk.GetComponent<C5>(index), ref chunk.GetComponent<C6>(index), ref chunk.GetComponent<C7>(index), ref chunk.GetComponent<C8>(index));
        }

        void ForEach(ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, ref C5 c5, ref C6 c6, ref C7 c7, ref C8 c8);
    }

    public interface IForEachEntity<C1, C2, C3, C4, C5, C6, C7, C8> : IForEach where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
    {
        BitSet IForEach.ComponentTypes => ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8>();
        
        void IForEach.ForEach(ComponentChunk chunk, uint index)
        {
            uint entity = chunk.Entities[index];
            ForEach(in entity, ref chunk.GetComponent<C1>(index), ref chunk.GetComponent<C2>(index), ref chunk.GetComponent<C3>(index), ref chunk.GetComponent<C4>(index), ref chunk.GetComponent<C5>(index), ref chunk.GetComponent<C6>(index), ref chunk.GetComponent<C7>(index), ref chunk.GetComponent<C8>(index));
        }

        void ForEach(in uint entity, ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, ref C5 c5, ref C6 c6, ref C7 c7, ref C8 c8);
    }
}
