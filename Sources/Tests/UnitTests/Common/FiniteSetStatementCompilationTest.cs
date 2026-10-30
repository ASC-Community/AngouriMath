//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Numerics;
using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A quantifier, a subset and a cardinality over a finite set compile, in FastExpression and in the LINQ compiler, to
    /// what evaluation gives: a quantifier as the connective of its body at each member, a subset as the memberships of
    /// its members, and a cardinality as the number of distinct values among them. Each was refused, and over any other
    /// set still is.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2195">#2195</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class FiniteSetStatementCompilationTest
    {
        // Not at NaN: see AtNaNTheValueIsUndecidedOrTheVacuousTruth.
        private static readonly string[] points = { "-1", "0", "1/2", "1", "2", "5/2", "3", "4", "i", "1 + i", "+oo", "-oo" };

        private static Complex Numeric(string value) => ((Entity.Number.Complex)value.ToEntity().Evaled).ToNumerics();

        private static Entity At(string expression, string point) => expression.ToEntity().Substitute("x", point);

        private static bool Same(Complex expected, Complex actual)
            => double.IsNaN(expected.Real) || double.IsNaN(expected.Imaginary)
                ? double.IsNaN(actual.Real) || double.IsNaN(actual.Imaginary)
                : expected == actual;

        [Theory]
        [InlineData("forall t in { 1, 2, 3 } : t < x")]
        [InlineData("exists t in { 1, 2, 3 } : t = x")]
        [InlineData("exists! t in { 1, 2, 3 } : t > x")]
        [InlineData("forall t in { 1, 2 } : exists s in { 2, 3 } : s > t + x")]
        [InlineData("forall t in { } : t = x")]
        [InlineData("exists t in { } : t = x")]
        [InlineData("{ x } subset [0; 3]")]
        [InlineData("{ x, 2 } subset ZZ")]
        [InlineData("{ } subset { x }")]
        public void AStatementOverAFiniteSetIsWhatEvaluationSays(string statement)
        {
            var fast = statement.ToEntity().Compile("x");
            var linq = statement.ToEntity().Compile<Complex, bool?>("x");
            var compared = 0;
            foreach (var a in points)
            {
                var evaluated = At(statement, a).Evaled;
                if (evaluated is not Entity.Boolean && !evaluated.IsNaN)
                    continue;
                bool? expected = evaluated is Entity.Boolean truth ? (bool)truth : null;
                var x = Numeric(a);
                var value = fast.Call(x);
                bool? fastTruth = value == 1 ? true : value == 0 ? false : null;
                Assert.True(expected == fastTruth, $"{statement} at {a}: {expected?.ToString() ?? "none"} evaluated, {value} by FastExpression");
                Assert.True(expected == linq(x), $"{statement} at {a}: {expected?.ToString() ?? "none"} evaluated, {linq(x)?.ToString() ?? "none"} by LINQ");
                compared++;
            }
            Assert.True(compared >= points.Length / 2, $"only {compared} of {statement} decided");
        }

        [Theory]
        [InlineData("card({ 1, x })")]
        [InlineData("card({ 1, 2, x, x^2 })")]
        [InlineData("card({ })")]
        public void ACardinalityIsWhatEvaluationSays(string expression)
        {
            var fast = expression.ToEntity().Compile("x");
            var linq = expression.ToEntity().Compile<Complex, Complex>("x");
            var compared = 0;
            foreach (var a in points)
            {
                if (At(expression, a).Evaled is not Entity.Number.Complex evaluated)
                    continue;
                var expected = evaluated.ToNumerics();
                var x = Numeric(a);
                Assert.True(Same(expected, fast.Call(x)), $"{expression} at {a}: {expected} evaluated, {fast.Call(x)} by FastExpression");
                Assert.True(Same(expected, linq(x)), $"{expression} at {a}: {expected} evaluated, {linq(x)} by LINQ");
                compared++;
            }
            Assert.True(compared >= points.Length / 2, $"only {compared} of {expression} evaluated");
        }

        /// <summary>
        /// At NaN the compiled value is undecided, or the vacuous truth of a quantifier over the empty set, where
        /// evaluation differs: a finite set drops a member whose value is NaN, so <c>{ NaN }</c> is <c>{ }</c> and
        /// <c>card({ 1, NaN })</c> is 1, and a NaN anywhere in a quantifier's body makes the quantifier NaN.
        /// </summary>
        [Theory]
        [InlineData("{ x } subset [0; 3]", double.NaN)]
        [InlineData("card({ 1, x })", double.NaN)]
        [InlineData("forall t in { } : t = x", 1)]
        [InlineData("exists t in { } : t = x", 0)]
        public void AtNaNTheValueIsUndecidedOrTheVacuousTruth(string expression, double expected)
        {
            var value = expression.ToEntity().Compile("x").Call(double.NaN);
            Assert.True(double.IsNaN(expected) ? double.IsNaN(value.Real) : value == expected, $"{expression} at NaN: {value}");
        }

        [Theory]
        [InlineData("forall t in [0; 1] : t < x")]
        [InlineData("exists t in ZZ : t = x")]
        [InlineData("{ x } subset QQ")]
        [InlineData("card([0; x])")]
        public void OverAnyOtherSetItIsRefused(string expression)
            => Assert.Throws<UncompilableNodeException>(() => expression.ToEntity().Compile("x"));
    }
}
