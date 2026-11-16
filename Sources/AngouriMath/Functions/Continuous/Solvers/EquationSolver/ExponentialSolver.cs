//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Multithreading;
using AngouriMath.Extensions;
using PeterO.Numbers;
using System;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions.Algebra.AnalyticalSolving
{
    internal static class ExponentialSolver
    {
        internal static Set? SolveLinear(Entity expr, Entity.Variable x)
        {
            if (MathS.Settings.Codomain.Value != Core.Domain.Real)
                return SolveLinearOverTheComplexNumbers(expr, x);
            var replacement = Variable.CreateTemp(expr.Vars);
            static Entity NonZeroPow(Entity @base, Entity exponent) => exponent == Integer.Zero ? Integer.One : MathS.Pow(@base, exponent);
            Entity preparator(Entity e) => e switch
            {
                Powf(var @base, var arg) when
                    TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out var b) =>
                        // Transformation base^(a*x + b) = base^b * e^(ln(base)*a*x) is safe when:
                        // - base ≠ 0 (ensures ln(base) is defined and no 0^b issues)
                        // For complex bases, ln uses the principal branch
                        @base.Evaled switch
                        {
                            Complex { IsZero: false } => NonZeroPow(@base.InnerSimplified, b) * NonZeroPow(NonZeroPow(MathS.e, x), MathS.Ln(@base.InnerSimplified) * a),
                            // If base is definitely 0, keep original form (will likely fail to solve anyway)
                            Complex { IsZero: true } => e,
                            // For symbolic bases that might be zero, add condition
                            _ => (NonZeroPow(@base, b) * NonZeroPow(NonZeroPow(MathS.e, x), MathS.Ln(@base) * a)).Provided(!@base.EqualTo(0))
                        },
                _ => e,
            };

            Entity replacer(Entity e) => e switch
            {
                Powf(var @base, var arg)
                    when @base == MathS.e && arg == x
                        => replacement,

                _ => e,
            };

            expr = expr.Replace(preparator);
            expr = expr.Replace(replacer);

            if (expr.ContainsNode(x)) return null; // cannot be solved, not a pure exponential

            expr = expr.InnerSimplified;
            if (AnalyticalEquationSolver.Solve(expr, replacement) is FiniteSet els && els.Any())
                return MathS.Pow(MathS.e, x).InvertEach(els, x);
            else
                return null;
        }

        /// <summary>
        /// A sum of powers <c>a ^ (p x + q)</c> over the complex numbers, solved as a polynomial in
        /// one exponential <c>t = e ^ (u x)</c> whose whole powers they all are.
        /// </summary>
        /// <remarks>
        /// <c>a ^ (p x + q)</c> is <c>a ^ q e ^ (c x)</c> with the rate <c>c = p ln(a)</c>. Over the
        /// reals that is <c>(e ^ x) ^ c</c>, and the equation is solved in <c>e ^ x</c>; over the
        /// complex numbers <c>(e ^ x) ^ c</c> is <c>e ^ (c ln(e ^ x))</c>, which is <c>e ^ (c x)</c>
        /// only where the imaginary part of <c>x</c> is in <c>(-pi, pi]</c> or <c>c</c> is whole.
        /// So each rate is written as a whole multiple of one unit <c>u</c>, and <c>e ^ (c x)</c>
        /// is then exactly <c>t ^ (c / u)</c>: <c>4 ^ x - 3 * 2 ^ x + 2 = 0</c> is
        /// <c>t ^ 2 - 3 t + 2 = 0</c> in <c>t = 2 ^ x</c>, and each root <c>t</c> gives the family
        /// <c>(ln(t) + 2 pi i n) / ln(2)</c>. Through <c>e ^ x</c> it gave <c>1 + 2 pi i n</c>, where
        /// <c>2 ^ x</c> is <c>2 e ^ (2 pi i n ln(2))</c> and not 2. Rates with no common unit,
        /// <c>2 ^ x + 3 ^ x = 5</c>, are no polynomial in one exponential and are declined.
        /// https://github.com/asc-community/AngouriMath/issues/2319
        /// </remarks>
        private static Set? SolveLinearOverTheComplexNumbers(Entity expr, Variable x)
        {
            var powers = new List<(Powf Node, Entity Base, Entity Slope, Entity Rate)>();
            var declined = false;
            expr.Replace(e =>
            {
                if (e is Powf(var @base, var arg) power && arg.ContainsNode(x))
                    if (!@base.ContainsNode(x) && TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _)
                        && @base.Evaled is not Complex { IsZero: true })
                        powers.Add((power, @base, a, (MathS.Ln(@base) * a).InnerSimplified));
                    else
                        declined = true;
                return e;
            });
            if (declined || powers.Count == 0)
                return null;

            // Each rate as a rational multiple of the first; their greatest common divisor is the
            // unit. A ratio that simplifies to a rational from an expression with no variables in it
            // may have gone through a decimal on the way, and is confirmed to fifty places.
            var first = powers[0].Rate;
            var multiples = new List<ERational>();
            foreach (var (_, _, _, rate) in powers)
            {
                var ratio = rate / first;
                ERational q;
                if (ratio.InnerSimplified is Rational { ERational: var exact })
                    q = exact;
                else if (!ratio.Vars.Any() && ratio.EvalNumerical() is Complex numeric
                         && SmallRationalNear(numeric.RealPart.EDecimal.ToDouble()) is { } near)
                    q = near;
                else
                    return null;
                if (!ratio.Vars.Any()
                    && ((rate - Rational.Create(q) * first).EvalNumerical() is not Complex residual
                        || residual.Abs().EDecimal.Abs().CompareTo(EDecimal.FromString("1e-50")) >= 0))
                    return null;
                multiples.Add(q);
            }
            var denominator = multiples.Select(q => q.Denominator)
                .Aggregate((p, q) => p.Divide(p.GreatestCommonDivisor(q)).Multiply(q));
            var numerator = multiples.Select(q => q.Numerator.Multiply(denominator).Divide(q.Denominator).Abs())
                .Aggregate((p, q) => p.GreatestCommonDivisor(q));
            if (numerator.IsZero)
                return null;
            var unitMultiple = ERational.Create(numerator, denominator);
            // ERational does not reduce, so each whole power is a quotient of integers.
            EInteger WholePower(ERational q)
                => q.Numerator.Multiply(denominator).Divide(q.Denominator).Divide(numerator);
            var wholePowers = new Dictionary<Entity, Entity>();
            for (var k = 0; k < powers.Count; k++)
                wholePowers[powers[k].Node] = Integer.Create(WholePower(multiples[k]));
            // One of the powers where its rate is the unit, so that 4 ^ x - 3 * 2 ^ x + 2 = 0 is
            // solved in 2 ^ x, and its roots read log(2, t), rather than in e ^ (1/2 ln(4) x).
            var unitIndex = multiples.FindIndex(q => WholePower(q).Equals(EInteger.One));
            var substitution = unitIndex >= 0
                ? MathS.Pow(powers[unitIndex].Base, powers[unitIndex].Slope * x)
                : MathS.Pow(MathS.e, (Rational.Create(unitMultiple) * first).InnerSimplified * x);

            var replacement = Variable.CreateTemp(expr.Vars);
            static Entity NonZeroPow(Entity @base, Entity exponent) => exponent == Integer.Zero ? Integer.One : MathS.Pow(@base, exponent);
            Entity InTheUnit(Entity e)
            {
                if (e is not Powf(var @base, var arg) || !wholePowers.TryGetValue(e, out var power)
                    || !TreeAnalyzer.TryGetPolyLinear(arg, x, out _, out var b))
                    return e;
                var t = NonZeroPow(replacement, power);
                return @base.Evaled is Complex { IsZero: false }
                    ? NonZeroPow(@base.InnerSimplified, b) * t
                    : (NonZeroPow(@base, b) * t).Provided(!@base.EqualTo(0));
            }
            expr = expr.Replace(InTheUnit);
            if (expr.ContainsNode(x))
                return null;

            expr = expr.InnerSimplified;
            if (AnalyticalEquationSolver.Solve(expr, replacement) is FiniteSet els && els.Any())
                return substitution.InvertEach(els, x);
            return null;
        }

        /// <summary>
        /// The rational with a denominator below a thousand nearest to <paramref name="value"/>,
        /// by its continued fraction, or <see langword="null"/> where none is within 1e-9. Only a
        /// candidate: the caller confirms it exactly.
        /// </summary>
        private static ERational? SmallRationalNear(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || System.Math.Abs(value) > 1e6)
                return null;
            EInteger h0 = EInteger.One, h1 = EInteger.Zero, k0 = EInteger.Zero, k1 = EInteger.One;
            var rest = value;
            for (var step = 0; step < 20; step++)
            {
                var whole = System.Math.Floor(rest);
                var a = EInteger.FromInt64((long)whole);
                (h0, h1) = (a.Multiply(h0).Add(h1), h0);
                (k0, k1) = (a.Multiply(k0).Add(k1), k0);
                if (k0.CompareTo(EInteger.FromInt32(1000)) > 0)
                    return null;
                if (System.Math.Abs(value - (double)h0.ToInt64Checked() / k0.ToInt64Checked()) < 1e-9)
                    return ERational.Create(h0, k0);
                if (rest - whole < 1e-12)
                    return null;
                rest = 1 / (rest - whole);
            }
            return null;
        }

        internal static Entity GetConstantOutOfLogarithm(Entity expr)
            => expr switch
            {
                Logf(var anyBase1, Powf(var anyBase2, Integer cst))
                    => cst * new Logf(anyBase1, anyBase2),
                _ => expr
            };

        /// <summary>
        /// <c>a ^ p(x) = b ^ q(x)</c> for numeric <c>a</c> and <c>b</c>, solved by taking
        /// logarithms, which keeps the answer exact.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="SolveMultiplicative"/> reaches these by substitution: it divides one
        /// exponent by the other and simplifies, and for two different integer bases that ratio
        /// is <c>ln(3)/ln(2)</c> — irrational — so <c>InnerSimplified</c> settles it to a decimal
        /// and everything downstream is numeric. The answer then agrees with the exact one to
        /// about seventeen figures and diverges after, which is a <c>double</c> promoted to a
        /// decimal rather than a number that was computed.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1007">#1007</a>
        /// </para>
        /// <para>
        /// Taking logarithms has no such step: <c>a ^ p = b ^ q</c> is <c>p ln a = q ln b</c>
        /// for positive real <c>a</c> and <c>b</c>, and that is an ordinary equation the
        /// analytical solver answers exactly — <c>3 ^ (x+1) = 2 ^ (x-1)</c> becomes
        /// <c>-ln(6) / ln(3/2)</c>.
        /// </para>
        /// <para>
        /// Both bases must be <b>decidably positive reals</b>, which is what makes <c>ln</c> of
        /// them real and the step an equivalence rather than a branch choice. Anything else
        /// declines and the substitution path still gets its turn, so this only ever adds
        /// answers.
        /// </para>
        /// </remarks>
        internal static Set? SolveTwoPowersOfNumericBases(Entity expr, Variable x)
        {
            // a ^ p - b ^ q, however the subtraction is spelled: `a - b` and `a + (-1) * b` are
            // the same tree to the solver by the time it gets here.
            // Zero terms are dropped first: the solver is handed `lhs - rhs - 0`, and the
            // trailing zero makes a two-power equation look like a three-term one.
            var terms = Entity.Sumf.LinearChildren(expr)
                .Where(term => term.Evaled is not Number number || number != 0)
                .ToList();
            if (terms.Count != 2)
                return null;
            if (MathS.Settings.Codomain.Value != Core.Domain.Real)
                return SolveTwoPowersOverTheComplexNumbers(terms[0], terms[1], x);

            var (leftBase, leftPower, leftSign) = AsPower(terms[0]);
            var (rightBase, rightPower, rightSign) = AsPower(terms[1]);
            if (leftBase is null || rightBase is null)
                return null;
            // One of each sign, or the equation is a sum of two powers and has no such root.
            if (leftSign == rightSign)
                return null;
            // Different bases only: one base is what SolveMultiplicative already does exactly.
            if (leftBase == rightBase)
                return null;

            var logarithmic = (leftPower! * MathS.Ln(leftBase) - rightPower! * MathS.Ln(rightBase))
                .InnerSimplified;
            if (!logarithmic.ContainsNode(x))
                return null;
            // Over the complex numbers e ^ u = e ^ v is u = v + 2 pi i n for every whole n, not
            // u = v alone: 3 ^ (x + 1) = 2 ^ (x - 1) has a root for each n, n = 0 the real one.
            // https://github.com/asc-community/AngouriMath/issues/2319
            if (MathS.Settings.Codomain.Value != Core.Domain.Real)
                logarithmic -= 2 * MathS.pi * MathS.i * SolutionParameters.Mint(expr, "n", MathS.Sets.Z);
            return AnalyticalEquationSolver.Solve(logarithmic, x) as FiniteSet;
        }

        /// <summary>
        /// <c>c a ^ p + d b ^ q = 0</c> over the complex numbers, for numbers <c>a</c> and <c>b</c>
        /// other than 0 and each other, and <c>c</c> and <c>d</c> other than 0: the principal power
        /// is <c>a ^ p = e ^ (p ln(a))</c>, so it is <c>e ^ (p ln(a)) = e ^ (ln(-d / c) + q ln(b))</c>,
        /// which is <c>p ln(a) = ln(-d / c) + q ln(b) + 2 pi i k</c> for every whole <c>k</c>.
        /// <c>(-2) ^ x + 4 ^ x = 0</c> is <c>x = i pi (2 k + 1) / (i pi - ln(2))</c>, where Newton's
        /// method found nine of them in decimals. Anything else declines.
        /// https://github.com/asc-community/AngouriMath/issues/2319,
        /// https://github.com/asc-community/AngouriMath/issues/2424
        /// </summary>
        private static Set? SolveTwoPowersOverTheComplexNumbers(Entity first, Entity second, Variable x)
        {
            if (AsScaledPower(first, x) is not var (c, a, p) || AsScaledPower(second, x) is not var (d, b, q) || a == b)
                return null;
            var logarithmic = (p * MathS.Ln(a) - q * MathS.Ln(b) - MathS.Ln(-d / c)
                - 2 * MathS.pi * MathS.i * SolutionParameters.Mint(first + second, "n", MathS.Sets.Z)).InnerSimplified;
            if (!logarithmic.ContainsNode(x))
                return null;
            return AnalyticalEquationSolver.Solve(logarithmic, x) as FiniteSet;
        }

        /// <summary>
        /// <paramref name="term"/> as <c>c a ^ p</c>, with <c>c</c> and <c>a</c> numbers other than 0
        /// and <c>p</c> linear in <paramref name="x"/>, or <see langword="null"/>.
        /// </summary>
        private static (Entity Coefficient, Entity Base, Entity Power)? AsScaledPower(Entity term, Variable x)
        {
            Entity coefficient = Integer.One;
            Powf? power = null;
            foreach (var factor in Mulf.LinearChildren(term))
                if (factor is Powf candidate && candidate.Exponent.ContainsNode(x) && power is null)
                    power = candidate;
                else if (!factor.ContainsNode(x) && !factor.Vars.Any())
                    coefficient *= factor;
                else
                    return null;
            if (power is not Powf(var @base, var exponent) || @base.ContainsNode(x) || @base.Vars.Any()
                || !TreeAnalyzer.TryGetPolyLinear(exponent, x, out _, out _)
                || @base.Evaled is not Complex { IsZero: false, IsFinite: true }
                || coefficient.Evaled is not Complex { IsZero: false, IsFinite: true })
                return null;
            return (coefficient.InnerSimplified, @base, exponent);
        }

        /// <summary>
        /// <paramref name="term"/> read as a signed power of a decidably positive real base, or
        /// a null base where it is not one.
        /// </summary>
        private static (Entity? Base, Entity? Power, bool Negated) AsPower(Entity term)
        {
            var negated = false;
            if (term is Mulf(Real { IsNegative: true } coefficient, var rest)
                && coefficient == -1)
            {
                negated = true;
                term = rest;
            }
            if (term is not Powf(var @base, var power))
                return (null, null, negated);
            // Decidably positive so that ln is real and `a ^ p = b ^ q` iff `p ln a = q ln b`.
            if (@base.Evaled is not Real { EDecimal.IsFinite: true } value || !value.IsPositive)
                return (null, null, negated);
            return (@base, power, negated);
        }

        internal static Set? SolveMultiplicative(Entity expr, Variable x)
        {
            Entity? substitution = null;
            var innerPowerList = new List<Entity>();
            var outerPowerList = new List<Entity>();
            Entity ApplyPowerTransform(Entity @base, Entity arg)
            {
                MultithreadingFunctional.ExitIfCancelled();

                arg = arg.Replace(GetConstantOutOfLogarithm);
                var mults = Entity.Mulf.LinearChildren(arg);
                if (!mults.Any()) return MathS.Pow(@base, arg);

                Entity innerPower = 1;
                Entity outerPower = 1;
                foreach (var mult in mults)
                {
                    if (mult.EvaluableNumerical)
                        outerPower *= mult;
                    else
                        innerPower *= mult;
                }
                substitution = innerPower == 1 ? @base : MathS.Pow(@base, innerPower);
                if(innerPower != 1) innerPowerList.Add(innerPower);
                if(outerPower != 1) outerPowerList.Add(outerPower);
                return MathS.Pow(substitution, outerPower.InnerSimplified);
            }

            Func<Entity, Entity> powerTransform = e => e switch
            {
                Powf(var @base, var arg)
                    when @base == x && !arg.ContainsNode(x) =>
                        ApplyPowerTransform(@base, arg),

                _ => e,
            };

            expr = expr.Replace(powerTransform);
            if (substitution is null) return null;

            var replacement = Variable.CreateTemp(expr.Vars);
            
            if(innerPowerList.Count == 0) 
                (innerPowerList, outerPowerList) = (outerPowerList, innerPowerList);

            // handle special case when all bases are numerical
            if (innerPowerList.All(e => e.EvaluableNumerical && e.Evaled is Real))
            {
                // Each power is to be a whole power of the substitution. x^5 written as (x^4)^(5/4) is
                // not x^5 off the positive reals, and solved through it x^5 - 30x^4 + 1 = 0 lost roots;
                // so the substitution is x to the greatest common divisor of the powers where they are
                // rational, and to the least of them only where each is a whole multiple of it. A
                // divisor of 1 is no substitution: the equation is a polynomial in x as it stands.
                Entity unit;
                if (innerPowerList.All(e => e.InnerSimplified is Rational))
                {
                    var powers = innerPowerList.Select(e => ((Rational)e.InnerSimplified).ERational).ToList();
                    var denominator = powers.Select(r => r.Denominator)
                        .Aggregate((a, b) => a.Divide(a.GreatestCommonDivisor(b)).Multiply(b));
                    var numerator = powers.Select(r => r.Numerator.Multiply(denominator).Divide(r.Denominator).Abs())
                        .Aggregate((a, b) => a.GreatestCommonDivisor(b));
                    if (numerator.Equals(denominator))
                        return null;
                    unit = Rational.Create(ERational.Create(numerator, denominator));
                }
                else
                    unit = innerPowerList.Aggregate((a, b)
                        => (a.EvalNumerical() < b.EvalNumerical()).EvalBoolean() ? a : b);

                substitution = MathS.Pow(x, unit).InnerSimplified;

                foreach (var pow in innerPowerList)
                {
                    var divided = (pow / unit).InnerSimplified;
                    if (divided is not Integer)
                        return null;
                    expr = expr.Substitute(MathS.Pow(x, pow.InnerSimplified), MathS.Pow(substitution, divided));
                }

                MultithreadingFunctional.ExitIfCancelled();
            }
            // A substitution of x by itself only renames the variable, and solving the renamed
            // equation asks this solver the same question again, without end. It is what is left
            // when every exponent is numeric but one is not real, so the case above cannot choose
            // a power of x: (-2)^x + 4^x = 0 reaches here as t^ln(-2) + t^ln(4), and so does
            // x^(1 + i) + x = 0. https://github.com/asc-community/AngouriMath/issues/2424
            if (substitution == x)
                return null;
            expr = expr.Substitute(substitution, replacement);
            if (expr.ContainsNode(x)) return null; // cannot be solved, not a multiplicative exponenial equation

            expr = expr.InnerSimplified;
            if (AnalyticalEquationSolver.Solve(expr, replacement) is FiniteSet els && els.Any())
                return substitution.InvertEach(els, x);
            else
                return null;
        }
    }
}
