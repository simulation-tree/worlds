using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Game
{
    public interface INode : IDisposable
    {
        IReadOnlyList<INode> Children { get; }

        public uint Count => (uint)Children.Count;
        public INode this[uint index] => Children[(int)index];

        void Receive<T>(T message) where T : unmanaged;
    }

    public static class INodeFunctions
    {
        public static uint GetCount(this INode node)
        {
            return node.Count;
        }

        public static void Broadcast<T>(this INode node, T message) where T : unmanaged
        {
            foreach (INode child in node.Children)
            {
                child.Broadcast(message);
            }

            node.Receive(message);
        }

        public static IEnumerable<INode> GetDescendants(this INode node)
        {
            Stack<INode> stack = new();
            foreach (INode child in node.Children)
            {
                stack.Push(child);
            }

            while (stack.Count > 0)
            {
                INode current = stack.Pop();
                yield return current;
                foreach (INode child in current.Children)
                {
                    stack.Push(child);
                }
            }

            stack.Clear();
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
    }

    public abstract class Node : INode
    {
        private readonly List<INode> children = [];

        public IReadOnlyList<INode> Children => children;

        public abstract void Dispose();

        /// <summary>
        /// Puts the given node into this node.
        /// </summary>
        public void Add(Node node)
        {
            children.Add(node);
        }

        void INode.Receive<T>(T message)
        {
            Received(message);
        }

        protected virtual void Received<T>(T message) where T : unmanaged
        {
        }
    }
}
