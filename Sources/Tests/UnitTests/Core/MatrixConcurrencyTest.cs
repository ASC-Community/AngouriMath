//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// A matrix operation asked for on one thread and then on many has to give the same answer.
    /// <see cref="MathS.Multithreading"/> documents concurrent use as supported, so this is a
    /// claim about the public surface rather than about the test runner.
    ///
    /// These bind against GenericTensor's process-wide statics, reported as
    /// https://github.com/asc-community/GenericTensor/issues/40 and worked around in
    /// <c>Functions.GenTensorGuard</c>. Measured on the commit before that guard: 38 of 40
    /// determinants, 18 of 40 inverses and 11 of 40 adjugates came back with different values,
    /// and the elementwise operators threw on a corrupted dictionary. The first three fail by
    /// returning a wrong entity rather than by throwing, so those assertions count disagreements
    /// instead of stopping at the first one.
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class MatrixConcurrencyTest
    {
        private const int Matrices = 40;

        /// <summary>
        /// The entries are deliberately not polynomial. <c>Determinant</c> tries
        /// <c>PolynomialDeterminant.Of</c> first and only falls back to GenericTensor's Laplace
        /// where that declines, so a matrix of polynomials never reaches the shared scratch buffer
        /// -- which is exactly why the first version of this test found nothing.
        /// </summary>
        private static Matrix Build(int seed)
        {
            var random = new Random(seed);
            var a = MathS.Var("a");
            return MathS.Matrices.Matrix(4, 4,
                MathS.Sin(a) * random.Next(1, 4), random.Next(-3, 4), MathS.Cos(a), random.Next(-3, 4),
                random.Next(-3, 4), MathS.Cos(a) * random.Next(1, 4), random.Next(-3, 4), MathS.Sin(a),
                MathS.Sin(a), random.Next(-3, 4), MathS.Sin(a) * random.Next(1, 4), random.Next(-3, 4),
                random.Next(-3, 4), MathS.Sin(a), random.Next(-3, 4), MathS.Cos(a) * random.Next(1, 4));
        }

        /// <summary>
        /// Runs <paramref name="operation"/> over freshly built matrices sequentially, then again
        /// in parallel, and counts the ones whose answers differ.
        ///
        /// The matrices are rebuilt inside each pass, and that is load-bearing. Determinant,
        /// Inverse and Adjugate are all cached lazy properties, so reusing the instances would let
        /// the parallel pass read the values the sequential pass had already computed. That test
        /// passes against broken code because it never runs the operation twice.
        /// </summary>
        private static int Disagreements(Func<Matrix, Entity?> operation)
        {
            var expected = new Entity?[Matrices];
            for (var i = 0; i < Matrices; i++)
                expected[i] = operation(Build(i));

            var differed = new ConcurrentBag<int>();
            Parallel.For(0, Matrices, i =>
            {
                if (operation(Build(i)) != expected[i])
                    differed.Add(i);
            });
            return differed.Count;
        }

        [Fact]
        public void DeterminantIsTheSameOnManyThreads()
            => Assert.Equal(0, Disagreements(m => m.Determinant));

        [Fact]
        public void AdjugateIsTheSameOnManyThreads()
            => Assert.Equal(0, Disagreements(m => m.Adjugate));

        [Fact]
        public void InverseIsTheSameOnManyThreads()
            => Assert.Equal(0, Disagreements(m => m.Inverse));

        /// <summary>
        /// The elementwise operators reach a different shared static: an unsynchronised dictionary
        /// of compiled loops. It is only unsafe while it is being filled, so this one has no
        /// sequential warm-up -- a warm-up would populate every key before the threads started and
        /// pass against the unguarded code.
        /// </summary>
        [Fact]
        public void ElementwiseOperatorsAreTheSameOnManyThreads()
        {
            var results = new Entity?[Matrices];
            Parallel.For(0, Matrices, i =>
            {
                var m = Build(i);
                results[i] = (i % 3) switch
                {
                    0 => m + m,
                    1 => m - m,
                    _ => MathS.Matrices.PointwiseMultiplication(m, m)
                };
            });

            for (var i = 0; i < Matrices; i++)
            {
                var m = Build(i);
                Entity expected = (i % 3) switch
                {
                    0 => m + m,
                    1 => m - m,
                    _ => MathS.Matrices.PointwiseMultiplication(m, m)
                };
                Assert.Equal(expected, results[i]);
            }
        }
    }
}
