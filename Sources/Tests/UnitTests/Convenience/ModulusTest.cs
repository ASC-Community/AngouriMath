//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using System;
using System.Numerics;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The remainder after division, asked for by
    /// https://github.com/asc-community/AngouriMath/issues/402 and
    /// https://github.com/asc-community/AngouriMath/issues/618. There was no node for it at
    /// all: there was no <c>mod</c> and no <c>MathS.Mod</c>.
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class ModulusTest
    {
        private static readonly Entity.Variable x = MathS.Var("x");

        /// <summary>
        /// The remainder takes the sign of the divisor -- the floored convention, a - b*floor(a/b).
        /// This is what a mathematician means by mod: it is the convention under which the
        /// residues modulo n are the numbers from 0 to n - 1, and it is what SymPy, Mathematica
        /// and Maxima all answer. C and its descendants truncate instead and so give -1 for
        /// -7 mod 3, but their % is an operation on machine integers rather than this one.
        /// Pinned because the two conventions disagree on every case where the signs differ.
        /// </summary>
        [Theory]
        [InlineData("7 mod 3", "1")]
        [InlineData("(-7) mod 3", "2")]
        [InlineData("7 mod (-3)", "-2")]
        [InlineData("(-7) mod (-3)", "-1")]
        [InlineData("6 mod 3", "0")]
        [InlineData("(-6) mod 3", "0")]
        [InlineData("2 mod 5", "2")]
        [InlineData("7.5 mod 2", "3/2")]
        [InlineData("(1/2) mod (1/3)", "1/6")]
        public void TheRemainderFollowsTheDivisorsSign(string expression, string expected) =>
            Assert.Equal(expected.ToEntity().Evaled, expression.ToEntity().Evaled);

        /// <summary>Dividing by zero is undefined, as it is for the quotient.</summary>
        [Fact]
        public void ByZeroIsUndefined() =>
            Assert.Equal(MathS.NaN.Evaled, "7 mod 0".ToEntity().Evaled);

        /// <summary>
        /// mod binds as tightly as * and /, and associates to the left. Every one of these is a
        /// case where the two conventions agree, so what is being pinned is the grouping and
        /// nothing else.
        /// </summary>
        [Theory]
        [InlineData("2 * 7 mod 3", 2)]
        [InlineData("7 mod 3 * 2", 2)]
        [InlineData("12 / 4 mod 2", 1)]
        [InlineData("1 + 7 mod 3", 2)]
        [InlineData("7 mod 3 + 1", 2)]
        [InlineData("2 ^ 3 mod 3", 2)]
        public void ItBindsAsTightlyAsTimesAndDivide(string expression, int expected) =>
            Assert.Equal(Entity.Number.Integer.Create(expected), expression.ToEntity().Evaled);

        /// <summary>
        /// mod is written out, and % is left alone -- it is percent in mathematical writing, and
        /// the parser should stay free to mean that by it.
        /// </summary>
        [Fact]
        public void PercentIsNotTheParsersModulus() =>
            Assert.Throws<UnhandledParseException>(() => "7 % 3".ToEntity());

        [Fact]
        public void TheOperatorBuildsTheSameNodeAsTheParser() =>
            Assert.Equal("x mod 3".ToEntity(), x % 3);

        [Fact]
        public void MathSModBuildsTheSameNodeAsTheParser() =>
            Assert.Equal("x mod 3".ToEntity(), MathS.Mod(x, 3));

        [Fact]
        public void ItStringizesBackToWhatWasParsed() =>
            Assert.Equal("x mod 3", "x mod 3".ToEntity().Stringize());

        [Fact]
        public void ItLatexizesAsBmod() =>
            Assert.Equal(@"x \bmod 3", "x mod 3".ToEntity().Latexize());

        [Theory]
        [InlineData("x mod x", "0")]
        [InlineData("(x + 1) mod (x + 1)", "0")]
        [InlineData("0 mod x", "0")]
        [InlineData("(x mod 3) mod 3", "x mod 3")]
        public void TheIdentitiesThatHoldWhereverTheNodeIsDefined(string expression, string expected)
        {
            var simplified = expression.ToEntity().Simplify();
            // The rewrites carry the condition that the divisor is not zero, which is the
            // node's own condition and not something the rewrite introduced.
            while (simplified is Entity.Providedf(var inner, _)) simplified = inner;
            Assert.Equal(expected.ToEntity(), simplified);
        }

        /// <summary>
        /// x mod 1 is 0 only for whole x -- 2.5 mod 1 is 0.5 -- so it must not be reduced for a
        /// variable that could be anything.
        /// </summary>
        [Fact]
        public void ItIsNotReducedWhereTheIdentityDoesNotHold() =>
            Assert.Equal("x mod 1".ToEntity(), "x mod 1".ToEntity().Simplify());

        [Fact]
        public void SubstitutionGoesThrough() =>
            Assert.Equal(Entity.Number.Integer.Create(1), "x mod 3".ToEntity().Substitute("x", 10).Evaled);

        /// <summary>
        /// a mod b is a - b * floor(a / b), so where b does not depend on the variable the
        /// derivative is the dividend's own, away from the jumps. Where b does depend on it,
        /// nothing is claimed: this library has no floor node, so the general case could only
        /// be written as something that is wrong at the jumps.
        /// </summary>
        [Theory]
        [InlineData("x mod 3", "1")]
        [InlineData("x ^ 2 mod 3", "2 * x")]
        public void TheDerivativeIsTheDividendsWhereTheDivisorIsConstant(string expression, string expected) =>
            Assert.Equal(expected.ToEntity(), expression.ToEntity().Differentiate("x").Simplify());

        [Fact]
        public void TheDerivativeIsLeftAloneWhereTheDivisorMoves() =>
            Assert.IsType<Entity.Derivativef>("5 mod x".ToEntity().Differentiate("x").Simplify());

        /// <summary>
        /// The remainder is continuous except where the dividend reaches a non-zero multiple of
        /// the divisor. Zero is not one of those: the remainder takes the dividend's sign, so
        /// x mod 3 is x on either side of 0.
        /// </summary>
        [Theory]
        [InlineData("x mod 3", "2", "2")]
        [InlineData("x mod 3", "0", "0")]
        [InlineData("x ^ 2 mod 5", "2", "4")]
        public void ALimitAtAPointItIsContinuousAtIsTheValueThere(string expression, string destination, string expected) =>
            Assert.Equal(expected.ToEntity().Evaled,
                expression.ToEntity().Limit("x", destination.ToEntity()).Simplify().Evaled);

        /// <summary>
        /// At a jump there is no two-sided limit, and which one-sided value is taken depends on
        /// the direction the dividend crosses in rather than only on what it tends to. Left
        /// unevaluated rather than answered with the value at the point, which is one of the two
        /// one-sided limits and neither the other nor the two-sided one. It must also terminate:
        /// a node with no reading of its own sends the descent back through Limitf's own
        /// evaluation, and that is a cycle.
        /// </summary>
        [Theory]
        [InlineData("x mod 3", "3")]
        [InlineData("x mod 3", "+oo")]
        public void AJumpIsLeftUnevaluatedAndTerminates(string expression, string destination)
        {
            var task = System.Threading.Tasks.Task.Run(
                () => expression.ToEntity().Limit("x", destination.ToEntity()).Simplify());
            Assert.True(task.Wait(LimitTermination.Guard), "the limit did not terminate");
            Assert.IsType<Entity.Limitf>(task.Result);
        }

        /// <summary>
        /// Both compilers, against the interpreter. The stack machine carries every value as a
        /// complex number, so it answers NaN where the two parts are not real -- the same
        /// refusal the interpreter makes by leaving the node alone.
        /// </summary>
        [Theory]
        [InlineData(7, 1)]
        [InlineData(-7, 2)]
        [InlineData(6, 0)]
        public void TheStackMachineAgreesWithTheInterpreter(int at, int expected)
        {
            var compiled = "x mod 3".ToEntity().Compile("x");
            Assert.Equal(expected, compiled.Call(new Complex(at, 0)).Real, 9);
        }

        [Fact]
        public void TheStackMachineRefusesANonRealArgument() =>
            Assert.True(double.IsNaN("x mod 3".ToEntity().Compile("x").Call(new Complex(2, 1)).Real));

        [Theory]
        [InlineData(7, 1)]
        [InlineData(-7, 2)]
        public void TheLinqCompilerAgreesWithTheInterpreter(int at, int expected) =>
            Assert.Equal(expected, "x mod 3".ToEntity().Compile<int, int>("x")(at));

        [Fact]
        public void TheLinqCompilerKeepsTheFractionalPart() =>
            Assert.Equal(1.5, "x mod y".ToEntity().Compile<double, double, double>("x", "y")(7.5, 2), 9);

        /// <summary>
        /// x mod a = value has one solution per period, and is answered the way the
        /// trigonometric inversions are, with a whole parameter: x mod 3 = 1 is x = 1 + 3 n,
        /// which is 4 at n = 1. https://github.com/asc-community/AngouriMath/issues/1629
        /// </summary>
        [Fact]
        public void SolvingGivesEverySolution()
        {
            var solution = Assert.Single(Assert.IsType<Entity.Set.FiniteSet>(("x mod 3".ToEntity() - 1).SolveEquation("x")).Elements);
            var parameter = Assert.Single(solution.Vars);
            Assert.Equal((Entity)4, solution.Substitute(parameter, 1).Evaled);
        }
    }
}
