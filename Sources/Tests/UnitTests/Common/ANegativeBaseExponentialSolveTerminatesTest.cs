//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A power of x to an exponent that is a number but not a real one was substituted by x itself,
    /// which renamed the variable and asked the same equation again until the stack overflowed:
    /// <c>(-2)^x + 4^x = 0</c> reaches that as <c>t^ln(-2) + t^ln(4)</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2424">#2424</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ANegativeBaseExponentialSolveTerminatesTest
    {
        private static readonly TimeSpan Budget = LimitTermination.Guard;

        private static Entity.Set Solved(string equation)
        {
            var task = Task.Run(() => equation.ToEntity().Solve("x"));
            Assert.True(task.Wait(Budget), $"{equation} did not finish within {Budget.TotalSeconds}s");
            return task.Result;
        }

        private static NumericsComplex Value(Entity root)
            => Assert.IsAssignableFrom<Entity.Number.Complex>(root.EvalNumerical()).ToNumerics();

        /// <summary>
        /// <c>a^x + s b^x = 0</c> with <c>s = 1</c> or <c>-1</c>. With principal powers it is
        /// <c>e^(x (ln a - ln b)) = -s</c>, so the roots are
        /// <c>x = (ln(-s) + 2 pi i k) / (ln a - ln b)</c> for every whole <c>k</c>:
        /// <c>(-2)^x + 4^x = 0</c> has <c>x = (2k + 1) pi i / (pi i - ln 2)</c>, the first of which is
        /// <c>0.95358 - 0.21039i</c>, and <c>(-2)^x + 2^x = 0</c> has the odd integers. Each root
        /// the solver answers is to be one of them; declining is honest, an empty set is not.
        /// </summary>
        [Theory]
        [InlineData("(-2)^x + 4^x = 0", -2, 4, 1)]
        [InlineData("(-3)^x + 9^x = 0", -3, 9, 1)]
        [InlineData("(-2)^x - 4^x = 0", -2, 4, -1)]
        [InlineData("(-2)^x + 2^x = 0", -2, 2, 1)]
        public void TheRootsAreOnTheFamily(string equation, int a, int b, int sign)
        {
            var solved = Solved(equation);
            if (solved is not Entity.Set.FiniteSet roots)
                return;
            Assert.NotEmpty(roots);
            var rate = NumericsComplex.Log(a) - NumericsComplex.Log(b);
            var shift = NumericsComplex.Log(-sign);
            foreach (var root in roots)
            {
                var x = Value(root);
                var left = NumericsComplex.Exp(x * NumericsComplex.Log(a));
                var right = sign * NumericsComplex.Exp(x * NumericsComplex.Log(b));
                Assert.True(NumericsComplex.Abs(left + right) <= 1e-9 * (NumericsComplex.Abs(left) + NumericsComplex.Abs(right)),
                    $"{root} leaves {left + right} in {equation}");
                var k = (x * rate - shift) / (2 * Math.PI * NumericsComplex.ImaginaryOne);
                Assert.True(Math.Abs(k.Real - Math.Round(k.Real)) < 1e-9 && Math.Abs(k.Imaginary) < 1e-9,
                    $"{root} is not on the family of {equation}: k = {k}");
            }
        }

        /// <summary>
        /// The same substitution without a negative base. <c>x^(1 + i) + x</c> is
        /// <c>x (x^i + 1)</c>, whose roots are 0, where <c>0^(1 + i)</c> is 0, and <c>e^((2k + 1) pi)</c>.
        /// <c>x^i + x</c> and <c>x^i + x^(2i)</c> have no root at 0, where <c>0^i</c> is undefined,
        /// though the function Newton's method compiles is 0 there.
        /// </summary>
        [Theory]
        [InlineData("x^(1 + i) + x")]
        [InlineData("x^i + x")]
        [InlineData("x^i + x^(2*i)")]
        public void AComplexExponentOfXTerminates(string expression)
        {
            if (Solved(expression + " = 0") is not Entity.Set.FiniteSet roots)
                return;
            Assert.NotEmpty(roots);
            foreach (var root in roots)
            {
                var residual = expression.ToEntity().Substitute("x", root).EvalNumerical();
                var value = Assert.IsAssignableFrom<Entity.Number.Complex>(residual);
                Assert.True(value.IsFinite && NumericsComplex.Abs(value.ToNumerics()) <= 1e-9 * Math.Max(1, NumericsComplex.Abs(Value(root))),
                    $"{root} leaves {residual} in {expression} = 0");
            }
        }
    }
}
