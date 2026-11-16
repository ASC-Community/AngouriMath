//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// <see cref="Entity.Nodes"/> and the internal walks over it visit every node in pre-order --
    /// the node, then each child's nodes in order -- whether the walk is repeated, abandoned,
    /// nested inside another or run on several threads at once, and whatever the depth.
    /// https://github.com/asc-community/AngouriMath/issues/534
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class NodesWalkTest
    {
        private static List<Entity> PreOrder(Entity e)
        {
            var list = new List<Entity> { e };
            foreach (var child in e.DirectChildren)
                list.AddRange(PreOrder(child));
            return list;
        }

        public static TheoryData<string> Expressions => new()
        {
            "x",
            "a + b / 2 ^ 3",
            "sin(x)^2 + cos(x)^2 + x/x + (x+1)*(x-1)",
            "log(sqrt(x), a + b) + integral(sin(y) * x, y) + { 1, 2, x }",
            "[[x, 1], [y + 1, sin(z)]] * [[1], [2]]",
            "{ x in RR : x > 0 } \\/ [1; 2]",
        };

        [Theory, MemberData(nameof(Expressions))]
        public void NodesArePreOrder(string expression)
        {
            Entity expr = expression;
            var expected = PreOrder(expr);
            Assert.Equal(expected, expr.Nodes.ToList(), ReferenceEqualityComparer.Instance);
            var nodes = expr.Nodes;
            Assert.Equal(expected, nodes.ToList(), ReferenceEqualityComparer.Instance);
            Assert.Equal(expected, nodes.ToList(), ReferenceEqualityComparer.Instance);
        }

        [Theory, MemberData(nameof(Expressions))]
        public void InternalWalksAgreeWithNodes(string expression)
        {
            Entity expr = expression;
            foreach (var node in expr.Nodes)
            {
                Assert.Equal(expr.Nodes.Count(n => n == node), expr.CountNode(node));
                // A variable is answered from VarsAndConsts, which leaves out the placeholder a
                // set builder publishes for its bound name, so only the walk is asked here.
                if (node is not Entity.Variable)
                    Assert.True(expr.ContainsNode(node));
                Assert.True(expr.AnyNode(n => ReferenceEquals(n, node)));
                Assert.False(expr.AllNodes(n => !ReferenceEquals(n, node)));
            }
            Assert.False(expr.ContainsNode("w + 17"));
            Assert.True(expr.AllNodes(_ => true));
            Assert.False(expr.AnyNode(_ => false));
        }

        [Fact]
        public void AnyStopsAtTheFirstMatchInOrder()
        {
            Entity expr = "(a + b) * (c + d)";
            var asked = new List<Entity>();
            Assert.True(expr.AnyNode(n => { asked.Add(n); return n == "b"; }));
            Assert.Equal(PreOrder(expr).Take(asked.Count), asked);
            Assert.Equal(MathS.Var("b"), asked[^1]);
        }

        [Fact]
        public void AnAbandonedWalkLeavesTheNextOneWhole()
        {
            Entity expr = "sin(x)^2 + cos(x)^2 + x/x + (x+1)*(x-1)";
            using (var e = expr.Nodes.GetEnumerator())
                for (var i = 0; i < 5; i++)
                    Assert.True(e.MoveNext());
            var abandoned = expr.Nodes.GetEnumerator();
            abandoned.MoveNext();
            abandoned.MoveNext();
            Assert.Equal(PreOrder(expr), expr.Nodes.ToList(), ReferenceEqualityComparer.Instance);
            Assert.True(abandoned.MoveNext());
        }

        [Fact]
        public void AWalkInsideAWalk()
        {
            Entity expr = "sin(x)^2 + cos(x)^2 + x/x + (x+1)*(x-1)";
            var expected = PreOrder(expr).SelectMany(n => PreOrder(n)).ToList();
            var actual = new List<Entity>();
            foreach (var node in expr.Nodes)
                foreach (var inner in node.Nodes)
                    actual.Add(inner);
            Assert.Equal(expected, actual, ReferenceEqualityComparer.Instance);
            Assert.True(expr.AllNodes(n => n.AllNodes(m => expr.ContainsNode(m))));
        }

        [Fact]
        public void ADeepTreeOnSeveralThreads()
        {
            Entity sum = "t_0";
            for (var i = 1; i < 2000; i++)
                sum += MathS.Var("t_" + i);
            var expected = PreOrder(sum);
            Parallel.For(0, 8, _ =>
            {
                for (var k = 0; k < 20; k++)
                    Assert.Equal(expected, sum.Nodes.ToList(), ReferenceEqualityComparer.Instance);
            });
            Assert.Equal(1, sum.CountNode(MathS.Var("t_1999")));
        }
    }
}
