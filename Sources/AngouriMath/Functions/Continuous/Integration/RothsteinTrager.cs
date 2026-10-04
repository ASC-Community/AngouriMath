//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//
using System.Collections.Generic;
using System.Linq;
using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions.Algebra
{
    /// <summary>
    /// The integral of a rational function with rational coefficients, whatever its
    /// denominator: the Hermite reduction for the rational part of the answer, and the
    /// Rothstein–Trager resultant for the logarithmic part, written in real terms after Rioboo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The partial-fraction rules answer a denominator that factors over the rationals, and
    /// a biquadratic one over the reals, and stop there: Bronstein's
    /// <c>(6 - 3x^2 + x^4)/(4 + 5x^2 - 5x^4 + x^6)</c> has a denominator irreducible over
    /// <c>Q</c> whose real factors carry the roots of a cubic, and its antiderivative is
    /// <c>arctan(x (1 - 3x^2 + x^4)/2)</c> and two more arctangents of linears, with no root
    /// of that cubic anywhere in it. The logarithmic part of the integral of <c>A/D</c>, with
    /// <c>D</c> squarefree, is <c>sum c ln(gcd(D, A - c D'))</c> over the roots <c>c</c> of the
    /// resultant <c>R(t) = res_x(D, A - t D')</c>, and the residues <c>c</c> are what decide the
    /// field the answer needs, not the roots of <c>D</c>: here they are <c>±i/2</c> and
    /// <c>±i</c>, and the answer is over <c>Q</c> with an <c>i</c> that pairs into arctangents.
    /// </para>
    /// <para>
    /// This takes the rational part first by the Hermite reduction, then the resultant, its
    /// squarefree decomposition -- a root of multiplicity <c>i</c> is a residue shared by
    /// <c>i</c> roots of <c>D</c>, and the gcd for it has degree <c>i</c> -- and the
    /// irreducible factors of each squarefree part over <c>Q</c>. A linear factor is a
    /// rational residue and a logarithm; a quadratic one is a pair of conjugate residues
    /// <c>a ± w</c> with <c>w^2</c> rational, and the gcd is computed in <c>Q(w)</c>: for
    /// <c>w</c> real the pair is two logarithms with a root in them, and for <c>w = i b</c> it
    /// is <c>a ln(P^2 + b^2 Q^2) + b LogToAtan(P, b Q)</c>, Rioboo's continuous arctangent
    /// form. A factor of higher degree puts the residues in a field this does no arithmetic in,
    /// and those logarithms are written as a sum over the roots of the factor of the denominator
    /// they belong to, <c>sum(A(r)/D'(r) ln(x - r), r in { r : E(r) = 0 })</c>
    /// (https://github.com/asc-community/AngouriMath/issues/1285).
    /// </para>
    /// <para>
    /// Bronstein, <i>Symbolic Integration I</i>, §2.2 (HermiteReduce), §2.5
    /// (IntRationalLogPart) and §2.8 (LogToReal, LogToAtan). The resultant is taken as a
    /// Sylvester determinant rather than from a subresultant remainder sequence, so the gcd
    /// is computed for each residue in its own field instead of being read off the
    /// sequence -- that is the classical Rothstein–Trager rather than Lazard–Rioboo–Trager,
    /// affordable here because the fields are of degree at most two. The answer is checked
    /// against the integrand at sampled points before it is returned.
    /// https://github.com/asc-community/AngouriMath/issues/718
    /// </para>
    /// </remarks>
    internal static class RothsteinTrager
    {
        /// <summary>
        /// The largest denominator degree taken. The resultant is a Sylvester determinant of
        /// twice that size, and a denominator past it is not what this exists for.
        /// </summary>
        private const int MaxDegree = 12;

        /// <summary>
        /// The integral of <paramref name="numerator"/> over <paramref name="denominator"/>,
        /// both polynomials in <paramref name="x"/> with rational coefficients and the fraction
        /// proper; <see langword="null"/> where they are not, or where the answer does not
        /// differentiate back to the integrand at the sampled points.
        /// </summary>
        internal static Entity? Integrate(Entity numerator, Entity denominator, Variable x)
        {
            if (!PolynomialFactoring.TryGetRationalCoefficients(numerator, x, 1, 0, MaxDegree, out var aboveCoefficients)
                || !PolynomialFactoring.TryGetRationalCoefficients(denominator, x, 1, 1, MaxDegree, out var belowCoefficients))
                return null;
            var above = RationalPolynomial.Create(aboveCoefficients);
            var below = RationalPolynomial.Create(belowCoefficients);
            if (above.IsZero || above.Degree >= below.Degree)
                return null;
            var scale = ERational.One.Divide(below.Leading);
            above = above.ScaleBy(scale);
            below = below.ScaleBy(scale);

            if (HermiteReduce(above, below, x) is not var (rationalPart, logAbove, logBelow))
                return null;
            var answer = rationalPart;
            if (!logAbove.IsZero)
            {
                if (LogarithmicPart(logAbove, logBelow, x) is not { } logarithmic)
                    return null;
                answer = answer + logarithmic;
            }
            return Functions.PartialFractions.HoldsAtSampledPoints(answer.Differentiate(x), numerator / denominator, x)
                ? answer.InnerSimplified
                : null;
        }

        /// <summary>
        /// Bronstein's HermiteReduce, the quadratic version: <c>A/D</c> as <c>g + B/E</c> with
        /// <c>E</c> squarefree, <c>g</c> the rational part as an entity and <c>B/E</c> as the
        /// pair of polynomials. <see langword="null"/> where the squarefree decomposition
        /// declines.
        /// </summary>
        private static (Entity Rational, RationalPolynomial Above, RationalPolynomial Below)? HermiteReduce(
            RationalPolynomial above, RationalPolynomial below, Variable x)
        {
            if (SquareFree(below) is not { } parts)
                return null;
            Entity rational = Integer.Create(0);
            var a = above;
            var d = below;
            foreach (var part in parts)
            {
                if (part.Multiplicity < 2)
                    continue;
                var v = part.Factor;
                if (ExactDivide(d, v.Pow(part.Multiplicity)) is not { } u)
                    return null;
                var vPrime = Derivative(v);
                for (var j = part.Multiplicity - 1; j >= 1; j--)
                {
                    // b (u v') + c v = -a/j
                    var target = a.ScaleBy(ERational.Create(EInteger.FromInt32(-1), EInteger.FromInt32(j)));
                    if (!TrySolveDiophantine(u.Multiply(vPrime), v, target, out var b, out var c))
                        return null;
                    rational = rational + b.ToEntity(x) / MathS.Pow(v.ToEntity(x), j);
                    a = c.ScaleBy(ERational.FromInt32(-j)).Subtract(u.Multiply(Derivative(b)));
                }
                d = u.Multiply(v);
            }
            return (rational, a, d);
        }

        /// <summary>
        /// The logarithmic part of the integral of <paramref name="above"/> over
        /// <paramref name="below"/>, the latter squarefree and monic and the fraction proper.
        /// </summary>
        private static Entity? LogarithmicPart(RationalPolynomial above, RationalPolynomial below, Variable x)
        {
            var derivative = Derivative(below);
            // The resultant in t of D and A - t D' has degree at most that of D in t, and is
            // found by evaluating it at that many rational t and interpolating: each value is
            // one numeric Sylvester determinant. As a determinant with polynomial entries
            // eliminated fraction-free it was thirteen seconds for a denominator of degree six
            // with four-digit coefficients, Timofeev's `(1 + 2x)/((4 + 4x + 3x^2) sqrt(x^2 + 6x - 1))`
            // under Euler's substitution, asked several times over.
            if (ResultantInT(below, above, derivative) is not { } resultantPolynomial || resultantPolynomial.IsZero)
                return null;
            if (SquareFree(resultantPolynomial) is not { } residueParts)
                return null;

            // Every residue field first, and the residues only then. A residue in a field of degree
            // above two is written as a sum over roots, below, and where there is one, every
            // residue that is not rational goes into that sum with it: the conjugate pair of a
            // quadratic factor beside a quartic one was twelve seconds of gcds over the extension,
            // and a sum over roots needs no arithmetic in any extension at all.
            var factors = new List<(IReadOnlyList<SquareFreeDecomposition.SquareFreePart> Factors, int Multiplicity)>();
            foreach (var part in residueParts)
            {
                if (PolynomialFactorization.FactorPrimitive(ToInteger(part.Factor).PrimitivePart()) is not { } irreducibles)
                    return null;
                factors.Add((irreducibles, part.Multiplicity));
            }
            var overRoots = factors.Any(part => part.Factors.Any(irreducible => irreducible.Factor.Degree > 2));
            // Only once nothing else has answered the question: see
            // Integration.SumsOverRootsAllowed. Until then this declines, as it always did.
            if (overRoots && !Integration.SumsOverRootsAllowed)
            {
                Integration.ASumOverRootsWouldAnswer = true;
                return null;
            }
            var factored = new List<(IReadOnlyList<SquareFreeDecomposition.SquareFreePart> Factors, int Multiplicity)>();
            var summed = RationalPolynomial.One;
            var summedRoots = 0;
            foreach (var (irreducibles, multiplicity) in factors)
            {
                if (!overRoots)
                {
                    factored.Add((irreducibles, multiplicity));
                    continue;
                }
                factored.Add((irreducibles.Where(irreducible => irreducible.Factor.Degree == 1).ToList(), multiplicity));
                foreach (var irreducible in irreducibles.Where(irreducible => irreducible.Factor.Degree > 1))
                {
                    summed = summed.Multiply(RationalPolynomial.FromInteger(irreducible.Factor));
                    summedRoots += irreducible.Factor.Degree * multiplicity;
                }
            }
            Entity total = Integer.Create(0);
            if (overRoots)
            {
                if (OverTheRoots(summed, summedRoots, above, below, derivative, x) is not { } sum)
                    return null;
                total = sum;
            }
            foreach (var (kept, multiplicity) in factored)
            {
                var part = (Multiplicity: multiplicity, Factors: kept);
                foreach (var irreducible in kept)
                {
                    var f = irreducible.Factor;
                    Entity? term = f.Degree switch
                    {
                        1 => RationalResidue(ERational.Create(f[0], f[1]).Negate().ToLowestTerms(), part.Multiplicity, above, below, derivative, x),
                        2 => QuadraticResidues(f, part.Multiplicity, above, below, derivative, x),
                        _ => null,
                    };
                    if (term is null)
                        return null;
                    total = total + term;
                }
            }
            return total;
        }

        /// <summary>
        /// The logarithms at the poles whose residues are roots of <paramref name="residues"/>,
        /// as a sum over those poles: <c>sum(A(r)/D'(r) ln(x - r), r in { r : E(r) = 0 })</c>,
        /// where <c>E</c> is the factor of <c>D</c> they are the roots of. <see langword="null"/>
        /// where <c>E</c> does not have the <paramref name="roots"/> the residues' multiplicities
        /// count.
        /// </summary>
        /// <remarks>
        /// The residue at a simple pole <c>b</c> of <c>A/D</c> is <c>A(b)/D'(b)</c>, a root of the
        /// resultant, and <c>D'(b)</c> is not zero there. So the poles whose residues are roots of
        /// <c>R</c> are the common roots of <c>D</c> and the polynomial <c>D'^n R(A/D')</c>, and
        /// <c>E</c> is their gcd over the rationals: no arithmetic in the field the residues lie in
        /// is needed. The sum is the one Rothstein–Trager's theorem writes over the roots of the
        /// resultant, with its terms taken pole by pole. <c>E</c> has a factor of degree above two,
        /// since a pole in a field of degree at most two has its residue there too, so the sum is
        /// left standing rather than written out in radicals, unless each such factor is a
        /// binomial, whose roots are written.
        /// https://github.com/asc-community/AngouriMath/issues/1285
        /// </remarks>
        private static Entity? OverTheRoots(RationalPolynomial residues, int roots,
            RationalPolynomial above, RationalPolynomial below, RationalPolynomial derivative, Variable x)
        {
            // D'^n R(A/D') = sum over k of r_k A^k D'^(n - k).
            var n = residues.Degree;
            var cleared = RationalPolynomial.Zero;
            for (var k = 0; k <= n; k++)
                cleared = cleared.Add(above.Pow(k).Multiply(derivative.Pow(n - k)).ScaleBy(residues[k]));
            var poles = RationalPolynomial.FromInteger(IntegerPolynomial.Gcd(ToInteger(below), ToInteger(cleared)).PrimitivePart());
            if (poles.Degree != roots)
                return null;
            // The polynomials are in x alone, so any other name is free.
            var r = MathS.Var(x.Name == "r" ? "w" : "r");
            var summand = above.ToEntity(r) / derivative.ToEntity(r) * MathS.Ln(x - r);
            return MathS.Sum(summand, r, new Set.ConditionalSet(r, poles.ToEntity(r).Equalizes(Integer.Zero)));
        }

        /// <summary><c>c ln(gcd(D, A - c D'))</c> for a rational residue <paramref name="c"/>.</summary>
        private static Entity? RationalResidue(
            ERational c, int degree, RationalPolynomial above, RationalPolynomial below, RationalPolynomial derivative, Variable x)
        {
            var field = new QuadraticExtension(ERational.Zero);
            var s = ResidueGcd(field, new Element(c, ERational.Zero), degree, above, below, derivative);
            if (s is null)
                return null;
            return Rational.Create(c) * MathS.Ln(s.ToEntity(x, e => Rational.Create(e.Re)));
        }

        /// <summary>
        /// The two conjugate residues of an irreducible quadratic factor
        /// <paramref name="f"/> of the resultant, <c>a ± w</c> with <c>w^2</c> rational, and
        /// their logarithms in real form.
        /// </summary>
        private static Entity? QuadraticResidues(
            IntegerPolynomial f, int degree, RationalPolynomial above, RationalPolynomial below, RationalPolynomial derivative, Variable x)
        {
            // f = f2 t^2 + f1 t + f0, roots -f1/(2 f2) ± sqrt(f1^2/(4 f2^2) - f0/f2).
            var a = ERational.Create(f[1], f[2].Multiply(EInteger.FromInt32(2))).Negate().ToLowestTerms();
            var w = a.Multiply(a).Subtract(ERational.Create(f[0], f[2])).ToLowestTerms();
            if (w.IsZero)
                return null;
            var field = new QuadraticExtension(w);
            var s = ResidueGcd(field, new Element(a, ERational.One), degree, above, below, derivative);
            if (s is null)
                return null;
            var p = s.RealPart();
            var q = s.ImaginaryPart();

            if (w.Sign > 0)
            {
                // Real conjugates: (a + sqrt(w)) ln(P + sqrt(w) Q) + (a - sqrt(w)) ln(P - sqrt(w) Q).
                var root = MathS.Sqrt(Rational.Create(w));
                var aEntity = Rational.Create(a);
                var pEntity = p.ToEntity(x);
                var qEntity = q.ToEntity(x);
                return (aEntity + root) * MathS.Ln(pEntity + root * qEntity)
                     + (aEntity - root) * MathS.Ln(pEntity - root * qEntity);
            }

            // w = i b: a ln(P^2 + b^2 Q^2) + b LogToAtan(P, b Q), the arctangent computed over
            // Q(b) -- or over Q where b is rational, since the extension arithmetic cannot
            // tell a zero written as b - b from a unit.
            var bSquared = w.Negate();
            var modulus = p.Multiply(p).Add(q.Multiply(q).ScaleBy(bSquared));
            Entity logarithm = Rational.Create(a) * MathS.Ln(modulus.ToEntity(x));
            if (a.IsZero)
                logarithm = Integer.Create(0);
            if (TryRationalSquareRoot(bSquared, out var b))
            {
                var rationals = new QuadraticExtension(ERational.Zero);
                var arctangent = LogToAtan(rationals,
                    ExtensionPolynomial.FromRational(p), ExtensionPolynomial.FromRational(q.ScaleBy(b)), x,
                    e => Rational.Create(e.Re));
                return arctangent is null ? null : logarithm + Rational.Create(b) * arctangent;
            }
            else
            {
                var overB = new QuadraticExtension(bSquared);
                var bRoot = MathS.Sqrt(Rational.Create(bSquared));
                var arctangent = LogToAtan(overB,
                    ExtensionPolynomial.FromRational(p), ExtensionPolynomial.FromRational(q).TimesGenerator(), x,
                    e => e.Im.IsZero ? Rational.Create(e.Re) : Rational.Create(e.Re) + Rational.Create(e.Im) * bRoot);
                return arctangent is null ? null : logarithm + bRoot * arctangent;
            }
        }

        /// <summary>
        /// The monic gcd of <c>D</c> and <c>A - c D'</c> over the field, which must have
        /// degree <paramref name="degree"/>: the multiplicity of <c>c</c> as a root of the
        /// resultant is the number of roots of <c>D</c> whose residue is <c>c</c>.
        /// </summary>
        private static ExtensionPolynomial? ResidueGcd(
            QuadraticExtension field, Element c, int degree,
            RationalPolynomial above, RationalPolynomial below, RationalPolynomial derivative)
        {
            var d = ExtensionPolynomial.FromRational(below);
            var shifted = ExtensionPolynomial.FromRational(above)
                .Subtract(ExtensionPolynomial.FromRational(derivative).ScaleBy(field, c));
            var gcd = ExtensionPolynomial.Gcd(field, d, shifted);
            return gcd is not null && gcd.Degree == degree ? gcd : null;
        }

        /// <summary>
        /// Rioboo's LogToAtan: an expression whose derivative is that of
        /// <c>i ln((A + iB)/(A - iB))</c>, as a sum of arctangents of polynomials, continuous
        /// where the single <c>2 arctan(A/B)</c> is not.
        /// </summary>
        private static Entity? LogToAtan(
            QuadraticExtension field, ExtensionPolynomial a, ExtensionPolynomial b, Variable x,
            System.Func<Element, Entity> coefficient)
        {
            if (b.IsZero)
                return null;
            if (ExtensionPolynomial.DivRem(field, a, b) is var (quotient, remainder) && remainder.IsZero)
                return 2 * MathS.Arctan(quotient.ToEntity(x, coefficient));
            if (a.Degree < b.Degree)
                return LogToAtan(field, b.Negate(), a, x, coefficient);
            // d b + c (-a) = g
            if (ExtensionPolynomial.Bezout(field, b, a.Negate()) is not var (d, c, g))
                return null;
            var argument = a.Multiply(d, field).Add(b.Multiply(c, field));
            if (ExtensionPolynomial.DivRem(field, argument, g) is not var (exact, rest) || !rest.IsZero)
                return null;
            var tail = LogToAtan(field, d, c, x, coefficient);
            if (tail is null)
                return null;
            return 2 * MathS.Arctan(exact.ToEntity(x, coefficient)) + tail;
        }

        /// <summary>
        /// The squarefree decomposition of <paramref name="poly"/> into monic factors with
        /// their multiplicities, or <see langword="null"/> where it declines.
        /// </summary>
        private static List<(RationalPolynomial Factor, int Multiplicity)>? SquareFree(RationalPolynomial poly)
        {
            var parts = SquareFreeDecomposition.Decompose(ToInteger(poly));
            if (parts is null)
                return null;
            var monic = new List<(RationalPolynomial, int)>();
            foreach (var part in parts)
            {
                var factor = RationalPolynomial.FromInteger(part.Factor);
                monic.Add((factor.ScaleBy(ERational.One.Divide(factor.Leading)), part.Multiplicity));
            }
            return monic;
        }

        /// <summary>
        /// <paramref name="poly"/> with its denominators cleared.
        /// </summary>
        private static IntegerPolynomial ToInteger(RationalPolynomial poly)
        {
            var common = EInteger.One;
            for (var i = 0; i <= poly.Degree; i++)
                common = Lcm(common, poly[i].Denominator);
            var whole = new EInteger[poly.Degree + 1];
            for (var i = 0; i < whole.Length; i++)
                whole[i] = poly[i].Numerator.Multiply(common.Divide(poly[i].Denominator));
            return IntegerPolynomial.Create(whole);
        }

        private static EInteger Lcm(EInteger a, EInteger b)
            => a.IsZero || b.IsZero ? EInteger.Zero : a.Multiply(b).Abs().Divide(a.Gcd(b));

        /// <summary>
        /// <c>Res_x(D, A - t D')</c> as a polynomial in <c>t</c>: of degree at most <c>deg D</c>,
        /// evaluated at <c>deg D + 1</c> integers and interpolated.
        /// </summary>
        private static RationalPolynomial? ResultantInT(RationalPolynomial d, RationalPolynomial a, RationalPolynomial dPrime)
        {
            var degree = d.Degree;
            if (degree < 1)
                return null;
            var points = new ERational[degree + 1];
            var values = new ERational[degree + 1];
            for (var k = 0; k <= degree; k++)
            {
                var t = ERational.FromInt32(k);
                var shifted = a.Subtract(dPrime.ScaleBy(t));
                if (SylvesterDeterminant(d, shifted, degree - 1) is not { } value)
                    return null;
                points[k] = t;
                values[k] = value;
            }
            // Newton's divided differences, then the polynomial.
            var coefficients = (ERational[])values.Clone();
            for (var level = 1; level <= degree; level++)
                for (var k = degree; k >= level; k--)
                    coefficients[k] = coefficients[k].Subtract(coefficients[k - 1]).Divide(points[k].Subtract(points[k - level])).ToLowestTerms();
            var result = RationalPolynomial.Zero;
            for (var k = degree; k >= 0; k--)
            {
                // result = result * (t - points[k]) + coefficients[k]
                var shiftedUp = new ERational[result.Degree + 2];
                for (var i = 0; i < shiftedUp.Length; i++)
                    shiftedUp[i] = ERational.Zero;
                for (var i = 0; i <= result.Degree; i++)
                {
                    shiftedUp[i + 1] = shiftedUp[i + 1].Add(result[i]);
                    shiftedUp[i] = shiftedUp[i].Subtract(result[i].Multiply(points[k]));
                }
                shiftedUp[0] = shiftedUp[0].Add(coefficients[k]);
                result = RationalPolynomial.Create(shiftedUp);
            }
            return result;
        }

        /// <summary>
        /// The Sylvester determinant of <paramref name="f"/> and <paramref name="g"/>, the
        /// latter taken to be of degree <paramref name="degreeOfG"/> whatever its leading
        /// coefficients happen to be at this <c>t</c>, by fraction-free elimination over the
        /// integers after scaling every row.
        /// </summary>
        private static ERational? SylvesterDeterminant(RationalPolynomial f, RationalPolynomial g, int degreeOfG)
        {
            var m = f.Degree;
            var n = degreeOfG;
            if (m < 0 || n < 0)
                return null;
            var size = m + n;
            if (size == 0)
                return ERational.One;
            var rows = new ERational[size][];
            for (var row = 0; row < n; row++)
            {
                rows[row] = new ERational[size];
                for (var column = 0; column < size; column++)
                    rows[row][column] = ERational.Zero;
                for (var k = 0; k <= m; k++)
                    rows[row][row + k] = f[m - k];
            }
            for (var row = 0; row < m; row++)
            {
                rows[n + row] = new ERational[size];
                for (var column = 0; column < size; column++)
                    rows[n + row][column] = ERational.Zero;
                for (var k = 0; k <= n; k++)
                    rows[n + row][row + k] = g[n - k];
            }
            // Rows scaled to integers, the scale taken out of the determinant at the end.
            var scale = ERational.One;
            var a = new EInteger[size][];
            for (var row = 0; row < size; row++)
            {
                var common = EInteger.One;
                for (var column = 0; column < size; column++)
                {
                    var reduced = rows[row][column].ToLowestTerms();
                    rows[row][column] = reduced;
                    common = common.Multiply(reduced.Denominator).Divide(common.Gcd(reduced.Denominator));
                }
                scale = scale.Multiply(ERational.FromEInteger(common));
                a[row] = new EInteger[size];
                for (var column = 0; column < size; column++)
                    a[row][column] = rows[row][column].Multiply(ERational.FromEInteger(common)).ToLowestTerms().Numerator;
            }
            var sign = 1;
            var previous = EInteger.One;
            for (var column = 0; column < size; column++)
            {
                var pivot = -1;
                for (var row = column; row < size; row++)
                    if (!a[row][column].IsZero)
                    {
                        pivot = row;
                        break;
                    }
                if (pivot < 0)
                    return ERational.Zero;
                if (pivot != column)
                {
                    (a[pivot], a[column]) = (a[column], a[pivot]);
                    sign = -sign;
                }
                var pivotValue = a[column][column];
                for (var row = column + 1; row < size; row++)
                {
                    var factor = a[row][column];
                    for (var k = column; k < size; k++)
                    {
                        var crossed = pivotValue.Multiply(a[row][k]).Subtract(factor.Multiply(a[column][k]));
                        a[row][k] = crossed.IsZero ? EInteger.Zero : crossed.Divide(previous);
                    }
                }
                previous = pivotValue;
            }
            // Bareiss leaves the determinant in the last pivot.
            var determinant = ERational.FromEInteger(a[size - 1][size - 1]);
            if (sign < 0)
                determinant = determinant.Negate();
            return determinant.Divide(scale).ToLowestTerms();
        }

        private static RationalPolynomial Derivative(RationalPolynomial poly)
        {
            if (poly.Degree < 1)
                return RationalPolynomial.Zero;
            var coefficients = new ERational[poly.Degree];
            for (var i = 1; i <= poly.Degree; i++)
                coefficients[i - 1] = poly[i].Multiply(ERational.FromInt32(i));
            return RationalPolynomial.Create(coefficients);
        }

        private static RationalPolynomial? ExactDivide(RationalPolynomial dividend, RationalPolynomial divisor)
            => dividend.TryDivide(divisor, out var quotient, out var remainder) && remainder.IsZero ? quotient : null;

        /// <summary>
        /// <c>s a + t b = c</c> with <c>deg s &lt; deg b</c>, for coprime <paramref name="a"/>
        /// and <paramref name="b"/>.
        /// </summary>
        private static bool TrySolveDiophantine(
            RationalPolynomial a, RationalPolynomial b, RationalPolynomial c,
            out RationalPolynomial s, out RationalPolynomial t)
        {
            s = t = RationalPolynomial.Zero;
            if (!RationalPolynomial.TryBezout(a, b, out var u, out _))
                return false;
            if (!u.Multiply(c).TryDivide(b, out _, out s))
                return false;
            return c.Subtract(s.Multiply(a)).TryDivide(b, out t, out var remainder) && remainder.IsZero;
        }

        /// <summary>Whether <paramref name="value"/> is the square of a rational, and that rational.</summary>
        private static bool TryRationalSquareRoot(ERational value, out ERational root)
        {
            root = ERational.Zero;
            if (value.Sign < 0)
                return false;
            var numerator = value.Numerator.Sqrt();
            var denominator = value.Denominator.Sqrt();
            if (!numerator.Multiply(numerator).Equals(value.Numerator) || !denominator.Multiply(denominator).Equals(value.Denominator))
                return false;
            root = ERational.Create(numerator, denominator).ToLowestTerms();
            return true;
        }

        /// <summary>An element <c>Re + Im w</c> of <c>Q(w)</c>.</summary>
        private readonly struct Element
        {
            internal Element(ERational re, ERational im)
            {
                Re = re.ToLowestTerms();
                Im = im.ToLowestTerms();
            }

            internal ERational Re { get; }
            internal ERational Im { get; }
            internal bool IsZero => Re.IsZero && Im.IsZero;
        }

        /// <summary>
        /// Arithmetic in <c>Q(w)</c> with <c>w^2 = W</c> rational and not a square -- or
        /// <c>W = 0</c> for the rationals themselves, where every element has a zero
        /// <c>Im</c>.
        /// </summary>
        private sealed class QuadraticExtension
        {
            private readonly ERational w;
            internal QuadraticExtension(ERational w) => this.w = w;

            internal Element Add(Element l, Element r) => new(l.Re.Add(r.Re), l.Im.Add(r.Im));
            internal Element Subtract(Element l, Element r) => new(l.Re.Subtract(r.Re), l.Im.Subtract(r.Im));
            internal Element Negate(Element e) => new(e.Re.Negate(), e.Im.Negate());
            internal Element Multiply(Element l, Element r)
                => new(l.Re.Multiply(r.Re).Add(l.Im.Multiply(r.Im).Multiply(w)), l.Re.Multiply(r.Im).Add(l.Im.Multiply(r.Re)));

            /// <summary>The inverse through the conjugate: <c>1/(p + q w) = (p - q w)/(p^2 - q^2 W)</c>.</summary>
            internal Element Divide(Element l, Element r)
            {
                var norm = r.Re.Multiply(r.Re).Subtract(r.Im.Multiply(r.Im).Multiply(w));
                var conjugate = new Element(r.Re, r.Im.Negate());
                var product = Multiply(l, conjugate);
                return new(product.Re.Divide(norm), product.Im.Divide(norm));
            }
        }

        /// <summary>A polynomial over <c>Q(w)</c>, lowest power first, trimmed.</summary>
        private sealed class ExtensionPolynomial
        {
            private readonly Element[] coefficients;
            private ExtensionPolynomial(Element[] coefficients) => this.coefficients = coefficients;

            internal static ExtensionPolynomial Create(IReadOnlyList<Element> lowestFirst)
            {
                var length = lowestFirst.Count;
                while (length > 0 && lowestFirst[length - 1].IsZero)
                    length--;
                var trimmed = new Element[length];
                for (var i = 0; i < length; i++)
                    trimmed[i] = lowestFirst[i];
                return new(trimmed);
            }

            internal static ExtensionPolynomial FromRational(RationalPolynomial poly)
            {
                var elements = new Element[poly.Degree + 1];
                for (var i = 0; i < elements.Length; i++)
                    elements[i] = new Element(poly[i], ERational.Zero);
                return Create(elements);
            }

            internal int Degree => coefficients.Length - 1;
            internal bool IsZero => coefficients.Length == 0;
            internal Element Leading => coefficients[coefficients.Length - 1];

            /// <summary>The polynomial of the <c>Re</c> parts.</summary>
            internal RationalPolynomial RealPart() => RationalPolynomial.Create(coefficients.Select(c => c.Re).ToList());

            /// <summary>The polynomial of the <c>Im</c> parts.</summary>
            internal RationalPolynomial ImaginaryPart() => RationalPolynomial.Create(coefficients.Select(c => c.Im).ToList());

            /// <summary>Multiplied by <c>w</c> itself, for a rational polynomial: <c>p</c> becomes <c>p w</c>.</summary>
            internal ExtensionPolynomial TimesGenerator()
                => Create(coefficients.Select(c => new Element(ERational.Zero, c.Re)).ToList());

            internal ExtensionPolynomial Negate()
                => Create(coefficients.Select(c => new Element(c.Re.Negate(), c.Im.Negate())).ToList());

            internal ExtensionPolynomial ScaleBy(QuadraticExtension field, Element factor)
                => Create(coefficients.Select(c => field.Multiply(c, factor)).ToList());

            internal ExtensionPolynomial Add(ExtensionPolynomial other) => Combine(other, subtract: false);
            internal ExtensionPolynomial Subtract(ExtensionPolynomial other) => Combine(other, subtract: true);

            private ExtensionPolynomial Combine(ExtensionPolynomial other, bool subtract)
            {
                var length = System.Math.Max(coefficients.Length, other.coefficients.Length);
                var result = new Element[length];
                for (var i = 0; i < length; i++)
                {
                    var l = i < coefficients.Length ? coefficients[i] : new Element(ERational.Zero, ERational.Zero);
                    var r = i < other.coefficients.Length ? other.coefficients[i] : new Element(ERational.Zero, ERational.Zero);
                    result[i] = subtract ? new Element(l.Re.Subtract(r.Re), l.Im.Subtract(r.Im)) : new Element(l.Re.Add(r.Re), l.Im.Add(r.Im));
                }
                return Create(result);
            }

            internal ExtensionPolynomial Multiply(ExtensionPolynomial other, QuadraticExtension field)
            {
                if (IsZero || other.IsZero)
                    return Create(System.Array.Empty<Element>());
                var result = new Element[coefficients.Length + other.coefficients.Length - 1];
                for (var i = 0; i < result.Length; i++)
                    result[i] = new Element(ERational.Zero, ERational.Zero);
                for (var i = 0; i < coefficients.Length; i++)
                    for (var j = 0; j < other.coefficients.Length; j++)
                        result[i + j] = field.Add(result[i + j], field.Multiply(coefficients[i], other.coefficients[j]));
                return Create(result);
            }

            /// <summary><c>dividend = quotient * divisor + remainder</c>; null for a zero divisor.</summary>
            internal static (ExtensionPolynomial Quotient, ExtensionPolynomial Remainder)? DivRem(
                QuadraticExtension field, ExtensionPolynomial dividend, ExtensionPolynomial divisor)
            {
                if (divisor.IsZero)
                    return null;
                var zero = new Element(ERational.Zero, ERational.Zero);
                if (dividend.Degree < divisor.Degree)
                    return (Create(System.Array.Empty<Element>()), dividend);
                var working = (Element[])dividend.coefficients.Clone();
                var quotient = new Element[dividend.Degree - divisor.Degree + 1];
                for (var i = 0; i < quotient.Length; i++)
                    quotient[i] = zero;
                for (var power = dividend.Degree; power >= divisor.Degree; power--)
                {
                    if (working[power].IsZero)
                        continue;
                    var factor = field.Divide(working[power], divisor.Leading);
                    quotient[power - divisor.Degree] = factor;
                    for (var i = 0; i <= divisor.Degree; i++)
                        working[power - divisor.Degree + i] =
                            field.Subtract(working[power - divisor.Degree + i], field.Multiply(factor, divisor.coefficients[i]));
                }
                return (Create(quotient), Create(working));
            }

            /// <summary>The monic gcd; null where both are zero.</summary>
            internal static ExtensionPolynomial? Gcd(QuadraticExtension field, ExtensionPolynomial a, ExtensionPolynomial b)
            {
                var (previous, current) = (a, b);
                while (!current.IsZero)
                {
                    if (DivRem(field, previous, current) is not var (_, remainder))
                        return null;
                    (previous, current) = (current, remainder);
                }
                if (previous.IsZero)
                    return null;
                return previous.ScaleBy(field, field.Divide(new Element(ERational.One, ERational.Zero), previous.Leading));
            }

            /// <summary><c>s a + t b = g</c>, the monic gcd; null where both are zero.</summary>
            internal static (ExtensionPolynomial S, ExtensionPolynomial T, ExtensionPolynomial G)? Bezout(
                QuadraticExtension field, ExtensionPolynomial a, ExtensionPolynomial b)
            {
                var one = Create(new[] { new Element(ERational.One, ERational.Zero) });
                var zero = Create(System.Array.Empty<Element>());
                var (remainderPrevious, remainderCurrent) = (a, b);
                var (sPrevious, sCurrent) = (one, zero);
                var (tPrevious, tCurrent) = (zero, one);
                while (!remainderCurrent.IsZero)
                {
                    if (DivRem(field, remainderPrevious, remainderCurrent) is not var (quotient, next))
                        return null;
                    (remainderPrevious, remainderCurrent) = (remainderCurrent, next);
                    (sPrevious, sCurrent) = (sCurrent, sPrevious.Subtract(quotient.Multiply(sCurrent, field)));
                    (tPrevious, tCurrent) = (tCurrent, tPrevious.Subtract(quotient.Multiply(tCurrent, field)));
                }
                if (remainderPrevious.IsZero)
                    return null;
                var inverse = field.Divide(new Element(ERational.One, ERational.Zero), remainderPrevious.Leading);
                return (sPrevious.ScaleBy(field, inverse), tPrevious.ScaleBy(field, inverse), remainderPrevious.ScaleBy(field, inverse));
            }

            internal Entity ToEntity(Variable x, System.Func<Element, Entity> coefficient)
            {
                Entity result = Integer.Create(0);
                for (var i = Degree; i >= 0; i--)
                {
                    if (coefficients[i].IsZero)
                        continue;
                    var term = coefficient(coefficients[i]);
                    if (i == 1)
                        term = term * x;
                    else if (i > 1)
                        term = term * MathS.Pow(x, i);
                    result = result + term;
                }
                return result.InnerSimplified;
            }
        }
    }
}
