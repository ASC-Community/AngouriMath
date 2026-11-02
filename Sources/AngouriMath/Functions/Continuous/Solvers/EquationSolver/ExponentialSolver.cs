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
            return AnalyticalEquationSolver.Solve(logarithmic, x) as FiniteSet;
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
