using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Game
{
    public static class INodeFunctions
    {
        public static void Add(this INode node, INode add)
        {
            node.Add(add);
        }

        public static void RemoveAt(this INode node, uint index)
        {
            node.RemoveAt(index);
        }

        public static bool TryIndexOf(this INode node, INode find, out uint index)
        {
            return node.TryIndexOf(find, out index);
        }

        public static bool Contains(this INode node, INode find)
        {
            return node.TryIndexOf(find, out _);
        }

        public static bool Remove(this INode node, INode remove)
        {
            if (node.TryIndexOf(remove, out uint index))
            {
                node.RemoveAt(index);
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Broadcasts a message to all descendants of this node,
        /// and the node itself.
        /// </summary>
        public static void Broadcast<T>(this INode node, T message) where T : unmanaged
        {
            foreach (INode child in node.Children)
            {
                child.Broadcast(message);
            }

            node.Receive(message);
        }

        /// <summary>
        /// Fills the given list with all descendants of this node.
        /// </summary>
        public static void FillDescendants(this INode node, ICollection<INode> collection)
        {
            Stack<INode> stack = Pool.GetNodeStack();
            for (int i = 0; i < node.Children.Count; i++)
            {
                stack.Push(node.Children[i]);
            }

            while (stack.Count > 0)
            {
                INode n = stack.Pop();
                collection.Add(n);

                for (uint i = 0; i < n.Count; i++)
                {
                    stack.Push(n[i]);
                }
            }

            Pool.Return(stack);
        }

        public static bool TryFindFirst<T>(this INode node, [NotNullWhen(true)] out T? found) where T : class, INode
        {
            foreach (INode child in node.Children)
            {
                if (child is T t)
                {
                    found = t;
                    return true;
                }

                if (child.TryFindFirst(out T? foundInChild))
                {
                    found = foundInChild;
                    return true;
                }
            }

            found = null;
            return false;
        }

        public static bool IsDescendantOf(this INode node, INode parent)
        {
            INode? current = node.Parent;
            while (current is not null)
            {
                if (current == parent)
                {
                    return true;
                }

                current = current.Parent;
            }

            return false;
        }

        internal static class Pool
        {
            private static readonly Stack<Stack<INode>> nodeStacks = new();

            public static Stack<INode> GetNodeStack()
            {
                if (nodeStacks.Count > 0)
                {
                    return nodeStacks.Pop();
                }

                return new();
            }

            public static void Return(Stack<INode> stack)
            {
                stack.Clear();
                nodeStacks.Push(stack);
            }
        }
    }
}
