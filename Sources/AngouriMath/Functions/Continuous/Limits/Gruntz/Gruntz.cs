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
    /// Gruntz's algorithm for the limit of an expression as x tends to positive infinity,
    /// after D. Gruntz, "On Computing Limits in a Symbolic Manipulation System", ETH 1996.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The idea is to work out which subexpressions grow fastest, rewrite the expression in
    /// terms of a single one of them, and read the answer off the leading term of the power
    /// series that leaves. Two functions are in the same comparability class when
    /// log|f| / log|g| tends to something finite and non-zero, and the set of subexpressions
    /// in the fastest class present is the mrv set. Every member of it can be written as a
    /// power of any other member times something slower, so one member is picked -- turned
    /// upside down if need be, so that it tends to zero -- and everything is written in terms
    /// of it. What is left is a series in that one quantity, and the sign of the leading
    /// exponent says whether the limit is zero, infinite, or the limit of the leading
    /// coefficient, which is a smaller problem of the same kind.
    /// </para>
    /// <para>
    /// This answers the limits that defeat term-by-term expansion because the terms cancel to
    /// every order. lim x -> +oo e^(x + e^(-x)) - e^x is the standard one: expanding the two
    /// exponentials separately gives two divergent series whose difference cancels entirely,
    /// while rewriting the whole expression in w = e^(-x) gives (e^w - 1)/w, whose leading
    /// term is 1.
    /// </para>
    /// <para>
    /// The scope here is the exp-log functions: what can be built from x and the rationals
    /// with the four operations, exp and log. That is the class the algorithm is proved for,
    /// because those functions are eventually monotone and so comparable at all. Anything
    /// else -- a sine, an unknown function, a factorial -- makes this decline rather than
    /// guess, since sin(x) has no limit at infinity and no comparability class either.
    /// </para>
    /// </remarks>
    internal static class Gruntz
    {
        /// <summary>
        /// How deep the mutual recursion between the limit, the sign and the comparison may
        /// go. Each of them asks the others about strictly smaller subexpressions, so this is
        /// a backstop against an expression the class comparison cannot settle rather than a
        /// bound the algorithm is expected to reach.
        /// </summary>
        private const int MaxDepth = 32;

        [ThreadStatic] private static int depth;

        /// <summary>
        /// The recursion, printed when GRUNTZ_DEBUG is set. The algorithm calls itself through
        /// four different routes and a failure anywhere comes back as a plain decline, so
        /// there is no following it without this.
        /// </summary>
        private static void Trace(string what, object? value)
        {
            if (Environment.GetEnvironmentVariable("GRUNTZ_DEBUG") == "1")
                Console.WriteLine($"{new string(' ', 2 * depth)}{what} = {value ?? "null"}");
        }

        /// <summary>
        /// The limit of <paramref name="expr"/> as <paramref name="x"/> tends to positive
        /// infinity, or <see langword="null"/> where this cannot say.
        /// </summary>
        internal static Entity? LimitToPositiveInfinity(Entity expr, Variable x)
        {
            if (depth > 0)
                return null;                       // already inside; the caller is the entry
            // The algorithm is for real functions, and its series takes the logarithm of a
            // negative leading coefficient as the principal one, which is the limit from above
            // the cut. ln(x^x) at -1 from the right is ln((1/x - 1)^(1/x - 1)) here, whose
            // argument comes to -1 from below, and it answered i pi where the limit is -i pi.
            // https://github.com/asc-community/AngouriMath/issues/2266
            if (expr.Nodes.Any(node => LimitFunctional.TakesItsValueOnACut(node, x, Real.PositiveInfinity)))
                return null;
            expr = AsExponentials(expr, x);
            return LimitInf(expr, x);
        }

        private static Entity? LimitInf(Entity e, Variable x)
        {
            if (!e.ContainsNode(x))
                return e;
            if (++depth > MaxDepth) { depth--; return null; }
            try
            {
                MultithreadingFunctional.ExitIfCancelled();
                Trace($"limitinf({e.Stringize()})", "...");
                if (MrvLeadTerm(e, x) is not var (coefficient, power))
                { Trace($"limitinf({e.Stringize()}) leadterm", null); return null; }
                Trace($"  leadterm({e.Stringize()})", $"({coefficient.Stringize()}, {power})");
                var sign = power.Sign;
                if (sign > 0)
                    return 0;                       // w^positive tends to zero
                if (sign == 0)
                    return LimitInf(coefficient, x);
                return Sign(coefficient, x) switch
                {
                    1 => Real.PositiveInfinity,
                    -1 => Real.NegativeInfinity,
                    _ => null
                };
            }
            finally { depth--; }
        }

        /// <summary>
        /// Whether the expression is eventually positive, negative or identically zero, or
        /// <see langword="null"/> where that cannot be settled. This is the one place the
        /// algorithm needs an oracle it cannot have in general, so it declines rather than
        /// assumes.
        /// </summary>
        private static int? Sign(Entity e, Variable x)
        {
            if (!e.ContainsNode(x))
                return Bare(e).Evaled switch
                {
                    Real { IsNaN: false } value => value.IsZero ? 0 : value.IsNegative ? -1 : 1,
                    _ => null
                };
            if (e == x)
                return 1;
            if (IsExponential(e, out _))
                return 1;                           // an exponential is positive wherever defined
            switch (e)
            {
                case Mulf(var multiplier, var multiplicand):
                    return Sign(multiplier, x) is { } left && Sign(multiplicand, x) is { } right
                        ? left * right : null;
                case Divf(var dividend, var divisor):
                    return Sign(dividend, x) is { } above && Sign(divisor, x) is { } below && below != 0
                        ? above * below : null;
            }
            return SignByLeadingTerm(e, x);
        }

        private static int? SignByLeadingTerm(Entity e, Variable x)
        {
            if (++depth > MaxDepth) { depth--; return null; }
            try
            {
                return MrvLeadTerm(e, x) is var (coefficient, _) ? Sign(coefficient, x) : null;
            }
            finally { depth--; }
        }

        private static bool IsExponential(Entity e, out Entity exponent)
        {
            if (e is Powf(var @base, var power) && @base == MathS.e)
            {
                exponent = power;
                return true;
            }
            exponent = Integer.Zero;
            return false;
        }

        private static Entity Exponential(Entity exponent) => MathS.Pow(MathS.e, exponent);

        /// <summary>
        /// Every power whose exponent moves rewritten as an exponential, which is how
        /// <see cref="Mrv"/> reads one in any case.
        /// </summary>
        /// <remarks>
        /// The mrv set holds subexpressions of the expression and <see cref="Rewrite"/>
        /// substitutes them by name, so a member has to occur in the expression as it stands.
        /// Reading b^p as exp(p * ln(b)) inside Mrv alone put a member in the set that was
        /// nowhere in the expression: for x^x / e^(x * ln(x)) the set came back holding the
        /// same exponential twice, once as the constructed e^(x * ln(x)) and once as the
        /// denominator's own e^(ln(x) * x), and the substitution found only the second. The
        /// numerator went into the series as x^x, whose leading exponent reads as +1, and the
        /// limit came back 0 where the two sides are equal and the answer is 1.
        /// https://github.com/asc-community/AngouriMath/issues/735
        /// This assumes b is positive, which is what Mrv's own reading of the same node
        /// already assumed; the algorithm is scoped to the exp-log functions, and a moving
        /// exponent over a base that changes sign is outside that class either way.
        /// </remarks>
        private static Entity AsExponentials(Entity e, Variable x) => e.Replace(node =>
            node is Powf(var @base, var power)
            && @base != MathS.e
            && power.ContainsNode(x)
                ? Exponential((power * MathS.Ln(@base)).InnerSimplified)
                : node);

        /// <summary>
        /// The expression without the domain conditions simplification leaves behind. A limit
        /// is taken of an expression read as continuous, so a condition that excludes a point
        /// says nothing about it: x/x simplifies to 1 provided x is not zero, and what is
        /// wanted here is the 1.
        /// </summary>
        private static Entity Bare(Entity e)
        {
            while (e is Providedf(var inner, _)) e = inner;
            return e;
        }

        /// <summary>
        /// Which of the two grows faster: 1 for the first, -1 for the second, 0 for the same
        /// comparability class, or <see langword="null"/> where it cannot be told.
        /// </summary>
        private static int? Compare(Entity a, Entity b, Variable x)
        {
            // The logarithm of an exponential is taken off directly rather than left as
            // log(exp(...)), which the ratio below would never be able to settle.
            var logA = IsExponential(a, out var powerA) ? powerA : MathS.Ln(a);
            var logB = IsExponential(b, out var powerB) ? powerB : MathS.Ln(b);
            if (LimitInf(Bare((logA / logB).Simplify()), x) is not { } ratio)
                return null;
            ratio = Bare(ratio);
            if (ratio.Evaled is Complex { IsZero: true })
                return -1;
            if (IsInfinite(ratio))
                return 1;
            return ratio.Evaled is Complex { IsNaN: false } ? 0 : null;
        }

        private static bool IsInfinite(Entity e)
            => Bare(e).Evaled is Real { IsFinite: false, IsNaN: false };

        /// <summary>
        /// The set of subexpressions in the fastest growing comparability class present, or
        /// <see langword="null"/> if the expression is outside what this can read.
        /// </summary>
        private static HashSet<Entity>? Mrv(Entity e, Variable x)
        {
            MultithreadingFunctional.ExitIfCancelled();
            if (!e.ContainsNode(x))
                return new HashSet<Entity>();
            if (e == x)
                return new HashSet<Entity> { x };
            switch (e)
            {
                case Sumf(var sumLeft, var sumRight):
                    return Both(sumLeft, sumRight);
                case Minusf(var minuend, var subtrahend):
                    return Both(minuend, subtrahend);
                case Mulf(var multiplier, var multiplicand):
                    return Both(multiplier, multiplicand);
                case Divf(var dividend, var divisor):
                    return Both(dividend, divisor);

                case Powf(var @base, var power) when @base == MathS.e && power == x:
                    return new HashSet<Entity> { e };   // e^x, without asking for the limit of x

                case Powf(var @base, var power) when @base == MathS.e:
                    {
                        if (Mrv(power, x) is not { } inner)
                            return null;
                        if (LimitInf(power, x) is not { } limit)
                            return null;
                        return IsInfinite(limit)
                            ? MrvMax(new HashSet<Entity> { e }, inner, x)
                            : inner;
                    }

                case Powf(var @base, var power) when !power.ContainsNode(x):
                    return Mrv(@base, x);

                case Powf(var @base, var power):
                    // b^p with x in the exponent is exp(p log b), which the case above reads.
                    return Mrv(Exponential(power * MathS.Ln(@base)), x);

                case Logf(var @base, var antilog) when !@base.ContainsNode(x):
                    return Mrv(antilog, x);

                default:
                    // Anything else -- a sine, an unknown function, a factorial -- has no
                    // comparability class this knows how to place.
                    return null;
            }

            HashSet<Entity>? Both(Entity left, Entity right)
                => Mrv(left, x) is { } a && Mrv(right, x) is { } b ? MrvMax(a, b, x) : null;
        }

        private static HashSet<Entity>? MrvMax(HashSet<Entity> f, HashSet<Entity> g, Variable x)
        {
            if (f.Count == 0) return g;
            if (g.Count == 0) return f;
            if (f.Overlaps(g)) return Union(f, g);
            return Compare(f.First(), g.First(), x) switch
            {
                1 => f,
                -1 => g,
                0 => Union(f, g),
                _ => null
            };
        }

        private static HashSet<Entity> Union(HashSet<Entity> f, HashSet<Entity> g)
        {
            var result = new HashSet<Entity>(f);
            result.UnionWith(g);
            return result;
        }

        /// <summary>
        /// The leading coefficient of the expression and the exponent it sits at, once the
        /// expression has been rewritten in terms of one member of its mrv set.
        /// </summary>
        private static (Entity Coefficient, ERational Power)? MrvLeadTerm(Entity e, Variable x)
        {
            if (!e.ContainsNode(x))
                return (e, ERational.Zero);
            if (Mrv(e, x) is not { } omega)
            { Trace($"  mrv({e.Stringize()})", null); return null; }
            Trace($"  mrv({e.Stringize()})", "{" + string.Join(", ", omega.Select(o => o.Stringize())) + "}");
            if (omega.Count == 0)
                return (e, ERational.Zero);

            if (omega.Contains(x))
            {
                // Everything is in the class of x or below, which this cannot resolve: the
                // series would come back as the expression itself times w^0. Substituting
                // e^x for x leaves the limit alone, since e^x tends to infinity, and lifts
                // every class above that of x -- log(x) becomes x, and x becomes e^x, which
                // is an exponential the algorithm can use.
                // Substituted into rather than worked out again: asking for the mrv set of
                // the lifted expression would ask for the limit of x, which is the problem
                // being solved, and the two would call each other until the depth ran out.
                var lifted = new HashSet<Entity>();
                foreach (var member in omega)
                    lifted.Add(member.Substitute(x, Exponential(x)));
                e = e.Substitute(x, Exponential(x));
                omega = lifted;
            }

            var w = Variable.CreateTemp(e.Vars);
            if (Rewrite(e, omega, x, w) is not var (rewritten, logarithmOfW))
            { Trace($"  rewrite({e.Stringize()})", null); return null; }
            Trace($"  rewrite -> ", $"{rewritten.Stringize()}   logw={logarithmOfW.Stringize()}");

            // How far the series has to be carried is not known in advance -- the leading
            // terms may cancel, which is the case the whole algorithm exists for -- so the
            // order is raised until something survives.
            for (var order = 2; order <= 32; order *= 2)
            {
                MultithreadingFunctional.ExitIfCancelled();
                var series = AsymptoticSeries.Expand(rewritten, w, logarithmOfW, ERational.FromInt32(order));
                if (series is null)
                { Trace($"  series(order {order})", null); return null; }
                if (series.LeadingTerm() is not var (coefficient, power))
                    continue;                       // everything cancelled; look further out
                // The coefficient has to be free of w and slower than it, or the split into
                // coefficient and power is not the one the algorithm's conclusion rests on.
                return coefficient.ContainsNode(w) ? null : (coefficient, power);
            }
            return null;
        }

        /// <summary>
        /// The expression rewritten with every member of its mrv set expressed as a power of
        /// one of them, together with the value of the logarithm of that one.
        /// </summary>
        private static (Entity Rewritten, Entity LogarithmOfW)? Rewrite(
            Entity e, HashSet<Entity> omega, Variable x, Variable w)
        {
            var members = new List<(Entity Member, Entity Exponent)>();
            foreach (var member in omega)
            {
                if (!IsExponential(member, out var exponent))
                    return null;                    // after lifting, every member is an exponential
                members.Add((member, exponent));
            }

            // A member containing another member has to be rewritten before the one it
            // contains, or the inner one is no longer there to be found. The one containing
            // none of the others is the one everything is written in terms of: choosing any
            // other reintroduces a member inside the coefficients, and the leading
            // coefficient then lies in the same class as w, which is what the conclusion
            // below is not allowed to rest on.
            int Contained(Entity member)
                => members.Count(other => other.Member != member && member.ContainsNode(other.Member));
            members.Sort((a, b) => Contained(b.Member).CompareTo(Contained(a.Member)));

            var (_, chosen) = members[members.Count - 1];
            if (Sign(chosen, x) is not { } signOfExponent || signOfExponent == 0)
                return null;

            // w has to tend to zero. Where the chosen member runs off to infinity it is its
            // reciprocal that does, so the substitution is by 1/w and the logarithm flips.
            Entity substituted = signOfExponent > 0 ? 1 / w : w;
            var logarithmOfW = signOfExponent > 0 ? -chosen : chosen;

            var rewritten = e;
            foreach (var (member, exponent) in members)
            {
                if (LimitInf(Bare((exponent / chosen).Simplify()), x) is not { } comparison)
                    return null;
                if (Bare(comparison).Evaled is not Rational power || !power.ERational.IsFinite)
                    return null;
                // exp(exponent - c * chosen) is left as one exponential on purpose. Splitting
                // it into a product of exponentials would put back a function of a faster
                // class than w and the series below would no longer mean anything.
                var coefficient = Exponential(Bare((exponent - power * chosen).Simplify()));
                rewritten = rewritten.Substitute(member, coefficient * MathS.Pow(substituted, power));
            }
            if (rewritten.ContainsNode(x) && !logarithmOfW.ContainsNode(x))
                return null;
            // A member the substitution did not find is a member left in the series, where it
            // is read as part of a coefficient and the conclusion is drawn from a leading term
            // that is not the leading term. There is nothing to salvage from that, and saying
            // nothing is the only safe reading -- this is where x^x / e^(x * ln(x)) answered 0.
            if (members.Any(member => rewritten.ContainsNode(member.Member)))
                return null;
            return (rewritten, logarithmOfW);
        }
    }
}
