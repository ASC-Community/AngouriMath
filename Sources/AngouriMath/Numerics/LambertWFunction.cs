//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Numerics
{
    /// <summary>
    /// The Lambert W function in double precision, for the compilers, on the branches the interpreter
    /// takes: those of Corless et al. (1996), <c>W_0</c> real on <c>[-1/e, +oo)</c> and <c>W_(-1)</c> on
    /// <c>[-1/e, 0)</c>, <c>NaN</c> on a branch that is not whole, and <c>-oo</c> at 0 off the principal one.
    /// https://github.com/asc-community/AngouriMath/issues/2195
    /// </summary>
    internal static class LambertWFunction
    {
        [AngouriMath.Core.ConstantField] private static readonly NumericsComplex NaN = new(double.NaN, 0);

        public static NumericsComplex LambertW(NumericsComplex branch, NumericsComplex z)
        {
            if (branch.Imaginary != 0 || double.IsNaN(branch.Real) || double.IsInfinity(branch.Real)
                || Math.Floor(branch.Real) != branch.Real || Math.Abs(branch.Real) > int.MaxValue)
                return NaN;
            if (double.IsNaN(z.Real) || double.IsNaN(z.Imaginary) || double.IsInfinity(z.Real) || double.IsInfinity(z.Imaginary))
                return NaN;
            var k = (int)branch.Real;
            if (z == 0)
                return k == 0 ? 0 : double.NegativeInfinity;
            // Without a signed zero, which a computation can leave behind: z on the axis is approached from above.
            if (z.Imaginary == 0)
                z = new NumericsComplex(z.Real, 0);

            var nearTheBranchPoint = Math.E * z + 1;
            // W_0 from either side, W_(-1) from above and on the axis, W_1 from below meet at -1/e.
            var meetsTheBranchPoint = k == 0 || k == -1 && z.Imaginary >= 0 || k == 1 && z.Imaginary < 0;
            // -1/e is not a double, and within its rounding the value is the branch point's.
            if (meetsTheBranchPoint && NumericsComplex.Abs(nearTheBranchPoint) < 1e-15)
                return -1;
            var isReal = z.Imaginary == 0 && nearTheBranchPoint.Real >= 0 && (k == 0 || k == -1 && z.Real < 0);

            // The start, by the regions SciPy's lambertw takes on the principal branch: the series about the
            // branch point within 0.3 of it (and within a quarter of it on the other branches that meet
            // there), ln(1 + z) in a box right of -1 about the axis, and the asymptotic start elsewhere. ln(1 + z)
            // is no start near -1, where it is ln 0 and its iteration reaches another branch, nor on the axis
            // from -1 to -1/e, where it is real and W_0 is not.
            NumericsComplex w;
            var branchPointSeries = k == 0 ? NumericsComplex.Abs(z + 1 / Math.E) < 0.3 : meetsTheBranchPoint && NumericsComplex.Abs(nearTheBranchPoint) < 0.25;
            if (branchPointSeries)
            {
                // p = sqrt(2 (e z + 1)), and w = -1 + p - p^2/3 + 11 p^3/72 on W_0, -p on the other branch.
                var p = NumericsComplex.Sqrt(2 * nearTheBranchPoint);
                if (k != 0)
                    p = -p;
                w = -1 + p - p * p / 3 + 11 * p * p * p / 72;
            }
            else if (k == 0 && z.Real > -1 && z.Real < 1.5 && Math.Abs(z.Imaginary) < 1 && z.Real > -2.5 * Math.Abs(z.Imaginary) - 0.2)
                w = NumericsComplex.Log(1 + z);
            else
            {
                var l1 = NumericsComplex.Log(z) + new NumericsComplex(0, 2 * Math.PI * k);
                var l2 = NumericsComplex.Log(l1);
                w = l1 - l2 + l2 / l1;
            }
            if (isReal)
                w = new NumericsComplex(w.Real, 0);
            for (var step = 0; step < 64; step++)
            {
                // Halley: w - f / (e^w (w + 1) - (w + 2) f / (2 w + 2)), with f = w e^w - z.
                var ew = NumericsComplex.Exp(w);
                var f = w * ew - z;
                if (w + 1 == 0)
                    break;
                var change = f / (ew * (w + 1) - (w + 2) * f / (2 * w + 2));
                w -= change;
                if (isReal)
                    w = new NumericsComplex(w.Real, 0);
                if (NumericsComplex.Abs(change) <= 1e-15 * Math.Max(NumericsComplex.Abs(w), 1))
                    break;
            }
            return w;
        }

        // A real value where the branch is real there, and NaN where it is not.
        public static double LambertW(double branch, double z)
        {
            var w = LambertW(new NumericsComplex(branch, 0), new NumericsComplex(z, 0));
            return w.Imaginary == 0 ? w.Real : double.NaN;
        }
    }
}
