//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using System.Numerics;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A compiled <c>provided</c> or <c>piecewise</c> computes the case it takes and no other, as
    /// evaluation does. Both compilers computed every case and kept one, so that
    /// <c>piecewise(primepi(x) provided x &lt; 100, 0)</c> at 9999991 sieved ten million numbers for a
    /// value it threw away: 18 ms a call, where evaluation takes 13 µs.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2195">#2195</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class BranchNotTakenCompilationTest
    {
        public static readonly TheoryData<string> Expressions = new()
        {
            // A subexpression met first in a case that is not taken, and again where it is computed.
            "piecewise(sin(x) provided x > 0, 0) + sin(x)",
            "sin(x) + piecewise(sin(x) provided x > 0, 0)",
            "piecewise(sin(x) provided x > 1, sin(x) + 1 provided x > 0, sin(x) + 2)",
            "(cos(x) provided x > 0) + piecewise(cos(x) provided x < 0, 1)",
            // One shared by a predicate and the cases after it.
            "piecewise(x^2 provided x^2 > 1, x^2 + 1)",
            "piecewise(1 provided x^3 > 8, x^3 provided x^3 > 0, x^3 - 1)",
            // Nested.
            "piecewise(piecewise(cos(x) provided x > 2, cos(x) + 1) provided x > 0, cos(x) - 1) * cos(x)",
            "(x provided x > 0) provided x < 2",
            // A case with no value where it is not taken.
            "piecewise(1/x provided not x = 0, 0)",
            "piecewise(ln(x) provided x > 0, ln(-x) provided x < 0, 0) + x",
            "piecewise(sqrt(x - 1) provided x >= 1, sqrt(1 - x))",
            "x provided x > 1", "iverson(x > 0) * piecewise(x provided x > 1, 2 x)",
        };

        // Real, negative, complex and NaN, and each visited twice in both orders, so that a value
        // a call leaves behind cannot pass for the next call's.
        private static readonly (Entity At, Complex Value)[] points =
        {
            (2, 2), ("1/2", 0.5), (0, 0), ("-13/10", -1.3), (-3, -3), ("i", Complex.ImaginaryOne),
            ("1 + i", new(1, 1)), ("-1 + 2i", new(-1, 2)), (MathS.NaN, double.NaN),
        };

        private static Complex Interpreted(string expression, Entity at)
            => expression.ToEntity().Substitute("x", at).EvalNumerical().ToNumerics();

        private static bool Agrees(Complex expected, Complex actual)
            => double.IsNaN(expected.Real) || double.IsNaN(expected.Imaginary)
                ? double.IsNaN(actual.Real) || double.IsNaN(actual.Imaginary)
                : Complex.Abs(expected - actual) <= 1e-12 * Math.Max(1, Complex.Abs(expected));

        private static void AgreesAtEveryPoint(string expression, Func<Complex, Complex> compiled, string compiler)
        {
            foreach (var order in new[] { points, points.AsEnumerable().Reverse().ToArray(), points })
                foreach (var (at, value) in order)
                {
                    var expected = Interpreted(expression, at);
                    var actual = compiled(value);
                    Assert.True(Agrees(expected, actual), $"{compiler}: {expression} at {at}: {expected} interpreted, {actual} compiled");
                }
        }

        [Theory]
        [MemberData(nameof(Expressions))]
        public void FastExpressionIsWhatEvaluationSays(string expression)
        {
            var compiled = expression.ToEntity().Compile("x");
            AgreesAtEveryPoint(expression, value => compiled.Call(value), "FastExpression");
        }

        [Theory]
        [MemberData(nameof(Expressions))]
        public void LinqOverTheComplexNumbersIsWhatEvaluationSays(string expression)
        {
            var compiled = expression.ToEntity().Compile<Complex, Complex>("x");
            AgreesAtEveryPoint(expression, value => compiled(value), "LINQ over Complex");
        }

        [Theory]
        [MemberData(nameof(Expressions))]
        public void LinqOverTheRealsIsWhatEvaluationSaysWhereItIsReal(string expression)
        {
            var compiled = expression.ToEntity().Compile<double, double>("x");
            foreach (var (at, value) in points)
            {
                if (value.Imaginary != 0)
                    continue;
                var expected = Interpreted(expression, at);
                if (expected.Imaginary != 0)
                    expected = double.NaN;
                var actual = compiled(value.Real);
                Assert.True(Agrees(expected, actual), $"LINQ over double: {expression} at {at}: {expected} interpreted, {actual} compiled");
            }
        }

        // primepi sieves up to its argument, and at 9999991 the sieve is 5 million bits, 625 KB, on every
        // call. What a call allocates is the measure, not how long it takes, which is the machine's.
        public static readonly TheoryData<string> NotTakenAt9999991 = new()
        {
            "piecewise(primepi(x) provided x < 100, 0)",
            "primepi(x) provided x < 100",
            "piecewise(1 provided x > 100, primepi(x) provided primepi(x) > 0, 2)",
            "piecewise(piecewise(primepi(x) provided x < 50, 1) provided x < 100, 2) + 1",
        };

        private const double Untaken = 9999991;
        private const long WellBelowTheSieve = 64 * 1024;

        private static long Allocated(Func<Complex> call)
        {
            call();
            var before = GC.GetAllocatedBytesForCurrentThread();
            call();
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        [Theory]
        [MemberData(nameof(NotTakenAt9999991))]
        public void FastExpressionDoesNotComputeACaseItDoesNotTake(string expression)
        {
            var compiled = expression.ToEntity().Compile("x");
            Assert.True(Agrees(Interpreted(expression, Untaken), compiled.Call(Untaken)));
            var bytes = Allocated(() => compiled.Call(Untaken));
            Assert.True(bytes < WellBelowTheSieve, $"{expression} at {Untaken} allocated {bytes} bytes");
        }

        [Theory]
        [MemberData(nameof(NotTakenAt9999991))]
        public void LinqDoesNotComputeACaseItDoesNotTake(string expression)
        {
            var overComplex = expression.ToEntity().Compile<Complex, Complex>("x");
            var overReals = expression.ToEntity().Compile<double, double>("x");
            Assert.True(Agrees(Interpreted(expression, Untaken), overComplex(Untaken)));
            Assert.True(Agrees(Interpreted(expression, Untaken), overReals(Untaken)));
            var bytes = Allocated(() => overComplex(Untaken));
            Assert.True(bytes < WellBelowTheSieve, $"{expression} over Complex at {Untaken} allocated {bytes} bytes");
            bytes = Allocated(() => overReals(Untaken));
            Assert.True(bytes < WellBelowTheSieve, $"{expression} over double at {Untaken} allocated {bytes} bytes");
        }

        /// <summary>Where the case is taken, it is computed: the sieve is there.</summary>
        [Fact]
        public void ACaseThatIsTakenIsComputed()
        {
            var compiled = "piecewise(primepi(x) provided x > 100, 0)".ToEntity().Compile("x");
            Assert.Equal(new Complex(664579, 0), compiled.Call(Untaken));
            Assert.True(Allocated(() => compiled.Call(Untaken)) > WellBelowTheSieve);
        }
    }
}
