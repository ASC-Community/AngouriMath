//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading;
using GenTensor = GenericTensor.Core.GenTensor<AngouriMath.Entity, AngouriMath.Entity.Matrix.EntityTensorWrapperOperations>;

namespace AngouriMath.Functions
{
    /// <summary>
    /// Makes the matrix operations that go through GenericTensor safe to call from more than one
    /// thread, which <see cref="MathS.Multithreading"/> documents as supported.
    /// </summary>
    /// <remarks>
    /// GenericTensor 1.0.4 keeps two process-wide mutable statics, and neither fails loudly.
    /// Reported upstream as https://github.com/asc-community/GenericTensor/issues/40 with a fix in
    /// https://github.com/asc-community/GenericTensor/pull/41; this type is what stands in until a
    /// release carrying that fix exists, and it should be deleted when one does.
    ///
    /// The two need opposite treatments, because only one of them can be closed without a lock:
    ///
    /// <b>The scratch-matrix pool</b> behind <c>DeterminantLaplace</c> and <c>Adjoint</c> hands
    /// every caller the same tensor for a given size, and the caller writes into it. There is
    /// nothing to warm and no way to avoid it from out here, so those calls take
    /// <see cref="ScratchPool"/>. Measured on GenericTensor's own suite, sixty 5x5 matrices
    /// computed sequentially and then again in parallel: 53 of 60 Laplace determinants and 60 of
    /// 60 adjugates came back with different values. Serialising them is the cost of not doing
    /// that.
    ///
    /// <b>The compiled-operation cache</b> behind the piecewise operators is an unsynchronised
    /// <c>Dictionary</c>, so it is only unsafe while it is being filled -- once an entry is there
    /// the reads are pure. That one needs no lock at all, because the set of entries this library
    /// can ever ask for is fixed and tiny: <see cref="Entity.Matrix"/> rejects any tensor that is
    /// not rank 2, and every call here uses the default single-threaded mode, so the only keys
    /// reachable are addition, subtraction and multiplication at rank 2. Filling all three once,
    /// under <see cref="Lazy{T}"/>, leaves the dictionary read-only from then on.
    /// </remarks>
    internal static class GenTensorGuard
    {
        /// <summary>
        /// Held across any call that reaches GenericTensor's shared scratch-matrix pool -- which
        /// is <c>DeterminantLaplace</c>, <c>Adjoint</c>, and <c>InvertMatrix</c>, since inverting
        /// goes through the adjugate. <c>DeterminantGaussianSafeDivision</c> and
        /// <c>MatrixMultiply</c> do not touch the pool and are deliberately not covered.
        /// </summary>
        /// <remarks>
        /// It is private, and the three operations below are the whole surface, because mutual
        /// exclusion here is not a property any one caller can hold up: a single call that skips
        /// the lock corrupts every call that takes it, and reads corrupted minors back. Exposing
        /// the lock lets a caller reach GenericTensor without it; exposing only the operations
        /// does not.
        /// </remarks>
        [ConstantField] private static readonly object ScratchPool = new object();

        /// <summary>
        /// Laplace expansion, serialised on <see cref="ScratchPool"/>. <see cref="Entity.Matrix.Determinant"/>
        /// has its own expansion now, which a native build can publish; this is GenericTensor's,
        /// kept as the independent one the tests compare it with.
        /// </summary>
        internal static Entity DeterminantLaplace(GenTensor matrix)
        {
            lock (ScratchPool)
                return matrix.DeterminantLaplace();
        }

        /// <summary>
        /// The adjugate, serialised on <see cref="ScratchPool"/>, and returned already read out
        /// of the tensor rather than as one.
        /// </summary>
        /// <remarks>
        /// Reading it out is what the lock has to cover, and returning a <c>GenTensor</c> would
        /// put that read on the far side of the lock: <c>Entity.Matrix</c>'s constructor copies,
        /// so the copy is where the elements are actually touched, and nothing here documents
        /// whether the adjugate is a fresh tensor or the pool's own.
        /// </remarks>
        internal static Entity Adjoint(GenTensor matrix)
        {
            lock (ScratchPool)
                return new Entity.Matrix(matrix.Adjoint()).InnerSimplified;
        }

        /// <summary>
        /// Inversion in place, serialised on <see cref="ScratchPool"/> -- it goes through the
        /// adjugate, so it takes its minors in the same pool.
        /// </summary>
        internal static void InvertMatrix(GenTensor matrix)
        {
            lock (ScratchPool)
                matrix.InvertMatrix();
        }

        [ConstantField] private static readonly Lazy<bool> piecewiseCache =
            new Lazy<bool>(WarmPiecewiseCache, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Call before any piecewise operator. The first caller fills GenericTensor's compiled-
        /// operation cache single-threaded; everyone after that pays a read of an already-computed
        /// <see cref="Lazy{T}"/>.
        /// </summary>
        internal static void EnsurePiecewiseCacheWarmed() => _ = piecewiseCache.Value;

        private static bool WarmPiecewiseCache()
        {
            // Rank 2, because that is the only rank Entity.Matrix admits, and one element, because
            // the point is to insert the cache entry rather than to compute anything. The results
            // are discarded.
            var seed = new GenTensor(1, 1);
            seed.SetValueNoCheck(Entity.Number.Integer.Zero, 0, 0);
            GenTensor.PiecewiseAdd(seed, seed);
            GenTensor.PiecewiseSubtract(seed, seed);
            GenTensor.PiecewiseMultiply(seed, seed);
            return true;
        }
    }
}
