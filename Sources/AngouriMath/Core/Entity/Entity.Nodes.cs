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
using System.Threading;

namespace AngouriMath
{
    partial record Entity
    {
        /// <summary>
        /// The pre-order walk behind <see cref="Nodes"/>: the node, then each child's walk in
        /// order, kept on an explicit stack rented from <see cref="ArrayPool{T}.Shared"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// It costs one object per enumeration and nothing per node, and keeps nothing on the
        /// tree. A chain of LINQ iterators costs an iterator per node per enumeration, and caching
        /// it on every node keeps that chain alive as long as the node; caching the walk's result
        /// instead is quadratic in memory, since every node would hold its whole subtree (31 MB on
        /// a sum of 2000 terms).
        /// <a href="https://github.com/asc-community/AngouriMath/issues/534">#534</a>
        /// </para>
        /// <para>
        /// Every enumeration has its own stack, so a predicate that walks another tree while this
        /// one is being walked, on this thread or another, shares nothing with it. A walk that is
        /// abandoned without being disposed only fails to give its array back to the pool.
        /// </para>
        /// </remarks>
        private sealed class NodeWalk : IEnumerable<Entity>, IEnumerator<Entity>
        {
            private const int NotStarted = 0, Walking = 1, Finished = 2;

            private readonly Entity root;
            private readonly int ownerThread;
            private bool handedOut;
            private int state;
            private Entity? current;
            private Entity[]? stack;
            private int count;

            internal NodeWalk(Entity root)
            {
                this.root = root;
                ownerThread = Environment.CurrentManagedThreadId;
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

            public Entity Current => current!;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                switch (state)
                {
                    case NotStarted:
                        current = root;
                        state = Walking;
                        return true;
                    case Walking:
                        // The children of the node just yielded are pushed only now, so a walk
                        // stopped at a node never asks for its children.
                        var children = current!.DirectChildren;
                        if (children.Count > 0)
                        {
                            Reserve(children.Count);
                            for (var i = children.Count - 1; i >= 0; i--)
                                stack![count++] = children[i];
                        }
                        if (count == 0)
                        {
                            Dispose();
                            return false;
                        }
                        current = stack![--count];
                        stack[count] = null!;
                        return true;
                    default:
                        return false;
                }
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

            public void Dispose()
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
                current = null;
                state = Finished;
            }

            public void Reset() => throw new NotSupportedException();
        }
    }
}
