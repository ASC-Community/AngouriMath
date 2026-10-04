//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using static AngouriMath.Entity.Number;
using Xunit;
using System.Collections.Generic;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    [Trait("Area", "Algebra")]
    public sealed class SolveEquationWithConstraints
    {
        /// <summary>Numerically checks if a root fits an equation</summary>
        internal static void AssertRoots(Entity equation, Variable toSub, Entity varValue, Integer? subValue = null)
        {
            subValue ??= 3;
            string? eqNormal = equation.Stringize();
            equation = equation.Substitute(toSub, varValue);
            // MUST be integer to correspond to integer coefficient of periodic roots
            var substitutions = new Dictionary<Entity.Variable, Integer>();
            foreach (var vr in equation.Vars)
                substitutions.Add(vr, subValue + substitutions.Count);
            equation = equation.Substitute(substitutions);
            var err = equation.EvalBoolean();
            Assert.True(err, $"\nError = {err.Stringize()}\n{eqNormal}\nWrong root: {toSub.Stringize()} = {varValue.Stringize()}");
        }

        [Theory]
        [InlineData("x + 2 = 0", 1)]
        [InlineData("x2 = 16", 2)]
        [InlineData("x2 = 16 and x = 4", 1)]
        [InlineData("x2 = 16 and x = -4", 1)]
        [InlineData("x2 = 16 and x = 3", 0)]
        [InlineData("x2 = 16 or x = 3", 3)]
        [InlineData("x4 = 16 and x > 0", 1)]
        [InlineData("x4 = 0 and x > 0", 0)]
        [InlineData("x4 = 0 and x >= 0", 1)]
        [InlineData("x = 1 or x = 2 or x = 3", 3)]
        public void TestFinite(string expr, int rootCount)
        {
            var eq = expr.ToEntity();
            Variable x = "x";
            var solutions = eq.Solve(x);
            solutions = (Set)solutions.InnerSimplified;
            var roots = Assert.IsType<FiniteSet>(solutions);
            Assert.Equal(rootCount, roots.Count);
            foreach (var root in roots)
                AssertRoots(eq, "x", root);
        }

        // x^2 = a and x = a holds only where a^2 = a: x = 0 at a = 0, x = 1 at a = 1, and for no
        // x elsewhere. https://github.com/asc-community/AngouriMath/issues/1680
        [Theory]
        [InlineData(0, "{ 0 }")]
        [InlineData(1, "{ 1 }")]
        [InlineData(4, "{ }")]
        [InlineData(-1, "{ }")]
        public void TestFiniteIntersection(int a, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, "x2 = a and x = a".Solve("x").Substitute("a", a).Evaled);

        [Theory]
        [InlineData(@"x \/ { 1, 2 } = { 1, 2 }", "{ { }, { 1 }, { 2 }, { 1, 2 } }")]
        [InlineData(@"x \/ { 1, 2 } = { 1, 2, 3 }", "{ { 3 }, { 3, 1 }, { 3, 2 }, { 1, 2, 3 } }")]
        [InlineData(@"x \/ { 1, 2 } = { 1, 3 }", "{ }")]
        [InlineData(@"{ 1, 2 } \/ x = { 1, 2 }", "{ { }, { 1 }, { 2 }, { 1, 2 } }")]
        [InlineData(@"{ 1, 2 } \/ x = { 1, 2, 3 }", "{ { 3 }, { 3, 1 }, { 3, 2 }, { 1, 2, 3 } }")]
        [InlineData(@"{ 1, 2 } \/ x = { 1, 3 }", "{ }")]
        public void TestSetSolver(string eq, string expected)
        {
            Assert.Equal(expected.ToEntity(), eq.Solve("x"));
        }
    }
}
