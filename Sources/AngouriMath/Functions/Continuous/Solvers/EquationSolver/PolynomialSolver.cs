//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using AngouriMath.Core.Multithreading;
using PeterO.Numbers;
using static AngouriMath.Entity;

namespace AngouriMath.Functions
{
    internal static partial class TreeAnalyzer
    {
        internal interface IPrimitive<T>
        {
            void Add(Real a);
            void AddMp(T a, Real b);
            void Clear();
            T Value { get; }
            bool AllowFloat { get; }
        }
        internal sealed class PrimitiveDecimal : IPrimitive<EDecimal>
        {
            public void Add(Real a) => Value += a.EDecimal;
            public void AddMp(EDecimal a, Real b) => Value += a * b.EDecimal;
            public void Clear() => Value = 0;
            public EDecimal Value { get; private set; } = 0;
            public bool AllowFloat => true;
        }
        internal sealed class PrimitiveInteger : IPrimitive<EInteger>
        {
            public void Add(Real a) => Value += a.EDecimal.ToEInteger();
            public void AddMp(EInteger a, Real b) => Value += (a * b.EDecimal).ToEInteger();
            public void Clear() => Value = 0;
            public EInteger Value { get; private set; } = 0;
            public bool AllowFloat => false;
        }
        /// <summary>
        /// If an evaluable expression is equal to zero, <see langword="true"/>, otherwise, <see langword="false"/>
        /// For example, 1 - 1 is zero, but 1 + a is not
        /// </summary>
        internal static bool IsZero(Entity e) => e.Evaled is Complex c && c == 0;
    }
}

namespace AngouriMath.Functions.Algebra.AnalyticalSolving
{
    /// <summary>Solves all forms of Polynomials that are trivially solved</summary>
    internal static class PolynomialSolver
    {
        /// <summary>Solves ax + b</summary>
        /// <param name="a">Coefficient of x</param>
        /// <param name="b">Free coefficient</param>
        /// <returns>Set of roots</returns>
        // ax + b = 0
        // ax = -b
        // x = -b / a
        internal static IEnumerable<Entity> SolveLinear(Entity a, Entity b) => new[] { (-b / a).InnerSimplified };

        /// <summary>Solves ax^2 + bx + c</summary>
        /// <param name="a">Coefficient of x^2</param>
        /// <param name="b">Coefficient of x</param>
        /// <param name="c">Free coefficient</param>
        /// <returns>Set of roots</returns>
        internal static IEnumerable<Entity> SolveQuadratic(Entity a, Entity b, Entity c)
        {
            if (TreeAnalyzer.IsZero(c)) return SolveLinear(a, b).Append(0);
            if (TreeAnalyzer.IsZero(a)) return SolveLinear(b, c);
            if (RootsOverASquareDiscriminant(a, b, c) is { } exact)
                return exact;
            var D = MathS.Sqr(b) - 4 * a * c;
            return new[] { ((-b - MathS.Sqrt(D)) / (2 * a)).InnerSimplified,
                           ((-b + MathS.Sqrt(D)) / (2 * a)).InnerSimplified };
        }

        /// <summary>
        /// The two roots, written without a radical, where the coefficients are polynomials and
        /// the discriminant is a rational number times the square of one; otherwise
        /// <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Either square root of the discriminant will do. The roots are <c>(-b - r)/(2a)</c> and
        /// <c>(-b + r)/(2a)</c>, the same pair for <c>r</c> and <c>-r</c>, so any <c>r</c> with
        /// <c>r^2 = b^2 - 4ac</c> gives the same set, and a polynomial one needs no absolute value.
        /// <c>a z^2 - (a^2 - bc) z - abc</c> has discriminant <c>(a^2 + bc)^2</c> and roots
        /// <c>a</c> and <c>-bc/a</c>; through <c>sqrt</c> they were written with the square under
        /// it, and <c>z^2 - (p + q) z + pq</c> with <c>sqrt((p + q)^2 - 4pq)</c> for <c>p - q</c>.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/2140">#2140</a>
        /// </para>
        /// <para>
        /// The numerators are polynomials then too, so each root is reduced by the greatest common
        /// divisor of its numerator and <c>2a</c> rather than left for a later pass to cancel.
        /// The discriminant's rational factor comes out first, and where its square root is not
        /// rational -- <c>2 (p - q)^2</c>, or <c>-(p - q)^2</c> -- the roots are written with it,
        /// <c>sqrt(2) (p - q)</c> and <c>i (p - q)</c>, and left as quotients. A discriminant that
        /// is a number is left to the formula, whose evaluation settles it already.
        /// </para>
        /// </remarks>
        private static Entity[]? RootsOverASquareDiscriminant(Entity a, Entity b, Entity c)
        {
            // A square has an even degree in every variable, and where 2 deg b and deg a + deg c
            // differ, that larger one is the discriminant's degree. Most quadratics with a symbol
            // in them are refused here, before any polynomial is built: x^2 + x + a, whose
            // discriminant 1 - 4a is of degree one in a, allocated 4.6% more to be solved without
            // it. Degrees as written bound the true ones, so a coefficient that cancels inside
            // itself can be refused when it need not be, which only leaves it to the formula.
            if (!HasEvenDegreesWherever(a, b, c))
                return null;
            var names = new SortedSet<Variable>(Comparer<Variable>.Create(static (x, y) => string.CompareOrdinal(x.Name, y.Name)));
            foreach (var coefficient in new[] { a, b, c })
                foreach (var variable in coefficient.Vars)
                    names.Add(variable);
            if (names.Count == 0 || names.Count > MultivariatePolynomial.MaxVariables)
                return null;
            var variables = names.ToList();
            var indices = new Dictionary<Variable, int>(variables.Count);
            for (var i = 0; i < variables.Count; i++)
                indices[variables[i]] = i;
            if (MultivariatePolynomial.TryParse(a, indices) is not { } pa
                || MultivariatePolynomial.TryParse(b, indices) is not { } pb
                || MultivariatePolynomial.TryParse(c, indices) is not { } pc
                || pb.Multiply(pb) is not { } bSquared || pa.Multiply(pc) is not { } ac)
                return null;
            var discriminant = bSquared.Subtract(ac.ScaleBy(ERational.FromInt32(4)));
            if (discriminant.Normalized(out var scale).TrySquareRoot() is not { } root)
                return null;
            var denominator = pa.ScaleBy(ERational.FromInt32(2));
            var order = Enumerable.Range(0, variables.Count).ToArray();
            // A quotient over 2a with what it shares with 2a cancelled, rational factors included.
            Entity Reduced(MultivariatePolynomial numerator)
            {
                if (numerator.IsZero)
                    return Integer.Zero;
                var below = denominator;
                if (PolynomialGcd.Gcd(numerator, denominator, order, 0) is { } common
                    && numerator.DivideExact(common) is { } reducedAbove
                    && denominator.DivideExact(common) is { } reducedBelow)
                    (numerator, below) = (reducedAbove, reducedBelow);
                below = below.Normalized(out var unit);
                numerator = numerator.ScaleBy(unit);
                return below.IsConstant
                    ? numerator.ScaleBy(ERational.One.Divide(below.Terms.Single().Value)).ToEntity(variables)
                    : numerator.ToEntity(variables) / below.ToEntity(variables);
            }
            var minusB = MultivariatePolynomial.Zero(variables.Count).Subtract(pb);
            var overScale = ERational.Create(scale.Denominator, scale.Numerator);
            if (MultivariatePolynomial.Constant(variables.Count, overScale).TrySquareRoot() is { } rational)
            {
                var r = root.ScaleBy(rational.IsZero ? ERational.Zero : rational.Terms.Single().Value);
                return new[] { Reduced(minusB.Subtract(r)), Reduced(minusB.Add(r)) };
            }
            // sqrt(2) (p - q), i (p - q): sqrt(n/d) is (s/d) sqrt(k) for n d = s^2 k, the s/d goes
            // into the polynomial, and sqrt(k) -- or i sqrt(k) for a negative scale -- is the one
            // factor left outside it.
            var (square, rest) = SquarePart(overScale.Numerator.Multiply(overScale.Denominator).Abs());
            var outside = MathS.Sqrt(overScale.Sign < 0 ? -(Entity)Integer.Create(rest) : Integer.Create(rest)).InnerSimplified;
            var middle = Reduced(minusB);
            var spread = (outside * Reduced(root.ScaleBy(ERational.Create(square, overScale.Denominator)))).InnerSimplified;
            return new[] { (middle - spread).InnerSimplified, (middle + spread).InnerSimplified };
        }

        /// <summary>
        /// Whether, for every variable in <paramref name="a"/>, <paramref name="b"/> or
        /// <paramref name="c"/>, the degree of <c>b^2 - 4ac</c> is not known to be odd.
        /// </summary>
        private static bool HasEvenDegreesWherever(Entity a, Entity b, Entity c)
        {
            static bool Even(Entity a, Entity b, Entity c, Variable v)
            {
                var (twiceB, ac) = (2 * DegreeAsWritten(b, v), DegreeAsWritten(a, v) + DegreeAsWritten(c, v));
                return twiceB == ac || System.Math.Max(twiceB, ac) % 2 == 0;
            }
            foreach (var coefficient in new[] { a, b, c })
                foreach (var variable in coefficient.Vars)
                    if (!Even(a, b, c, variable))
                        return false;
            return true;
        }

        /// <summary>
        /// The degree of <paramref name="expr"/> in <paramref name="x"/> as it is written, which
        /// bounds the degree it has: of a power its base's times the exponent, of a product the
        /// sum, of a sum the most.
        /// </summary>
        private static int DegreeAsWritten(Entity expr, Variable x) => expr switch
        {
            _ when !expr.ContainsNode(x) => 0,
            Variable => 1,
            Powf(var @base, Integer power) when power.EInteger.CanFitInInt32() => DegreeAsWritten(@base, x) * power.EInteger.ToInt32Checked(),
            Mulf(var left, var right) => DegreeAsWritten(left, x) + DegreeAsWritten(right, x),
            Sumf(var left, var right) => System.Math.Max(DegreeAsWritten(left, x), DegreeAsWritten(right, x)),
            Minusf(var left, var right) => System.Math.Max(DegreeAsWritten(left, x), DegreeAsWritten(right, x)),
            _ => 0,
        };

        /// <summary>
        /// <paramref name="value"/> as <c>s^2 k</c>, with the square taken out by trial division
        /// by the primes up to 1000 and by whatever of <c>k</c> is left a square; a larger square
        /// factor stays in <c>k</c>, which leaves <c>sqrt(k)</c> correct and only less reduced.
        /// </summary>
        private static (EInteger Root, EInteger Remaining) SquarePart(EInteger value)
        {
            var root = EInteger.One;
            var rest = value;
            for (var prime = 2; prime <= 1000 && EInteger.FromInt32(prime * prime).CompareTo(rest) <= 0; prime++)
            {
                var square = EInteger.FromInt32(prime * prime);
                while (rest.Remainder(square).IsZero)
                {
                    rest = rest.Divide(square);
                    root = root.Multiply(prime);
                }
            }
            var whole = rest.Sqrt();
            return whole.Multiply(whole).Equals(rest) ? (root.Multiply(whole), EInteger.One) : (root, rest);
        }

        /// <summary>Solves ax^3 + bx^2 + cx + d</summary>
        /// <param name="a">Coefficient of x^3</param>
        /// <param name="b">Coefficient of x^2</param>
        /// <param name="c">Coefficient of x</param>
        /// <param name="d">Free coefficient</param>
        /// <returns>Set of roots</returns>
        internal static IEnumerable<Entity> SolveCubic(Entity a, Entity b, Entity c, Entity d)
        {
            // en: https://en.wikipedia.org/wiki/Cubic_equation
            // ru: https://ru.wikipedia.org/wiki/%D0%A4%D0%BE%D1%80%D0%BC%D1%83%D0%BB%D0%B0_%D0%9A%D0%B0%D1%80%D0%B4%D0%B0%D0%BD%D0%BE

            if (TreeAnalyzer.IsZero(d)) return SolveQuadratic(a, b, c).Append(0);
            if (TreeAnalyzer.IsZero(a)) return SolveQuadratic(b, c, d);

            var coeff = MathS.i * MathS.Sqrt(3) / 2;
            var u1 = Integer.Create(1);
            var u2 = Rational.Create(-1, 2) + coeff;
            var u3 = Rational.Create(-1, 2) - coeff;
            var D0 = MathS.Sqr(b) - 3 * a * c;
            var D1 = (2 * MathS.Pow(b, 3) - 9 * a * b * c + 27 * MathS.Sqr(a) * d).InnerSimplified;
            var C = MathS.Pow((D1 + MathS.Sqrt(MathS.Sqr(D1) - 4 * MathS.Pow(D0, 3))) / 2, Rational.Create(1, 3));

            return new[] { u1, u2, u3 }.Select(uk =>
                C.Evaled == 0 && D0.Evaled == 0 ? -(b + uk * C) / 3 / a : -(b + uk * C + D0 / C / uk) / 3 / a);
        }

        [ConstantField] private static readonly int[] sqrtsOf1 = { -1, 1 };
        /// <summary>Solves ax^4 + bx^3 + cx^2 + dx + e</summary>
        /// <param name="a">Coefficient of x^4</param>
        /// <param name="b">Coefficient of x^3</param>
        /// <param name="c">Coefficient of x^2</param>
        /// <param name="d">Coefficient of x</param>
        /// <param name="e">Free coefficient</param>
        /// <returns>Set of roots</returns>
        internal static IEnumerable<Entity> SolveQuartic(Entity a, Entity b, Entity c, Entity d, Entity e)
        {
            // en: https://en.wikipedia.org/wiki/Quartic_function
            // ru: https://ru.wikipedia.org/wiki/%D0%9C%D0%B5%D1%82%D0%BE%D0%B4_%D0%A4%D0%B5%D1%80%D1%80%D0%B0%D1%80%D0%B8

            if (TreeAnalyzer.IsZero(e)) return SolveCubic(a, b, c, d).Append(0);
            if (TreeAnalyzer.IsZero(a)) return SolveCubic(b, c, d, e);

            var alpha = (-3 * MathS.Sqr(b) / (8 * MathS.Sqr(a)) + c / a)
                .InnerSimplified;
            var beta = (MathS.Pow(b, 3) / (8 * MathS.Pow(a, 3)) - (b * c) / (2 * MathS.Sqr(a)) + d / a)
                .InnerSimplified;
            var gamma = (-3 * MathS.Pow(b, 4) / (256 * MathS.Pow(a, 4)) + MathS.Sqr(b) * c / (16 * MathS.Pow(a, 3)) - (b * d) / (4 * MathS.Sqr(a)) + e / a)
                .InnerSimplified;

            // Biquadratic once depressed: y^4 + alpha y^2 + gamma = 0 for y = x + b/(4a), so x is
            // the root of the quadratic in y^2 shifted back by b/(4a). The shift was written
            // -b / 4 * a, which is -b a / 4, and for a leading coefficient other than 1 or -1 every
            // candidate was off and failed its check: 31x^4 - 62x^3 + 33x^2 - 2x - 1 = 0, with four
            // real roots, was answered { }.
            if (beta.Evaled == 0)
                return sqrtsOf1.SelectMany(_ => sqrtsOf1,
                    (s, t) => -b / (4 * a) + s * MathS.Sqrt((-alpha + t * MathS.Sqrt(MathS.Sqr(alpha) - 4 * gamma)) / 2));

            var oneThird = Rational.Create(1, 3);
            var P = (-MathS.Sqr(alpha) / 12 - gamma)
                .InnerSimplified;
            var Q = (-MathS.Pow(alpha, 3) / 108 + alpha * gamma / 3 - MathS.Sqr(beta) / 8)
                .InnerSimplified;
            var R = -Q / 2 + MathS.Sqrt(MathS.Sqr(Q) / 4 + MathS.Pow(P, 3) / 27);
            var U = MathS.Pow(R, oneThird)
                .InnerSimplified;
            var y = (Rational.Create(-5, 6) * alpha + U + (U.Evaled == 0 ? -MathS.Pow(Q, oneThird) : -P / (3 * U)))
                .InnerSimplified;
            var W = MathS.Sqrt(alpha + 2 * y)
                .InnerSimplified;

            // Now we need to permutate all four combinations
            return sqrtsOf1.SelectMany(_ => sqrtsOf1,
                (s, t) => -b / (4 * a) + (s * W + t * MathS.Sqrt(-(3 * alpha + 2 * y + s * 2 * beta / W))) / 2);
        }

        /// <summary>
        /// So that the final list of powers contains power = 0 and all powers >= 0
        /// (e. g. if the dictionaty's keys are 3, 4, 6, the final answer will contain keys
        /// 0, 1, 3, if the dictionary's keys are -2, 0, 3, the final answer will contain keys
        /// 0, 2, 5)
        /// </summary>
        /// <param name="monomials">
        /// Dictionary to process. Key - power, value - coefficient of the corresponding term
        /// </param>
        /// <returns>
        /// Whether all initial powers where > 0 (if so, x = 0 is a root)
        /// </returns>
        internal static bool ReduceCommonPower(ref Dictionary<EInteger, Entity> monomials)
        {
            var commonPower = monomials.Keys.Min() ?? throw new AngouriBugException("No null expected");
            if (commonPower.IsZero)
                return false;
            var newDict = new Dictionary<EInteger, Entity>();
            foreach (var pair in monomials)
                newDict[pair.Key - commonPower] = pair.Value;
            monomials = newDict;
            return commonPower > 0;
        }

        /// <summary>Tries to solve as polynomial</summary>
        /// <param name="expr">Polynomial of an expression</param>
        /// <param name="subtree">
        /// The expression the polynomial of (e. g. cos(x)^2 + cos(x) + 1 is a polynomial of cos(x))
        /// </param>
        /// <param name="isIdentity">
        /// Whether every coefficient cancelled, which leaves an equation that every value of
        /// <paramref name="subtree"/> satisfies rather than one that none does
        /// </param>
        /// <returns>A finite <see cref="Set"/> if successful, <see langword="null"/> otherwise</returns>
        internal static IEnumerable<Entity>? SolveAsPolynomial(Entity expr, Variable subtree, out bool isIdentity)
        {
            isIdentity = false;

            // Safely expand the expression
            // Here we find all terms
            var children = TreeAnalyzer.GatherLinearChildrenOverSumAndExpand(expr, entity => entity.ContainsNode(subtree));
            if (children is null)
                return null;
            // // //

            // Check if all are like {1} * x^n & gather information about them
            var monomialsByPower = GatherMonomialInformation
                <EInteger, TreeAnalyzer.PrimitiveInteger>(children, subtree);
            if (monomialsByPower == null)
                return null; // meaning that the given equation is not polynomial

            // A monomial whose coefficient cancelled is not a term of the polynomial, and
            // leaving it in makes the equation look as though it had a degree it does not.
            // x - x was read as one times x and answered with the root 0, when in fact
            // every x satisfies it -- https://github.com/asc-community/AngouriMath/issues/550.
            foreach (var power in monomialsByPower.Keys.ToList())
                if (monomialsByPower[power].Evaled is Complex { IsZero: true })
                    monomialsByPower.Remove(power);
            if (monomialsByPower.Count == 0)
            {
                isIdentity = true;
                return null;
            }

            var res = ReduceCommonPower(ref monomialsByPower)
                ? new Entity[] { 0 } // x5 + x3 + x2 - common power is 2, one root is 0, then x3 + x + 1
                : Enumerable.Empty<Entity>();
            var powers = monomialsByPower.Keys.ToList();
            var gcdPower = powers.Aggregate((accumulate, current) => accumulate.GreatestCommonDivisor(current));
            // // //

            if (gcdPower.IsZero)
                gcdPower = EInteger.One;
            // Change all replacements, x6 + x3 + 1 => x2 + x + 1
            if (!gcdPower.Equals(EInteger.One))
            {
                for (int i = 0; i < powers.Count; i++)
                    powers[i] /= gcdPower;
                monomialsByPower = monomialsByPower.ToDictionary(pair => pair.Key / gcdPower, pair => pair.Value);
            }
            // // //

            MultithreadingFunctional.ExitIfCancelled();

            var gcdPowerRoots = GetAllRootsOf1(gcdPower);
            Entity GetMonomialByPower(EInteger power) =>
                monomialsByPower.TryGetValue(power, out var monomial) ? monomial.InnerSimplified : 0;
            powers.Sort();
            if (!powers.Last().CanFitInInt32())
                return null;
            return (powers.Count, powers.Last().ToInt32Unchecked()) switch
            {
                (0, _) => null,
                (_, 0) => Enumerable.Empty<Entity>(),
                (> 2, > 4) => null, // So far, we can't solve equations of powers more than 4
                // By this moment we know for sure that expr's power is <= 4, that expr is not a monomial,
                // and that it consists of more than 2 monomials
                (1, _) => new Entity[] { 0 },
                (2, _) =>
                    // Solve a x ^ n + b = 0 for x ^ n -> x ^ n = -b / a
                    MathS.Pow(subtree, Integer.Create(powers[1]))
                    .Invert((-1 * monomialsByPower[powers[0]] / monomialsByPower[powers[1]]).InnerSimplified, subtree),
                (_, 2) => SolveQuadratic(GetMonomialByPower(2), GetMonomialByPower(1), GetMonomialByPower(0)),
                (_, 3) => SolveCubic(GetMonomialByPower(3), GetMonomialByPower(2), GetMonomialByPower(1), GetMonomialByPower(0)),
                (_, 4) => SolveQuartic(GetMonomialByPower(4), GetMonomialByPower(3),
                    GetMonomialByPower(2), GetMonomialByPower(1), GetMonomialByPower(0)),
                _ => null
            } is { } resConcat
            ? !gcdPower.Equals(EInteger.One)
              // if we had x^6 + x^3 + 1, we replace it with x^2 + x + 1 and find all cubic roots of the final answer
              // x^2 = y is a quadratic in x, so a y that is a rational number times a square has
              // its two roots written without the root, as the formula's are: x^2 = (a + b)^2 was
              // { sqrt((a + b)^2), -sqrt((a + b)^2) } and is { -a - b, a + b }.
              // https://github.com/asc-community/AngouriMath/issues/2140
              ? res.Concat(resConcat).SelectMany(root =>
                  gcdPower.Equals(EInteger.FromInt32(2)) && RootsOverASquareDiscriminant(1, 0, -root) is { } exact
                  ? exact
                  : gcdPowerRoots.Select(coef => coef * MathS.Pow(root, Rational.Create(1, gcdPower))))
              : res.Concat(resConcat)
            : null;
        }

        /// <summary>Finds all terms of a polynomial</summary>
        /// <param name="terms">The terms of the sum.</param>
        /// <param name="subtree">The variable the polynomial is in.</param>
        /// <param name="aPositiveVariable">Whether the caller reads the terms only where the
        /// variable is positive: see <see cref="ParseMonomial{T, TPrimitive}"/>.</param>
        /// <returns><see langword="null"/> if polynomial is bad</returns>
        internal static Dictionary<T, Entity>? GatherMonomialInformation<T, TPrimitive>(IEnumerable<Entity> terms, Variable subtree, bool aPositiveVariable = false)
            where TPrimitive : TreeAnalyzer.IPrimitive<T>, new()
            where T : notnull
        {
            var monomialsByPower = new Dictionary<T, Entity>();
            // here we fill the dictionary with information about monomials' coefficiants
            foreach (var child in terms)
                if (ParseMonomial<T, TPrimitive>(subtree, child, aPositiveVariable) is var (free, q))
                    monomialsByPower[q.Value] =
                        monomialsByPower.TryGetValue(q.Value, out var power) ? power + free : free;
                else return null;
            return monomialsByPower;
        }
        /// <summary>Finds all terms of a polynomial</summary>
        internal static (Dictionary<T, Entity> poly, Entity rem) GatherMonomialInformationAllowingBad<T, TPrimitive>(IEnumerable<Entity> terms, Variable subtree)
            where TPrimitive : TreeAnalyzer.IPrimitive<T>, new()
            where T : notnull
        {
            var monomialsByPower = new Dictionary<T, Entity>();
            Entity rem = 0;
            // here we fill the dictionary with information about monomials' coefficiants
            foreach (var child in terms)
                if (ParseMonomial<T, TPrimitive>(subtree, child) is var (free, q))
                    monomialsByPower[q.Value] =
                        monomialsByPower.TryGetValue(q.Value, out var power) ? power + free : free;
                else
                    rem += child;
            return (monomialsByPower, rem);
        }


        /// <summary>Whether a factor is a number greater than zero, which a power of a product
        /// that is not whole may take out of it.</summary>
        private static bool IsAPositiveNumber(Entity factor) =>
            !factor.Vars.Any() && factor.Evaled is Real { IsPositive: true };

        /// <summary>Whether a monomial's power, integer, rational or decimal, is one.</summary>
        private static bool IsOne<T>(T power) => power switch
        {
            EInteger whole => whole.Equals(EInteger.One),
            ERational rational => rational.Equals(ERational.One),
            EDecimal decimalPower => decimalPower.CompareTo(EDecimal.One) == 0,
            _ => false,
        };

        /// <summary>
        /// A term read as a coefficient free of <paramref name="aVar"/> times a power of it, or
        /// <see langword="null"/> where it is not one.
        /// </summary>
        /// <remarks>
        /// A power of a product that is not whole is the product of the powers only where the
        /// factor taken out is positive: <c>(b x)^(5/2)</c> is <c>b^(5/2) x^(5/2)</c> for
        /// <c>b &gt; 0</c> or <c>x &gt; 0</c>, and its negative at <c>b &lt; 0</c>, <c>x &lt; 0</c>,
        /// where <c>b x</c> is positive and each of the two roots is imaginary. Read so, the long
        /// division made <c>b^(5/2) x^(5/2) + b^(5/2) x^(3/2)</c> of <c>(b x)^(5/2) (x + 1)/x</c>, and
        /// the integrator answered Rubi's <c>(b sec(x))^(5/2) tan(x)^3</c> wrongly wherever
        /// <c>b sec(x) &gt; 0</c> with <c>b &lt; 0</c>; a negative number likewise, and
        /// <c>sqrt(-x) (x + 1)/x</c> was simplified to <c>i (sqrt(x) + x^(-1/2))</c>, its negative
        /// for every <c>x &lt; 0</c>. A positive number comes out, and any factor where
        /// <paramref name="aPositiveVariable"/> says the variable is positive, as the limits at
        /// <c>+oo</c> read it.
        /// https://github.com/asc-community/AngouriMath/issues/233
        /// </remarks>
        internal static (Entity FreeMono, TPrimitive Power)? ParseMonomial<T, TPrimitive>(Variable aVar, Entity expr, bool aPositiveVariable = false)
            where TPrimitive : TreeAnalyzer.IPrimitive<T>, new()
        {
            var power = new TPrimitive();
            if (!expr.ContainsNode(aVar))
                return (expr, power);

            Entity freeMono = 1; // a * b
            foreach (var mp in Mulf.LinearChildren(expr))
                if (!mp.ContainsNode(aVar))
                    freeMono *= mp;
                else if (mp is Powf(var @base, var pow_num))
                {
                    // x ^ a or x ^ i is bad
                    if (pow_num.Evaled is not Real value)
                        return null;
                    // x ^ 0.3 is bad
                    if (!power.AllowFloat && value is not Integer)
                        return null;
                    if (mp == aVar)
                        power.Add(value);
                    // (x^q)^v is x^(q v) for a whole v, and for a fractional v only where x^q
                    // is not negative: (x^2)^(3/2) is |x|^3, and read as x^3 it let the long
                    // division make u^2 of (u^2)^(3/2)/u, whereupon (sin(x)^2)^(3/2) under
                    // u = cos(x) was integrated as sin(x)^3, wrong on every other half-turn.
                    // Any q but one is refused, an odd one too: (u^3)^(3/2)/u is not u^(7/2)
                    // at u = -1, where the first is i and the second -i, and Simplify does not
                    // change a value off the real line either (#752).
                    // https://github.com/asc-community/AngouriMath/issues/1387
                    else if (ParseMonomial<T, TPrimitive>(aVar, @base, aPositiveVariable) is var (tmpFree, q))
                    {
                        if (value is not Integer && !IsOne(q.Value))
                            return null;
                        if (value is not Integer && !aPositiveVariable && !IsAPositiveNumber(tmpFree))
                            return null;
                        freeMono *= MathS.Pow(tmpFree, value);
                        power.AddMp(q.Value, value);
                    }
                    else return null;
                }
                else if (mp == aVar)
                    power.Add(1);
                // a ^ x, (a + x) etc. are bad
                else if (mp.ContainsNode(aVar))
                    return null;
                else freeMono *= mp;
            // TODO: do we need to simplify freeMono?
            return (freeMono, power);
        }
    }
}