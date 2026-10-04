//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Multithreading;
using PeterO.Numbers;
using System;
using System.Collections.Generic;
using System.Linq;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions.Algebra
{
    /// <summary>
    /// A power series in <c>w</c> around <c>w -> 0+</c>, truncated at a known exponent, whose
    /// coefficients are arbitrary expressions not containing <c>w</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what Gruntz's algorithm expands in, and it is not a Taylor series: the
    /// exponents may be negative and fractional, and the coefficients are symbolic rather
    /// than numeric. What is wanted from it in the end is only the leading term, but the
    /// leading terms of a sum can cancel -- which is the whole point of the algorithm, since
    /// e^(x + e^-x) - e^x cancels to every order when its two parts are expanded separately
    /// -- so the series has to be carried far enough to see past the cancellation, and how
    /// far that is is not known in advance. The caller raises the order until a leading term
    /// survives.
    /// </para>
    /// <para>
    /// The value of <c>log(w)</c> is supplied from outside rather than left as a logarithm of
    /// a symbol. Gruntz chooses w as an exponential, so its logarithm is an ordinary
    /// expression in x, and expanding a logarithm needs it: without it the constant term of
    /// <c>log(c * w^e)</c> cannot be told from zero.
    /// </para>
    /// </remarks>
    internal sealed class AsymptoticSeries
    {
        /// <summary>Coefficients by exponent, ascending. None of them contain w.</summary>
        internal SortedDictionary<ERational, Entity> Terms { get; }

        /// <summary>Nothing is known at or beyond this exponent.</summary>
        internal ERational Order { get; }

        private AsymptoticSeries(SortedDictionary<ERational, Entity> terms, ERational order)
            => (Terms, Order) = (terms, order);

        /// <summary>
        /// The order of a series that is not truncated at all. A constant, a power of w, and
        /// anything built from those by the four operations is known exactly, and saying so
        /// matters: a series carrying a finite order is one whose terms beyond it have been
        /// thrown away, and multiplying by it would throw away the other factor's terms too.
        /// </summary>
        [ConstantField] internal static readonly ERational Exact = ERational.FromInt32(1 << 24);

        internal static AsymptoticSeries Constant(Entity value)
        {
            var terms = new SortedDictionary<ERational, Entity>();
            if (!IsKnownZero(value))
                terms[ERational.Zero] = value;
            return new AsymptoticSeries(terms, Exact);
        }

        /// <summary>The one term <c>coefficient * w^power</c>.</summary>
        internal static AsymptoticSeries Monomial(Entity coefficient, ERational power)
        {
            var terms = new SortedDictionary<ERational, Entity>();
            if (!IsKnownZero(coefficient))
                terms[power] = coefficient;
            return new AsymptoticSeries(terms, Exact);
        }

        /// <summary>
        /// The leading coefficient and its exponent, or <see langword="null"/> if every term
        /// known so far cancelled and the series is indistinguishable from zero at this order.
        /// </summary>
        internal (Entity Coefficient, ERational Power)? LeadingTerm()
        {
            foreach (var term in Terms)
                if (!IsKnownZero(term.Value))
                    return (term.Value, term.Key);
            return null;
        }

        /// <summary>
        /// Whether the coefficient is zero on the evidence available. A coefficient that
        /// cannot be decided is kept, so that a leading term is never claimed for something
        /// that might have cancelled.
        /// </summary>
        private static bool IsKnownZero(Entity coefficient)
        {
            if (coefficient == Integer.Zero)
                return true;
            var simplified = coefficient.InnerSimplified;
            if (simplified == Integer.Zero)
                return true;
            if (simplified.Evaled is Complex { IsZero: true })
                return true;
            // A sum can cancel term for term where the inner simplification leaves it written
            // out: e^(1/x) - e^(1/x) is what the constant terms of the two exponentials leave in
            // e^x (e^(1/x + e^(-x)) - e^(1/x)), the example Gruntz's algorithm is introduced with.
            // Collecting like terms settles that without a search. It answers 0 provided not
            // x = 0, a zero wherever the coefficient is defined, and the condition excludes a
            // point that says nothing about x running off to infinity -- the same reading as
            // Gruntz.Bare's.
            if (simplified is not (Sumf or Minusf))
                return false;
            var collected = Simplificator.SimplifyChildren(simplified);
            while (collected is Providedf(var value, _))
                collected = value;
            return collected == Integer.Zero;
        }

        private static SortedDictionary<ERational, Entity> Fresh() => new();

        internal AsymptoticSeries WithOrder(ERational order)
        {
            if (order.CompareTo(Order) >= 0)
                return this;
            var terms = Fresh();
            foreach (var term in Terms)
                if (term.Key.CompareTo(order) < 0)
                    terms[term.Key] = term.Value;
            return new AsymptoticSeries(terms, order);
        }

        internal AsymptoticSeries Add(AsymptoticSeries other)
        {
            var order = Order.CompareTo(other.Order) <= 0 ? Order : other.Order;
            var terms = Fresh();
            foreach (var term in Terms.Concat(other.Terms))
            {
                if (term.Key.CompareTo(order) >= 0)
                    continue;
                terms[term.Key] = terms.TryGetValue(term.Key, out var already)
                    ? (already + term.Value).InnerSimplified
                    : term.Value;
            }
            return new AsymptoticSeries(terms, order);
        }

        internal AsymptoticSeries Negate()
        {
            var terms = Fresh();
            foreach (var term in Terms)
                terms[term.Key] = (-term.Value).InnerSimplified;
            return new AsymptoticSeries(terms, Order);
        }

        internal AsymptoticSeries Multiply(AsymptoticSeries other)
        {
            // Everything below the leading exponent of one factor is unknown in the product,
            // so the order of the product is each order raised by the other's leading power.
            var mine = LeadingTerm()?.Power ?? Order;
            var theirs = other.LeadingTerm()?.Power ?? other.Order;
            var order = Min(Order.Add(theirs), other.Order.Add(mine));
            var terms = Fresh();
            foreach (var left in Terms)
                foreach (var right in other.Terms)
                {
                    var power = left.Key.Add(right.Key);
                    if (power.CompareTo(order) >= 0)
                        continue;
                    var product = (left.Value * right.Value).InnerSimplified;
                    terms[power] = terms.TryGetValue(power, out var already)
                        ? (already + product).InnerSimplified
                        : product;
                }
            return new AsymptoticSeries(terms, order);
        }

        private static ERational Min(ERational a, ERational b) => a.CompareTo(b) <= 0 ? a : b;

        /// <summary>
        /// The series divided by its own leading term, which leaves a series starting at 1.
        /// </summary>
        private AsymptoticSeries? Normalised(out Entity coefficient, out ERational power)
        {
            coefficient = Integer.Zero;
            power = ERational.Zero;
            if (LeadingTerm() is not var (c, e))
                return null;
            (coefficient, power) = (c, e);
            var terms = Fresh();
            foreach (var term in Terms)
                if (term.Key.CompareTo(e) >= 0)
                    // The leading coefficient over itself is 1 exactly, and is written so: the
                    // inner simplification leaves x/x as it is, and the rest of the series is
                    // this minus 1, where x/x - 1 would read as a term at w^0 that nothing can
                    // tell from zero.
                    terms[term.Key.Subtract(e)] = term.Key.CompareTo(e) == 0 ? Integer.One : (term.Value / c).InnerSimplified;
            return new AsymptoticSeries(terms, Order.Subtract(e));
        }

        /// <summary>
        /// The series without its constant term: the series minus that term, taken off
        /// exactly rather than subtracted, so that nothing is left at <c>w^0</c> that the zero
        /// test would have to recognise.
        /// </summary>
        internal AsymptoticSeries WithoutConstant()
        {
            var terms = Fresh();
            foreach (var term in Terms)
                if (term.Key.CompareTo(ERational.Zero) != 0)
                    terms[term.Key] = term.Value;
            return new AsymptoticSeries(terms, Order);
        }

        /// <summary>
        /// The reciprocal, by the geometric series on what is left once the leading term is
        /// taken out. Declines if the leading term cannot be found, since dividing by a
        /// series that might be zero is not something to guess at.
        /// </summary>
        internal AsymptoticSeries? Invert(ERational requested)
        {
            if (Normalised(out var coefficient, out var power) is not { } unit)
                return null;
            // 1/(1 + d) = 1 - d + d^2 - ..., which terminates because d starts above w^0.
            var rest = unit.WithoutConstant();
            var sum = Constant(1);
            var term = Constant(1);
            for (var i = 0; i < MaxExpansionTerms; i++)
            {
                term = term.Multiply(rest.Negate());
                if (term.LeadingTerm() is not var (_, at) || at.CompareTo(requested) >= 0)
                    break;
                sum = sum.Add(term);
            }
            return sum.WithOrder(requested).Multiply(Monomial((1 / coefficient).InnerSimplified, power.Negate()));
        }

        /// <summary>How many terms of a geometric, exponential or logarithmic series to take.</summary>
        private const int MaxExpansionTerms = 24;

        /// <summary>
        /// The exponential of the series. The argument must not run off to infinity -- a
        /// leading exponent below zero would mean the exponential belongs to a faster
        /// comparability class than w, which the algorithm has already ruled out by putting
        /// it in the set of most rapidly varying subexpressions instead.
        /// </summary>
        internal AsymptoticSeries? Exponentiate(ERational requested)
        {
            var constant = Terms.TryGetValue(ERational.Zero, out var atZero) ? atZero : Integer.Zero;
            var rest = WithoutConstant();
            if (rest.LeadingTerm() is var (_, least) && least.CompareTo(ERational.Zero) <= 0)
                return null;
            // exp(constant) is left unexpanded on purpose: it is a coefficient, and opening
            // it up would reveal a function of a faster class than w.
            var scale = MathS.Pow(MathS.e, constant).InnerSimplified;
            var sum = Constant(1);
            var term = Constant(1);
            for (var i = 1; i < MaxExpansionTerms; i++)
            {
                term = term.Multiply(rest).Multiply(Constant(Rational.Create(1, i)));
                if (term.LeadingTerm() is not var (_, at) || at.CompareTo(requested) >= 0)
                    break;
                sum = sum.Add(term);
            }
            return sum.WithOrder(requested).Multiply(Constant(scale));
        }

        /// <summary>
        /// The logarithm of the series, given the value of <c>log(w)</c>. The leading term
        /// has to be found, since <c>log</c> of something that might be zero is not something
        /// to guess at.
        /// </summary>
        internal AsymptoticSeries? Logarithm(Entity logarithmOfW, ERational requested)
        {
            if (Normalised(out var coefficient, out var power) is not { } unit)
                return null;
            var head = (MathS.Ln(coefficient) + Rational.Create(power) * logarithmOfW).InnerSimplified;
            // log(1 + d) = d - d^2/2 + d^3/3 - ...
            var rest = unit.WithoutConstant();
            var sum = Constant(head);
            var term = Constant(1);
            for (var i = 1; i < MaxExpansionTerms; i++)
            {
                term = term.Multiply(rest);
                if (term.LeadingTerm() is not var (_, at) || at.CompareTo(requested) >= 0)
                    break;
                sum = sum.Add(term.Multiply(Constant(Rational.Create(i % 2 == 1 ? 1 : -1, i))));
            }
            return sum.WithOrder(requested);
        }

        /// <summary>
        /// The series of <paramref name="expr"/> in <paramref name="w"/> about zero, carried
        /// to <paramref name="order"/>, or <see langword="null"/> if some part of it is not
        /// something this can expand.
        /// </summary>
        internal static AsymptoticSeries? Expand(Entity expr, Variable w, Entity logarithmOfW, ERational order)
        {
            MultithreadingFunctional.ExitIfCancelled();
            if (!expr.ContainsNode(w))
                return Constant(expr);
            if (expr == w)
                return Monomial(1, ERational.One);
            switch (expr)
            {
                case Sumf(var left, var right):
                    return Both(left, right, (a, b) => a.Add(b));
                case Minusf(var left, var right):
                    return Both(left, right, (a, b) => a.Add(b.Negate()));
                case Mulf(var left, var right):
                    return Both(left, right, (a, b) => a.Multiply(b));
                case Divf(var left, var right):
                    return Expand(right, w, logarithmOfW, order) is { } divisor
                        && divisor.Invert(order) is { } inverted
                        && Expand(left, w, logarithmOfW, inverted.Order) is { } dividend
                            ? dividend.Multiply(inverted)
                            : null;
                case Powf(var @base, var power) when @base == MathS.e:
                    return Expand(power, w, logarithmOfW, order)?.Exponentiate(order);
                case Powf(var @base, var power) when !power.ContainsNode(w):
                    return RaiseToConstant(@base, power, w, logarithmOfW, order);
                case Powf(var @base, var power):
                    // b^p with w in the exponent is exp(p log b), which the two cases above
                    // between them can expand.
                    return Expand(MathS.Pow(MathS.e, power * MathS.Ln(@base)), w, logarithmOfW, order);
                case Logf(var @base, var antilog) when !@base.ContainsNode(w):
                    return Expand(antilog, w, logarithmOfW, order)?.Logarithm(logarithmOfW, order) is { } logarithm
                        ? logarithm.Multiply(Constant((1 / MathS.Ln(@base)).InnerSimplified))
                        : null;
                default:
                    return null;
            }

            AsymptoticSeries? Both(Entity left, Entity right, Func<AsymptoticSeries, AsymptoticSeries, AsymptoticSeries> combine)
                => Expand(left, w, logarithmOfW, order) is { } a && Expand(right, w, logarithmOfW, order) is { } b
                    ? combine(a, b)
                    : null;
        }

        private static AsymptoticSeries? RaiseToConstant(
            Entity @base, Entity power, Variable w, Entity logarithmOfW, ERational order)
        {
            var requested = order;
            if (Expand(@base, w, logarithmOfW, order) is not { } expanded)
                return null;
            if (power.Evaled is not Rational exponent)
                return null;
            var wanted = exponent.ERational;
            // A whole power is a product, which keeps the coefficients as they are.
            if (wanted.IsInteger() && wanted.Abs().CompareTo(ERational.FromInt32(MaxExpansionTerms)) <= 0)
            {
                var times = wanted.ToEIntegerIfExact().ToInt32Checked();
                if (times == 0)
                    return Constant(1);
                if (times < 0)
                    return expanded.Invert(order) is { } inverted ? Repeated(inverted, -times) : null;
                return Repeated(expanded, times);
            }
            // Otherwise take the leading term out and use the binomial series on the rest.
            if (expanded.Normalised(out var coefficient, out var leading) is not { } unit)
                return null;
            var rest = unit.WithoutConstant();
            var sum = Constant(1);
            var term = Constant(1);
            for (var i = 1; i < MaxExpansionTerms; i++)
            {
                // The binomial coefficient built up one factor at a time, so that it works
                // for a fractional exponent as much as a whole one.
                var factor = (Rational.Create(wanted) - (i - 1)) / i;
                term = term.Multiply(rest).Multiply(Constant(factor.InnerSimplified));
                if (term.LeadingTerm() is not var (_, at) || at.CompareTo(requested) >= 0)
                    break;
                sum = sum.Add(term);
            }
            sum = sum.WithOrder(requested);
            var scaled = MathS.Pow(coefficient, power).InnerSimplified;
            return sum.Multiply(Monomial(scaled, wanted.Multiply(leading)));
        }

        private static AsymptoticSeries Repeated(AsymptoticSeries series, int times)
        {
            var result = Constant(1);
            for (var i = 0; i < times; i++)
                result = result.Multiply(series);
            return result;
        }
    }
}
