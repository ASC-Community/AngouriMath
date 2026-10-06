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

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// <c>floor</c> and <c>ceil</c> — <a href="https://github.com/asc-community/AngouriMath/issues/809">#809</a>.
    /// </summary>
    /// <remarks>
    /// The expected values are SymPy 1.14's, measured rather than reasoned about, as
    /// AGENTS.md asks when a convention has to be chosen. The ones that matter are the
    /// negative arguments — rounding toward the infinities rather than toward zero — and the
    /// complex case, which both SymPy and Mathematica take componentwise.
    /// </remarks>
    [Trait("Area", "Convenience")]
    public sealed class FloorCeilTest
    {
        [Theory]
        [InlineData("floor(3/2)", 1)]
        [InlineData("floor(-3/2)", -2)]
        [InlineData("floor(-1/2)", -1)]
        [InlineData("floor(5/2)", 2)]
        [InlineData("floor(-5/2)", -3)]
        [InlineData("floor(2)", 2)]
        [InlineData("floor(-2)", -2)]
        [InlineData("ceil(3/2)", 2)]
        [InlineData("ceil(-3/2)", -1)]
        [InlineData("ceil(-1/2)", 0)]
        [InlineData("ceil(5/2)", 3)]
        [InlineData("ceil(-5/2)", -2)]
        [InlineData("ceil(2)", 2)]
        [InlineData("ceil(-2)", -2)]
        public void TheValueIsWhatSymPyGives(string input, int expected)
            => Assert.Equal(Entity.Number.Integer.Create(expected), input.ToEntity().Simplify());

        // Componentwise, so that floor of a real keeps its meaning when the imaginary part
        // happens to be zero.
        [Theory]
        [InlineData("floor(3/2 + 5/2 * i)", "1 + 2i")]
        [InlineData("ceil(3/2 + 5/2 * i)", "2 + 3i")]
        [InlineData("floor(-3/2 - 5/2 * i)", "-2 - 3i")]
        [InlineData("ceil(-3/2 - 5/2 * i)", "-1 - 2i")]
        public void AComplexArgumentIsTakenComponentwise(string input, string expected)
            => Assert.Equal(expected.ToEntity().EvalNumerical(), input.ToEntity().EvalNumerical());

        // Both produce an integer, and both leave one alone.
        [Theory]
        [InlineData("floor(floor(x))", "floor(x)")]
        [InlineData("ceil(ceil(x))", "ceil(x)")]
        [InlineData("floor(ceil(x))", "ceil(x)")]
        [InlineData("ceil(floor(x))", "floor(x)")]
        public void ApplyingItTwiceChangesNothing(string input, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Simplify());

        [Theory]
        [InlineData("floor(x)")]
        [InlineData("ceil(x)")]
        [InlineData("floor(x + y)")]
        [InlineData("ceil(sin(x))")]
        public void ASymbolicArgumentIsLeftAlone(string input)
            => Assert.Equal(input.ToEntity(), input.ToEntity().Simplify());

        // Flat between the integers and discontinuous at each of them, so the derivative is
        // zero where it exists and the condition says where that is. SymPy declines to
        // answer at all here; this library can say more, because it has Providedf.
        [Theory]
        [InlineData("floor(x)")]
        [InlineData("ceil(x)")]
        public void TheDerivativeIsZeroAwayFromTheIntegers(string input)
        {
            var derivative = input.ToEntity().Differentiate("x");
            var provided = Assert.IsType<Entity.Providedf>(derivative);
            Assert.Equal(Entity.Number.Integer.Create(0), provided.Expression);
        }

        [Theory]
        [InlineData("floor(x)", @"\left\lfloor{x}\right\rfloor")]
        [InlineData("ceil(x)", @"\left\lceil{x}\right\rceil")]
        public void TheLatexIsTheUsualBrackets(string input, string expected)
            => Assert.Equal(expected, input.ToEntity().Latexize());

        // Stringize prints the short spelling, and SymPy's `ceiling` is accepted on the way
        // in so that an expression copied from there parses.
        [Theory]
        [InlineData("floor(x)", "floor(x)")]
        [InlineData("ceil(x)", "ceil(x)")]
        [InlineData("ceiling(x)", "ceil(x)")]
        public void ItPrintsTheShortSpellingAndParsesBoth(string input, string expected)
            => Assert.Equal(expected, input.ToEntity().Stringize());

        [Theory]
        [InlineData("floor(x)")]
        [InlineData("ceil(x)")]
        [InlineData("floor(x) + ceil(y)")]
        public void ThePrintedFormParsesBackToTheSameExpression(string input)
            => Assert.Equal(input.ToEntity(), input.ToEntity().Stringize().ToEntity());

        [Theory]
        [InlineData("floor(x)", "7/2", 3)]
        [InlineData("ceil(x)", "7/2", 4)]
        [InlineData("floor(x)", "-7/2", -4)]
        [InlineData("ceil(x)", "-7/2", -3)]
        public void SubstitutingReachesTheValue(string input, string at, int expected)
            => Assert.Equal(Entity.Number.Integer.Create(expected),
                input.ToEntity().Substitute("x", at.ToEntity()).Simplify());

        /// <summary>
        /// <c>floor(x) = 3</c> holds on the whole of <c>[3, 4)</c>, so the inverse is a
        /// parameter over that interval rather than a point. What it must not do is invent a
        /// single root.
        /// </summary>
        [Fact]
        public void SolvingGivesTheWholeIntervalAndNotAPoint()
        {
            var solutions = "floor(x) - 3 = 0".ToEntity().Solve("x");
            Assert.NotNull(solutions);
            var conditions = solutions!.Nodes.OfType<Entity.Providedf>().ToList();
            Assert.NotEmpty(conditions);

            // Every point of [3, 4) is a solution, and 4 is not.
            foreach (var point in new[] { "3", "3.5", "39/10" })
                Assert.Equal(Entity.Number.Integer.Create(3),
                    $"floor({point})".ToEntity().Simplify());
            Assert.Equal(Entity.Number.Integer.Create(4), "floor(4)".ToEntity().Simplify());
        }

        /// <summary>
        /// An infinity is its own floor and its own ceil, and NaN propagates —
        /// <a href="https://github.com/asc-community/AngouriMath/issues/830">#830</a>.
        /// </summary>
        /// <remarks>
        /// These used to throw <see cref="System.OverflowException"/> ("Value is infinity or
        /// NaN") out of evaluation, from the <c>EInteger</c> conversion. An internal exception
        /// from the numeric library is not one a caller has any reason to expect, and the
        /// neighbours do not do it: <c>abs(+oo)</c> is <c>+oo</c> and <c>abs(0/0)</c> is NaN.
        /// </remarks>
        [Theory]
        [InlineData("floor(+oo)", "+oo")]
        [InlineData("ceil(+oo)", "+oo")]
        [InlineData("floor(-oo)", "-oo")]
        [InlineData("ceil(-oo)", "-oo")]
        public void AnInfiniteArgumentIsItsOwnFloorAndCeil(string input, string expected)
        {
            Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Evaled);
            Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Simplify().Evaled);
        }

        [Theory]
        [InlineData("floor(0/0)")]
        [InlineData("ceil(0/0)")]
        public void AnUndefinedArgumentStaysUndefined(string input)
            => Assert.True(input.ToEntity().Evaled.IsNaN);

        /// <summary>
        /// A limit over <c>floor</c> or <c>ceil</c> terminates —
        /// <a href="https://github.com/asc-community/AngouriMath/issues/829">#829</a>.
        /// </summary>
        /// <remarks>
        /// Every one of these used to overflow the stack, because the nodes inherited a default
        /// <c>ComputeLimitDivideEtImpera</c> that returns an unevaluated limit of the very node
        /// being asked about, and evaluating that computes it again. That kills the process
        /// rather than raising anything, so this test cannot assert an exception — it asserts
        /// that an answer arrives at all, which a regression would turn into a dead test run
        /// rather than a silent pass. It is the same fault
        /// <a href="https://github.com/asc-community/AngouriMath/issues/704">#704</a> fixed on
        /// <c>signum</c>.
        /// </remarks>
        [Theory]
        [InlineData("floor(x)", "0")]
        [InlineData("floor(x)", "2")]
        [InlineData("floor(x)", "1/2")]
        [InlineData("floor(x)", "+oo")]
        [InlineData("floor(x)", "-oo")]
        [InlineData("ceil(x)", "0")]
        [InlineData("ceil(x)", "3/2")]
        [InlineData("ceil(x)", "+oo")]
        [InlineData("floor(cos(x))", "0")]
        public void TakingALimitTerminates(string input, string destination)
        {
            var task = System.Threading.Tasks.Task.Run(
                () => input.ToEntity().Limit("x", destination.ToEntity()));
            Assert.True(task.Wait(LimitTermination.Guard),
                $"limit({input}, x, {destination}) did not finish");
            Assert.NotNull(task.Result);
        }

        /// <summary>
        /// Between two consecutive integers the function is constant, so the limit is that
        /// constant — including at the infinities, where the floor of an infinity is itself.
        /// </summary>
        [Theory]
        [InlineData("floor(x)", "1/2", "0")]
        [InlineData("floor(x)", "3/2", "1")]
        [InlineData("floor(x)", "-1/2", "-1")]
        [InlineData("ceil(x)", "3/2", "2")]
        [InlineData("ceil(x)", "-1/2", "0")]
        [InlineData("floor(x + 1/2)", "0", "0")]
        [InlineData("ceil(x + 1/2)", "0", "1")]
        [InlineData("floor(1/2 + x^2)", "0", "0")]
        [InlineData("floor(x)", "+oo", "+oo")]
        [InlineData("floor(x)", "-oo", "-oo")]
        [InlineData("ceil(x)", "-oo", "-oo")]
        public void TheLimitAwayFromAJumpIsTheValue(string input, string destination, string expected)
            => Assert.Equal(expected.ToEntity().Evaled,
                input.ToEntity().Limit("x", destination.ToEntity()).Evaled);

        /// <summary>
        /// On a jump the two sides disagree, and which side the argument arrives from is not
        /// decided by the side <c>x</c> approaches its destination from. So the answer is an
        /// unevaluated limit — the same thing <c>signum</c> returns at zero. What it must not
        /// do is pick one of the two values.
        /// </summary>
        [Theory]
        [InlineData("floor(x)", "0")]
        [InlineData("floor(x)", "2")]
        [InlineData("floor(x)", "-3")]
        [InlineData("ceil(x)", "0")]
        [InlineData("ceil(x)", "2")]
        public void TheLimitOnAJumpIsDeclined(string input, string destination)
            => Assert.IsType<Entity.Limitf>(
                input.ToEntity().Limit("x", destination.ToEntity()));

        /// <summary>
        /// A rational with more digits than the precision, a third away from an integer on either
        /// side, rounded exactly: through <c>EDecimal</c> it was rounded onto the integer first, and
        /// <c>floor(10^120 - 1/3)</c> came out <c>10^120</c>.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1807">#1807</a>
        /// </summary>
        [Theory]
        [InlineData("floor((3 * 10^120 - 1)/3)", "10^120 - 1")]
        [InlineData("floor((3 * 10^120 + 1)/3)", "10^120")]
        [InlineData("ceil((3 * 10^120 - 1)/3)", "10^120")]
        [InlineData("ceil((3 * 10^120 + 1)/3)", "10^120 + 1")]
        [InlineData("floor(-(3 * 10^400 - 1)/3)", "-10^400")]
        [InlineData("floor(-(3 * 10^400 + 1)/3)", "-10^400 - 1")]
        [InlineData("ceil(-(3 * 10^400 - 1)/3)", "-10^400 + 1")]
        [InlineData("ceil(-(3 * 10^400 + 1)/3)", "-10^400")]
        public void ARationalOfManyDigitsIsRoundedExactly(string input, string expected)
        {
            Assert.Equal(expected.ToEntity().InnerSimplified, input.ToEntity().InnerSimplified);
            Assert.Equal(expected.ToEntity().InnerSimplified, input.ToEntity().Evaled);
        }
    }
}
