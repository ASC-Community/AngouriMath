//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The variable of a derivative and of an indefinite integral is free in it, so a value put in
    /// for it is where the derivative or the antiderivative is evaluated. It was dropped:
    /// <c>derivative(x^3, x)</c> at <c>x = 1</c> evaluated to <c>3 x^2</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2189">#2189</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class DerivativeAtAPointTest
    {
        [Theory]
        [InlineData("derivative(x^3, x)", "1", "3")]
        [InlineData("derivative(x^3, x, 2)", "2", "12")]
        [InlineData("derivative(sin(x), x)", "0", "1")]
        [InlineData("derivative(x^2, x)", "y", "2 y")]
        [InlineData("derivative(x^2, x) + x", "3", "9")]
        [InlineData("integral(x^2, x)", "1", "1/3 + C")]
        public void ItIsEvaluatedAtTheValue(string expression, string value, string expected)
            => Assert.Equal(Number.Integer.Zero,
                (expression.ToEntity().Substitute("x", value.ToEntity()) - expected.ToEntity()).Simplify());

        /// <summary>Substituting and evaluating commute.</summary>
        [Theory]
        [InlineData("derivative(x^3 + sin(x), x)")]
        [InlineData("derivative(e^x x, x, 2)")]
        public void SubstitutingAndEvaluatingCommute(string expression)
        {
            var e = expression.ToEntity();
            Assert.Equal(e.Evaled.Substitute("x", 2).Evaled, e.Substitute("x", 2).Evaled);
        }

        /// <summary>A lambda whose body is a derivative in its parameter is the derivative, evaluated where it is applied.</summary>
        [Fact]
        public void ALambdaOfADerivativeIsTheDerivative()
            => Assert.Equal(Number.Integer.Create(3), "apply(lambda(x, derivative(x^3, x)), 1)".ToEntity().Evaled);

        /// <summary>
        /// A value holding the variable is not captured: a := x in derivative(a x^2, x) is 2 x^2, the
        /// derivative 2 a x with a put in, and not the derivative of x^3.
        /// </summary>
        [Fact]
        public void AValueHoldingTheVariableIsNotCaptured()
            => Assert.Equal(Number.Integer.Zero,
                ("derivative(a x^2, x)".ToEntity().Substitute("a", "x") - "2 x^2".ToEntity()).Simplify());

        /// <summary>Substituting the variable for itself changes nothing.</summary>
        [Fact]
        public void TheVariableForItselfIsTheSame()
            => Assert.Equal("derivative(x^3, x)".ToEntity(), "derivative(x^3, x)".ToEntity().Substitute("x", "x"));

        /// <summary>
        /// A derivative the library does not take is kept, evaluated at the point, rather than
        /// answered wrongly or taken again forever.
        /// </summary>
        [Fact]
        public void ADerivativeThatIsNotTakenIsKeptAtThePoint()
        {
            var atAPoint = "derivative(primepi(x), x)".ToEntity().Substitute("x", 2);
            Assert.IsType<Application>(atAPoint);
            Assert.IsType<Application>(atAPoint.Evaled);
        }

        /// <summary>A variable the body does not hold renames the derivative's variable.</summary>
        [Fact]
        public void AFreshVariableRenamesIt()
            => Assert.Equal("derivative(y^3, y)".ToEntity(), "derivative(x^3, x)".ToEntity().Substitute("x", "y"));

        /// <summary>
        /// A lambda renaming its parameter puts a fresh variable in for it, which renames the
        /// derivative rather than wrapping it in a lambda over the old name to be renamed again, so
        /// factorizing derivative(apply(f, x), x, -2) finishes with the double integral.
        /// </summary>
        [Fact]
        public void RenamingTerminates()
        {
            var task = System.Threading.Tasks.Task.Factory.StartNew(
                () => "derivative(apply(f, x), x, -2)".ToEntity().Factorize(), System.Threading.Tasks.TaskCreationOptions.LongRunning);
            Assert.True(task.Wait(System.TimeSpan.FromSeconds(60)), "did not finish");
            Assert.Equal("integral(integral(apply(f, x), x), x)".ToEntity(), task.Result);
        }

        /// <summary>A definite integral binds its variable, and a value for it changes nothing.</summary>
        [Fact]
        public void ADefiniteIntegralStillBindsItsVariable()
            => Assert.Equal("integral(x, x, 0, 1)".ToEntity(), "integral(x, x, 0, 1)".ToEntity().Substitute("x", 5));
    }
}
