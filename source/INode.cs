using System.Collections.Generic;

namespace Game
{
    public interface INode
    {
        /// <summary>
        /// Parent of this node.
        /// </summary>
        INode? Parent { get; set; }

        /// <summary>
        /// List of all children of this node.
        /// </summary>
        IReadOnlyList<INode> Children { get; }

        /// <summary>
        /// Count of all children of this node.
        /// </summary>
        public uint Count => (uint)Children.Count;

        public INode this[uint index] => Children[(int)index];

        void Receive<T>(T message) where T : unmanaged;

        /// <summary>
        /// Adds a new child node.
        /// </summary>
        void Add(INode node);

        /// <summary>
        /// Removes a child node at the given index.
        /// </summary>
        void RemoveAt(uint index);

        /// <summary>
        /// Tries to find the index of the given node.
        /// </summary>
        bool TryIndexOf(INode node, out uint index);
    }
}
