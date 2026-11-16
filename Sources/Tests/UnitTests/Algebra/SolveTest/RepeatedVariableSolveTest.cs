//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// Inverting an expression isolates the variable only where the variable occurs once,
    /// which is what <c>Invert</c> requires of its callers. Solving <c>f(x) = 0</c> did not
    /// check it, so an equation whose variable occurs twice came back rearranged rather than
    /// solved: <c>(x^2 + x + 1)^2 = 0</c> was answered <c>{ sqrt(-1 - x), -sqrt(-1 - x) }</c>,
    /// which is <c>x^2 = -1 - x</c> with the square inverted and the <c>x</c> on the right
    /// left where it stood.
    /// https://github.com/asc-community/AngouriMath/issues/744
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class RepeatedVariableSolveTest
    {
        /// <summary>
        /// The roots as written: a family's body with its index in it, which is <c>2 pi n</c>
        /// for <c>union({ 2 pi n }, n in ZZ)</c>. https://github.com/asc-community/AngouriMath/issues/2319
        /// </summary>
        private static List<Entity> Roots(string equation) => Written(equation.ToEntity().Solve("x"));

        private static List<Entity> Written(Entity roots) => roots switch
        {
            Entity.Set.FiniteSet finite => finite.ToList(),
            Entity.Set.Unionf(var left, var right) => Written(left).Concat(Written(right)).ToList(),
            Entity.Set.IndexedUnionf family => Written(family.Body),
            _ => throw new Xunit.Sdk.XunitException($"{roots.Stringize()} lists no roots"),
        };

        private static bool IsNear(Entity root, double re, double im)
        {
            // The absolute-value inversion leaves `provided r_1 in RR` behind, which is a
            // condition on the working rather than part of the answer.
            while (root is Entity.Providedf(var inner, _))
                root = inner;
            if (root.Vars.Any())
                return false;
            var value = root.EvalNumerical();
            return System.Math.Abs(value.RealPart.EDecimal.ToDouble() - re) < 1e-9
                && System.Math.Abs(value.ImaginaryPart.EDecimal.ToDouble() - im) < 1e-9;
        }

        /// <summary>
        /// The whole class, stated as the property that failed rather than as the shapes that
        /// failed it: an answer to an equation in x cannot mention x. Such a "root" satisfies
        /// nothing and cannot even be evaluated, so substituting answers back does not catch
        /// it -- the residual is not a number, and a root is only ever dropped on evidence.
        /// </summary>
        [Theory]
        [InlineData("(x ^ 2 + x + 1) ^ 2 = 0")]
        [InlineData("(x ^ 2 + x) ^ 2 = 0")]
        [InlineData("(x ^ 3 + x + 1) ^ 2 = 0")]
        [InlineData("(x ^ 2 + 2 * x + 3) ^ 2 = 0")]
        [InlineData("(x ^ 2 - 2) ^ 2 = 0")]
        [InlineData("(x - 1) ^ 2 = 0")]
        [InlineData("sqrt(x ^ 2 + x + 1) = 0")]
        [InlineData("(x ^ 2 + x) ^ (3/2) = 0")]
        [InlineData("sin(x ^ 2 + x) = 0")]
        [InlineData("ln(x ^ 2 + x) = 0")]
        [InlineData("abs(x ^ 2 + x) = 0")]
        [InlineData("tan(x ^ 2 + x) = 0")]
        public void NoRootMentionsTheVariableItSolvesFor(string equation)
        {
            foreach (var root in Roots(equation))
                Assert.False(root.Vars.Any(v => v.Name == "x"),
                    $"{equation} was answered with {root.Stringize()}, which still contains x");
        }

        /// <summary>
        /// A power is zero exactly where its base is, so <c>f(x)^n = 0</c> has the roots of
        /// <c>f(x) = 0</c> -- and must give them in the same form, not merely the same values.
        /// </summary>
        [Theory]
        [InlineData("(x ^ 2 + x + 1) ^ 2 = 0", -0.5, 0.8660254037844386)]
        [InlineData("(x ^ 2 + x + 1) ^ 3 = 0", -0.5, 0.8660254037844386)]
        [InlineData("(x ^ 2 + 2 * x + 3) ^ 2 = 0", -1.0, 1.4142135623730951)]
        // The exponent does not have to be a whole number, and the issue records only whole
        // ones. sqrt(f(x)) = 0 is the same equation as f(x) = 0 and failed the same way.
        [InlineData("sqrt(x ^ 2 + x + 1) = 0", -0.5, 0.8660254037844386)]
        [InlineData("(x ^ 2 + x + 1) ^ (3/2) = 0", -0.5, 0.8660254037844386)]
        public void APowerOfAPolynomialHasTheRootsOfThatPolynomial(
            string equation, double re, double im)
        {
            var roots = Roots(equation);
            Assert.Contains(roots, root => IsNear(root, re, im));
            Assert.Contains(roots, root => IsNear(root, re, -im));
            Assert.Equal(2, roots.Count);
        }

        /// <summary>
        /// A power of a polynomial must answer identically to the polynomial, not merely
        /// equivalently. Routing it through the replacement machinery instead reaches the
        /// same two numbers written <c>-1/2 + -sqrt(-3/4)</c>, which is the output-quality
        /// complaint of https://github.com/asc-community/AngouriMath/issues/272 one step on.
        /// </summary>
        [Theory]
        [InlineData("(x ^ 2 + x + 1) ^ 2 = 0", "x ^ 2 + x + 1 = 0")]
        [InlineData("(x ^ 2 + x + 1) ^ 3 = 0", "x ^ 2 + x + 1 = 0")]
        [InlineData("(x ^ 3 + x + 1) ^ 2 = 0", "x ^ 3 + x + 1 = 0")]
        [InlineData("(x ^ 2 - 2) ^ 2 = 0", "x ^ 2 - 2 = 0")]
        public void APowerIsAnsweredExactlyAsItsBaseIs(string powered, string @base) =>
            Assert.Equal(Roots(@base), Roots(powered));

        // The plainest case, because it is not a hard equation: (x^2 + x)^2 = 0 has the roots
        // 0 and -1, and it was answered { sqrt(-x), -sqrt(-x) }.
        [Theory]
        [InlineData("(x ^ 2 + x) ^ 2 = 0")]
        [InlineData("(x ^ 2 + x) ^ (3/2) = 0")]
        public void APowerOfAPolynomialWithRationalRootsKeepsThem(string equation)
        {
            var roots = Roots(equation);
            Assert.Contains(Entity.Number.Integer.Create(0), roots);
            Assert.Contains(Entity.Number.Integer.Create(-1), roots);
            Assert.Equal(2, roots.Count);
        }

        /// <summary>
        /// The same defect through any other function, and the half of it that turned into
        /// right answers rather than into no answer. A right-hand side of zero is the only
        /// reason the inverting route was taken at all: <c>cos(x^2 + x + 1) = 1</c> was
        /// answered correctly all along, because a non-zero side leaves a subtraction that
        /// reaches the replacement machinery instead.
        /// </summary>
        [Fact]
        public void ALogarithmOfARepeatedVariableIsSolvedThroughItsArgument()
        {
            // ln(x^2 + x) = 0 means x^2 + x = 1, whose roots are (-1 +- sqrt(5))/2.
            var roots = Roots("ln(x ^ 2 + x) = 0");
            var half = (System.Math.Sqrt(5) - 1) / 2;
            Assert.Contains(roots, root => IsNear(root, half, 0));
            Assert.Contains(roots, root => IsNear(root, -half - 1, 0));
        }

        [Fact]
        public void AnAbsoluteValueOfARepeatedVariableIsSolvedThroughItsArgument()
        {
            var roots = Roots("abs(x ^ 2 + x) = 0");
            Assert.Contains(roots, root => IsNear(root, 0, 0));
            Assert.Contains(roots, root => IsNear(root, -1, 0));
        }

        /// <summary>
        /// The parametric families keep their parameter and lose the variable:
        /// sin(x^2 + x) = 0 means x^2 + x = pi*k, so x = (-1 +- sqrt(1 + 4*pi*k))/2.
        /// </summary>
        [Fact]
        public void ASineOfARepeatedVariableIsSolvedThroughItsArgument()
        {
            var roots = Roots("sin(x ^ 2 + x) = 0");
            Assert.NotEmpty(roots);
            foreach (var root in roots)
            {
                Assert.False(root.Vars.Any(v => v.Name == "x"),
                    $"{root.Stringize()} still contains x");
                // Every root is a genuine one at k = 0, where the parameter drops out.
                var atZero = root.Substitute(root.Vars.First(), 0).EvalNumerical();
                var residual = "sin(x ^ 2 + x)".ToEntity()
                    .Substitute("x", atZero).EvalNumerical().Abs().EDecimal.ToDouble();
                Assert.True(residual < 1e-9,
                    $"{root.Stringize()} leaves a residual of {residual}");
            }
        }

        // What must not change: an equation whose variable occurs once is still inverted, and
        // is still answered the way it always was.
        [Theory]
        [InlineData("sin(x) = 0", "2 * pi * n")]
        [InlineData("ln(x) = 0", "1")]
        [InlineData("x ^ 2 - 2 = 0", "sqrt(2)")]
        [InlineData("sin(x ^ 2 - 2) = 0", "sqrt(2 * pi * n + 2)")]
        public void AVariableThatOccursOnceIsStillInverted(string equation, string expected) =>
            Assert.Contains(expected.ToEntity().InnerSimplified, Roots(equation));
    }
}
