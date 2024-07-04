using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Simulation
{
    public static class INodeFunctions
    {
        /// <summary>
        /// Adds the given node as a child.
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

        /// <summary>
        /// Returns a possibly <c>-1</c> that corresponds to the given node
        /// as a child.
        /// </summary>
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
        /// <returns><c>true</c> if the node was removed unless it wasn't a child.</returns>
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
        /// Iterates through all descendants of the node and invokes the given callback.
        /// In the order of furthest descendant to the closest.
        /// </summary>
        public static void ForEach(this INode node, Action<INode> callback)
        {
            foreach (INode child in node.Children)
            {
                callback(child);
                ForEach(child, callback);
            }
        }

        /// <summary>
        /// Iterates through all descendants of type <typeparamref name="T"/>.
        /// In order of furthest descendant to the closest.
        /// </summary>
        public static void ForEach<T>(this INode node, Action<T> callback) where T : INode
        {
            foreach (INode child in node.Children)
            {
                if (child is T t)
                {
                    callback(t);
                }

                ForEach(child, callback);
            }
        }

        /// <summary>
        /// Retrieves a collection of all descendants of type <typeparamref name="T"/>
        /// </summary>
        /// <returns>An <see cref="INode"/> that can be safely cast to <typeparamref name="T"/></returns>
        public static IEnumerable<INode> GetAll<T>(this INode node) where T : INode
        {
            Stack<INode> stack = Pool.GetNodeStack();
            int childCount = node.Children.Count;
            for (int i = childCount - 1; i >= 0; i--)
            {
                stack.Push(node.Children[i]);
            }

            while (stack.Count > 0)
            {
                INode n = stack.Pop();
                if (n is T)
                {
                    yield return n;
                }

                childCount = n.Children.Count;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    stack.Push(n.Children[i]);
                }
            }

            Pool.Return(stack);
        }

        /// <summary>
        /// Fills the given list with all descendants of this node, in order of
        /// furthest descendant to the closest.
        /// </summary>
        public static void FillDescendantNodes(this INode node, ICollection<INode> collection)
        {
            Stack<INode> stack = Pool.GetNodeStack();
            int childCount = node.Children.Count;
            for (int i = childCount - 1; i >= 0; i--)
            {
                stack.Push(node.Children[i]);
            }

            while (stack.Count > 0)
            {
                INode n = stack.Pop();
                collection.Add(n);

                childCount = n.Children.Count;
                for (int i = childCount - 1; i >= 0; i--)
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
                if (child.TryFindFirstNode(out T? foundInChild))
                {
                    found = foundInChild;
                    return true;
                }

                if (child is T t)
                {
                    found = t;
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

            return current ?? throw new NullReferenceException($"Node {node} has no root node.");
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
