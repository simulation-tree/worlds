using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Game
{
    public static class INodeFunctions
    {
        /// <summary>
        /// Adds the given child node as a child.
        /// </summary>
        public static void AddNode(this INode node, INode child)
        {
            child.Parent = node;
        }

        public static void AddNodes(this INode node, IReadOnlyCollection<INode> children)
        {
            foreach (INode child in children)
            {
                node.AddNode(child);
            }
        }

        public static int IndexOfNode(this INode node, INode child)
        {
            int count = node.Children.Count;
            for (int i = 0; i < count; i++)
            {
                if (node.Children[i] == child)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int CountNodes(this INode node)
        {
            return node.Children.Count;
        }

        /// <summary>
        /// Checks if the given child is a direct child.
        /// </summary>
        public static bool ContainsNode(this INode node, INode child)
        {
            return node.IndexOfNode(child) != -1;
        }

        /// <summary>
        /// Attempts to remove the given node from the list of children.
        /// </summary>
        public static bool RemoveNode(this INode node, INode child)
        {
            if (child.Parent == node)
            {
                child.Parent = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Orphans all children of the given node.
        /// </summary>
        public static List<INode> ClearNodes(this INode node)
        {
            List<INode> children = new(node.Children);
            for (int i = 0; i < children.Count; i++)
            {
                children[i].Parent = null;
            }

            return children;
        }

        /// <summary>
        /// Broadcasts a message to all descendants of this node,
        /// and the node itself. In descending order of depth.
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
        /// Fills the given list with all descendants of this node, in descending
        /// order of depth.
        /// </summary>
        public static void FillDescendantNodes(this INode node, ICollection<INode> collection)
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

                int nChildrenCount = n.Children.Count;
                for (int i = 0; i < nChildrenCount; i++)
                {
                    stack.Push(n.Children[i]);
                }
            }

            Pool.Return(stack);
        }

        /// <summary>
        /// Tries to find the first node in the tree that is of the given type.
        /// </summary>
        public static bool TryFindFirstNode<T>(this INode node, [NotNullWhen(true)] out T? found) where T : class, INode
        {
            foreach (INode child in node.Children)
            {
                if (child is T t)
                {
                    found = t;
                    return true;
                }

                if (child.TryFindFirstNode(out T? foundInChild))
                {
                    found = foundInChild;
                    return true;
                }
            }

            found = null;
            return false;
        }

        public static bool IsDescendantNodeOf(this INode node, INode parent)
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

        public static INode GetRootNode(this INode node)
        {
            INode? current = node;
            while (current is not null)
            {
                INode? next = current.Parent;
                if (next is null)
                {
                    break;
                }

                current = next;
            }

            return current ?? throw new System.InvalidOperationException();
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
