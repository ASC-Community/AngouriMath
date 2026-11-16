//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// Differentiating in an unknown function, <c>f(x)</c>, both ways: <c>derivative</c> is the
    /// total derivative, a change of variables, and <c>partialderivative</c> the partial one, which
    /// holds the other slots, <c>f'(x)</c> and <c>x</c>, fixed.
    /// https://github.com/asc-community/AngouriMath/issues/230
    /// </summary>
    /// <remarks>
    /// Every answer is checked by value with <c>f</c> made concrete, against a value computed
    /// another way: the total derivative against <c>(d expr/dx) / (du/dx)</c> differentiated in
    /// <c>x</c> after <c>f</c> is put in, and the partial one against the derivative in a plain
    /// variable standing for the slot.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class DerivativeInAFunctionTest
    {
        private const string F = "apply(f, x)";
        private const string DF = "derivative(apply(f, x), x)";

        /// <summary>Concrete functions to put in for <c>f</c>, chosen so that <c>f'</c> is not 0 at the points.</summary>
        public static readonly TheoryData<string> Functions = new() { "sin(t)", "e^t", "t^3 + t" };

        private static readonly double[] Points = { 0.3, 0.7, 1.3 };

        private static Entity WithF(Entity expr, string function)
            => expr.Substitute("f", $"lambda(t, {function})".ToEntity()).Simplify();

        private static System.Numerics.Complex At(Entity expr, double x)
            => expr.Substitute("x", x).Simplify().EvalNumerical().ToNumerics();

        private static void AssertAgree(Entity expected, Entity actual)
        {
            foreach (var point in Points)
            {
                var (e, a) = (At(expected, point), At(actual, point));
                Assert.True(double.IsFinite(e.Real) && double.IsFinite(e.Imaginary), $"{expected} at {point} is {e}");
                Assert.True((e - a).Magnitude < 1e-9 * (1 + e.Magnitude), $"at x = {point}: expected {e}, got {a} from {actual}");
            }
        }

        private static Entity Total(Entity expr, Entity over, string function)
            => (WithF(expr, function).Differentiate("x") / WithF(over, function).Differentiate("x")).Simplify();

        /// <summary>
        /// The total derivative of <c>expr</c> in <c>u</c>, with <c>u</c> a function of <c>x</c>,
        /// is <c>(d expr/dx) / (du/dx)</c>: these were left unevaluated, and are answered.
        /// </summary>
        [Theory]
        [InlineData("x * apply(f, x)", F)]
        [InlineData("apply(f, x)^2 * derivative(apply(f, x), x)", F)]
        [InlineData("apply(f, x)^2 + derivative(apply(f, x), x)^2", F)]
        [InlineData("x * apply(f, x)", DF)]
        [InlineData("apply(g, x) * apply(f, x)", F)]
        public void TheTotalDerivativeChangesVariables(string expr, string over)
        {
            var answer = MathS.Derivative(expr, over).Simplify();
            Assert.False(answer.Nodes.Any(n => n is Derivativef d && d.Var == over.ToEntity()), $"{answer}");
            foreach (var function in new[] { "sin(t)", "e^t", "t^3 + t" })
                AssertAgree(Total(expr.ToEntity().Substitute("g", "lambda(t, cos(t))".ToEntity()), over, function),
                    WithF(answer.Substitute("g", "lambda(t, cos(t))".ToEntity()), function));
        }

        /// <summary>
        /// The pin from the issue: the total derivative of <c>f^2 + f'^2</c> in <c>f</c> is
        /// <c>2f + 2f''</c>, defined where <c>f' ≠ 0</c>, where the partial one is <c>2f</c>.
        /// </summary>
        [Theory]
        [MemberData(nameof(Functions))]
        public void TheTwoDerivativesOfTheSameExpressionDiffer(string function)
        {
            var lagrangian = $"{F}^2 + {DF}^2";
            var total = MathS.Derivative(lagrangian, F).Simplify();
            var partial = MathS.PartialDerivative(lagrangian, F).Simplify();
            Assert.Contains(total.Nodes, n => n is Providedf p && p.Predicate.ContainsNode(DF));
            AssertAgree(WithF($"2 {F} + 2 derivative({F}, x, 2)", function), WithF(total, function));
            Assert.Equal($"2 * {F}".ToEntity(), partial);
        }

        /// <summary>Taken twice, the change of variables is applied twice: <c>d/du ((f + x f')/f')</c>.</summary>
        [Theory]
        [MemberData(nameof(Functions))]
        public void TheSecondTotalDerivativeIsTheFirstDerivedAgain(string function)
        {
            var second = MathS.Derivative($"x * {F}", F, 2).Simplify();
            var first = Total($"x * {F}".ToEntity(), F, function);
            var expected = (first.Differentiate("x") / WithF(F, function).Differentiate("x")).Simplify();
            AssertAgree(expected, WithF(second, function));
        }

        /// <summary>Where the total derivative was answered or declined before, it still is.</summary>
        [Fact]
        public void WhatWasAnsweredOrDeclinedIsAsItWas()
        {
            AssertAgree("-y csc(x) cos(x)".ToEntity().Substitute("y", 3), "derivative(y * sin(x), cos(x))".ToEntity().Simplify().Substitute("y", 3));
            Assert.Equal($"2 * {F}".ToEntity(), MathS.Derivative($"{F}^2", F).Simplify());
            // Two variables in what it is taken over: how they move together is not given.
            Assert.IsType<Derivativef>("derivative(x + y, x * y)".ToEntity().Simplify());
            // Nor two variables under an unknown function.
            Assert.IsType<Derivativef>("derivative(x, apply(f, x, y))".ToEntity().Simplify());
        }

        [Fact]
        public void ThePartialDerivativeIsWrittenAsItIsRead()
        {
            var node = $"partialderivative(x * {F}^2, {F})".ToEntity();
            Assert.IsType<PartialDerivativef>(node);
            Assert.Equal(node, MathS.PartialDerivative($"x * {F}^2", F));
            Assert.Equal(node, node.Stringize().ToEntity());
            var twice = "partialderivative(y, x, 2)".ToEntity();
            Assert.Equal(MathS.PartialDerivative("y", "x", 2), twice);
            Assert.Equal(@"\frac{\partial^{2}}{\partial x^{2}}y", twice.Latexize());
            Assert.Equal(@"\frac{\partial}{\partial x}y", "partialderivative(y, x)".ToEntity().Latexize());
            Assert.Throws<FunctionArgumentCountException>(() => "partialderivative(x)".ToEntity());
            Assert.Throws<InvalidArgumentParseException>(() => "partialderivative(x, y, 0)".ToEntity());
            Assert.Throws<ArgumentOutOfRangeException>(() => MathS.PartialDerivative("x", "y", 0));
        }

        /// <summary>
        /// Each slot is held fixed while another varies: <c>f'</c> and <c>x</c> in the partial
        /// derivative in <c>f</c>, and <c>f</c> in the one in <c>f'</c>. The expected value is the
        /// derivative in a plain variable put in for the slot.
        /// </summary>
        [Theory]
        [InlineData("a^2 + b^2", "a", "2 a")]
        [InlineData("a^2 + b^2", "b", "2 b")]
        [InlineData("x a", "a", "x")]
        [InlineData("sin(a) c", "a", "cos(a) c")]
        [InlineData("a^2 b + x b^3", "b", "a^2 + 3 x b^2")]
        [InlineData("x^2 a + c", "c", "1")]
        public void EveryOtherSlotIsHeldFixed(string written, string slot, string expected)
        {
            static Entity Slots(string e) => e.ToEntity()
                .Substitute("a", F.ToEntity())
                .Substitute("b", DF.ToEntity())
                .Substitute("c", "derivative(apply(f, x), x, 2)".ToEntity());
            var answer = MathS.PartialDerivative(Slots(written), Slots(slot)).Simplify();
            Assert.False(answer.Nodes.Any(n => n is PartialDerivativef), $"{answer}");
            foreach (var function in new[] { "sin(t)", "e^t", "t^3 + t" })
                AssertAgree(WithF(Slots(expected), function), WithF(answer, function));
        }

        /// <summary>
        /// An unknown function of the slot is a function of it, not another slot:
        /// <c>∂ g(f) / ∂f = g'(f)</c>, and twice <c>∂² f^3 / ∂f² = 6f</c>.
        /// </summary>
        [Fact]
        public void AFunctionOfTheSlotVariesWithIt()
        {
            var answer = MathS.PartialDerivative($"apply(g, {F}) + apply(g, x)", F).Simplify()
                .Substitute("g", "lambda(t, t^3)".ToEntity());
            AssertAgree(WithF($"3 {F}^2", "sin(t)"), WithF(answer, "sin(t)"));
            Assert.Equal($"6 * {F}".ToEntity(), MathS.PartialDerivative($"{F}^3", F, 2).Simplify());
        }

        /// <summary>Over a variable it is the derivative, every other variable held fixed.</summary>
        [Theory]
        [InlineData("x^2 y")]
        [InlineData("sin(x y) + apply(f, x, y)")]
        [InlineData("x * apply(f, x)")]
        public void OverAVariableItIsTheDerivative(string expr)
            => Assert.Equal(MathS.Derivative(expr, "x").Simplify(), MathS.PartialDerivative(expr, "x").Simplify());

        /// <summary>
        /// Over an expression that is not a slot it is left as written: <c>x^2</c> is a slot of
        /// <c>x^2</c> and not of <c>x * x</c>, which is the same value. So is a slot that occurs
        /// where it cannot be renamed, inside a derivative over it.
        /// </summary>
        [Theory]
        [InlineData("partialderivative(x^2, x^2)")]
        [InlineData("partialderivative(x * apply(f, x), sin(x))")]
        [InlineData("partialderivative(x, 3)")]
        public void OverWhatIsNotASlotItIsLeftAsWritten(string expr)
            => Assert.IsType<PartialDerivativef>(expr.ToEntity().Simplify());

        /// <summary>
        /// A total derivative over the slot, left unevaluated inside, is not held fixed: it is
        /// taken first where it can be, <c>d sin(f)/df = cos(f)</c>, whose partial derivative in
        /// <c>f</c> is <c>-sin(f)</c>.
        /// </summary>
        [Fact]
        public void ADerivativeOverTheSlotIsTakenNotHeld()
            => AssertAgree(WithF($"-sin({F})", "e^t"),
                WithF(MathS.PartialDerivative($"derivative(sin({F}), {F})", F).Simplify(), "e^t"));

        /// <summary>
        /// It does not bind: its slot's variables are free. A value for <c>x</c> goes into the slot
        /// too, so at <c>x = 2</c> the partial derivative of <c>x f(x)^2</c> in <c>f(x)</c> is
        /// <c>4 f(2)</c>; and differentiated in <c>x</c> it is a function of <c>x</c>.
        /// </summary>
        [Fact]
        public void ItIsAFunctionOfItsSlot()
        {
            var node = MathS.PartialDerivative($"x * {F}^2", F);
            Assert.Contains((Variable)"x", node.FreeVariables);
            Assert.Contains((Variable)"f", node.FreeVariables);
            Assert.Equal("4 * apply(f, 2)".ToEntity(), node.Substitute("x", 2).Simplify());
            AssertAgree(WithF($"2 {F} + 2 x {DF}", "sin(t)"), WithF(node.Differentiate("x"), "sin(t)"));
            Assert.Equal(MathS.Apply(MathS.Lambda("x", "partialderivative(x^3, x)"), 2), "partialderivative(x^3, x)".ToEntity().Substitute("x", 2));
            Assert.Equal((Entity)12, "partialderivative(x^3, x)".ToEntity().Substitute("x", 2).Simplify());
        }

        /// <summary>
        /// A value put in for the variable a total derivative is taken by letting move is put into
        /// its answer, not into the expression alone: <c>x f^2</c> over <c>f</c> is
        /// <c>f^2/f' + 2 x f</c>, and at <c>x = p</c> it is that at <c>p</c>. Put into the
        /// expression alone it was <c>derivative(p f(p)^2, f(x))</c>, which is 0.
        /// </summary>
        [Theory]
        [MemberData(nameof(Functions))]
        public void AValueForTheVariableGoesIntoTheAnswer(string function)
        {
            var node = MathS.Derivative($"x * {F}^2", F);
            foreach (var point in Points)
            {
                var expected = At(WithF($"{F}^2 / {DF} + 2 x {F}", function), point);
                var actual = WithF(node.Substitute("x", point), function).EvalNumerical().ToNumerics();
                Assert.True((expected - actual).Magnitude < 1e-9 * (1 + expected.Magnitude), $"at x = {point}: expected {expected}, got {actual}");
            }
        }

        /// <summary>
        /// The same over an expression of the variable: <c>d(x^3)/d(x^2) = 3x/2</c> is 3 at 2, and
        /// <c>∫ x d(x^2) = 2x^3/3 + C</c>. Where it cannot be taken it is held outside as a lambda
        /// applied to the value. Over a variable nothing changes: the value goes into the expression.
        /// </summary>
        [Fact]
        public void AValueForWhatItIsTakenOverIsPutIntoTheAnswer()
        {
            Assert.Equal((Entity)3, "derivative(x^3, x^2)".ToEntity().Substitute("x", 2).Simplify());
            Assert.Equal((Entity)Entity.Number.Rational.Create(16, 3), "integral(x, x^2)".ToEntity().Substitute("x", 2).Simplify().Substitute("C", 0).Simplify());
            Assert.Contains("derivative(x + y, x * y)".ToEntity().Substitute("x", 2).Simplify().Nodes, n => n is Derivativef);
            Assert.Equal(MathS.Derivative("2 * y", "y"), "derivative(x * y, y)".ToEntity().Substitute("x", 2));
            Assert.Equal((Entity)2, "derivative(x * y, y)".ToEntity().Substitute("x", 2).Simplify());
        }

        [Fact]
        public void ItIsNotCompiled()
            => Assert.Throws<UncompilableNodeException>(() => MathS.PartialDerivative("x^2", "x").Compile("x"));
    }
}
