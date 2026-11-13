//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Extensions;
using AngouriMath.Functions.Algebra.NumericalSolving;
using PeterO.Numbers;

namespace AngouriMath.Functions.Algebra.NumericalSolving
{
    using static Entity.Number;
    using NumericsComplex = System.Numerics.Complex;
    internal static class NewtonSolver
    {
        /// <summary>Performs a grid search with each iteration done by NewtonIter</summary>
        /// <param name="expr">The equation with one variable to be solved</param>
        /// <param name="v">The variable to solve over</param>
        /// <param name="settings">
        /// Some settings regarding how we should perform the Newton solver process
        /// A complex number, thus, if stepCount.Im == 0, no operations will be performed at all. If you
        /// need to iterate over real numbers only, set it to 1, i. e. new Number(your_number, 1)
        /// How many approximations we need to do before we reach the most precise result.
        /// </param>
        internal static HashSet<Complex> SolveNt(Entity expr, Entity.Variable v, MathS.Settings.NewtonSetting settings)
            => Search(expr, v, settings, forTheSolver: false)!;

        /// <summary>
        /// <see cref="SolveNt(Entity, Entity.Variable, MathS.Settings.NewtonSetting)"/> for the
        /// solver's last resort, which answers a set: <see langword="null"/> where the expression
        /// or its derivative holds a node the compiler has no form for, where that method throws
        /// <see cref="Core.Exceptions.UncompilableNodeException"/> -- an unevaluated derivative,
        /// integral or limit binds the variable and has no value at a point -- and where the
        /// roots it finds are not isolated, so that they are samples of a continuum rather than
        /// the set. https://github.com/asc-community/AngouriMath/issues/1603
        /// </summary>
        internal static HashSet<Complex>? TrySolveNt(Entity expr, Entity.Variable v, MathS.Settings.NewtonSetting settings)
            => Search(expr, v, settings, forTheSolver: true);

        private static HashSet<Complex>? Search(Entity expr, Entity.Variable v, MathS.Settings.NewtonSetting settings, bool forTheSolver)
        {
            // Perform one iteration of searching for a root with Newton-Raphson method
            static Complex NewtonIter(FastExpression f, FastExpression df, NumericsComplex value, int precision)
            {
                var prev = value;

                // Converged means the last step was small beside the point, not only small: near a
                // zero that every term shares, t ^ ln(-2) + t ^ ln(4) at t = 0, the iteration crawls
                // and its steps fall under any fixed tolerance while it is still a fifth of the way
                // from where it stops, and those points came back as roots. So below 1 the step is
                // measured against the point; from 1 up the tolerance is what it always was.
                // https://github.com/asc-community/AngouriMath/issues/2424
                NumericsComplex ChooseGood()
                {
                    var tolerance = (double)MathS.Settings.PrecisionErrorCommon.Value;
                    var scale = Math.Min(1, Math.Max(NumericsComplex.Abs(value), tolerance));
                    return NumericsComplex.Abs(prev - value) > tolerance * scale ? double.NaN : value;
                }

                int minCheckIters = (int)Math.Sqrt(precision);
                for (int i = 0; i < precision; i++)
                {
                    if (i == precision - 1)
                        prev = value;//.Copy();
                    var dfv = df.Substitute(value);
                    if (dfv == 0)
                        return ChooseGood();
                    value -= f.Substitute(value) / dfv;
                    if (i > minCheckIters && prev == value)
                        return value;
                }
                return ChooseGood();
            }
            if (expr.Vars.Single() != v)
                throw new Core.Exceptions.WrongNumberOfArgumentsException($"{nameof(expr)} should only contain {nameof(Entity.Variable)} {nameof(v)}");

            using var _ = MathS.Settings.FloatToRationalIterCount.Set(0);
            var res = new HashSet<Complex>();
            var derivative = WithoutConditions(expr.Differentiate(v).Simplify());
            var function = WithoutConditions(expr.Simplify());
            if (forTheSolver && !(derivative.HasCompiledForm && function.HasCompiledForm))
                return null;
            var df = derivative.Compile(v);
            var f = function.Compile(v);
            void IterateFrom(NumericsComplex start)
            {
                var root = NewtonIter(f, df, start, settings.Precision);
                if (root.IsFinite && f.Call(root.ToNumerics()).ToNumber().Abs() <
                    MathS.Settings.PrecisionErrorCommon.Value)
                    res.Add(root);
            }
            // The shares are worked out by CtxDivide, which rounds to the working precision,
            // rather than by EDecimal's own operator, which has no context and answers NaN
            // whenever the quotient does not terminate in base ten. Only step counts of the
            // form 2^a * 5^b divide exactly, so for 3, 6, 7, 9, 12, 21 and most other values
            // every share but the first came out NaN, and with it every starting point: the
            // grid quietly shrank to the single corner where x and y are both zero. The
            // default of 10 divides exactly, which is why this went unseen -- and why asking
            // for a finer search made the answer worse rather than better. x^3 - 2x gives all
            // three of its roots at 10 steps and only one at 21.
            for (int x = 0; x < settings.StepCount.Re; x++)
                for (int y = 0; y < settings.StepCount.Im; y++)
                {
                    var xShare = CtxDivide((EDecimal)x, settings.StepCount.Re);
                    var yShare = CtxDivide((EDecimal)y, settings.StepCount.Im);
                    var value = Complex.Create(
                        settings.From.Re * xShare + settings.To.Re * (1 - xShare),
                        settings.From.Im * yShare + settings.To.Im * (1 - yShare));
                    IterateFrom(value.ToNumerics());
                }
            foreach (var start in RealSignChanges(f, settings))
                IterateFrom(start);
            // Rounded and collapsed first, then verified: both cut down the duplicates the
            // grid search produces, so there is less to verify, and what gets verified is
            // the values actually handed back.
            var distinct = OnePerRoot(WithoutIterationNoise(res), f);
            distinct.RemoveWhere(root => !Satisfies(expr, v, root));
            // A root the search finds is one of a finite set only where it is isolated. Where the
            // equation holds midway between two of them as well, it holds along the segment, and
            // the points the grid converged to are samples of the solutions and not the set:
            // `abs(x) - x = 0` came back as fifty-one points of [0, 10], and `max(x, 0) = x`, once
            // `max` compiled, did too. The solver leaves such an equation unsolved; asked for by
            // name, the search still returns the points it found.
            // https://github.com/asc-community/AngouriMath/issues/1420
            if (forTheSolver && !AreIsolated(distinct, f))
                return null;
            return distinct;
        }

        /// <summary>
        /// Whether no two of the real <paramref name="roots"/> that are more than a thousandth
        /// apart have the equation holding midway between them as well.
        /// </summary>
        private static bool AreIsolated(HashSet<Complex> roots, FastExpression f)
        {
            var reals = roots
                .Where(root => root.ImaginaryPart.IsZero)
                .Select(root => root.RealPart.EDecimal.ToDouble())
                .OrderBy(root => root)
                .ToList();
            for (var i = 1; i < reals.Count; i++)
                if (reals[i] - reals[i - 1] > 1e-3
                    && f.Call(new NumericsComplex((reals[i - 1] + reals[i]) / 2, 0)).ToNumber().Abs() < MathS.Settings.PrecisionErrorCommon.Value)
                    return false;
            return true;
        }

        /// <summary>
        /// How much of the value at a real point may be imaginary before the expression
        /// is taken not to be real-valued there, relative to the real part's own size.
        /// </summary>
        private const double NotRealHere = 1e-9;

        /// <summary>
        /// Extra starting points, read off the sign changes of the expression along the
        /// real axis.
        /// </summary>
        /// <remarks>
        /// The grid is two-dimensional, so its resolution along the real axis is only the
        /// square root of what it costs: the default 10 x 10 lays real starting points 2
        /// apart, and two roots closer together than that share one, so which of them gets
        /// reached is left to where the iteration happens to go. A polynomial usually
        /// survives that, its basins being interleaved across the whole plane -- the roots
        /// of <c>x*(x - 1/2)*(x + 1/2)</c> all come back. An expression that is only real
        /// on a small interval does not: outside <c>[-1, 1]</c> every starting point hands
        /// <c>arcsin</c> a complex value and the iteration wanders off. That is
        /// https://github.com/asc-community/AngouriMath/issues/115 -- <c>arcsin(x) - x*pi/3</c>
        /// has roots at -1/2, 0 and 1/2, and only 0 came back. It is not a matter of
        /// iterating harder: all three were already inside the region being searched.
        /// <para/>
        /// A sign change is a far cheaper witness of a root than a Newton run is -- one
        /// evaluation against the sixty an iteration to precision 30 costs. So the scan can
        /// afford as many points as the whole grid has, <see cref="MathS.Settings.NewtonSetting.StepCount"/>
        /// multiplied out, which lays them 0.2 apart by default rather than 2, and it costs
        /// a couple of percent of what the grid already spends. Newton then runs only from
        /// the brackets, of which a well-behaved expression has a handful.
        /// <para/>
        /// This is additive, not a replacement. A sign change witnesses a root of odd
        /// multiplicity on an interval where the expression is real; it says nothing about
        /// a repeated root like <c>x^2</c>, nor about any root off the real axis. Those stay
        /// the grid's to find, and the grid is left as it was.
        /// </remarks>
        private static IEnumerable<NumericsComplex> RealSignChanges(
            FastExpression f, MathS.Settings.NewtonSetting settings)
        {
            // The value at a real point, or null where the expression is not real-valued
            // there. arcsin outside [-1, 1] is the case that matters: half the default
            // region is outside the domain of the reporter's own equation, and the
            // intermediate value theorem has nothing to say about a complex value.
            double? RealValueAt(double at)
            {
                var value = f.Call(new NumericsComplex(at, 0));
                if (double.IsNaN(value.Real) || double.IsInfinity(value.Real)
                    || double.IsNaN(value.Imaginary) || double.IsInfinity(value.Imaginary))
                    return null;
                return Math.Abs(value.Imaginary)
                    > NotRealHere * (1 + Math.Abs(value.Real)) ? null : value.Real;
            }
            var count = (long)settings.StepCount.Re * settings.StepCount.Im;
            if (count < 2)
                yield break;
            var from = settings.From.Re.ToDouble();
            var to = settings.To.Re.ToDouble();
            var previousAt = double.NaN;
            double? previous = null;
            for (long i = 0; i <= count; i++)
            {
                var share = (double)i / count;
                var at = from * share + to * (1 - share);
                var value = RealValueAt(at);
                if (value is 0d)
                    yield return new NumericsComplex(at, 0);
                else if (value is { } here && previous is { } there
                    && (here < 0) != (there < 0))
                    // The bracket halved down to the root, rather than its midpoint: the
                    // bracket holds a root and Newton converges fastest from as near it as we
                    // can say it is. From the midpoint it could not converge at all where the
                    // slope is unbounded inside the bracket -- (x^2 - 3x - 3)^(1/3)
                    // + sqrt(3x - 4) = 3 has its root at 3.7962, past the cube root's zero at
                    // 3.7913, and from 3.7 the iteration went off the real line and its one
                    // real root was never answered. A pole between the two is halved down to as
                    // well, and its value there rejects it as it did before.
                    yield return new NumericsComplex(Halved(previousAt, there, at, RealValueAt), 0);
                previousAt = at;
                previous = value;
            }
        }

        /// <summary>
        /// A point of <c>[low, high]</c>, or <c>[high, low]</c>, as near a sign change of
        /// <paramref name="valueAt"/> as halving the interval gets in double precision, given
        /// the value at <paramref name="low"/>. Stops where a midpoint has no real value.
        /// </summary>
        private static double Halved(double low, double atLow, double high, Func<double, double?> valueAt)
        {
            for (var step = 0; step < 64; step++)
            {
                var middle = (low + high) / 2;
                if (middle == low || middle == high || valueAt(middle) is not { } here)
                    return middle;
                if (here == 0)
                    return middle;
                if ((here < 0) == (atLow < 0))
                    (low, atLow) = (middle, here);
                else
                    high = middle;
            }
            return (low + high) / 2;
        }

        /// <summary>
        /// How far apart two candidates may be, relative to their own size, and still be
        /// the same root.
        /// </summary>
        /// <remarks>
        /// The search starts from a grid and iterates in double precision, so the same root
        /// reached from different starting points comes back agreeing to about sixteen
        /// significant digits and differing after that. Left as they were, those were
        /// counted as separate answers: x^5 + 3x + 1 came back with 28 roots and x^6 + x + 1
        /// with 23, four of the 28 being -0.83907243306660750, -0.83907243306660761,
        /// -0.83907243306660773 and -0.83907243306660784.
        ///
        /// The measured room is wide. Candidates for one root lie within 1e-15 of each other
        /// relative to their size, while the nearest two distinct roots over the twelve
        /// polynomials tried are 0.42 apart. This is 1e-13: a hundred times above the noise
        /// and still twelve orders below the closest two roots that were ever told apart.
        /// </remarks>
        [ConstantField] private static readonly EDecimal SameRootTolerance = EDecimal.Create(1, -13);

        private static bool AreTheSameRoot(Complex a, Complex b)
        {
            var apart = EDecimal.Max(
                a.RealPart.EDecimal.Subtract(b.RealPart.EDecimal).Abs(),
                a.ImaginaryPart.EDecimal.Subtract(b.ImaginaryPart.EDecimal).Abs());
            var size = EDecimal.Max(EDecimal.One, EDecimal.Max(
                a.RealPart.EDecimal.Abs(), a.ImaginaryPart.EDecimal.Abs()));
            return !apart.GreaterThan(size.Multiply(SameRootTolerance));
        }

        /// <summary>
        /// One candidate per root, keeping whichever of each group leaves the equation
        /// closest to zero. Ordered first, so that which candidate is compared against
        /// which does not depend on the order the grid happened to produce them in.
        /// </summary>
        private static HashSet<Complex> OnePerRoot(HashSet<Complex> roots, FastExpression f)
        {
            // A residual that is not a finite number ranks last.
            EDecimal Residual(Complex root)
            {
                var residual = f.Call(root.ToNumerics());
                return double.IsNaN(residual.Real) || double.IsInfinity(residual.Real)
                    || double.IsNaN(residual.Imaginary) || double.IsInfinity(residual.Imaginary)
                    ? EDecimal.PositiveInfinity
                    : residual.ToNumber().Abs().EDecimal;
            }
            var kept = new List<Complex>();
            foreach (var root in roots
                .OrderBy(root => root.RealPart.EDecimal)
                .ThenBy(root => root.ImaginaryPart.EDecimal))
            {
                var same = kept.FindIndex(other => AreTheSameRoot(other, root));
                if (same < 0)
                    kept.Add(root);
                else if (Residual(root).CompareTo(Residual(kept[same])) < 0)
                    kept[same] = root;
            }
            return new HashSet<Complex>(kept);
        }

        /// <summary>
        /// Rounds away the part of each root that the iteration cannot have got right.
        /// </summary>
        /// <remarks>
        /// The iteration works in <see cref="System.Numerics.Complex"/>, so a
        /// component below roughly 1e-15 of the root is noise from the iteration and not
        /// part of the answer. It matters because the search starts from a grid: the same
        /// root reached from different starting points differs in those last digits, so
        /// left in, the one real root of <c>x^5 + 3x + 1</c> comes back as four different
        /// complex numbers with imaginary parts around 1e-19. Rounded, they are one root
        /// again, and the set collapses them.
        /// </remarks>
        private static HashSet<Complex> WithoutIterationNoise(HashSet<Complex> roots)
        {
            using var _ = MathS.Settings.PrecisionErrorZeroRange.Set(EDecimal.Create(1, -15));
            return new HashSet<Complex>(roots.Select(root =>
                Complex.Create(root.RealPart.EDecimal, root.ImaginaryPart.EDecimal)));
        }

        /// <summary>
        /// Strips the domain conditions off an expression. Simplification leaves them
        /// behind -- log(a) + log(b) only collapses to log(a * b) where both are positive,
        /// for one -- and the compiler has no way to represent an <see cref="Entity.Providedf"/>,
        /// so <c>x + ln(x) = 0</c> used to come out of the public solver as an
        /// <see cref="Core.Exceptions.UncompilableNodeException"/>. Newton iterates on the
        /// unconditioned expression and <see cref="Satisfies"/> re-imposes the conditions.
        /// </summary>
        private static Entity WithoutConditions(Entity expr)
            => expr.Replace(node => node is Entity.Providedf(var inner, _) ? inner : node);

        /// <summary>
        /// Whether a candidate root really is one, checked against the expression as it
        /// was handed in rather than against the simplified form Newton iterated on.
        /// Simplification can widen the domain: <c>ln(x) + ln(x+1)</c> becomes
        /// <c>ln(x * (x+1))</c>, whose root -1.618... leaves the original at 2*pi*i, and
        /// the solver reported it as a root of the original equation.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> for anything that cannot be checked, so that a root is
        /// only ever dropped on positive evidence that it is spurious. A point where the
        /// expression is undefined at exact arguments is such evidence: the compiled form
        /// takes <c>0^i</c> to be 0, and <c>x^i + x</c> was answered with the root 0.
        /// </returns>
        private static bool Satisfies(Entity expr, Entity.Variable v, Complex root)
        {
            var substituted = expr.Substitute(v, root);
            if (AnalyticalSolving.StatementSolver.IsUndefinedExactly(substituted))
                return false;
            return substituted.Evaled is not Complex residual
                || !residual.IsFinite
                || residual.Abs().EDecimal.LessThan(MathS.Settings.PrecisionErrorCommon);
        }
    }
}

namespace AngouriMath
{
    partial record Entity : ILatexizeable
    {
        /// <summary>
        /// Searches for numerical solutions via Newton's method
        /// <a href="https://en.wikipedia.org/wiki/Newton%27s_method"/>
        /// To change parameters see <see cref="MathS.Settings.NewtonSolver"/>
        /// </summary>
        public HashSet<Number.Complex> SolveNt(Variable v) =>
            NewtonSolver.SolveNt(this, v, MathS.Settings.NewtonSolver);
    }
}