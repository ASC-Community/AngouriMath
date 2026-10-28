//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions
{
    internal static partial class TreeAnalyzer
    {
        /// <summary>
        /// Whether the expression is real wherever it is defined, given that <paramref name="x"/>
        /// is, and given what <see cref="Assuming"/> says is known to hold. A free variable is
        /// read as complex by this library, so one appearing anywhere but under a modulus
        /// settles nothing and the answer is no. Nothing is claimed for a shape not listed: the
        /// list costs coverage and never correctness.
        /// </summary>
        /// <remarks>
        /// Asked by the limit reader, for which a bound or an oscillation is a fact about a real
        /// argument, and by differentiation, for which <c>|f|' = sgn(f) * f'</c> and
        /// <c>sgn(f)' = 0</c> are facts about a real-valued <c>f</c>: <c>|i / x|</c> is
        /// <c>1 / |x|</c>, whose derivative is <c>-sgn(x) / x^2</c>, where the real-line formula
        /// gives <c>sgn(i / x) * (-i / x^2) = sgn(x) / x^2</c> -- the wrong sign, and through
        /// l'Hopital's rule the limit of <c>(i / x) / |i / x|</c> at <c>+oo</c> read as <c>1</c>
        /// where it is <c>i</c>.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1186">#1186</a>
        /// </remarks>
        internal static bool IsRealValued(Entity expr, Variable x)
        {
            if (!expr.ContainsNode(x))
                return IsARealNumber(expr.Evaled);
            switch (expr)
            {
                case Variable variable:
                    return variable == x;
                case Sumf(var augend, var addend):
                    return IsRealValued(augend, x) && IsRealValued(addend, x);
                case Minusf(var minuend, var subtrahend):
                    return IsRealValued(minuend, x) && IsRealValued(subtrahend, x);
                case Mulf(var multiplier, var multiplicand):
                    return IsRealValued(multiplier, x) && IsRealValued(multiplicand, x);
                case Divf(var dividend, var divisor):
                    return IsRealValued(dividend, x) && IsRealValued(divisor, x);
                // Only an integer exponent keeps a real base real: x ^ (1/2) is not real below 0.
                case Powf(var @base, Integer):
                    return IsRealValued(@base, x);
                // A positive constant base is real to any real exponent: e^x, and so
                // `e^x - 1/e^x - 2`, whose sign the antiderivative of a hyperbolic integrand
                // carries under `u = e^x`.
                case Powf(var constantBase, var exponent) when !constantBase.ContainsNode(x)
                        && IsARealNumber(constantBase.Evaled) && ((Complex)constantBase.Evaled).RealPart.IsPositive:
                    return IsRealValued(exponent, x);
                // So is a base that what is known keeps positive: x ^ (1/3) under `x > 0`. Only a
                // strict bound is read as one, so the base is positive on an open set around the
                // point, which is what a derivative needs.
                case Powf(var @base, var exponent) when assumed is { } known
                        && PredicateEntailment.Entails(known, new Greaterf(@base, Integer.Zero)):
                    return IsRealValued(@base, x) && IsRealValued(exponent, x);
                case Sinf or Cosf or Tanf or Cotanf or Secantf or Cosecantf
                     or Arctanf or Arccotanf or Signumf or Erff or Erfcf or Erfif or Eif or Sif or Shif
                     or Conjugatef:
                    return IsRealValued(expr.DirectChildren[0], x);
                // A polygamma function of a whole order is real on the real line between its poles.
                case Polygammaf(var order, var argument) when !order.ContainsNode(x):
                    return IsRealValued(argument, x);
                // A modulus is real whatever it is taken of, and so is an argument; an Iverson
                // bracket is 0 or 1.
                case Absf or Iversonf or Argf:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Where an expression that is not real everywhere is real on an open set: the
        /// conjunction of <c>b &gt; 0</c> over the bases <c>b</c> of its powers to a real
        /// exponent that is not whole and the arguments <c>b</c> of its logarithms to a positive
        /// constant base, or <see langword="null"/> where it has no such power or logarithm or
        /// a shape <see cref="IsRealValued"/> does not list. <c>sqrt(1 + x) + 1/2</c> is real
        /// where <c>1 + x &gt; 0</c>, <c>x^(1/3) - 1</c> where <c>x &gt; 0</c>, and
        /// <c>ln(2 x^3)</c> where <c>2 x^3 &gt; 0</c>, which is all that is claimed: with the
        /// downcasting off a cube root of a negative is the principal one. Strict for the reason
        /// <see cref="IsRealValued"/> reads only a strict bound: a derivative needs the
        /// expression real on an interval around the point.
        /// </summary>
        /// <remarks>
        /// Asked by differentiation where the sign or the modulus of such an expression is
        /// differentiated, so that the real-line rule is claimed where the expression is real and
        /// the derivative is left unevaluated elsewhere. The antiderivatives of
        /// <c>sqrt(1 + x + sqrt(1 + x))</c> and <c>(x - 4)/((1 + x^(1/3)) sqrt(x))</c> carry
        /// <c>sgn(sqrt(1 + x) + 1/2)</c> and <c>sgn(x^(1/3))</c>, and could not be checked by
        /// differentiating them back anywhere.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1186">#1186</a>
        /// </remarks>
        internal static Entity? WhereRealValued(Entity expr, Variable x)
        {
            var bases = new List<Entity>();
            if (!GatherTheBasesOfRoots(expr, x, bases) || bases.Count is 0)
                return null;
            var condition = (Entity)new Greaterf(bases[0], Integer.Zero);
            foreach (var @base in bases.Skip(1))
                condition &= new Greaterf(@base, Integer.Zero);
            return condition;
        }

        // The shapes IsRealValued lists, with a power to a real exponent that is not whole read
        // as real where its base is positive. An inner base is gathered before the one it is in,
        // so `x > 0` is decided before `sqrt(x) - 1 > 0` is asked of a complex number.
        private static bool GatherTheBasesOfRoots(Entity expr, Variable x, List<Entity> bases)
        {
            if (!expr.ContainsNode(x))
                return IsARealNumber(expr.Evaled);
            switch (expr)
            {
                case Variable variable:
                    return variable == x;
                case Sumf(var augend, var addend):
                    return GatherTheBasesOfRoots(augend, x, bases) && GatherTheBasesOfRoots(addend, x, bases);
                case Minusf(var minuend, var subtrahend):
                    return GatherTheBasesOfRoots(minuend, x, bases) && GatherTheBasesOfRoots(subtrahend, x, bases);
                case Mulf(var multiplier, var multiplicand):
                    return GatherTheBasesOfRoots(multiplier, x, bases) && GatherTheBasesOfRoots(multiplicand, x, bases);
                case Divf(var dividend, var divisor):
                    return GatherTheBasesOfRoots(dividend, x, bases) && GatherTheBasesOfRoots(divisor, x, bases);
                case Powf(var @base, Integer):
                    return GatherTheBasesOfRoots(@base, x, bases);
                case Powf(var constantBase, var exponent) when !constantBase.ContainsNode(x)
                        && IsARealNumber(constantBase.Evaled) && ((Complex)constantBase.Evaled).RealPart.IsPositive:
                    return GatherTheBasesOfRoots(exponent, x, bases);
                case Powf(var @base, var exponent) when !exponent.ContainsNode(x) && IsARealNumber(exponent.Evaled):
                    if (!GatherTheBasesOfRoots(@base, x, bases))
                        return false;
                    if (!bases.Contains(@base))
                        bases.Add(@base);
                    return true;
                // A logarithm to a positive constant base is real where its argument is positive,
                // and so is gathered as a root's base is: sgn(ln(a x^n)), which the antiderivative
                // of sqrt(ln(a x^n)^2)/x carries, is flat where a x^n > 0.
                case Logf(var logBase, var antilogarithm) when !logBase.ContainsNode(x)
                        && IsARealNumber(logBase.Evaled) && ((Complex)logBase.Evaled).RealPart.IsPositive:
                    if (!GatherTheBasesOfRoots(antilogarithm, x, bases))
                        return false;
                    if (!bases.Contains(antilogarithm))
                        bases.Add(antilogarithm);
                    return true;
                case Sinf or Cosf or Tanf or Cotanf or Secantf or Cosecantf
                     or Arctanf or Arccotanf or Signumf or Erff or Erfcf or Erfif or Eif or Sif or Shif
                     or Conjugatef:
                    return GatherTheBasesOfRoots(expr.DirectChildren[0], x, bases);
                case Polygammaf(var order, var argument) when !order.ContainsNode(x):
                    return GatherTheBasesOfRoots(argument, x, bases);
                case Absf or Iversonf or Argf:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// What is known to hold wherever the expression being read has a value, or
        /// <see langword="null"/>: the condition of the <c>provided</c>, or of the piecewise
        /// case, being differentiated. Thread-static, as the limit machinery's approach is, and
        /// swapped rather than set, so a nested one restores the outer.
        /// </summary>
        [ThreadStatic] private static Entity? assumed;

        /// <summary>
        /// Runs <paramref name="work"/> with <paramref name="condition"/> known to hold, as well
        /// as whatever was known already.
        /// </summary>
        internal static T Assuming<T>(Entity condition, Func<T> work)
        {
            var outer = assumed;
            assumed = outer is null ? condition : outer & condition;
            try
            {
                return work();
            }
            finally
            {
                assumed = outer;
            }
        }

        // A real number, or a complex one whose imaginary part is zero: with the downcasting
        // off, `e` evaluates to the latter, and it is no less real for it.
        private static bool IsARealNumber(Entity evaled)
            => evaled is Real { IsNaN: false } || evaled is Complex { IsNaN: false } complex && complex.ImaginaryPart.IsZero;

        /// <summary>
        /// Rewrites <c>abs(c * g)</c>, <c>abs(c / g)</c> and <c>abs(g / c)</c>, for a numeric
        /// <c>c</c> off the real line whose modulus is exact, as <c>|c| * abs(g)</c>,
        /// <c>|c| / abs(g)</c> and <c>abs(g) / |c|</c>, with <c>|c|</c> a number. The modulus is
        /// multiplicative on the whole plane, so nothing is assumed.
        /// </summary>
        /// <remarks>
        /// Only where the modulus is exact: <c>|1 + i|</c> is <c>sqrt(2)</c>, which the number
        /// type can only hold rounded, and a rounded coefficient is a different value. Left inside
        /// the modulus it stays exact. A real factor is left where it is as well -- <c>|3 * x|</c>
        /// and <c>3 * |x|</c> are the same size and there is no phase to take out; what this
        /// exists for is the factor that carries one, <c>|i * x| = |x|</c>, which is what lets a
        /// derivative and a limit read <c>|i / x|</c> as <c>1 / |x|</c>.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1186">#1186</a>
        /// </remarks>
        internal static bool TryTakeNumericModulusOut(Entity absArgument, out Entity rewritten)
        {
            switch (absArgument)
            {
                case Mulf(var c, var rest) when ExactModulus(c) is { } modulus:
                    rewritten = modulus * new Absf(rest);
                    return true;
                case Mulf(var rest, var c) when ExactModulus(c) is { } modulus:
                    rewritten = modulus * new Absf(rest);
                    return true;
                case Divf(var c, var rest) when ExactModulus(c) is { } modulus:
                    rewritten = modulus / new Absf(rest);
                    return true;
                case Divf(var rest, var c) when ExactModulus(c) is { } modulus:
                    rewritten = new Absf(rest) / modulus;
                    return true;
                default:
                    rewritten = absArgument;
                    return false;
            }
        }

        /// <summary>
        /// The modulus of a closed expression whose value is a number off the real line, where
        /// that modulus is a rational number; <see langword="null"/> for anything else -- a
        /// symbol, a real value, and a modulus the number type could only hold rounded. Read off
        /// the value rather than the node, since <c>3 + 4i</c> is written as a sum.
        /// </summary>
        internal static Rational? ExactModulus(Entity c)
            => c.Evaled is Complex number and not Real ? number.Abs() as Rational : null;
    }
}
