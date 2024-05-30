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
        /// Removes a child node at the given index.
        /// </summary>
        void RemoveAt(int index);

        /// <summary>
        /// Inserts a child node at the given index.
        /// </summary>
        void Insert(int index, INode child);
    }
}
