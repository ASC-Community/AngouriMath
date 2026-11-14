//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using AngouriMath.Extensions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The squeeze theorem in the one shape that does not need the bounded factor's own limit.
    /// Every rule above this one reads a limit as a value -- the descent substitutes each part's
    /// limit, l'Hopital's rule wants a determinate quotient, Gruntz compares rates of growth --
    /// so a factor with no limit at all, such as sin(x) at infinity, made each of them decline
    /// and left the product indeterminate in the shape (no limit) * 0.
    /// https://github.com/asc-community/AngouriMath/issues/723
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class BoundedTimesVanishingTest
    {
        private static void AssertLimit(string expression, string destination, string expected)
        {
            var task = Task.Run(() =>
                expression.ToEntity().Limit("x", destination.ToEntity()).Simplify());
            Assert.True(task.Wait(LimitTermination.Guard), "the limit did not terminate");
            Assert.Equal(expected.ToEntity().Evaled, task.Result.Evaled);
        }

        /// <summary>
        /// A bounded dividend over a diverging divisor. The standard example of the theorem is
        /// the first of these.
        /// </summary>
        [Theory]
        [InlineData("sin(x) / x", "+oo")]
        [InlineData("cos(x) / x", "+oo")]
        [InlineData("sin(x) / x ^ 2", "+oo")]
        [InlineData("sin(x) / x", "-oo")]
        [InlineData("sin(x ^ 2) / x", "+oo")]
        [InlineData("(2 + sin(x)) / x", "+oo")]
        [InlineData("(sin(x) + cos(x)) / x", "+oo")]
        [InlineData("sin(x) * cos(x) / x", "+oo")]
        public void BoundedOverDiverging(string expression, string destination) =>
            AssertLimit(expression, destination, "0");

        /// <summary>
        /// The theorem behind a term of a sum: <c>(x + sin(x))/x</c> is <c>1 + sin(x)/x</c>,
        /// and the second term is the standard example. It used to be declined, since the
        /// division left <c>1 + sin(x)/x provided not x = 0</c> and the condition stood between
        /// the descent and the sum; the quotient states that condition on its own now, and the
        /// simplified form carries none. https://github.com/asc-community/AngouriMath/issues/1394
        /// </summary>
        [Theory]
        [InlineData("(x + sin(x)) / x", "+oo", "1")]
        [InlineData("(2 x + cos(x)) / x", "+oo", "2")]
        public void ABoundedTermBesideADivergingOne(string expression, string destination, string expected) =>
            AssertLimit(expression, destination, expected);

        /// <summary>The same theorem written as a product rather than as a quotient.</summary>
        [Theory]
        [InlineData("sin(x) * e ^ (-x)", "+oo")]
        [InlineData("x * sin(1 / x)", "0")]
        [InlineData("x ^ 2 * sin(1 / x)", "0")]
        [InlineData("x * cos(1 / x)", "0")]
        [InlineData("signum(x) * x", "0")]
        public void BoundedTimesVanishing(string expression, string destination) =>
            AssertLimit(expression, destination, "0");

        /// <summary>
        /// A part bounded away from 0 settles a vanishing or diverging one beside it, and a bounded
        /// term a diverging one, though the bounded part has no limit of its own. The first three
        /// were NaN, which claims there is no limit: e^(cos(x)) has none, and the descent read a
        /// quotient over it as having none either. Each value checked at sample points: e^x / e^(cos(x))
        /// is 7.3e-23 at x = -50, x * e^(sin(1/x)) is 7.0e-7 at 1e-6 and -1.4e-6 at -1e-6.
        /// </summary>
        [Theory]
        [InlineData("exp(x) / exp(cos(x))", "-oo", "0")]
        [InlineData("exp(x) * exp(-cos(x))", "-oo", "0")]
        [InlineData("x * exp(sin(1/x))", "0", "0")]
        [InlineData("1 / (x^2 * (3 + cos(1/x)))", "0", "+oo")]
        [InlineData("exp(x) / exp(sin(x))", "-oo", "0")]
        [InlineData("exp(x) / (2 + cos(x))", "-oo", "0")]
        [InlineData("exp(x - cos(x))", "-oo", "0")]
        [InlineData("exp(cos(x)) / x", "-oo", "0")]
        [InlineData("x / exp(cos(x))", "-oo", "-oo")]
        [InlineData("x * exp(cos(x))", "+oo", "+oo")]
        [InlineData("-x * (2 + cos(x))", "+oo", "-oo")]
        [InlineData("x^2 / (2 + sin(x))", "+oo", "+oo")]
        [InlineData("ln(x) / (3 + sin(x))", "+oo", "+oo")]
        [InlineData("x + sin(x)", "+oo", "+oo")]
        [InlineData("x - sin(x)", "-oo", "-oo")]
        [InlineData("exp(x + sin(x))", "+oo", "+oo")]
        [InlineData("x^2 / (2 + cos(1/x))", "0", "0")]
        public void APartBoundedAwayFromZero(string expression, string destination, string expected) =>
            AssertLimit(expression, destination, expected);

        /// <summary>From one side, the sign of the diverging part is the side's.</summary>
        [Theory]
        [InlineData("exp(sin(1/x)) / x", ApproachFrom.Right, "+oo")]
        [InlineData("exp(sin(1/x)) / x", ApproachFrom.Left, "-oo")]
        [InlineData("exp(1/x) / (2 + sin(1/x))", ApproachFrom.Right, "+oo")]
        [InlineData("exp(1/x) / (2 + sin(1/x))", ApproachFrom.Left, "0")]
        public void APartBoundedAwayFromZeroFromOneSide(string expression, ApproachFrom side, string expected)
        {
            var task = Task.Run(() => expression.ToEntity().Limit("x", 0, side).Simplify());
            Assert.True(task.Wait(LimitTermination.Guard), "the limit did not terminate");
            Assert.Equal(expected.ToEntity().Evaled, task.Result.Evaled);
        }

        /// <summary>
        /// Where the bound reaches 0, or the part has no bound, nothing is claimed: x / (1 + cos(x))
        /// has poles all the way in, and x (sin(x) + 1/2) changes sign without settling. A bounded
        /// part with no limit alone, and a part diverging from each side with opposite signs, still
        /// have no limit.
        /// </summary>
        [Theory]
        [InlineData("x / (1 + cos(x))", "+oo")]
        [InlineData("exp(-x) / (1 + cos(x))", "+oo")]
        [InlineData("x * (sin(x) + 1/2)", "+oo")]
        [InlineData("x * sin(x)", "+oo")]
        [InlineData("exp(x) * sin(x)", "+oo")]
        [InlineData("1 / (x * sin(x))", "+oo")]
        [InlineData("x * exp(a * cos(x))", "+oo")]
        public void NotClaimedWhereTheBoundReachesZero(string expression, string destination)
        {
            var task = Task.Run(() => expression.ToEntity().Limit("x", destination.ToEntity()));
            Assert.True(task.Wait(LimitTermination.Guard), "the limit did not terminate");
            Assert.IsType<Entity.Limitf>(task.Result);
        }

        /// <summary>
        /// An infinity written into the expression is not a part diverging. ln(0) is -oo, so the
        /// tangent's exponentials carry e^(+oo / ln(x)), which the descent answers +oo; read as a
        /// diverging term beside 1 in numerator and denominator alike, the first came to 0 and the
        /// second to NaN, where limitcheck's sampled values settle at 1.
        /// </summary>
        [Theory]
        [InlineData("tanh(log(x + 1, (-1) + 1))", "-1", ApproachFrom.Right, "1")]
        [InlineData("tanh(log(abs(x), floor(1/2)))", "0", ApproachFrom.BothSides, "1")]
        public void AnInfinityWrittenInIsNotADivergingPart(string expression, string destination, ApproachFrom side, string expected)
        {
            var task = Task.Run(() => expression.ToEntity().Limit("x", destination.ToEntity(), side));
            Assert.True(task.Wait(LimitTermination.Guard), "the limit did not terminate");
            Assert.Equal(expected.ToEntity().Evaled, task.Result.Evaled);
        }

        /// <summary>
        /// A diverging part must be real on the side it is approached from. x^cos(2) is complex for
        /// x &lt; 0, cos(2) being about -0.416, so the left side of tanh(x^cos(2)) at 0 is left
        /// undecided and the two-sided limit is NaN, under the convention x^x at 0 is held to; read
        /// as a diverging term beside 1, both had come to 1.
        /// </summary>
        [Fact]
        public void ADivergingPartNotRealOnASideIsNotRead()
        {
            var expression = "tanh(x^cos(2))".ToEntity();
            var twoSided = Task.Run(() => expression.Limit("x", 0, ApproachFrom.BothSides));
            var left = Task.Run(() => expression.Limit("x", 0, ApproachFrom.Left));
            var right = Task.Run(() => expression.Limit("x", 0, ApproachFrom.Right));
            Assert.True(Task.WaitAll(new Task[] { twoSided, left, right }, LimitTermination.Guard), "the limit did not terminate");
            Assert.Equal(MathS.NaN, twoSided.Result.Evaled);
            Assert.IsType<Entity.Limitf>(left.Result);
            Assert.Equal("1".ToEntity().Evaled, right.Result.Evaled);
        }

        /// <summary>Bounded, and away from 0, is still not a limit.</summary>
        [Theory]
        [InlineData("exp(cos(x))", "+oo")]
        [InlineData("exp(sin(1/x)) / x", "0")]
        public void ABoundedPartAloneStillHasNoLimit(string expression, string destination)
        {
            var task = Task.Run(() => expression.ToEntity().Limit("x", destination.ToEntity(), ApproachFrom.BothSides));
            Assert.True(task.Wait(LimitTermination.Guard), "the limit did not terminate");
            Assert.Equal(MathS.NaN, task.Result.Evaled);
        }

        /// <summary>
        /// The answers that were already given by putting the parts' own limits in place of the
        /// parts, which this must not disturb: an arctangent does have a limit at infinity, and
        /// the quotient is settled without any appeal to boundedness.
        /// </summary>
        [Theory]
        [InlineData("arctan(x) / x", "+oo")]
        [InlineData("arccotan(x) / x", "+oo")]
        [InlineData("signum(sin(x)) / x", "+oo")]
        public void TheLimitsThatWereAlreadyGiven(string expression, string destination) =>
            AssertLimit(expression, destination, "0");

        /// <summary>
        /// An oscillation whose argument grows without bound has no limit at all, which is a
        /// different claim from the one above and a stronger one than leaving the node
        /// unevaluated. It is asked after the squeeze theorem and not before it: the theorem is
        /// precisely the case where a factor with no limit of its own still leaves the product
        /// with one, and sin(x) / x would otherwise be settled as (no limit) / (+oo).
        /// </summary>
        [Theory]
        [InlineData("sin(x)", "+oo")]
        [InlineData("cos(x)", "+oo")]
        [InlineData("sin(x)", "-oo")]
        [InlineData("sin(x ^ 2)", "+oo")]
        [InlineData("sin(1 / x)", "0")]
        [InlineData("cos(1 / x)", "0")]
        public void AnOscillationHasNoLimit(string expression, string destination)
        {
            var task = Task.Run(() => expression.ToEntity()
                .Limit("x", destination.ToEntity(), ApproachFrom.BothSides));
            Assert.True(task.Wait(LimitTermination.Guard), "the limit did not terminate");
            Assert.Equal(MathS.NaN, task.Result.Evaled);
        }

        /// <summary>An argument that settles leaves the function settled with it.</summary>
        [Theory]
        [InlineData("cos(1 / x)", "+oo", "1")]
        [InlineData("sin(1 / x)", "+oo", "0")]
        [InlineData("sin(x) / x", "0", "1")]
        public void AnArgumentThatSettlesIsNotAnOscillation(string expression, string destination, string expected) =>
            AssertLimit(expression, destination, expected);

        /// <summary>
        /// Boundedness of a sine is a fact about a *real* argument. This library reads a free
        /// variable as complex, and sin(i * x) is sinh(x) up to a factor, which grows without
        /// bound -- so lim x-&gt;+oo sin(a * x) / x is not 0 for every a, and is left unanswered.
        /// It is the example in <see cref="Entity.Limit(Entity.Variable, Entity)"/>'s own
        /// documentation, which prints it unevaluated.
        /// </summary>
        [Theory]
        [InlineData("sin(x * a) / x", "+oo")]
        [InlineData("cos(a * x) / x", "+oo")]
        [InlineData("sin(x * a)", "+oo")]
        [InlineData("cos(a * x)", "+oo")]
        public void AComplexArgumentIsNotBounded(string expression, string destination)
        {
            var task = Task.Run(() => expression.ToEntity().Limit("x", destination.ToEntity()));
            Assert.True(task.Wait(LimitTermination.Guard), "the limit did not terminate");
            Assert.IsType<Entity.Limitf>(task.Result);
        }
    }
}
