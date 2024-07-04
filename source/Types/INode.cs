using System.Collections.Generic;

namespace Simulation
{
    public interface INode
    {
        /// <summary>
        /// Modifiable parent of this node.
        /// </summary>
        INode? Parent { get; set; }

        /// <summary>
        /// All children of this node.
        /// </summary>
        IReadOnlyList<INode> Children { get; }

        /// <summary>
        /// Removes the an item from the <see cref="Children"/> list.
        /// </summary>
        void RemoveAt(int index);

        /// <summary>
        /// Inserts a new item into the <see cref="Children"/> list.
        /// </summary>
        void Insert(int index, INode child);
    }
}
