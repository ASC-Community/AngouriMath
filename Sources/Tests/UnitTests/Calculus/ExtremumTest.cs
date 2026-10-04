//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// <c>max(f(t), t in S)</c>, <c>min</c>, <c>argmax</c> and <c>argmin</c>: the extremum of an
    /// expression over a set, as binders. https://github.com/asc-community/AngouriMath/issues/1212
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ExtremumTest
    {
        // The sheet's question I.6: the area of a projectile's flight is largest at pi/3.
        [Fact]
        public void TheLargestAreaIsAtSixtyDegrees()
        {
            Assert.Equal("3/16 * sqrt(3)".ToEntity(), "max(sin(t)^3 * cos(t), t in [0; pi/2])".ToEntity().Simplify());
            var where = "argmax(sin(t)^3 * cos(t), t in [0; pi/2])".ToEntity().Simplify();
            var point = Assert.Single(Assert.IsType<Set.FiniteSet>(where).Elements);
            // The point is a root the solver wrote as ln(...)/i, equal to pi/3; the difference
            // cancels to an exact zero where matching printed digits would not.
            Assert.Equal(Number.Integer.Zero, (point - "pi/3".ToEntity()).Simplify());
        }

        [Theory]
        [InlineData("max(x^2, x in { 1, -3, 2 })", "9")]
        [InlineData("min(x^2, x in { 1, -3, 2 })", "1")]
        [InlineData("max(x, x in { 1/2, 0.3, 1/3 })", "1/2")]
        [InlineData("max(sin(x), x in { 0, pi/2, pi })", "1")]
        [InlineData("max(x * (4 - x), x in [0; 4])", "4")]
        [InlineData("max(x, x in [0; 1])", "1")]
        [InlineData("min(x, x in [0; 1])", "0")]
        [InlineData("max(x^3 - 3x, x in [-2; 2])", "2")]
        [InlineData("min(x^3 - 3x, x in [-2; 2])", "-2")]
        [InlineData("max(x^2, x in [2; 2])", "4")]
        [InlineData("max(cos(x), x in [-1; 1])", "1")]
        [InlineData("min(x^4 - x^2, x in [-1; 1])", "-1/4")]
        [InlineData("max(2^x - x, x in [0; 2])", "2")]
        [InlineData("max(x * e^(-x), x in [0; 3])", "e^(-1)")]
        public void TheExtremumIsAnswered(string extremum, string expected)
            => Assert.Equal(expected.ToEntity().Simplify(), extremum.ToEntity().Simplify());

        [Theory]
        [InlineData("argmax(x^2, x in { 1, -3, 2 })", "{ -3 }")]
        [InlineData("argmin(x^2, x in { 1, -1 })", "{ 1, -1 }")]
        [InlineData("argmax(x * (4 - x), x in [0; 4])", "{ 2 }")]
        [InlineData("argmin(x^2, x in [-1; 2])", "{ 0 }")]
        [InlineData("argmax(x^3 - 3x, x in [-2; 2])", "{ -1, 2 }")]
        [InlineData("argmax(cos(x), x in [-1; 1])", "{ 0 }")]
        [InlineData("argmin(x^4 - x^2, x in [-1; 1])", "{ 1/2 * sqrt(2), -1/2 * sqrt(2) }")]
        public void ThePointsAreAnswered(string extremum, string expected)
            => Assert.Equal(expected.ToEntity(), extremum.ToEntity().Simplify());

        // A value at an open endpoint is not attained; a symbolic set or end, a pole, a kink,
        // and a set with a symbol in it are not settled.
        [Theory]
        [InlineData("max(x, x in [0; 1))")]
        [InlineData("min(x^2, x in (0; 1))")]
        [InlineData("max(x^2, x in (-1; 1))")]
        [InlineData("max(x, x in [0; a])")]
        [InlineData("max(x, x in S)")]
        [InlineData("max(x, x in RR)")]
        [InlineData("max(1/x, x in [-1; 1])")]
        [InlineData("max(abs(x), x in [-1; 1])")]
        [InlineData("max(sqrt(x), x in [0; 1])")]
        [InlineData("max(x, x in { 1, a })")]
        [InlineData("max(x, x in { x : x > 0 })")]
        [InlineData("argmax(x, x in [0; 1))")]
        public void WhatIsNotSettledIsLeftAsWritten(string extremum)
        {
            var answer = extremum.ToEntity().Simplify();
            Assert.True(answer is Maximumf or Minimumf or Argmaxf or Argminf, answer.ToString());
        }

        [Theory]
        [InlineData("max(sin(t), t in [0; pi])", "max(sin(t), t in [0; pi])")]
        [InlineData("argmin(x, x in S)", "argmin(x, x in S)")]
        [InlineData("min(f, t in S) + 1", "min(f, t in S) + 1")]
        public void ItParsesAndPrintsAsWritten(string written, string printed)
        {
            var entity = written.ToEntity();
            Assert.Equal(printed, entity.ToString());
            Assert.Equal(entity, printed.ToEntity());
        }

        // max(a, b) of two values is what it always was; a binder is only the shape whose second
        // argument says which variable ranges over which set.
        [Fact]
        public void TheTwoValueMaxIsUnchanged()
        {
            Assert.Equal("2".ToEntity(), "max(1, 2)".ToEntity().Evaled);
            Assert.IsType<Maxf>("max(x, y)".ToEntity());
            Assert.IsType<Maximumf>("max(x, y in S)".ToEntity());
            Assert.IsType<Maxf>("max(x, 1 in S)".ToEntity());
        }

        [Fact]
        public void TheVariableIsBoundByTheBinder()
        {
            var extremum = "max(t^2 + a, t in [0; 1])".ToEntity();
            Assert.Equal(new[] { (Variable)"a" }, extremum.FreeVariables.ToArray());
            Assert.Equal("2".ToEntity(), extremum.Substitute("a", 1).Simplify());
            Assert.Equal(extremum, extremum.Substitute("t", 5));
        }

        [Fact]
        public void ItPrintsAsASubscriptedOperatorInLatex()
        {
            Assert.Equal(@"\max_{t \in S} f", "max(f, t in S)".ToEntity().Latexize());
            Assert.Equal(@"\operatorname{argmax}_{t \in S} f", "argmax(f, t in S)".ToEntity().Latexize());
        }

        [Fact]
        public void TheConvenienceSpellingsAgreeWithTheParser()
        {
            Entity f = "f", t = "t", s = "S";
            Assert.Equal("max(f, t in S)".ToEntity(), MathS.Maximum(f, t, s));
            Assert.Equal("min(f, t in S)".ToEntity(), MathS.Minimum(f, t, s));
            Assert.Equal("argmax(f, t in S)".ToEntity(), MathS.Argmax(f, t, s));
            Assert.Equal("argmin(f, t in S)".ToEntity(), MathS.Argmin(f, t, s));
        }

        [Fact]
        public void ItRoundTripsThroughJson()
        {
            var extremum = "argmax(sin(t), t in [0; pi])".ToEntity();
            var json = System.Text.Json.JsonSerializer.Serialize(extremum);
            Assert.Equal(extremum, System.Text.Json.JsonSerializer.Deserialize<Entity>(json));
        }
    }
}
