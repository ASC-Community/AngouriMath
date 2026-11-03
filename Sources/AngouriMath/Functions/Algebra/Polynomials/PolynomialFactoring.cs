//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Functions.Algebra.AnalyticalSolving;
using PeterO.Numbers;
using System.Diagnostics.CodeAnalysis;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions
{
    /// <summary>
    /// Factors a polynomial in one variable into linear factors with whole roots:
    /// <c>x^2 + 2x + 1</c> becomes <c>(x + 1)^2</c>, and
    /// <c>x^3 - 6x^2 + 11x - 6</c> becomes <c>(x - 1)(x - 2)(x - 3)</c>.
    /// </summary>
    /// <remarks>
    /// Deliberately narrow, on two counts.
    /// <para>
    /// Rational roots only. Factoring through every root would answer
    /// <c>(x - i)(x + i)</c> for <c>x^2 + 1</c> and <c>(x - sqrt(2))(x + sqrt(2))</c> for
    /// <c>x^2 - 2</c>, which is not what anyone means by factoring those.
    /// </para>
    /// <para>
    /// And only when the polynomial splits completely into whole roots. A partly factored
    /// answer is not obviously better than the sum it came from, and fractional roots turn
    /// up mostly in the output of calculus, where the expanded form is the conventional
    /// one: the antiderivative of <c>x^2 + x</c> reads better as <c>x^3/3 + x^2/2</c> than
    /// as <c>x^2 * (x + 3/2) / 3</c>.
    /// </para>
    /// <para>
    /// What comes out of here is a candidate, not a decision. The simplifier keeps it
    /// alongside the other forms it has found and picks between them by its complexity
    /// metric, which is why <c>x^2 - 1</c> stays as it is while <c>(x + 1)^2</c> wins.
    /// </para>
    /// </remarks>
    internal static class PolynomialFactoring
    {
        /// <summary>Above this the search costs more than the answer is worth.</summary>
        private const int MaxDegree = 16;

        /// <summary>
        /// Rational roots are found by trying divisors of the first and last coefficients,
        /// so those have to be small enough to factor cheaply. A polynomial with a
        /// coefficient past this is left alone rather than paid for.
        /// </summary>
        [ConstantField] private static readonly EInteger MaxCoefficientToFactor = EInteger.FromInt32(1000000);

        internal static bool TryFactor(Entity expr, Variable x, [NotNullWhen(true)] out Entity? factored)
        {
            factored = null;
            if (!TryGetRationalCoefficients(expr, x, out var coefficients))
                return false;

            var degree = coefficients.Length - 1;
            var roots = new List<ERational>();
            // A zero constant term means x itself divides the polynomial. Dividing by
            // (x - 0) is just dropping that coefficient, so it is done first and cheaply.
            while (coefficients.Length > 1 && coefficients[0].IsZero)
            {
                roots.Add(ERational.Zero);
                // Copied rather than sliced: the range operator needs a helper that
                // netstandard2.0, which this project also targets, does not have.
                var shifted = new ERational[coefficients.Length - 1];
                System.Array.Copy(coefficients, 1, shifted, 0, shifted.Length);
                coefficients = shifted;
            }

            foreach (var candidate in RationalRootCandidates(coefficients))
                while (coefficients.Length > 1 && Evaluate(coefficients, candidate).IsZero)
                {
                    roots.Add(candidate);
                    coefficients = DivideByLinear(coefficients, candidate);
                }

            // Offered only when the polynomial splits completely into linear factors with
            // whole roots. Partly factored answers are not obviously better than the sum
            // they came from, and fractional roots come up mostly in the output of
            // calculus, where the expanded form is the conventional one: the
            // antiderivative of x^2 + x reads better as x^3/3 + x^2/2 than as
            // x^2 * (x + 3/2) / 3, which is what factoring through its root at -3/2 gives.
            if (roots.Count != degree || roots.Any(root => !root.IsFinite || !root.Denominator.Equals(EInteger.One)))
                return false;

            var factors = new List<Entity>();
            var remainder = BuildPolynomial(coefficients, x);
            if (remainder != Integer.One)
                factors.Add(remainder);
            foreach (var group in roots.GroupBy(root => root))
            {
                // x - r, written as x + |r| when r is negative so that the printed form is
                // the usual one rather than `x - (-1)`.
                var root = group.Key;
                Entity linear = root.IsZero ? x
                    : root.Sign < 0 ? x + Rational.Create(root.Negate())
                    : x - Rational.Create(root);
                factors.Add(group.Count() == 1 ? linear : linear.Pow(group.Count()));
            }

            factored = factors.Aggregate((left, right) => left * right);
            return degree > 1;
        }

        /// <summary>
        /// <paramref name="expr"/> written as the linear factors of its rational roots
        /// times whatever will not divide, or <see langword="false"/> where it has no
        /// rational root at all.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="TryFactor"/> answers a different question and answers it more
        /// strictly: it offers a factorization only when the polynomial splits completely
        /// into whole linear factors, because a partly factored sum is not obviously a
        /// better way of *writing* the same thing. Solving is not a question of how to
        /// write it. <c>(x - 1)(x^2 - 3)</c> is no nicer to read than
        /// <c>x^3 - x^2 - 3x + 3</c>, but it is the difference between answering with
        /// <c>1</c> and <c>+-sqrt(3)</c> and answering with two nested cube roots of
        /// <c>26 + 18i</c> -- and above degree four, between answering at all and handing
        /// the equation to the numeric solver.
        /// https://github.com/asc-community/AngouriMath/issues/272
        /// </para>
        /// <para>
        /// Fractional roots are kept here where <see cref="TryFactor"/> declines them, for
        /// the same reason: a root of <c>1/2</c> is as exact an answer as any, whatever
        /// the factored form looks like on the page.
        /// </para>
        /// </remarks>
        internal static bool TrySplitOffRationalRoots(
            Entity expr, Variable x, [NotNullWhen(true)] out Entity? factored)
        {
            factored = null;
            if (!TryGetRationalCoefficients(expr, x, out var coefficients))
                return false;

            // Two terms is a x^n + b, which is answered whole by inverting x^n = -b/a, and
            // that gives the n roots in the form a + bi. Splitting it would divide out the
            // rational ones and leave the rest to be dug out of a quotient: x^3 - 8 reads
            // as 2 and (-1/2 +- i*sqrt(3)/2)*2 that way, and as 2 and (-2 -+ sqrt(-12))/2
            // this way. Nothing here improves on that, so it is left alone.
            if (coefficients.Count(coefficient => !coefficient.IsZero) <= 2)
                return false;

            var factors = new List<Entity>();
            foreach (var candidate in RootCandidates(coefficients))
                while (coefficients.Length > 1 && Evaluate(coefficients, candidate).IsZero)
                {
                    factors.Add(LinearFactor(candidate, x));
                    coefficients = DivideByLinear(coefficients, candidate);
                }
            if (factors.Count == 0)
                return false;

            var remainder = BuildPolynomial(coefficients, x);
            if (remainder != Integer.One)
                factors.Add(remainder);
            factored = factors.Aggregate((left, right) => left * right);
            return true;
        }

        /// <summary>
        /// One step of a partial fraction decomposition, at a rational root of the
        /// denominator: <c>N/D</c> becomes <c>A/(x - r) + R/Q</c>, where <c>D = (x - r)Q</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// One step, and not the whole decomposition, because a step is all that is needed:
        /// what is left over is a smaller problem of the same kind, and by the time its
        /// denominator is a quadratic there is already a rule for it. So
        /// <c>1/(x^3 + 1)</c> splits into <c>(1/3)/(x + 1)</c> and <c>(2 - x)/(3(x^2 - x + 1))</c>,
        /// and the second of those is a quotient the integrator can read as it stands.
        /// </para>
        /// <para>
        /// The coefficient is Heaviside's: at the root every other term of the
        /// decomposition is finite, so <c>A = N(r)/Q(r)</c>. What is left, <c>N - A*Q</c>,
        /// then has <c>r</c> for a root by construction and divides exactly.
        /// </para>
        /// <para>
        /// Rational roots only, and simple ones: a repeated root needs a term over
        /// <c>(x - r)^2</c> as well, which this does not produce, so it declines instead.
        /// Denominators of degree below three are left alone, since the rules for a linear
        /// or quadratic denominator already answer those and answer them in one piece.
        /// </para>
        /// </remarks>
        internal static bool TrySplitOffRationalRoot(
            Entity numerator, Entity denominator, Variable x,
            [NotNullWhen(true)] out Entity? simplePart,
            [NotNullWhen(true)] out Entity? restNumerator,
            [NotNullWhen(true)] out Entity? restDenominator)
        {
            simplePart = restNumerator = restDenominator = null;
            if (!TryGetRationalCoefficients(denominator, x, leastTerms: 1, leastDegree: 3, out var d)
                || !TryGetRationalCoefficients(numerator, x, leastTerms: 1, leastDegree: 0, out var n))
                return false;
            // Only a proper fraction decomposes; an improper one is a polynomial plus a
            // proper fraction and has to be divided out first, which is not done here.
            if (n.Length >= d.Length)
                return false;

            foreach (var root in RootCandidates(d))
            {
                if (!Evaluate(d, root).IsZero)
                    continue;

                // Divide the root out as many times as it goes. Stopping at one and giving up
                // on the rest left 1/(x^4 + x^2) with no antiderivative: its only rational
                // root is zero, twice, so a single division leaves x^3 + x, which is zero
                // there again. A root of multiplicity m contributes a term over the m-th
                // power, and one degree still comes off the denominator, so this ends for the
                // same reason the single case did.
                var q = d;
                var multiplicity = 0;
                do
                {
                    q = DivideByLinear(q, root);
                    multiplicity++;
                }
                while (q.Length > 1 && Evaluate(q, root).IsZero);

                var atRoot = Evaluate(q, root);
                if (atRoot.IsZero)
                    continue;                       // the denominator is a power of this factor alone

                var a = Evaluate(n, root).Divide(atRoot);
                var left = Subtract(n, Scale(q, a));
                if (!Evaluate(left, root).IsZero)
                    continue;                       // cannot happen; checked rather than assumed

                var factor = LinearFactor(root, x);
                simplePart = Rational.Create(a) / (multiplicity == 1 ? factor : factor.Pow(multiplicity));

                // What is left is over (x - r)^(m-1) times q, having cancelled one (x - r)
                // against the numerator, which vanishes at the root by construction.
                var restCoefficients = q;
                for (var i = 1; i < multiplicity; i++)
                    restCoefficients = MultiplyByLinear(restCoefficients, root);
                restNumerator = BuildPolynomial(DivideByLinear(left, root), x);
                restDenominator = BuildPolynomial(restCoefficients, x);
                return true;
            }
            return false;
        }

        /// <summary>Multiplies by <c>x - root</c>, the inverse of <see cref="DivideByLinear"/>.</summary>
        private static ERational[] MultiplyByLinear(ERational[] coefficients, ERational root)
        {
            var product = new ERational[coefficients.Length + 1];
            for (var i = 0; i < product.Length; i++)
                product[i] = ERational.Zero;
            for (var i = 0; i < coefficients.Length; i++)
            {
                product[i + 1] = product[i + 1].Add(coefficients[i]);
                product[i] = product[i].Subtract(coefficients[i].Multiply(root));
            }
            return product;
        }

        /// <summary>
        /// Zero first, then the candidates of the rational root theorem, which cannot offer
        /// it: they are built from divisors of the constant term, and a polynomial with a
        /// root at zero has none.
        /// </summary>
        private static IEnumerable<ERational> RootCandidates(ERational[] coefficients)
            => coefficients[0].IsZero
                ? new[] { ERational.Zero }.Concat(RationalRootCandidates(coefficients))
                : RationalRootCandidates(coefficients);

        /// <summary>x - r, or x where r is zero.</summary>
        private static Entity LinearFactor(ERational root, Variable x)
            => root.IsZero ? x
                : root.Sign < 0 ? x + Rational.Create(root.Negate())
                : x - Rational.Create(root);

        private static ERational[] Scale(ERational[] coefficients, ERational by)
            => coefficients.Select(c => c.Multiply(by)).ToArray();

        private static ERational[] Subtract(ERational[] left, ERational[] right)
        {
            var result = new ERational[System.Math.Max(left.Length, right.Length)];
            for (var i = 0; i < result.Length; i++)
                result[i] = (i < left.Length ? left[i] : ERational.Zero)
                    .Subtract(i < right.Length ? right[i] : ERational.Zero);
            return result;
        }

        /// <summary>
        /// The coefficients of <paramref name="expr"/> in <paramref name="x"/>, lowest
        /// power first, or <see langword="false"/> if it is not a polynomial in
        /// <paramref name="x"/> with rational coefficients alone.
        /// </summary>
        private static bool TryGetRationalCoefficients(Entity expr, Variable x, out ERational[] coefficients)
            => TryGetRationalCoefficients(expr, x, leastTerms: 2, leastDegree: 2, MaxDegree, out coefficients);

        private static bool TryGetRationalCoefficients(
            Entity expr, Variable x, int leastTerms, int leastDegree, out ERational[] coefficients)
            => TryGetRationalCoefficients(expr, x, leastTerms, leastDegree, MaxDegree, out coefficients);

        /// <summary>
        /// The coefficients of <paramref name="expr"/> in <paramref name="x"/>, lowest power
        /// first, or <see langword="false"/> if it is not a polynomial in <paramref name="x"/>
        /// with rational coefficients alone.
        /// </summary>
        /// <remarks>
        /// The degree ceiling is the caller's because it is a statement about what that caller
        /// can afford, not about what a polynomial is: root-finding here gives up at sixteen,
        /// while <see cref="PolynomialFactorization"/> reaches further because Berlekamp and
        /// the Hensel lift stay cheap where a divisor search does not.
        /// </remarks>
        internal static bool TryGetRationalCoefficients(
            Entity expr, Variable x, int leastTerms, int leastDegree, int maxDegree,
            out ERational[] coefficients)
        {
            coefficients = System.Array.Empty<ERational>();
            // Read off the tree before anything is expanded. The expansion below is the cost
            // of this whole method, and it was paid in full for an equation this cannot
            // accept: the solver asks twice per equation it visits, on the way down through
            // every replacement, and cos(x) + sin(x) - r with r a page of radicals was
            // expanded twice at each of eighteen visits to be told that a sine is not a
            // monomial. A third of SolveHard's allocation was that.
            // https://github.com/asc-community/AngouriMath/issues/746
            if (!MayBeAPolynomialWithRationalCoefficients(expr, x))
                return false;
            // Through the simplification's expansions: it asks this of every candidate in every
            // variable, and of the same candidates again in its nested runs.
            var monomials = PolynomialSolver.GatherMonomialInformation<EInteger, TreeAnalyzer.PrimitiveInteger>(
                Sumf.LinearChildren(Simplificator.Expanded(expr)), x);
            if (monomials is null || monomials.Count < leastTerms)
                return false;

            var degree = monomials.Keys.Max();
            if (degree is null || degree > maxDegree || degree < leastDegree)
                return false;

            var found = new ERational[degree.ToInt32Checked() + 1];
            for (var i = 0; i < found.Length; i++)
                found[i] = ERational.Zero;
            // .Key/.Value rather than deconstruction: KeyValuePair has no Deconstruct on
            // netstandard2.0, which this project also targets.
            foreach (var monomial in monomials)
            {
                var power = monomial.Key;
                if (power is null || power.Sign < 0 || monomial.Value.Evaled is not Rational ratio)
                    return false;
                found[power.ToInt32Checked()] = ratio.ERational;
            }

            coefficients = found;
            return true;
        }

        /// <summary>
        /// Whether <paramref name="expr"/> has the shape of a polynomial in <paramref name="x"/>
        /// whose coefficients could all be rational: <paramref name="x"/> occurs only under
        /// sums, products, quotients by something free of it, and whole non-negative powers,
        /// and no subtree free of <paramref name="x"/> carries a symbol or a constant. A
        /// necessary condition, read off the tree without building anything; what passes it
        /// is still expanded and checked.
        /// </summary>
        /// <remarks>
        /// A symbol in a coefficient is refused outright rather than expanded and evaluated,
        /// because a coefficient with a symbol in it is not rational, and the one way it could
        /// still be -- the symbol cancelling against itself across terms -- is what
        /// <see cref="Entity.InnerSimplified"/> has already collected before anything here is
        /// asked. A constant is refused for the same reason: pi is not rational either. An
        /// irrational literal such as <c>sqrt(2)</c> is not refused here, since it is a power of
        /// a rational and reads as one; the expansion still decides it.
        /// </remarks>
        private static bool MayBeAPolynomialWithRationalCoefficients(Entity expr, Variable x)
        {
            if (!expr.ContainsNode(x))
                return expr.VarsAndConsts.Count == 0;
            return expr switch
            {
                Variable => true,
                Sumf(var augend, var addend)
                    => MayBeAPolynomialWithRationalCoefficients(augend, x) && MayBeAPolynomialWithRationalCoefficients(addend, x),
                Minusf(var minuend, var subtrahend)
                    => MayBeAPolynomialWithRationalCoefficients(minuend, x) && MayBeAPolynomialWithRationalCoefficients(subtrahend, x),
                Mulf(var multiplier, var multiplicand)
                    => MayBeAPolynomialWithRationalCoefficients(multiplier, x) && MayBeAPolynomialWithRationalCoefficients(multiplicand, x),
                Divf(var dividend, var divisor)
                    => !divisor.ContainsNode(x)
                       && MayBeAPolynomialWithRationalCoefficients(dividend, x)
                       && MayBeAPolynomialWithRationalCoefficients(divisor, x),
                Powf(var @base, Integer { IsNegative: false })
                    => MayBeAPolynomialWithRationalCoefficients(@base, x),
                _ => false
            };
        }

        /// <summary>
        /// Every p/q that could be a root, by the rational root theorem: p divides the
        /// constant term and q the leading one.
        /// </summary>
        private static IEnumerable<ERational> RationalRootCandidates(ERational[] coefficients)
        {
            // Cleared of denominators first, so that the theorem applies.
            var scale = coefficients.Aggregate(EInteger.One, (acc, c) => Lcm(acc, c.Denominator));
            var whole = coefficients.Select(c => c.Numerator * scale.Divide(c.Denominator)).ToArray();

            var constant = whole[0].Abs();
            var leading = whole[whole.Length - 1].Abs();
            if (constant.IsZero || constant > MaxCoefficientToFactor || leading > MaxCoefficientToFactor)
                return Enumerable.Empty<ERational>();

            var candidates = new List<ERational>();
            foreach (var p in Divisors(constant))
                foreach (var q in Divisors(leading))
                {
                    candidates.Add(ERational.Create(p, q));
                    candidates.Add(ERational.Create(p.Negate(), q));
                }
            return candidates.Distinct();
        }

        private static EInteger Lcm(EInteger a, EInteger b) => a.Divide(a.GreatestCommonDivisor(b)).Multiply(b);

        private static IEnumerable<EInteger> Divisors(EInteger n)
        {
            var divisors = new List<EInteger> { EInteger.One };
            foreach (var (prime, power) in n.Factorize())
            {
                var extended = new List<EInteger>(divisors);
                var multiplier = EInteger.One;
                for (var i = EInteger.Zero; i < power; i += 1)
                {
                    multiplier = multiplier.Multiply(prime);
                    foreach (var divisor in divisors)
                        extended.Add(divisor.Multiply(multiplier));
                }
                divisors = extended;
            }
            return divisors;
        }

        /// <summary>Horner, on the exact ratios.</summary>
        private static ERational Evaluate(ERational[] coefficients, ERational at)
        {
            var acc = ERational.Zero;
            for (var i = coefficients.Length - 1; i >= 0; i--)
                acc = acc.Multiply(at).Add(coefficients[i]);
            return acc;
        }

        /// <summary>
        /// Synthetic division by <c>x - root</c>, which is exact because the root is one.
        /// </summary>
        private static ERational[] DivideByLinear(ERational[] coefficients, ERational root)
        {
            var quotient = new ERational[coefficients.Length - 1];
            var carry = ERational.Zero;
            for (var i = coefficients.Length - 1; i >= 1; i--)
            {
                carry = coefficients[i].Add(carry.Multiply(root));
                quotient[i - 1] = carry;
            }
            return quotient;
        }

        private static Entity BuildPolynomial(ERational[] coefficients, Variable x)
        {
            Entity result = Integer.Create(0);
            for (var i = coefficients.Length - 1; i >= 0; i--)
            {
                if (coefficients[i].IsZero)
                    continue;
                Entity term = Rational.Create(coefficients[i]);
                if (i == 1)
                    term = term == Integer.One ? x : term * x;
                else if (i > 1)
                    term = term == Integer.One ? x.Pow(i) : term * x.Pow(i);
                result = result == Integer.Create(0) ? term : result + term;
            }
            return result.InnerSimplified;
        }
    }
}
