//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace AngouriMath
{
    partial record Entity
    {
        /// <summary>Whether any node, this one included, satisfies <paramref name="predicate"/>,
        /// asked in the order of <see cref="Nodes"/> and stopping at the first; what
        /// <c>Nodes.Any(predicate)</c> answers, without allocating.</summary>
        internal bool AnyNode(Func<Entity, bool> predicate)
        {
            var walk = new NodeStack(this);
            try
            {
                while (walk.MoveNext())
                    if (predicate(walk.Current))
                        return true;
                return false;
            }
            finally { walk.Release(); }
        }

        /// <summary>What <c>Nodes.All(predicate)</c> answers, without allocating.</summary>
        internal bool AllNodes(Func<Entity, bool> predicate)
        {
            var walk = new NodeStack(this);
            try
            {
                while (walk.MoveNext())
                    if (!predicate(walk.Current))
                        return false;
                return true;
            }
            finally { walk.Release(); }
        }

        /// <summary>What <c>Nodes.Contains(node)</c> answers, without allocating.</summary>
        private bool WalkFinds(Entity node)
        {
            var walk = new NodeStack(this);
            try
            {
                while (walk.MoveNext())
                    if (walk.Current == node)
                        return true;
                return false;
            }
            finally { walk.Release(); }
        }

        /// <summary>How many nodes, this one included, are equal to <paramref name="node"/>.</summary>
        internal int CountNode(Entity node)
        {
            var walk = new NodeStack(this);
            try
            {
                var count = 0;
                while (walk.MoveNext())
                    if (walk.Current == node)
                        count++;
                return count;
            }
            finally { walk.Release(); }
        }

        /// <summary>
        /// The pre-order walk behind <see cref="Nodes"/>: the node, then each child's walk in
        /// order, kept on an explicit stack rented from <see cref="ArrayPool{T}.Shared"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// It allocates nothing, per node or per walk, and keeps nothing on the tree. A chain of
        /// LINQ iterators costs an iterator per node per enumeration, and caching it on every
        /// node keeps that chain alive as long as the node; caching the walk's result instead is
        /// quadratic in memory, since every node would hold its whole subtree (31 MB on a sum of
        /// 2000 terms).
        /// <a href="https://github.com/asc-community/AngouriMath/issues/534">#534</a>
        /// </para>
        /// <para>
        /// Every walk has its own stack, so a predicate that walks another tree while this one
        /// is being walked, on this thread or another, shares nothing with it. A walk that is
        /// abandoned without <see cref="Release"/> only fails to give its array back to the
        /// pool. It is a mutable struct: copying one mid-walk would share the array, so it is
        /// only ever held in a local or in <see cref="NodeWalk"/>.
        /// </para>
        /// </remarks>
        private struct NodeStack
        {
            private Entity? next;
            private Entity? current;
            private Entity[]? stack;
            private int count;

            internal NodeStack(Entity root)
            {
                next = root;
                current = null;
                stack = null;
                count = 0;
            }

            internal readonly Entity Current => current!;

            internal bool MoveNext()
            {
                if (next is not null)
                {
                    current = next;
                    next = null;
                    return true;
                }
                if (current is null)
                    return false;
                // The children of the node just yielded are pushed only now, so a walk stopped
                // at a node never asks for its children.
                var children = current.DirectChildren;
                if (children.Count > 0)
                {
                    Reserve(children.Count);
                    for (var i = children.Count - 1; i >= 0; i--)
                        stack![count++] = children[i];
                }
                if (count == 0)
                {
                    Release();
                    return false;
                }
                current = stack![--count];
                stack[count] = null!;
                return true;
            }

            private void Reserve(int more)
            {
                if (stack is null)
                    stack = ArrayPool<Entity>.Shared.Rent(Math.Max(16, more));
                else if (count + more > stack.Length)
                {
                    var larger = ArrayPool<Entity>.Shared.Rent(Math.Max(2 * stack.Length, count + more));
                    Array.Copy(stack, larger, count);
                    Array.Clear(stack, 0, count);
                    ArrayPool<Entity>.Shared.Return(stack);
                    stack = larger;
                }
            }

            internal void Release()
            {
                if (stack is not null)
                {
                    // The pool hands the array to the next renter, which must not keep this
                    // tree alive through it.
                    Array.Clear(stack, 0, count);
                    ArrayPool<Entity>.Shared.Return(stack);
                    stack = null;
                }
                count = 0;
                next = null;
                current = null;
            }
        }

        /// <summary>
        /// <see cref="NodeStack"/> as the <see cref="IEnumerable{T}"/> that <see cref="Nodes"/>
        /// publishes, at one object per enumeration.
        /// </summary>
        private sealed class NodeWalk : IEnumerable<Entity>, IEnumerator<Entity>
        {
            private readonly Entity root;
            private readonly int ownerThread;
            private bool handedOut;
            private NodeStack walk;

            internal NodeWalk(Entity root)
            {
                this.root = root;
                ownerThread = Environment.CurrentManagedThreadId;
                walk = new NodeStack(root);
            }

            // The first enumeration on the thread that asked for Nodes is this object itself, as
            // LINQ's own iterators do, so the common Nodes.Any(...) costs one object, not two.
            public IEnumerator<Entity> GetEnumerator()
            {
                if (!handedOut && ownerThread == Environment.CurrentManagedThreadId)
                {
                    handedOut = true;
                    return this;
                }
                return new NodeWalk(root) { handedOut = true };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public Entity Current => walk.Current;

            object IEnumerator.Current => Current;

            public bool MoveNext() => walk.MoveNext();

            public void Dispose() => walk.Release();

            public void Reset() => throw new NotSupportedException();
        }
    }
}
