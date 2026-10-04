//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Numerics;
using PeterO.Numbers;
using System.Diagnostics.CodeAnalysis;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions
{
    /// <summary>
    /// One step of a partial fraction decomposition, at a coprime pair of factors of the
    /// denominator rather than at a root of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sibling step, <see cref="PolynomialFactoring.TrySplitOffRationalRoot"/>, splits at a
    /// rational root, which is all the decomposition there was: a denominator with no rational
    /// root was left whole, so <c>1/(x^4 + 3x^2 + 2)</c> had no antiderivative even though it
    /// is <c>(x^2 + 1)(x^2 + 2)</c> and both of those are integrated by the rule for a linear
    /// numerator over a quadratic. Nothing was missing but the split.
    /// https://github.com/asc-community/AngouriMath/issues/919
    /// </para>
    /// <para>
    /// One step and not the whole decomposition, for the same reason as the sibling: what comes
    /// out is two strictly smaller problems of the same kind, and the integrator recurses into
    /// them. Splitting <c>D</c> into coprime <c>A</c> and <c>B</c>, the extended Euclidean
    /// algorithm gives <c>U*A + V*B = 1</c>, so <c>N = N*V*B + N*U*A</c> and
    /// <c>N/(A*B) = N*V/A + N*U/B</c>. Each numerator is then reduced modulo its own
    /// denominator; the polynomial parts that come off cannot survive, since a proper fraction
    /// minus two proper fractions is a polynomial that vanishes at infinity.
    /// </para>
    /// <para>
    /// <b>No condition is owed.</b> <c>A</c> and <c>B</c> being coprime, <c>A*B</c> is zero
    /// exactly where one of them is, so the two sides are undefined at the same points and the
    /// domain neither widens nor narrows. That is what makes this different from cancelling a
    /// shared factor, which is where a decomposition usually loses a singularity.
    /// </para>
    /// <para>
    /// The decomposition is produced only where every piece of it is a shape an integration
    /// rule reads — see the guard below, which is what keeps declining cheap. A denominator
    /// that is a power of one irreducible is declined for the further reason that it has no
    /// coprime pair to split into at all. The ladder that decomposes that one — <c>N/f^k</c>
    /// as terms over <c>f^k</c>, <c>f^(k-1)</c>, ... — is deliberately not built here: for
    /// <c>f</c> linear the sibling step already does it, and for <c>f</c> quadratic every term
    /// it produces is over <c>(x^2 + c)^k</c>, which nothing reads, so the decomposition would
    /// end in the same unevaluated integral it started from.
    /// </para>
    /// </remarks>
    internal static class PartialFractions
    {
        /// <summary>
        /// Whether a numerator over <paramref name="factor"/> to the power
        /// <paramref name="multiplicity"/> is a shape some integration rule answers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a claim about the rule set, not about polynomials, so it is kept in one place
        /// and named rather than left as a condition in a loop — it goes stale whenever a rule is
        /// added, and it had. Each line says which rule reads that shape.
        /// </para>
        /// <para>
        /// A factor with no rule leaves the whole integral unevaluated either way, so admitting
        /// one costs no answer and a great deal of time; that is what the guard is for and why
        /// widening it is done by measurement rather than by leaving it open.
        /// </para>
        /// https://github.com/asc-community/AngouriMath/issues/718
        /// </remarks>
        private static bool AnIntegrationRuleReadsIt(IntegerPolynomial factor, int multiplicity)
            => factor.Degree switch
            {
                // A power of a linear factor: the rule for a numerator over (a x + b)^k.
                <= 1 => true,
                // A quadratic, once. There is no rule for a numerator over (x^2 + c)^k.
                2 => multiplicity == 1,
                // A biquadratic quartic, once: TrySplitBiquadraticOverTheReals factors it into
                // two real quadratics. Only the even-powered shape -- that rule reads
                // x^4 + p x^2 + q and nothing else, and a general quartic has no rule.
                4 => multiplicity == 1 && IsBiquadratic(factor),
                _ => false
            };

        /// <summary>
        /// Whether <paramref name="factor"/> has only even powers, so that it is a quadratic in
        /// <c>x^2</c>.
        /// </summary>
        private static bool IsBiquadratic(IntegerPolynomial factor)
        {
            for (var power = 1; power <= factor.Degree; power += 2)
                if (!factor[power].IsZero)
                    return false;
            return true;
        }

        /// <summary>
        /// <c>N/D</c> written as two fractions over coprime factors of <paramref name="denominator"/>,
        /// each a strictly smaller problem of the same kind, or <see langword="false"/> where
        /// the denominator does not factor into a coprime pair.
        /// </summary>
        internal static bool TrySplitIntoCoprimeParts(
            Entity numerator, Entity denominator, Variable x,
            [NotNullWhen(true)] out Entity? left,
            [NotNullWhen(true)] out Entity? right)
        {
            left = right = null;

            // Degree four is the first at which a polynomial can factor with no rational root
            // anywhere in it, x^4 + 3x^2 + 2 being the smallest. Below that a factorisation
            // implies a root, and the step at a root has already been tried and has failed.
            if (!PolynomialFactoring.TryGetRationalCoefficients(
                    denominator, x, leastTerms: 2, leastDegree: 4, IntegerPolynomial.MaxDegree, out var d)
                || !PolynomialFactoring.TryGetRationalCoefficients(
                    numerator, x, leastTerms: 1, leastDegree: 0, IntegerPolynomial.MaxDegree, out var n))
                return false;

            // Only a proper fraction decomposes; an improper one is a polynomial plus a proper
            // fraction and has to be divided out first, which is not done here.
            if (n.Length >= d.Length)
                return false;

            if (PolynomialFactorization.Factor(denominator, x) is not { } factorization)
                return false;

            // Every piece the decomposition would produce has to be one an integration rule
            // reads, or the decomposition answers nothing and is not worth producing. What that
            // amounts to is written out in AnIntegrationRuleReadsIt, which is a claim about the
            // rules rather than about polynomials and goes stale when they change -- as it had:
            // this said "none for an irreducible factor of degree three or more at all", and an
            // irreducible *biquadratic* quartic has been read by
            // TrySplitBiquadraticOverTheReals since #1102. So x^6 + 1 was declined although the
            // library factors it into (x^2 + 1)(x^4 - x^2 + 1) and integrates both of those on
            // their own.
            //
            // Deleting the guard costs no answer and a great deal of time: a piece with no
            // rule leaves the whole integral unevaluated either way, but every half of every
            // split is a fresh problem the whole integrator searches before that is known.
            // Without it (1 - x^4)/(1 + x^4 + x^8), whose factorisation holds the irreducible
            // quartic x^4 - x^2 + 1, takes 18s to decline where it took 203ms. Read off the
            // factorisation, which is already in hand, the cost of declining is that one
            // factorisation.
            foreach (var part in factorization.Parts)
                if (!AnIntegrationRuleReadsIt(part.Factor, part.Multiplicity))
                    return false;

            // Each part of the factorisation is a distinct irreducible with its multiplicity,
            // so any one of them raised to that multiplicity is coprime to the product of the
            // rest. The smallest is taken, which keeps the intermediate coefficients of the
            // extended Euclidean run down; which one is chosen cannot change what is reachable,
            // since both sides recurse and the full decomposition is arrived at either way.
            var chosen = factorization.Parts[0];
            foreach (var part in factorization.Parts)
                if (part.Factor.Degree * part.Multiplicity < chosen.Factor.Degree * chosen.Multiplicity)
                    chosen = part;

            var whole = RationalPolynomial.Create(d);
            var a = RationalPolynomial.FromInteger(chosen.Factor).Pow(chosen.Multiplicity);
            if (!whole.TryDivide(a, out var b, out var remainder) || !remainder.IsZero || b.IsConstant)
                return false;

            if (!RationalPolynomial.TryBezout(a, b, out var u, out var v))
                return false;

            var wanted = RationalPolynomial.Create(n);
            if (!wanted.Multiply(v).TryDivide(a, out _, out var overA)
                || !wanted.Multiply(u).TryDivide(b, out _, out var overB))
                return false;

            // The identity the two fractions stand for, checked rather than assumed: nothing
            // above is trusted to have produced a decomposition of this numerator in
            // particular, and a wrong split would otherwise leave a wrong antiderivative that
            // only differentiating it back would catch.
            if (!overA.Multiply(b).Add(overB.Multiply(a)).SameAs(wanted))
                return false;

            left = AsFraction(overA, a, x);
            right = AsFraction(overB, b, x);
            return true;
        }

        /// <summary>
        /// <c>N/D</c> written as two fractions over the quadratic factors of a <b>biquadratic</b>
        /// <paramref name="denominator"/> — one with no odd power in it — or
        /// <see langword="false"/> where it is not one, or does not split into two distinct
        /// quadratics.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Why this exists next to the step above.</b> That one factors over the rationals,
        /// and stops where the rationals do: <c>x^4 + 1</c> is irreducible over <c>Q</c>, so it
        /// is left whole and <c>x^2/(x^4 + 1)</c> has no antiderivative — the case
        /// <a href="https://github.com/asc-community/AngouriMath/issues/233">#233</a> names as
        /// wanting "partial fractioning". Over the reals it is
        /// <c>(x^2 - sqrt(2)x + 1)(x^2 + sqrt(2)x + 1)</c>, and both halves are read by the rule
        /// for a linear numerator over a quadratic. Nothing was missing but a factorisation the
        /// rational one is right to refuse.
        /// </para>
        /// <para>
        /// <b>Biquadratic only, and that is a real boundary rather than a first cut.</b> A
        /// general quartic factors into real quadratics through its resolvent cubic, whose roots
        /// carry Cardano's nested radicals; a biquadratic <c>x^4 + px^2 + q</c> is the case where
        /// the resolvent is solvable by inspection, and the two factors stay in one square root.
        /// Two shapes come out of it, by the sign of <c>p^2 - 4q</c>:
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// <b>Negative</b> — no real root in <c>x^2</c>. Matching
        /// <c>(x^2 + ax + b)(x^2 - ax + b) = x^4 + (2b - a^2)x^2 + b^2</c> gives
        /// <c>b = sqrt(q)</c> and <c>a = sqrt(2b - p)</c>, both real because <c>q &gt; 0</c> and
        /// <c>p^2 &lt; 4q</c> forces <c>p &lt; 2sqrt(q)</c>. This is <c>x^4 + 1</c>, at
        /// <c>a = sqrt(2)</c>, <c>b = 1</c>.
        /// </item>
        /// <item>
        /// <b>Positive</b> — two distinct real roots in <c>x^2</c>, so
        /// <c>(x^2 + M)(x^2 + N)</c> with <c>M, N = (p +- sqrt(p^2 - 4q))/2</c>. Both factors are
        /// even, and the split is two independent pairs of equations rather than four.
        /// </item>
        /// <item>
        /// <b>Zero</b> — <c>(x^2 + p/2)^2</c>, a repeated quadratic, declined for the same reason
        /// the guard above declines one: there is no rule for a numerator over
        /// <c>(x^2 + c)^k</c>, so decomposing it ends in the integral it started from.
        /// </item>
        /// </list>
        /// <para>
        /// <b>No condition is owed</b>, on the same argument as the step above: the two factors
        /// are distinct and coprime, so their product vanishes exactly where the original
        /// denominator does. <c>q &gt; 0</c> is required rather than assumed, which is what keeps
        /// <c>b</c> real; a negative <c>q</c> puts a real root in <c>x^2</c> of either sign and
        /// is left to the rational step, which reaches it whenever the root is rational.
        /// </para>
        /// <para>
        /// Reached only after the rational split has declined, so a biquadratic that factors over
        /// <c>Q</c> — <c>x^4 + 3x^2 + 2</c> — is decomposed there, in exact arithmetic, and never
        /// arrives here to be given a square root it does not need.
        /// </para>
        /// </remarks>
        internal static bool TrySplitBiquadraticOverTheReals(
            Entity numerator, Entity denominator, Variable x,
            [NotNullWhen(true)] out Entity? left,
            [NotNullWhen(true)] out Entity? right)
        {
            left = right = null;

            // Every guard below is rational arithmetic on coefficients already in hand, so a
            // denominator this does not apply to costs one polynomial read to decline.
            if (!PolynomialFactoring.TryGetRationalCoefficients(
                    denominator, x, leastTerms: 2, leastDegree: 4, maxDegree: 4, out var d)
                || d.Length != 5
                || !d[1].IsZero || !d[3].IsZero)
                return false;

            // A proper fraction only, as above: an improper one is a polynomial plus a proper
            // fraction and has to be divided out first, which is not done here. The degree
            // ceiling of three is what says so, the denominator's being four.
            if (!PolynomialFactoring.TryGetRationalCoefficients(
                    numerator, x, leastTerms: 1, leastDegree: 0, maxDegree: 3, out var c))
                return false;

            var lead = d[4];
            var p = d[2].Divide(lead);
            var q = d[0].Divide(lead);

            // At q = 0 the quartic is x^2(x^2 + p), whose rational root zero the step above has
            // already had. Nothing else about the sign of q is required here: the branch that
            // needs sqrt(q) real is the one below with a negative discriminant, and p^2 < 4q
            // makes q positive on its own.
            if (q.IsZero)
                return false;

            // A coefficient that is not finite is not a coefficient: a rewrite upstream that
            // divided zero by zero reads as a rational NaN, and `Rational.Create` on it throws.
            if (!p.IsFinite || !q.IsFinite)
                return false;
            var discriminant = p.Multiply(p).Subtract(q.Multiply(ERational.FromInt32(4)));
            if (discriminant.IsZero)
                return false;

            // The numerator, padded to four coefficients so the two branches can index it
            // without asking how many terms it happened to have.
            var n = new Entity[4];
            for (var i = 0; i < n.Length; i++)
                n[i] = Rational.Create(i < c.Length ? c[i] : ERational.Zero);

            Entity leftNumerator, leftFactor, rightNumerator, rightFactor;
            if (discriminant.Sign < 0)
            {
                // (x^2 + ax + b)(x^2 - ax + b). Writing the split as (alpha x + beta)/A +
                // (gamma x + delta)/B and equating the four coefficients of
                // (alpha x + beta)B + (gamma x + delta)A against the numerator gives, using
                // B's -a where A has +a:
                //
                //   x^3:  alpha + gamma          = n3
                //   x^2:  a(gamma - alpha) + beta + delta = n2
                //   x^1:  b(alpha + gamma) + a(delta - beta) = n1
                //   x^0:  b(beta + delta)        = n0
                //
                // which is two sums and two differences rather than a linear solve.
                var b = MathS.Sqrt(Rational.Create(q)).InnerSimplified;
                var a = MathS.Sqrt(2 * b - Rational.Create(p)).InnerSimplified;

                var sum = (n[0] / b).InnerSimplified;                       // beta + delta
                var difference = ((n[1] - b * n[3]) / a).InnerSimplified;   // delta - beta
                var spread = ((n[2] - sum) / a).InnerSimplified;            // gamma - alpha

                leftFactor = MathS.Sqr(x) + a * x + b;
                rightFactor = MathS.Sqr(x) - a * x + b;
                leftNumerator = ((n[3] - spread) / 2 * x + (sum - difference) / 2).InnerSimplified;
                rightNumerator = ((n[3] + spread) / 2 * x + (sum + difference) / 2).InnerSimplified;
            }
            else
            {
                // (x^2 + u)(x^2 + v), both even, so the odd and even halves of the numerator
                // separate and each gives its own pair rather than one system of four. Writing
                // the split as (alpha x + beta)/(x^2 + u) + (gamma x + delta)/(x^2 + v), the
                // coefficients of (alpha x + beta)(x^2 + v) + (gamma x + delta)(x^2 + u) are
                //
                //   x^3: alpha + gamma = n3     x^1: v*alpha + u*gamma = n1
                //   x^2: beta  + delta = n2     x^0: v*beta  + u*delta = n0
                //
                // so alpha = (n1 - u*n3)/(v - u) and beta = (n0 - u*n2)/(v - u), with v - u the
                // square root of the discriminant. Note which of the two the numerators divide
                // by: pairing a numerator with the wrong factor flips the sign of the answer
                // and still satisfies the x^3 and x^2 rows, so it is not something the identity
                // check further down would catch on every numerator.
                var root = MathS.Sqrt(Rational.Create(discriminant)).InnerSimplified;
                var v = ((Rational.Create(p) + root) / 2).InnerSimplified;
                var u = ((Rational.Create(p) - root) / 2).InnerSimplified;

                var alpha = ((n[1] - u * n[3]) / root).InnerSimplified;
                var beta = ((n[0] - u * n[2]) / root).InnerSimplified;

                leftFactor = MathS.Sqr(x) + u;
                rightFactor = MathS.Sqr(x) + v;
                leftNumerator = (alpha * x + beta).InnerSimplified;
                rightNumerator = ((n[3] - alpha) * x + (n[2] - beta)).InnerSimplified;
            }

            // Two identities, checked rather than assumed -- the numerators against the numerator
            // they decompose, and the factors against the denominator they came from. The step
            // above checks one because its factorisation is exact by construction; here the
            // factors were built by matching coefficients through a square root, so the
            // factorisation is a claim of its own.
            //
            // Neither implies the other, so both are made. A wrong term common to the two factors
            // -- MathS.Sqr(x) misread as C#'s x ^ 2, which on an Entity is exclusive or and not a
            // power -- cancels between the two halves of the first identity and passes it, while
            // the second sees it immediately.
            if ((leftNumerator * rightFactor + rightNumerator * leftFactor
                    - numerator).Simplify() != Integer.Create(0))
                return false;
            if ((Rational.Create(lead) * leftFactor * rightFactor - denominator).Simplify()
                    != Integer.Create(0))
                return false;

            left = (leftNumerator / (Rational.Create(lead) * leftFactor)).InnerSimplified;
            right = (rightNumerator / (Rational.Create(lead) * rightFactor)).InnerSimplified;
            return true;
        }

        /// <summary>
        /// A vanishing numerator is answered as zero rather than as a quotient, so that a
        /// numerator sharing a factor with the denominator does not leave the integrator
        /// working out the antiderivative of the other side only to multiply it by nothing.
        /// </summary>
        private static Entity AsFraction(RationalPolynomial numerator, RationalPolynomial denominator, Variable x)
            => numerator.IsZero ? Integer.Create(0) : numerator.ToEntity(x) / denominator.ToEntity(x);

        /// <summary>
        /// The largest value of the elimination, by <see cref="Entity.Complexity"/>, that is
        /// put in lowest terms over the symbols; the ones that needed it were two thousand
        /// nodes, and the ones that could not be were twenty thousand.
        /// </summary>
        private const int LargestValuePutInLowestTerms = 4096;

        /// <summary>
        /// The largest decomposition, by the <see cref="Entity.Complexity"/> of its values
        /// together, that is checked and handed on. The elimination is fraction-free over the
        /// symbols, and its values grow as determinants do: for
        /// `(-8 c^11 x + ...)/((c x - 1)^2 (c x + 1))`, which by parts leaves from
        /// `acoth(c x) ln(1 - c^2 x^2)`, the nine values came out at ten million nodes each
        /// and the sum at thirty-three million, and pinning `c` in it to check it ran out of
        /// memory. Nothing downstream reads a decomposition of that size; the largest that
        /// was ever answered was eighty thousand, four values of twenty thousand.
        /// </summary>
        private const int LargestDecompositionHandedOn = 250_000;

        /// <summary>
        /// <c>N/D</c> written as one fraction per factor of <paramref name="denominator"/>,
        /// where the denominator is <b>written</b> as a product of distinct linear and quadratic
        /// factors whose coefficients may be symbols, or <see langword="false"/> where it is not
        /// or the decomposition cannot be settled.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The two splits above work in exact rational arithmetic and stop where the rationals
        /// do, and a symbol is not a rational: <c>1/((x + a)(x^2 + b))</c> has factors already in
        /// hand and each of them is a shape the integrator reads, and it was declined because
        /// nothing here could read the factors. This is the textbook method for it -- undetermined
        /// coefficients -- and it is the one that survives a symbol, because it solves a linear
        /// system in the unknown numerators rather than dividing polynomials whose coefficients
        /// have to be compared to zero.
        /// </para>
        /// <para>
        /// <b>Which pivots are safe.</b> The system's entries are polynomials in the parameters,
        /// and a row reduction has to decide whether a pivot is zero. A number is decided by
        /// looking; a symbolic pivot is taken only when it does not simplify to zero, which is a
        /// judgement and not a proof, and that is why <b>the decomposition is checked before it
        /// is returned</b>: the identity <c>N = sum P_i * D/F_i</c> is evaluated at several
        /// points with every symbol pinned, and a split that fails it is declined rather than
        /// handed on. A wrong pivot can cost an answer here and cannot produce a wrong one.
        /// </para>
        /// <para>
        /// The generic case, as everywhere in this integrator: two factors that coincide for
        /// one value of a parameter are coprime for every other, and the answer is the one for
        /// those. See the note on <c>PolynomialLongDivision</c>.
        /// </para>
        /// <para>
        /// Distinct factors, where a repeated linear one counts as a single block over its whole
        /// power. A repeated symbolic quadratic has no rule to land on and is declined.
        /// </para>
        /// https://github.com/asc-community/AngouriMath/issues/718
        /// </remarks>
        internal static bool TrySplitOverWrittenFactors(
            Entity numerator, Entity denominator, Variable x, [NotNullWhen(true)] out Entity? decomposition)
        {
            decomposition = null;

            // The factors as written, each a polynomial in x of degree one or two with
            // coefficients free of x; a factor free of x is a constant and comes out in front.
            var factors = new List<(Entity Factor, int Degree)>();
            Entity constant = Integer.One;
            foreach (var part in Mulf.LinearChildren(denominator))
            {
                if (!part.ContainsNode(x))
                {
                    constant *= part;
                    continue;
                }
                // A factor written as a power is a repeated factor, whatever its degree reads
                // as: `(a + b u)^2` is a quadratic to the reader below and a repeated linear
                // factor to the decomposition, and taking it as the former cost five seconds on
                // `1/(a + b e^(p x))^2` for a split that nothing downstream answers. A repeated
                // **linear** factor is taken as one block, `P/(a + b u)^k` with `P` of degree
                // `k - 1`, which is the shape the rule for a numerator over a power of a linear
                // reads; a repeated quadratic has no such rule and is declined.
                var toRead = part;
                var multiplicity = 1;
                if (part is Powf(var repeatedBase, Integer { EInteger.Sign: > 0 } repeated) && repeated != Integer.One)
                {
                    if (!repeated.EInteger.CanFitInInt32())
                        return false;
                    toRead = repeatedBase;
                    multiplicity = repeated.EInteger.ToInt32Unchecked();
                }
                if (!TreeAnalyzer.TryGetPolynomial(toRead, x, out var read) || read.Count == 0)
                    return false;
                var degree = read.Keys.Max()!;
                if (!degree.CanFitInInt32() || degree.ToInt32Unchecked() is not (1 or 2))
                    return false;
                // A repeated quadratic has a rule to land on only with a symbol somewhere,
                // the split by residues below; over the rationals the system would hand it a
                // cubic over the square, which no rule reads.
                if (multiplicity > 1 && degree.ToInt32Unchecked() != 1 && !(numerator + denominator).Vars.Any(v => v != x))
                    return false;
                foreach (var pair in read)
                    if (pair.Key.Sign < 0 || pair.Value.ContainsNode(x))
                        return false;
                factors.Add((part.InnerSimplified, degree.ToInt32Unchecked() * multiplicity));
            }
            // Two linear factors with one root are one factor: `(a + b x)(a + x b)^2` is
            // written so by the rules that make a factor monic and gather its powers, and
            // as two distinct factors the decomposition has no answer -- its coefficients
            // are values at a root of the other factor -- where `(a + b x)^3` is a line.
            // `(alpha_2 + beta_2 x)` is `(beta_2/beta_1)(alpha_1 + beta_1 x)`, the quotient
            // going out in front.
            var merged = false;
            for (var i = 0; i < factors.Count; i++)
                for (var j = factors.Count - 1; j > i; j--)
                {
                    if (!TryReadLinear(factors[i].Factor, x, out var alpha1, out var beta1, out var power1)
                        || !TryReadLinear(factors[j].Factor, x, out var alpha2, out var beta2, out var power2))
                        continue;
                    var crossed = Bare((alpha1 * beta2 - alpha2 * beta1).InnerSimplified);
                    if (crossed.Evaled is not Complex { IsZero: true } && (crossed.Vars.Any() ? Bare(crossed.Simplify()).Evaled is not Complex { IsZero: true } : true))
                        continue;
                    var linear = factors[i].Factor is Powf(var repeatedBase, _) ? repeatedBase : factors[i].Factor;
                    constant *= MathS.Pow(Bare((beta2 / beta1).InnerSimplified), power2);
                    factors[i] = (MathS.Pow(linear, power1 + power2), power1 + power2);
                    factors.RemoveAt(j);
                    merged = true;
                }
            // One factor once merged is one block, the numerator over the power as it is,
            // which the rule for a polynomial over a power of a linear reads.
            if (factors.Count == 1 && merged)
            {
                decomposition = numerator / (constant == Integer.One ? factors[0].Factor : constant * factors[0].Factor);
                return HoldsAtSampledPoints(numerator / denominator, decomposition, x);
            }
            if (factors.Count < 2)
                return false;
            for (var i = 0; i < factors.Count; i++)
                for (var j = i + 1; j < factors.Count; j++)
                    if (factors[i].Factor == factors[j].Factor)
                        return false;

            var total = factors.Sum(f => f.Degree);
            if (!TreeAnalyzer.TryGetPolynomial(numerator, x, out var above))
                return false;
            foreach (var pair in above)
                if (pair.Key.Sign < 0 || pair.Key.CompareTo(EInteger.FromInt32(total)) >= 0 || pair.Value.ContainsNode(x))
                    return false;

            // Linear factors with a symbol in a coefficient, by the derivatives: over
            // `(x - r)^k` the coefficient of `1/(x - r)^j` is the `(k - j)`th derivative of the
            // rest at `r` over `(k - j)!`, a small expression in the symbols, where the
            // system below solved fraction-free hands back each coefficient as a quotient of
            // determinants -- `1/((a + b x)(f + g x)^3)` came out with `210 a^8 b^2 f^24 g^14`
            // in every term and twenty kilobytes of answer, right and unreadable, and its
            // fourth power did not evaluate. Numeric coefficients keep the system, which is
            // exact over the rationals and answers them as before.
            // And beside a quadratic factor, the same way: the linear blocks by their Taylor
            // coefficients at the root, and the quadratic's numerator in the ring modulo the
            // quadratic. Rubi's `tan^4 (A + B tan)/(a + b tan)^4` is
            // `u^4 (A + B u)/((a + b u)^4 (1 + u^2))` under the tangent, and the system
            // answered it in `a^63 b^10` and did not evaluate within its budget; this way it
            // is a line of arctangents and logarithms.
            // https://github.com/asc-community/AngouriMath/issues/718
            // And quadratics alone, with no linear among them, the same way where a symbolic
            // one is repeated: each by its residues, where the system takes the symbols through
            // every row. https://github.com/asc-community/AngouriMath/issues/718
            if ((factors.Any(f => IsLinear(f.Factor, x))
                 || factors.Any(f => f.Factor is Powf(var repeated, Integer { EInteger.Sign: > 0 } times) && times != Integer.One && repeated.Vars.Any(v => v != x)))
                && (numerator + denominator).Vars.Any(v => v != x)
                && TrySplitOverSymbolicLinearFactors(numerator, denominator, constant, factors, x, out decomposition))
                return true;

            // One unknown per coefficient of each numerator P_i, of degree one less than F_i.
            var unknowns = new List<Variable>();
            var nameSource = numerator + denominator;
            var numerators = new List<Entity>();
            foreach (var (_, degree) in factors)
            {
                // Built without a `x^0`, which `Expand` guards with a `provided x != 0` that
                // nothing downstream can read as a polynomial.
                Entity? p = null;
                for (var k = 0; k < degree; k++)
                {
                    var unknown = Variable.CreateUnique(nameSource, "c");
                    nameSource += unknown;
                    unknowns.Add(unknown);
                    Entity term = k == 0 ? unknown : k == 1 ? unknown * x : unknown * MathS.Pow(x, k);
                    p = p is null ? term : p + term;
                }
                numerators.Add(p!);
            }

            // N = sum P_i * prod_{j != i} F_j, compared coefficient by coefficient in x.
            Entity identity = 0;
            for (var i = 0; i < factors.Count; i++)
            {
                Entity cofactor = numerators[i];
                for (var j = 0; j < factors.Count; j++)
                    if (j != i)
                        cofactor *= factors[j].Factor;
                identity += cofactor;
            }
            if (!TreeAnalyzer.TryGetPolynomial(identity, x, out var rows))
                return false;

            var width = unknowns.Count;
            var matrix = new Entity[total][];
            var rhs = new Entity[total];
            for (var power = 0; power < total; power++)
            {
                var row = rows.TryGetValue(EInteger.FromInt32(power), out var atPower) ? atPower : Integer.Zero;
                matrix[power] = new Entity[width];
                for (var column = 0; column < width; column++)
                    matrix[power][column] = row.Differentiate(unknowns[column]).InnerSimplified;
                rhs[power] = above.TryGetValue(EInteger.FromInt32(power), out var wanted) ? wanted : Integer.Zero;
            }

            if (!TrySolveSquare(matrix, rhs, out var values))
                return false;

            // Each value in lowest terms over the symbols where there are any: the
            // elimination hands back quotients of determinants, and `csch(x)^5/(a + b cosh(x))`
            // under `u = e^x`, after the Hermite reduction, is a numerator with a page of
            // `a` and `b` in it over `(u^2 - 1)(b + 2 a u + b u^2)`, whose values came out at
            // two thousand nodes each, and the integrand each makes with its factor went round
            // the chain and did not return.
            // https://github.com/asc-community/AngouriMath/issues/718
            // Bounded: the lowest terms are an expansion and a gcd, and a value of twenty
            // thousand nodes -- `cosh(c + d x)^6/(a + b sinh(c + d x)^2)^2` has four of them,
            // and is answered in six seconds with them as they come -- did not return from
            // either in three; past the bound a value is left as the elimination gave it.
            if (values.Any(value => value.Vars.Any()))
                for (var column = 0; column < width; column++)
                    if (values[column].Complexity <= LargestValuePutInLowestTerms)
                        values[column] = InLowestTermsOverTheSymbols(values[column]);
            // ...and not handed on at all past the bound: a decomposition of millions of
            // nodes is not one any rule reads, and pinning a symbol in it to check it is
            // what ran out of memory.
            if (values.Sum(value => (long)value.Complexity) > LargestDecompositionHandedOn)
                return false;
            Entity sum = 0;
            for (var i = 0; i < factors.Count; i++)
            {
                var p = numerators[i];
                for (var column = 0; column < width; column++)
                    p = p.Substitute(unknowns[column], values[column]);
                sum += Bare(p) / factors[i].Factor;
            }

            // Checked rather than trusted, at points with every symbol pinned: a pivot that was
            // zero without looking it would have produced a wrong decomposition, and this is
            // what turns that into a declined one.
            if (!HoldsAtSampledPoints(numerator / denominator, sum / constant, x))
                return false;

            decomposition = sum / constant;
            return true;
        }

        /// <summary>
        /// A constant that is a quotient of polynomials in the symbols, over one bar and in
        /// lowest terms by the polynomial gcd; over one bar as expanded where the gcd
        /// declines, and zero where the numerator is.
        /// </summary>
        internal static Entity InLowestTermsOverTheSymbols(Entity constant)
        {
            var (above, below) = SingleQuotient.Of(SingleQuotient.Combine(constant).InnerSimplified);
            var expandedAbove = Bare(above.Expand().InnerSimplified);
            // Zero as a value, decided at two sets of pinned symbols: `b (a + b (-a/b))` is
            // zero and neither the expansion nor the evaluation of numbers says so.
            if (expandedAbove == Integer.Zero || expandedAbove.Evaled is Complex { IsZero: true } || IsZeroAtPinnedSymbols(expandedAbove))
                return Integer.Zero;
            var expandedBelow = Bare(below.Expand().InnerSimplified);
            // With room for the coefficient a fourth-order block leaves: the series of
            // `x^4 (A + B x)/(1 + x^2)` at `-a/b` had `(a^2 + b^2)^3` in common at the third
            // order and eleven hundred nodes, and left uncancelled it carried into every
            // term of the answer.
            if (PolynomialGcd.TryCancel(expandedAbove, expandedBelow, out var cancelled, maxComplexity: 4096))
                return WithThePrimitiveDenominator(Bare(cancelled));
            // The gcd declines past its degree and step bounds, and a denominator that comes
            // out of a series is a product of written factors -- `b^9 (a^2 + b^2)^7` -- so the
            // common factor is one of them to some power: each is divided out of the
            // numerator exactly for as long as that goes.
            if (CancelledByTheWrittenFactors(expandedAbove, below) is { } byFactors)
                return WithThePrimitiveDenominator(byFactors);
            return WithThePrimitiveDenominator(expandedBelow == Integer.One ? expandedAbove : expandedAbove / expandedBelow);
        }

        /// <summary>
        /// Whether a constant in the symbols is zero as a value, decided as
        /// <see cref="InLowestTermsOverTheSymbols"/> decides it: over one bar, its numerator
        /// expanded is zero as written, as a number, or at two sets of pinned symbols. A
        /// coefficient read off a written polynomial can be zero so and not as written:
        /// <c>-3 a^3 + 3 a^3</c>, which the expansion of <c>(-(a - a u^2) - (u^2 + 1) a)^3</c>
        /// leaves at <c>u^6</c>, and <c>-(2 c/c - 1) + 1</c>.
        /// https://github.com/asc-community/AngouriMath/issues/1665
        /// </summary>
        internal static bool IsZeroAsAValue(Entity constant)
        {
            if (constant == Integer.Zero || constant.Evaled is Complex { IsZero: true })
                return true;
            if (!constant.Vars.Any())
                return false;
            var (above, _) = SingleQuotient.Of(SingleQuotient.Combine(constant).InnerSimplified);
            var expandedAbove = Bare(above.Expand().InnerSimplified);
            return expandedAbove == Integer.Zero || expandedAbove.Evaled is Complex { IsZero: true } || IsZeroAtPinnedSymbols(expandedAbove);
        }

        /// <summary>
        /// <paramref name="quotient"/> with the rational content of its denominator moved
        /// up: the denominator's coefficients whole and coprime with a positive leading one,
        /// and the numerator scaled by what that took out. <c>-1024/(1024 a)</c> is
        /// <c>-1/a</c>.
        /// </summary>
        /// <remarks>
        /// The polynomial gcd normalizes its divisor to whole coprime coefficients, so it
        /// never cancels a number, and two sides without a variable in common are coprime to
        /// it before it looks. Newton's iteration for the inverse modulo a power of a
        /// quadratic squares its iterate every round, and an uncancelled <c>1024/1024</c>
        /// after the first round was integers of twelve hundred digits after the third and
        /// a minute of gcd on them for <c>tanh(x)^6/(a + a sech(x))</c>, which is a sum of
        /// small fractions.
        /// https://github.com/asc-community/AngouriMath/issues/718
        /// </remarks>
        private static Entity WithThePrimitiveDenominator(Entity quotient)
        {
            var (above, below) = SingleQuotient.Of(quotient);
            if (below == Integer.One || below.Evaled is Complex { IsZero: true })
                return quotient;
            var variables = above.Vars.Concat(below.Vars).Distinct().OrderBy(variable => variable.Name, System.StringComparer.Ordinal).ToArray();
            if (variables.Length > MultivariatePolynomial.MaxVariables)
                return quotient;
            var indices = new Dictionary<Variable, int>(variables.Length);
            for (var i = 0; i < variables.Length; i++)
                indices[variables[i]] = i;
            if (MultivariatePolynomial.TryParse(below, indices) is not { } bottom || bottom.IsZero
                || MultivariatePolynomial.TryParse(above, indices) is not { } top)
                return quotient;
            var primitive = bottom.Normalized(out var scale);
            if (scale.CompareTo(ERational.One) == 0)
                return quotient;
            var scaledTop = top.ScaleBy(scale).ToEntity(variables);
            return primitive.IsConstant ? scaledTop : scaledTop / primitive.ToEntity(variables);
        }

        /// <summary>
        /// <paramref name="numerator"/> over <paramref name="denominator"/>, a product of
        /// powers as written, with every written factor that divides the numerator exactly
        /// divided out of both; null where none does, or where either side does not read as
        /// a polynomial over the rationals.
        /// </summary>
        private static Entity? CancelledByTheWrittenFactors(Entity numerator, Entity denominator)
        {
            var variables = numerator.Vars.Concat(denominator.Vars).Distinct().OrderBy(v => v.Name, System.StringComparer.Ordinal).ToArray();
            if (variables.Length == 0 || variables.Length > MultivariatePolynomial.MaxVariables)
                return null;
            var indices = new Dictionary<Variable, int>(variables.Length);
            for (var i = 0; i < variables.Length; i++)
                indices[variables[i]] = i;
            if (MultivariatePolynomial.TryParse(numerator, indices) is not { } top)
                return null;
            var cancelledAny = false;
            Entity left = Integer.One;
            foreach (var written in Mulf.LinearChildren(denominator))
            {
                var (@base, power) = written is Powf(var repeated, Integer { EInteger.Sign: > 0 } times) && times.EInteger.CanFitInInt32()
                    ? (repeated, times.EInteger.ToInt32Unchecked()) : (written, 1);
                if (@base is Number || !@base.Vars.Any() || MultivariatePolynomial.TryParse(@base, indices) is not { } factor || factor.IsConstant)
                {
                    left *= written;
                    continue;
                }
                while (power > 0 && top.DivideExact(factor) is { } quotient)
                {
                    top = quotient;
                    power--;
                    cancelledAny = true;
                }
                if (power > 0)
                    left *= power == 1 ? @base : MathS.Pow(@base, power);
            }
            if (!cancelledAny)
                return null;
            var reduced = top.ToEntity(variables);
            var leftExpanded = Bare(left.Expand().InnerSimplified);
            return leftExpanded == Integer.One ? reduced : reduced / leftExpanded;
        }

        /// <summary>
        /// Whether <paramref name="expr"/>, free of every variable but symbols, is zero -- within
        /// <c>1e-30</c> -- at two sets of values for them, off the integers and distinct; a value
        /// that is not a number at either, or that the evaluation cannot place, counts as not zero.
        /// </summary>
        private static bool IsZeroAtPinnedSymbols(Entity expr)
        {
            var symbols = expr.Vars.ToList();
            foreach (var seed in new[] { 0, 1 })
            {
                var pinned = expr;
                var i = 0;
                foreach (var symbol in symbols)
                {
                    var value = Real.Create(EDecimal.FromString((i % 3) switch { 0 => "1.37", 1 => "2.71", _ => "0.83" }).Add(EDecimal.FromInt32(i + 5 * seed)));
                    pinned = pinned.Substitute(symbol, value);
                    i++;
                }
                if (Agree(pinned, Integer.Zero, 1e-30, new System.Lazy<PreciseEvaluation>[] { new(() => PreciseEvaluation.In(40)), new(() => PreciseEvaluation.In(80)) }) is not true)
                    return false;
            }
            return true;
        }

        /// <summary>A written factor as <c>(alpha + beta x)^power</c>, for a linear base with coefficients free of <paramref name="x"/>.</summary>
        private static bool TryReadLinear(Entity factor, Variable x, out Entity alpha, out Entity beta, out int power)
        {
            alpha = beta = Integer.Zero;
            power = 1;
            var @base = factor;
            if (factor is Powf(var repeatedBase, Integer { EInteger.Sign: > 0 } repeated) && repeated.EInteger.CanFitInInt32())
            {
                @base = repeatedBase;
                power = repeated.EInteger.ToInt32Unchecked();
            }
            if (!TreeAnalyzer.TryGetPolynomial(@base, x, out var read) || read.Count == 0 || !read.Keys.Max()!.Equals(EInteger.One)
                || read.Values.Any(coefficient => coefficient.ContainsNode(x)))
                return false;
            beta = read[EInteger.One];
            alpha = read.TryGetValue(EInteger.Zero, out var constantTerm) ? constantTerm : Integer.Zero;
            return true;
        }

        /// <summary>Whether a written factor, or the base of a written power, is linear in <paramref name="x"/>.</summary>
        private static bool IsLinear(Entity factor, Variable x)
            => TreeAnalyzer.TryGetPolynomial(factor is Powf(var b, _) ? b : factor, x, out var linear) && linear.Count > 0 && linear.Keys.Max()!.Equals(EInteger.One);

        /// <summary>
        /// <see cref="TrySplitOverWrittenFactors"/> for linear and quadratic factors with
        /// their multiplicities, with a symbol somewhere. The coefficient of <c>1/F^j</c> for
        /// <c>F = alpha + beta x</c> of multiplicity <c>k</c> is <c>beta^(j-k) h_(k-j)</c> with
        /// <c>h_m</c> the <c>m</c>th Taylor coefficient at <c>r = -alpha/beta</c> of the
        /// numerator over the other factors; the numerators over the powers of a quadratic
        /// <c>Q</c> of multiplicity <c>j</c> are the digits of the numerator times the inverse
        /// of the other factors in powers of <c>Q</c>, modulo <c>Q^j</c>. Checked at sampled
        /// points like the system's answer.
        /// </summary>
        private static bool TrySplitOverSymbolicLinearFactors(
            Entity numerator, Entity denominator, Entity constant, List<(Entity Factor, int Degree)> factors, Variable x,
            [NotNullWhen(true)] out Entity? decomposition)
        {
            decomposition = null;
            Entity sum = 0;
            for (var i = 0; i < factors.Count; i++)
            {
                var (written, multiplicity) = factors[i];
                if (!IsLinear(written, x))
                    continue;
                var factor = written is Powf(var repeatedBase, _) ? repeatedBase : written;
                if (!TreeAnalyzer.TryGetPolynomial(factor, x, out var read))
                    return false;
                var beta = read[EInteger.One];
                var alpha = read.TryGetValue(EInteger.Zero, out var constantTerm) ? constantTerm : Integer.Zero;
                var root = Bare((-alpha / beta).InnerSimplified);
                Entity others = Integer.One;
                for (var j = 0; j < factors.Count; j++)
                    if (j != i)
                        others *= factors[j].Factor;
                // The Taylor coefficients of the numerator over the other factors at the
                // root, by the series quotient of their own: each is a polynomial in x, so
                // shifted to the root and expanded it is a polynomial in the offset with
                // coefficients that are small quotients in the symbols, and the quotient of
                // the two series is one division per order. Differentiating the quotient
                // symbolically and evaluating at the root gives the same coefficients as
                // quotients of derivatives of the other factors' product, which the gcd does
                // not cancel: the third derivative of `x^4 (A + B x)/(1 + x^2)` at `-a/b` comes
                // out in `b^249`.
                if (TaylorCoefficientsAtTheRoot(numerator, root, multiplicity, x) is not { } above
                    || TaylorCoefficientsAtTheRoot(others, root, multiplicity, x) is not { } below)
                    return false;
                // A root the other factors share is not this block's alone, and the generic
                // case has nothing to say about it.
                if (below[0] == Integer.Zero || below[0].Evaled is Complex { IsZero: true } || IsZeroAtPinnedSymbols(below[0]))
                    return false;
                var series = new Entity[multiplicity];
                for (var order = 0; order < multiplicity; order++)
                {
                    Entity value = above[order];
                    for (var k = 1; k <= order; k++)
                        value -= below[k] * series[order - k];
                    series[order] = InLowestTermsOverTheSymbols(value / below[0]);
                    if (series[order].Nodes.Any(node => node == MathS.NaN) || series[order].ContainsNode(x))
                        return false;
                }
                for (var order = 0; order < multiplicity; order++)
                {
                    var j = multiplicity - order;
                    var coefficient = InLowestTermsOverTheSymbols(MathS.Pow(beta, j - multiplicity) * series[order]);
                    if (coefficient == Integer.Zero || coefficient.Evaled is Complex { IsZero: true })
                        continue;
                    var term = j == 1 ? coefficient / factor : coefficient / MathS.Pow(factor, j);
                    sum += term;
                }
            }
            // A quadratic factor takes a numerator of degree one, and it is computed where
            // it lives: in the ring of polynomials modulo the quadratic, where every other
            // factor is a residue with an inverse, and the numerator is the numerator of the
            // whole times those inverses. Two coefficients, each a small quotient in the
            // symbols, and no expansion of anything but the quadratic's own reductions --
            // taking the linear blocks off the numerator and dividing by their factors
            // needs expansions of nested products of `b (a^2 + b^2)^3` past the term bound.
            foreach (var (written, _) in factors)
            {
                if (IsLinear(written, x))
                    continue;
                var (quadraticFactor, multiplicity) = written is Powf(var repeatedQuadratic, Integer { EInteger.Sign: > 0 } times) && times.EInteger.CanFitInInt32()
                    ? (repeatedQuadratic, times.EInteger.ToInt32Unchecked()) : (written, 1);
                if (!TreeAnalyzer.TryGetPolynomial(quadraticFactor, x, out var quadratic) || !quadratic.ContainsKey(EInteger.FromInt32(2)))
                    return false;
                var leading = quadratic[EInteger.FromInt32(2)];
                var s = InLowestTermsOverTheSymbols((quadratic.TryGetValue(EInteger.One, out var q1) ? q1 : Integer.Zero) / leading);
                var t = InLowestTermsOverTheSymbols((quadratic.TryGetValue(EInteger.Zero, out var q0) ? q0 : Integer.Zero) / leading);
                // The numerators over the powers of the quadratic are the digits of
                // `N/others` written in powers of the monic quadratic, modulo its
                // `multiplicity`th power: the inverse of the other factors in that ring, by
                // Newton's iteration from the inverse modulo the quadratic itself, times the
                // numerator, then divided by the quadratic digit by digit.
                if (Coefficients(numerator, x) is not { } top)
                    return false;
                Entity[] product = { Integer.One };
                foreach (var (other, _) in factors)
                {
                    if (other == written)
                        continue;
                    if (Coefficients(other, x) is not { } otherCoefficients)
                        return false;
                    product = Multiplied(product, otherCoefficients);
                }
                if (InverseModuloTheQuadratic(ResidueModuloTheQuadratic(product, s, t), s, t) is not { } firstInverse)
                    return false;
                Entity[] inverse = { firstInverse.Item1, firstInverse.Item2 };
                for (var round = 1; round < multiplicity; round++)
                {
                    // v <- v (2 - o v), which doubles the digits that are right each time.
                    var correction = Multiplied(product, inverse);
                    correction[0] = InLowestTermsOverTheSymbols(2 - correction[0]);
                    for (var k = 1; k < correction.Length; k++)
                        correction[k] = InLowestTermsOverTheSymbols(-correction[k]);
                    inverse = TruncatedInPowersOfTheQuadratic(Multiplied(inverse, correction), s, t, multiplicity);
                }
                var digits = DigitsInPowersOfTheQuadratic(Multiplied(top, inverse), s, t, multiplicity);
                for (var i = 0; i < multiplicity; i++)
                {
                    var (p, q) = digits[i];
                    if (p.Nodes.Any(node => node == MathS.NaN) || q.Nodes.Any(node => node == MathS.NaN))
                        return false;
                    Entity over = q == Integer.Zero ? p : p == Integer.Zero ? q * x : p + q * x;
                    if (over == Integer.Zero)
                        continue;
                    // The digits are of `N/others` in powers of the monic quadratic `Q/leading`,
                    // and `1/(Q/leading)^(j-i)` is `leading^(j-i)/Q^(j-i)`, so over `Q` itself the
                    // `i`th digit stands over `leading^i Q^(j-i)`.
                    // Written with the coefficients' common denominator in front: left inside,
                    // `(P/D + Q x/D)/(1 + x^2)` was combined into one quotient over `D (1 + x^2)`,
                    // a quadratic with symbols in every coefficient, and integrated as a
                    // piecewise on the sign of its discriminant.
                    var (pAbove, pBelow) = SingleQuotient.Of(p);
                    var (qAbove, qBelow) = SingleQuotient.Of(q);
                    var (above, below) = pBelow == qBelow
                        ? (pAbove + qAbove * x, pBelow)
                        : (pAbove * qBelow + qAbove * pBelow * x, pBelow * qBelow);
                    var remaining = multiplicity - i;
                    Entity under = remaining == 1 ? quadraticFactor : MathS.Pow(quadraticFactor, remaining);
                    if (i > 0 && leading != Integer.One)
                        below *= i == 1 ? leading : MathS.Pow(leading, i);
                    sum += below == Integer.One ? above / under : MathS.Pow(below, Integer.MinusOne) * (above / under);
                }
            }

            if (!HoldsAtSampledPoints(numerator / denominator, sum / constant, x))
                return false;
            decomposition = sum / constant;
            return true;
        }

        /// <summary>
        /// The first <paramref name="count"/> Taylor coefficients of the polynomial
        /// <paramref name="polynomial"/> in <paramref name="x"/> at <paramref name="root"/>,
        /// each in lowest terms over the symbols; null where it does not read as one.
        /// </summary>
        internal static Entity[]? TaylorCoefficientsAtTheRoot(Entity polynomial, Entity root, int count, Variable x)
        {
            var offset = Variable.CreateUnique(polynomial + root, "t");
            var shifted = WithWholePowersOfPowersMultiplied(Bare(polynomial.Substitute(x, root + offset).Expand().InnerSimplified));
            if (GatheredByPower(shifted, offset) is not { } read)
                return null;
            var coefficients = new Entity[count];
            for (var order = 0; order < count; order++)
            {
                var coefficient = read.TryGetValue(EInteger.FromInt32(order), out var at) ? InLowestTermsOverTheSymbols(at) : Integer.Zero;
                if (coefficient.ContainsNode(x) || coefficient.ContainsNode(offset))
                    return null;
                coefficients[order] = coefficient;
            }
            return coefficients;
        }

        /// <summary>
        /// The polynomial with <paramref name="coefficients"/>, lowest power first, reduced modulo
        /// the monic quadratic <c>x^2 + s x + t</c>, as the pair <c>(p, q)</c> of <c>p + q x</c>,
        /// each in lowest terms over the symbols.
        /// </summary>
        /// <remarks>
        /// From the coefficients, which the caller has, rather than from the polynomial written out
        /// and read back: a coefficient with a symbol over its square, `(4 b^2 - 2 a^2)/a^2` in the
        /// square of `x^2 - 2 b/a x - 1`, expands to an `a^0` guarded by a condition, which the
        /// reader does not read, and the split declined every quadratic block beside that square:
        /// `2 (1 - t^2)^5/((1 + t^2)^4 (a - a t^2 + 2 b t)^2)` went to the Hermite reduction and did
        /// not return in 20 s.
        /// </remarks>
        private static (Entity, Entity) ResidueModuloTheQuadratic(Entity[] coefficients, Entity s, Entity t)
        {
            // Horner's scheme, with `x^2` written as `-s x - t` at each step.
            (Entity, Entity) residue = (Integer.Zero, Integer.Zero);
            for (var power = coefficients.Length - 1; power >= 0; power--)
            {
                residue = ProductModuloTheQuadratic(residue, (Integer.Zero, Integer.One), s, t);
                if (coefficients[power] != Integer.Zero)
                    residue = (InLowestTermsOverTheSymbols(residue.Item1 + coefficients[power]), residue.Item2);
            }
            return residue;
        }

        /// <summary>
        /// The coefficients of <paramref name="expr"/> by power of <paramref name="x"/>, read
        /// one term at a time: the reader expands the whole sum first, and a term with a
        /// symbol over itself -- `2 (-b) c / b t^2`, as a shift to a root leaves it -- expands
        /// to a `b^0` guarded by a condition, which the reader then does not read at all.
        /// Null where a term does not read.
        /// </summary>
        private static Dictionary<EInteger, Entity>? GatheredByPower(Entity expr, Variable x)
        {
            var gathered = new Dictionary<EInteger, Entity>();
            foreach (var term in Sumf.LinearChildren(expr))
            {
                if (!TreeAnalyzer.TryGetPolynomial(term, x, out var read))
                    return null;
                foreach (var pair in read)
                    gathered[pair.Key] = gathered.TryGetValue(pair.Key, out var already) ? already + pair.Value : pair.Value;
            }
            return gathered;
        }

        /// <summary>
        /// <paramref name="expr"/> with every whole power of a whole power written as the one
        /// power it is: <c>(t^2)^2</c> is <c>t^4</c> exactly, and is what <c>Expand</c> leaves
        /// of <c>(K + t^2)^2</c>, in a shape the polynomial reader does not read.
        /// </summary>
        private static Entity WithWholePowersOfPowersMultiplied(Entity expr)
            => expr.Replace(node =>
                node is Powf(Powf(var @base, Integer inner), Integer outer) && outer.EInteger.Sign > 0
                    ? MathS.Pow(@base, Integer.Create(inner.EInteger.Multiply(outer.EInteger)))
                    : node);

        /// <summary>
        /// The coefficients of <paramref name="polynomial"/> in <paramref name="x"/> by
        /// power, each in lowest terms over the symbols; null where it does not read as one.
        /// </summary>
        private static Entity[]? Coefficients(Entity polynomial, Variable x)
        {
            polynomial = WithWholePowersOfPowersMultiplied(Bare(polynomial.Expand().InnerSimplified));
            if (GatheredByPower(polynomial, x) is not { } read || read.Count == 0)
                return null;
            var degree = read.Keys.Max()!;
            if (!degree.CanFitInInt32() || read.Keys.Any(power => power.Sign < 0))
                return null;
            var coefficients = new Entity[degree.ToInt32Unchecked() + 1];
            for (var k = 0; k < coefficients.Length; k++)
            {
                coefficients[k] = read.TryGetValue(EInteger.FromInt32(k), out var at) ? InLowestTermsOverTheSymbols(at) : Integer.Zero;
                if (coefficients[k].ContainsNode(x))
                    return null;
            }
            return coefficients;
        }

        /// <summary>The product of two polynomials given by their coefficients, each coefficient in lowest terms.</summary>
        private static Entity[] Multiplied(Entity[] left, Entity[] right)
        {
            var product = new Entity[left.Length + right.Length - 1];
            for (var k = 0; k < product.Length; k++)
                product[k] = Integer.Zero;
            for (var i = 0; i < left.Length; i++)
            {
                if (left[i] == Integer.Zero)
                    continue;
                for (var j = 0; j < right.Length; j++)
                {
                    if (right[j] == Integer.Zero)
                        continue;
                    product[i + j] = product[i + j] == Integer.Zero ? left[i] * right[j] : product[i + j] + left[i] * right[j];
                }
            }
            for (var k = 0; k < product.Length; k++)
                product[k] = product[k] == Integer.Zero ? product[k] : InLowestTermsOverTheSymbols(product[k]);
            return product;
        }

        /// <summary>
        /// <paramref name="polynomial"/> divided by the monic quadratic <c>x^2 + s x + t</c>:
        /// the quotient's coefficients and the remainder <c>r0 + r1 x</c>.
        /// </summary>
        private static (Entity[] Quotient, Entity R0, Entity R1) DividedByTheMonicQuadratic(Entity[] polynomial, Entity s, Entity t)
        {
            var remainder = (Entity[])polynomial.Clone();
            var quotient = new Entity[System.Math.Max(0, polynomial.Length - 2)];
            for (var k = polynomial.Length - 1; k >= 2; k--)
            {
                var coefficient = remainder[k];
                quotient[k - 2] = coefficient;
                if (coefficient == Integer.Zero)
                    continue;
                remainder[k - 1] = InLowestTermsOverTheSymbols(remainder[k - 1] - coefficient * s);
                remainder[k - 2] = InLowestTermsOverTheSymbols(remainder[k - 2] - coefficient * t);
            }
            return (quotient, remainder.Length > 0 ? remainder[0] : Integer.Zero, remainder.Length > 1 ? remainder[1] : Integer.Zero);
        }

        /// <summary>
        /// The first <paramref name="count"/> digits of <paramref name="polynomial"/> written
        /// in powers of the monic quadratic <c>x^2 + s x + t</c>, each a residue <c>(p, q)</c>
        /// for <c>p + q x</c>.
        /// </summary>
        private static (Entity, Entity)[] DigitsInPowersOfTheQuadratic(Entity[] polynomial, Entity s, Entity t, int count)
        {
            var digits = new (Entity, Entity)[count];
            var current = polynomial;
            for (var i = 0; i < count; i++)
            {
                if (current.Length == 0)
                {
                    digits[i] = (Integer.Zero, Integer.Zero);
                    continue;
                }
                var (quotient, r0, r1) = DividedByTheMonicQuadratic(current, s, t);
                digits[i] = (r0, r1);
                current = quotient;
            }
            return digits;
        }

        /// <summary>
        /// <paramref name="polynomial"/> modulo the <paramref name="count"/>th power of the
        /// monic quadratic <c>x^2 + s x + t</c>, as the polynomial its first digits make.
        /// </summary>
        private static Entity[] TruncatedInPowersOfTheQuadratic(Entity[] polynomial, Entity s, Entity t, int count)
        {
            var digits = DigitsInPowersOfTheQuadratic(polynomial, s, t, count);
            Entity[] quadratic = { t, s, Integer.One };
            Entity[] power = { Integer.One };
            Entity[] total = { Integer.Zero };
            for (var i = 0; i < count; i++)
            {
                Entity[] digit = { digits[i].Item1, digits[i].Item2 };
                total = Added(total, Multiplied(digit, power));
                power = Multiplied(power, quadratic);
            }
            return total;
        }

        /// <summary>The sum of two polynomials given by their coefficients.</summary>
        private static Entity[] Added(Entity[] left, Entity[] right)
        {
            var sum = new Entity[System.Math.Max(left.Length, right.Length)];
            for (var k = 0; k < sum.Length; k++)
            {
                var l = k < left.Length ? left[k] : Integer.Zero;
                var r = k < right.Length ? right[k] : Integer.Zero;
                sum[k] = l == Integer.Zero ? r : r == Integer.Zero ? l : InLowestTermsOverTheSymbols(l + r);
            }
            return sum;
        }

        /// <summary>The product of two residues modulo <c>x^2 + s x + t</c>, in lowest terms.</summary>
        private static (Entity, Entity) ProductModuloTheQuadratic((Entity, Entity) left, (Entity, Entity) right, Entity s, Entity t)
        {
            var (a, b) = left;
            var (c, d) = right;
            // (a + b x)(c + d x) = ac + (ad + bc) x + bd x^2, and x^2 is -s x - t.
            return (InLowestTermsOverTheSymbols(a * c - b * d * t), InLowestTermsOverTheSymbols(a * d + b * c - b * d * s));
        }

        /// <summary>
        /// The inverse of a residue modulo <c>x^2 + s x + t</c>, or null where it has none
        /// generically: its norm <c>a^2 - s a b + t b^2</c> is zero, which is the residue
        /// sharing a root with the quadratic.
        /// </summary>
        private static (Entity, Entity)? InverseModuloTheQuadratic((Entity, Entity) residue, Entity s, Entity t)
        {
            var (a, b) = residue;
            // (a + b x)(c + d x) = 1 is ac - t b d = 1 and a d + b c - s b d = 0.
            var norm = InLowestTermsOverTheSymbols(a * a - s * a * b + t * b * b);
            if (norm == Integer.Zero || norm.Evaled is Complex { IsZero: true } || IsZeroAtPinnedSymbols(norm))
                return null;
            return (InLowestTermsOverTheSymbols((a - s * b) / norm), InLowestTermsOverTheSymbols(-b / norm));
        }

        /// <summary>
        /// Gaussian elimination on a square system whose entries may be symbols. A pivot is a
        /// number that is not zero where one is available in its column, and otherwise an entry
        /// that does not simplify to zero; no such entry declines the system.
        /// </summary>
        private static bool TrySolveSquare(Entity[][] matrix, Entity[] rhs, [NotNullWhen(true)] out Entity[]? values)
        {
            values = null;
            var size = rhs.Length;
            if (size == 0 || matrix.Any(row => row.Length != size))
                return false;
            return TrySolveLinear(matrix, rhs, out values);
        }

        /// <summary>
        /// Gaussian elimination on a system of any shape whose entries may be symbols: a row
        /// that reduces to <c>0 = c</c> with <c>c</c> not decidably zero declines the system,
        /// and an unknown no row pivots on is set to zero. A pivot is a number that is not zero
        /// where a column has one, and otherwise an entry that does not simplify to zero — a
        /// judgement, which is why every caller checks what it builds from the answer.
        /// </summary>
        internal static bool TrySolveLinear(Entity[][] matrix, Entity[] rhs, [NotNullWhen(true)] out Entity[]? values)
            => TrySolveLinear(matrix, rhs, symbolsMayCancel: false, out values);

        /// <summary>
        /// <see cref="TrySolveLinear(Entity[][], Entity[], out Entity[])"/>, and where the
        /// entries carry symbols, with the symbols pinned first: solved exactly at two sets of
        /// rationals, and where the two solutions agree the unknowns are constants and that is
        /// the answer. Where they differ, the symbolic elimination is run on the columns the
        /// pinned solutions needed and no others, with a difference of symbols that simplifies
        /// to zero taken as zero -- <c>2a - 2a</c> is not a number as written, and the plain
        /// elimination declines a system it leaves in.
        /// </summary>
        internal static bool TrySolveLinearWithSymbols(Entity[][] matrix, Entity[] rhs, [NotNullWhen(true)] out Entity[]? values)
        {
            values = null;
            var symbols = matrix.SelectMany(row => row).Concat(rhs).SelectMany(entry => entry.Vars).Distinct().ToList();
            if (symbols.Count == 0)
                return TrySolveLinear(Copy(matrix), (Entity[])rhs.Clone(), out values);
            Entity Pin(Entity entry, int seed)
            {
                for (var i = 0; i < symbols.Count; i++)
                    entry = entry.Substitute(symbols[i], Rational.Create(ERational.Create(EInteger.FromInt32(7 + 4 * i + 3 * seed), EInteger.FromInt32(3 + seed))));
                return Bare(entry);
            }
            Entity[][] Pinned(int seed) => matrix.Select(row => row.Select(entry => Pin(entry, seed)).ToArray()).ToArray();
            Entity[] PinnedRhs(int seed) => rhs.Select(entry => Pin(entry, seed)).ToArray();
            if (!TrySolveLinear(Pinned(0), PinnedRhs(0), out var first) || !TrySolveLinear(Pinned(1), PinnedRhs(1), out var second))
                return false;
            var width = matrix[0].Length;
            if (Enumerable.Range(0, width).All(k => first[k] == second[k]))
            {
                values = first;
                return true;
            }
            var support = Enumerable.Range(0, width).Where(k => first[k] != Integer.Zero || second[k] != Integer.Zero).ToList();
            var reduced = matrix.Select(row => support.Select(k => row[k]).ToArray()).ToArray();
            if (!TrySolveLinear(reduced, (Entity[])rhs.Clone(), symbolsMayCancel: true, out var onSupport))
                return false;
            values = new Entity[width];
            for (var k = 0; k < width; k++)
                values[k] = Integer.Zero;
            for (var i = 0; i < support.Count; i++)
                values[support[i]] = onSupport[i];
            return true;
        }

        private static Entity[][] Copy(Entity[][] matrix) => matrix.Select(row => (Entity[])row.Clone()).ToArray();

        /// <summary>Zero as written or as a number, or, where <paramref name="simplify"/> and it has symbols, once simplified.</summary>
        private static bool IsZero(Entity entry, bool simplify)
            => entry == Integer.Zero || entry.Evaled is Complex { IsZero: true }
            || (simplify && entry.Evaled is not Complex && entry.Simplify().Evaled is Complex { IsZero: true });

        /// <summary>
        /// <see cref="TrySolveLinearUnbounded"/> with its values bounded by
        /// <see cref="LargestDecompositionHandedOn"/> together: the eliminations over the
        /// symbols hand back quotients of determinants, which grow as determinants do, and
        /// past the bound the values are declined here rather than substituted, checked and
        /// simplified downstream -- writing an atom back into nine values of ten million
        /// nodes each is where `acoth(c x) ln(1 - c^2 x^2)` ran out of memory.
        /// </summary>
        private static bool TrySolveLinear(Entity[][] matrix, Entity[] rhs, bool symbolsMayCancel, [NotNullWhen(true)] out Entity[]? values)
        {
            if (!TrySolveLinearUnbounded(matrix, rhs, symbolsMayCancel, out values))
                return false;
            if (values.Sum(value => (long)value.Complexity) <= LargestDecompositionHandedOn)
                return true;
            values = null;
            return false;
        }

        private static bool TrySolveLinearUnbounded(Entity[][] matrix, Entity[] rhs, bool symbolsMayCancel, [NotNullWhen(true)] out Entity[]? values)
        {
            values = null;
            var rows = rhs.Length;
            if (rows == 0 || matrix.Length != rows)
                return false;
            var width = matrix[0].Length;
            if (width == 0 || matrix.Any(row => row.Length != width))
                return false;
            if (TrySolveOverRationals(matrix, rhs, out values, out var everyEntryRational))
                return true;
            if (WithTheAtomsAsSymbols(matrix, rhs, symbolsMayCancel, out values))
                return true;
            if (everyEntryRational)
                return false;
            if (TrySolveOverPolynomials(matrix, rhs, out values))
                return true;

            var pivotColumnOfRow = new int[rows];
            for (var row = 0; row < rows; row++)
                pivotColumnOfRow[row] = -1;
            var rank = 0;
            for (var column = 0; column < width && rank < rows; column++)
            {
                var pivot = -1;
                for (var row = rank; row < rows; row++)
                    if (matrix[row][column].Evaled is Complex { IsExact: true, IsZero: false })
                    {
                        pivot = row;
                        break;
                    }
                if (pivot < 0)
                    for (var row = rank; row < rows; row++)
                        if (!IsZero(matrix[row][column], symbolsMayCancel))
                        {
                            pivot = row;
                            break;
                        }
                if (pivot < 0)
                    continue;
                if (pivot != rank)
                {
                    (matrix[pivot], matrix[rank]) = (matrix[rank], matrix[pivot]);
                    (rhs[pivot], rhs[rank]) = (rhs[rank], rhs[pivot]);
                }
                for (var row = rank + 1; row < rows; row++)
                {
                    if (matrix[row][column] == Integer.Zero)
                        continue;
                    var factor = Bare(matrix[row][column] / matrix[rank][column]);
                    for (var k = column; k < width; k++)
                        matrix[row][k] = Bare(matrix[row][k] - factor * matrix[rank][k]);
                    rhs[row] = Bare(rhs[row] - factor * rhs[rank]);
                }
                pivotColumnOfRow[rank] = column;
                rank++;
            }

            // A row with no pivot says 0 = rhs; the system is consistent only where that is
            // decidably so.
            for (var row = rank; row < rows; row++)
                if (!IsZero(rhs[row], symbolsMayCancel))
                    return false;

            values = new Entity[width];
            for (var column = 0; column < width; column++)
                values[column] = Integer.Zero;
            for (var row = rank - 1; row >= 0; row--)
            {
                var column = pivotColumnOfRow[row];
                var accumulated = rhs[row];
                for (var k = column + 1; k < width; k++)
                    if (matrix[row][k] != Integer.Zero)
                        accumulated -= matrix[row][k] * values[k];
                values[column] = Bare(accumulated / matrix[row][column]);
            }
            return true;
        }

        /// <summary>
        /// The system with every entry written as a polynomial over <c>Q</c> in symbols, each
        /// atom that is not one -- a function of the parameters, <c>ln(F)</c>; a root of one,
        /// <c>sqrt(b)</c>; a constant that is not exact, <c>ln(2)</c> or <c>pi</c> -- standing
        /// as a symbol of its own, solved as such, and the atoms written back. The polynomial
        /// elimination declines an entry that is not a polynomial in symbols, and the one on
        /// entities that follows does not collect terms, so its zero test is numeric: an
        /// inexact number is never a pivot and never decidably zero once a pivot has been
        /// through its row, and a function of a symbol is neither. So the ansatz for
        /// <c>2^(x + c x^3)(1 + 3 c x^2)</c>, whose identity has <c>ln(2)</c> in every
        /// coefficient, and Rubi's <c>F^(a + b x + c x^3)(b + 3 c x^2)</c> with <c>ln(F)</c>
        /// in every one, were declined where <c>e^(k (x + c x^3))(1 + 3 c x^2)</c> is answered.
        /// The innermost atoms, so that <c>3 ln(2)</c> is three times the symbol for <c>ln(2)</c>,
        /// and an atom holding another is written in that one's symbol; false where there is
        /// no atom, and the callers go on to the elimination on entities.
        /// </summary>
        private static bool WithTheAtomsAsSymbols(Entity[][] matrix, Entity[] rhs, bool symbolsMayCancel, [NotNullWhen(true)] out Entity[]? values)
        {
            values = null;
            var atoms = new List<(Entity Atom, Variable Symbol)>();
            Entity everything = Integer.Zero;
            foreach (var row in matrix)
                foreach (var entry in row)
                    everything += entry;
            foreach (var entry in rhs)
                everything += entry;
            Entity AsSymbols(Entity entry) => entry.Replace(node =>
            {
                if (node is Number or Variable or Sumf or Minusf or Mulf or Divf or Powf(_, Integer))
                    return node;
                foreach (var (atom, symbol) in atoms)
                    if (atom == node)
                        return symbol;
                var fresh = Variable.CreateUnique(everything, "k_atom");
                atoms.Add((node, fresh));
                everything += fresh;
                return fresh;
            });
            var written = matrix.Select(row => row.Select(AsSymbols).ToArray()).ToArray();
            var writtenRhs = rhs.Select(AsSymbols).ToArray();
            if (atoms.Count == 0)
                return false;
            if (!TrySolveLinear(written, writtenRhs, symbolsMayCancel, out var inSymbols))
                return false;
            values = inSymbols.Select(value =>
            {
                // Outermost first: an atom written in another's symbol has that symbol
                // written back after it.
                for (var i = atoms.Count - 1; i >= 0; i--)
                    value = value.Substitute(atoms[i].Symbol, atoms[i].Atom);
                return value;
            }).ToArray();
            return true;
        }

        /// <summary>
        /// The width past which a rational system is solved by p-adic lifting: the reduced
        /// fractions of Gauss-Jordan grow with every row they meet, and a system of sixty-three
        /// unknowns over a hundred and thirty-eight rows -- the Hermite reduction of Welz's
        /// <c>1/((3 - 2x)^(21/2) (1 + x + 2x^2)^10)</c> -- took two and a half minutes there,
        /// and fraction-free elimination longer.
        /// </summary>
        private const int LiftFrom = 24;

        /// <summary>The prime the lifting works modulo, below <c>2^31</c> so that a product of two residues fits a <see cref="long"/>.</summary>
        private const long LiftingPrime = 2147483647;

        /// <summary>
        /// Dixon's p-adic lifting on the system with every row scaled to integers: the rank
        /// and a square nonsingular subsystem are found modulo a prime, its solution is lifted
        /// digit by digit in that prime, each digit one solve modulo the prime, and the
        /// rationals are reconstructed from the digits (Wang) and checked against every row
        /// exactly. An unknown outside the pivot columns is zero, as in the eliminations.
        /// </summary>
        private static bool TrySolveByLifting(ERational[][] augmented, int rows, int width, [NotNullWhen(true)] out Entity[]? values)
        {
            values = null;
            var a = new EInteger[rows][];
            for (var row = 0; row < rows; row++)
            {
                var common = EInteger.One;
                for (var column = 0; column <= width; column++)
                {
                    var reduced = augmented[row][column].ToLowestTerms();
                    augmented[row][column] = reduced;
                    common = common.Multiply(reduced.Denominator).Divide(common.Gcd(reduced.Denominator));
                }
                a[row] = new EInteger[width + 1];
                for (var column = 0; column <= width; column++)
                    a[row][column] = augmented[row][column].Multiply(ERational.FromEInteger(common)).ToLowestTerms().Numerator;
            }
            var prime = EInteger.FromInt64(LiftingPrime);
            static long Mod(EInteger value, EInteger prime)
            {
                var remainder = value.Remainder(prime);
                if (remainder.Sign < 0)
                    remainder = remainder.Add(prime);
                return remainder.ToInt64Checked();
            }
            static long Inverse(long value)
            {
                // Extended Euclid modulo the prime.
                long t = 0, newT = 1, r = LiftingPrime, newR = value % LiftingPrime;
                if (newR < 0) newR += LiftingPrime;
                while (newR != 0)
                {
                    var quotient = r / newR;
                    (t, newT) = (newT, t - quotient * newT);
                    (r, newR) = (newR, r - quotient * newR);
                }
                return t < 0 ? t + LiftingPrime : t;
            }

            // The rank and the pivot rows and columns, modulo the prime.
            var modular = new long[rows][];
            for (var row = 0; row < rows; row++)
            {
                modular[row] = new long[width + 1];
                for (var column = 0; column <= width; column++)
                    modular[row][column] = Mod(a[row][column], prime);
            }
            var rowOrder = Enumerable.Range(0, rows).ToArray();
            var pivotColumns = new List<int>();
            var rank = 0;
            for (var column = 0; column < width && rank < rows; column++)
            {
                var pivot = -1;
                for (var row = rank; row < rows; row++)
                    if (modular[row][column] != 0)
                    {
                        pivot = row;
                        break;
                    }
                if (pivot < 0)
                    continue;
                (modular[pivot], modular[rank]) = (modular[rank], modular[pivot]);
                (rowOrder[pivot], rowOrder[rank]) = (rowOrder[rank], rowOrder[pivot]);
                var inverse = Inverse(modular[rank][column]);
                for (var k = 0; k <= width; k++)
                    modular[rank][k] = modular[rank][k] * inverse % LiftingPrime;
                for (var row = 0; row < rows; row++)
                {
                    if (row == rank || modular[row][column] == 0)
                        continue;
                    var factor = modular[row][column];
                    for (var k = 0; k <= width; k++)
                        modular[row][k] = ((modular[row][k] - factor * modular[rank][k]) % LiftingPrime + LiftingPrime) % LiftingPrime;
                }
                pivotColumns.Add(column);
                rank++;
            }
            for (var row = rank; row < rows; row++)
                if (modular[row][width] != 0)
                    return false;
            if (rank == 0)
            {
                for (var row = 0; row < rows; row++)
                    if (!a[row][width].IsZero)
                        return false;
                values = new Entity[width];
                for (var column = 0; column < width; column++)
                    values[column] = Integer.Zero;
                return true;
            }

            // The square subsystem S x = t on the pivot rows and columns, and S^(-1) modulo
            // the prime by Gauss-Jordan on [S | I].
            var pivotRows = rowOrder.Take(rank).ToArray();
            var s = new EInteger[rank][];
            var t = new EInteger[rank];
            for (var i = 0; i < rank; i++)
            {
                s[i] = new EInteger[rank];
                for (var j = 0; j < rank; j++)
                    s[i][j] = a[pivotRows[i]][pivotColumns[j]];
                t[i] = a[pivotRows[i]][width];
            }
            var inverseModular = new long[rank][];
            {
                var work = new long[rank][];
                for (var i = 0; i < rank; i++)
                {
                    work[i] = new long[2 * rank];
                    for (var j = 0; j < rank; j++)
                        work[i][j] = Mod(s[i][j], prime);
                    work[i][rank + i] = 1;
                }
                for (var column = 0; column < rank; column++)
                {
                    var pivot = -1;
                    for (var row = column; row < rank; row++)
                        if (work[row][column] != 0)
                        {
                            pivot = row;
                            break;
                        }
                    if (pivot < 0)
                        return false;
                    (work[pivot], work[column]) = (work[column], work[pivot]);
                    var inverse = Inverse(work[column][column]);
                    for (var k = 0; k < 2 * rank; k++)
                        work[column][k] = work[column][k] * inverse % LiftingPrime;
                    for (var row = 0; row < rank; row++)
                    {
                        if (row == column || work[row][column] == 0)
                            continue;
                        var factor = work[row][column];
                        for (var k = 0; k < 2 * rank; k++)
                            work[row][k] = ((work[row][k] - factor * work[column][k]) % LiftingPrime + LiftingPrime) % LiftingPrime;
                    }
                }
                for (var i = 0; i < rank; i++)
                {
                    inverseModular[i] = new long[rank];
                    for (var j = 0; j < rank; j++)
                        inverseModular[i][j] = work[i][rank + j];
                }
            }

            // The lifting: residual r_0 = t; digit x_i = S^(-1) r_i mod p; r_(i+1) = (r_i - S x_i)/p.
            var residual = (EInteger[])t.Clone();
            var lifted = new EInteger[rank];
            for (var i = 0; i < rank; i++)
                lifted[i] = EInteger.Zero;
            var primePower = EInteger.One;
            var solution = new ERational[rank];
            for (var step = 0; step < 4096; step++)
            {
                var residualModular = new long[rank];
                for (var i = 0; i < rank; i++)
                    residualModular[i] = Mod(residual[i], prime);
                var digit = new long[rank];
                for (var i = 0; i < rank; i++)
                {
                    long sum = 0;
                    for (var j = 0; j < rank; j++)
                        sum = (sum + inverseModular[i][j] * residualModular[j]) % LiftingPrime;
                    digit[i] = sum;
                }
                for (var i = 0; i < rank; i++)
                    lifted[i] = lifted[i].Add(primePower.Multiply(EInteger.FromInt64(digit[i])));
                primePower = primePower.Multiply(prime);
                var next = new EInteger[rank];
                for (var i = 0; i < rank; i++)
                {
                    var accumulated = residual[i];
                    for (var j = 0; j < rank; j++)
                        if (digit[j] != 0)
                            accumulated = accumulated.Subtract(s[i][j].Multiply(EInteger.FromInt64(digit[j])));
                    next[i] = accumulated.Divide(prime);
                }
                residual = next;
                // Every few digits, a reconstruction and an exact check.
                if (step % 8 != 7)
                    continue;
                var reconstructed = true;
                for (var i = 0; i < rank && reconstructed; i++)
                    reconstructed = TryReconstructRational(lifted[i], primePower, out solution[i]);
                if (!reconstructed)
                    continue;
                var holds = true;
                for (var i = 0; i < rank && holds; i++)
                {
                    var sum = ERational.Zero;
                    for (var j = 0; j < rank; j++)
                        sum = sum.Add(solution[j].Multiply(ERational.FromEInteger(s[i][j])));
                    holds = sum.Subtract(ERational.FromEInteger(t[i])).ToLowestTerms().IsZero;
                }
                if (holds)
                    break;
                if (step >= 4088)
                    return false;
            }
            if (solution.Any(value => value is null))
                return false;
            // The other rows, exactly.
            for (var row = 0; row < rows; row++)
            {
                var sum = ERational.Zero;
                for (var j = 0; j < rank; j++)
                    sum = sum.Add(solution[j].Multiply(ERational.FromEInteger(a[row][pivotColumns[j]])));
                if (!sum.Subtract(ERational.FromEInteger(a[row][width])).ToLowestTerms().IsZero)
                    return false;
            }
            values = new Entity[width];
            for (var column = 0; column < width; column++)
                values[column] = Integer.Zero;
            for (var j = 0; j < rank; j++)
                values[pivotColumns[j]] = Rational.Create(solution[j].ToLowestTerms());
            return true;
        }

        /// <summary>
        /// The rational <c>n/d</c> with <c>|n|, d &lt;= sqrt(m/2)</c> and <c>n = d u</c> modulo
        /// <paramref name="m"/>, by the extended Euclidean algorithm on <c>m</c> and <c>u</c>
        /// stopped at the bound (Wang's reconstruction); false where there is none yet.
        /// </summary>
        private static bool TryReconstructRational(EInteger u, EInteger m, out ERational value)
        {
            value = ERational.Zero;
            var bound = m.Divide(EInteger.FromInt32(2)).Sqrt();
            var (r0, r1) = (m, u.Remainder(m));
            if (r1.Sign < 0)
                r1 = r1.Add(m);
            var (t0, t1) = (EInteger.Zero, EInteger.One);
            while (r1.CompareTo(bound) > 0)
            {
                var quotient = r0.Divide(r1);
                (r0, r1) = (r1, r0.Subtract(quotient.Multiply(r1)));
                (t0, t1) = (t1, t0.Subtract(quotient.Multiply(t1)));
            }
            if (t1.IsZero || t1.Abs().CompareTo(bound) > 0)
                return false;
            value = ERational.Create(t1.Sign < 0 ? r1.Negate() : r1, t1.Abs()).ToLowestTerms();
            return true;
        }

        /// <summary>
        /// Gauss-Jordan elimination over the rationals, for a system whose every entry is one:
        /// a row reducing to <c>0 = c</c> with <c>c</c> not zero declines the system, and an
        /// unknown no row pivots on is set to zero. Exact, and free of the entity arithmetic
        /// the elimination below runs on -- a system of fifteen rows of the series
        /// coefficients of a square root took a minute there and takes a moment here. Where
        /// every entry is rational the verdict is final, and <paramref name="everyEntryRational"/>
        /// says so; where one is not, nothing is decided.
        /// </summary>
        private static bool TrySolveOverRationals(Entity[][] matrix, Entity[] rhs, [NotNullWhen(true)] out Entity[]? values, out bool everyEntryRational)
        {
            values = null;
            everyEntryRational = false;
            var rows = rhs.Length;
            var width = matrix[0].Length;
            var augmented = new ERational[rows][];
            for (var row = 0; row < rows; row++)
            {
                augmented[row] = new ERational[width + 1];
                for (var column = 0; column <= width; column++)
                {
                    var entry = column < width ? matrix[row][column] : rhs[row];
                    if (entry.Evaled is not Rational rational)
                        return false;
                    augmented[row][column] = rational.ERational;
                }
            }
            everyEntryRational = true;
            if (width > LiftFrom)
                return TrySolveByLifting(augmented, rows, width, out values);
            var pivotColumnOfRow = new int[rows];
            var rank = 0;
            for (var column = 0; column < width && rank < rows; column++)
            {
                var pivot = -1;
                for (var row = rank; row < rows; row++)
                    if (!augmented[row][column].IsZero)
                    {
                        pivot = row;
                        break;
                    }
                if (pivot < 0)
                    continue;
                if (pivot != rank)
                    (augmented[pivot], augmented[rank]) = (augmented[rank], augmented[pivot]);
                // Reduced after every operation: the arithmetic does not, and a fraction
                // that is not grows with every row it is used against.
                var pivotValue = augmented[rank][column];
                for (var k = 0; k <= width; k++)
                    augmented[rank][k] = augmented[rank][k].Divide(pivotValue).ToLowestTerms();
                for (var row = 0; row < rows; row++)
                {
                    if (row == rank)
                        continue;
                    var factor = augmented[row][column];
                    if (factor.IsZero)
                        continue;
                    for (var k = 0; k <= width; k++)
                        augmented[row][k] = augmented[row][k].Subtract(factor.Multiply(augmented[rank][k])).ToLowestTerms();
                }
                pivotColumnOfRow[rank] = column;
                rank++;
            }
            for (var row = rank; row < rows; row++)
                if (!augmented[row][width].IsZero)
                    return false;
            values = new Entity[width];
            for (var column = 0; column < width; column++)
                values[column] = Integer.Zero;
            for (var row = 0; row < rank; row++)
                values[pivotColumnOfRow[row]] = Rational.Create(augmented[row][width].ToLowestTerms());
            return true;
        }

        /// <summary>
        /// The system whose entries are polynomials over <c>Q</c> in some symbols, solved
        /// exactly as such: fraction-free Gauss-Jordan elimination, so that every entry on
        /// the way is a minor of the augmented matrix and every zero is a zero, and each
        /// unknown comes out as one polynomial over the last pivot, in lowest terms.
        /// Declined, for the elimination on entities, where an entry is not such a
        /// polynomial -- a number outside <c>Q</c>, a symbol under a root or below the bar
        /// -- or where the system has no symbols at all and the entities are exact already.
        /// </summary>
        /// <remarks>
        /// The elimination on entities does not collect terms, so with symbols in the entries
        /// its zero test is numeric and its answers are what the arithmetic wrote:
        /// <c>1/((x + 1) sqrt(x^2 + x + b))</c> through the Euler substitution came back
        /// correct in 24 KB, with partial-fraction coefficients like <c>(2b - 2b)</c> beside
        /// terms that were zero, and <c>1/((x + a) sqrt(x^2 + b x + c))</c> in 77 KB. Bareiss'
        /// elimination keeps every intermediate a polynomial, divided exactly by the previous
        /// pivot, and the Gauss-Jordan form of it (Nakos, Turner and Williams, 1997) reduces
        /// above the pivot too, so at the end each pivot row reads <c>d x_k = n_k</c> with
        /// <c>d</c> the last pivot -- one fraction per unknown, no back-substitution to
        /// compound them.
        /// </remarks>
        private static bool TrySolveOverPolynomials(Entity[][] matrix, Entity[] rhs, [NotNullWhen(true)] out Entity[]? values)
        {
            values = null;
            var symbols = matrix.SelectMany(row => row).Concat(rhs).SelectMany(entry => entry.Vars).Distinct()
                .OrderBy(variable => variable.Name, System.StringComparer.Ordinal).ToList();
            if (symbols.Count == 0 || symbols.Count > MultivariatePolynomial.MaxVariables)
                return false;
            var indices = new Dictionary<Variable, int>(symbols.Count);
            for (var i = 0; i < symbols.Count; i++)
                indices[symbols[i]] = i;
            var rows = rhs.Length;
            var width = matrix[0].Length;
            // The augmented matrix, the right-hand side its last column.
            var augmented = new MultivariatePolynomial[rows][];
            for (var row = 0; row < rows; row++)
            {
                augmented[row] = new MultivariatePolynomial[width + 1];
                for (var column = 0; column <= width; column++)
                {
                    var entry = column < width ? matrix[row][column] : rhs[row];
                    if (MultivariatePolynomial.TryParse(entry, indices) is not { } parsed)
                        return false;
                    augmented[row][column] = parsed;
                }
            }

            var pivotColumnOfRow = new int[rows];
            var previous = MultivariatePolynomial.One(symbols.Count);
            var rank = 0;
            for (var column = 0; column < width && rank < rows; column++)
            {
                var pivot = -1;
                for (var row = rank; row < rows; row++)
                    if (!augmented[row][column].IsZero)
                    {
                        pivot = row;
                        break;
                    }
                if (pivot < 0)
                    continue;
                if (pivot != rank)
                    (augmented[pivot], augmented[rank]) = (augmented[rank], augmented[pivot]);
                var pivotValue = augmented[rank][column];
                for (var row = 0; row < rows; row++)
                {
                    if (row == rank)
                        continue;
                    // A row is a product and an exact division per column, of polynomials that grow with
                    // the elimination: the step a budget has to be able to stop.
                    Core.Multithreading.MultithreadingFunctional.ExitIfCancelled();
                    var factor = augmented[row][column];
                    for (var k = 0; k <= width; k++)
                    {
                        // (pivot * entry - factor * pivotRowEntry) / previous, exactly.
                        var scaled = pivotValue.Multiply(augmented[row][k], MultivariatePolynomial.MaxIntermediateTerms);
                        var crossed = factor.IsZero ? MultivariatePolynomial.Zero(symbols.Count) : factor.Multiply(augmented[rank][k], MultivariatePolynomial.MaxIntermediateTerms);
                        if (scaled is null || crossed is null)
                            return false;
                        var divided = scaled.Subtract(crossed).DivideExact(previous, MultivariatePolynomial.MaxIntermediateTerms);
                        if (divided is null)
                            return false;
                        augmented[row][k] = divided;
                    }
                }
                pivotColumnOfRow[rank] = column;
                previous = pivotValue;
                rank++;
            }

            // A row with no pivot says 0 = rhs, and here that is decided exactly.
            for (var row = rank; row < rows; row++)
                if (!augmented[row][width].IsZero)
                    return false;

            values = new Entity[width];
            for (var column = 0; column < width; column++)
                values[column] = Integer.Zero;
            for (var row = 0; row < rank; row++)
            {
                var column = pivotColumnOfRow[row];
                var numerator = augmented[row][width];
                var denominator = augmented[row][column];
                if (numerator.IsZero)
                    continue;
                if (numerator.DivideExact(denominator) is { } exact)
                {
                    values[column] = exact.ToEntity(symbols);
                    continue;
                }
                // Both sides primitive with a positive leading coefficient, the rational
                // they were scaled by in front: `-300/(-600a - 600)` is `1/2 * 1/(a + 1)`.
                var primitiveNumerator = numerator.Normalized();
                var primitiveDenominator = denominator.Normalized();
                if (numerator.DivideExact(primitiveNumerator) is not { IsConstant: true } numeratorScale
                    || denominator.DivideExact(primitiveDenominator) is not { IsConstant: true } denominatorScale)
                    return false;
                var scale = numeratorScale.ToEntity(symbols) / denominatorScale.ToEntity(symbols);
                var top = primitiveNumerator.ToEntity(symbols);
                var bottom = primitiveDenominator.ToEntity(symbols);
                var fraction = PolynomialGcd.TryCancel(top, bottom, out var cancelled) && cancelled is not null
                    ? Bare(cancelled)
                    : top / bottom;
                values[column] = Bare(scale * fraction);
            }
            return true;
        }

        /// <summary>
        /// Simplified, and without the <c>provided pivot != 0</c> a division by a symbol
        /// acquires on the way: that condition is the generic case this decomposition is
        /// answering in, and left on it makes a term nothing downstream reads.
        /// </summary>
        internal static Entity Bare(Entity e)
            => e.InnerSimplified is Providedf(var inner, _) ? inner : e.InnerSimplified;

        /// <summary>
        /// Whether the derivative of <paramref name="antiderivative"/> is
        /// <paramref name="integrand"/>, numerically, at a few points in <paramref name="x"/>
        /// with every other symbol pinned to a fixed value first -- pinned before the
        /// differentiation, since <c>sgn(f)'</c> and <c>|f|'</c> are answered only where
        /// <c>f</c> is shown real-valued, which a symbol that might be complex prevents and a
        /// number does not: an answer with <c>sgn(tanh(c + d x))</c> in it, right, was
        /// unevaluable differentiated with <c>c</c> and <c>d</c> in it and threw out of the
        /// integrator.
        /// </summary>
        internal static bool DerivativeHoldsAtSampledPoints(Entity antiderivative, Entity integrand, Variable x)
        {
            // Off the downcasting for the simplification of the pinned answer, which folds its
            // numbers: downcast, a pinned 1.37 is 137/100 and every constant of the answer is
            // folded exactly -- 28 s for the answer to u^5/((u^2 - 1)^5 (2 a u + b (u^2 + 1))),
            // where in decimals it is 3 ms. The comparison itself reads no setting.
            using var _ = MathS.Settings.DowncastingEnabled.Set(false);
            var (pinnedAntiderivative, pinnedIntegrand) = Pinned(antiderivative, integrand, x);
            return HoldsAtSampledPoints(Bare(pinnedAntiderivative).Differentiate(x), pinnedIntegrand, x);
        }

        /// <summary>
        /// Whether the derivative of <paramref name="antiderivative"/> differs from
        /// <paramref name="integrand"/> at one of the default points where the integrand is
        /// real, as <see cref="DiffersWhereReal"/> tells, with the symbols pinned before the
        /// differentiation, as <see cref="DerivativeHoldsAtSampledPoints"/> pins them.
        /// </summary>
        internal static bool DerivativeDiffersWhereReal(Entity antiderivative, Entity integrand, Variable x)
        {
            using var _ = MathS.Settings.DowncastingEnabled.Set(false);
            var (pinnedAntiderivative, pinnedIntegrand) = Pinned(antiderivative, integrand, x);
            return DiffersWhereReal(Bare(pinnedAntiderivative).Differentiate(x), pinnedIntegrand, x);
        }

        /// <summary>
        /// The two with every symbol but <paramref name="x"/> pinned to a fixed value:
        /// distinct values, off the integers, so that no two factors coincide by accident and
        /// a sign or a root in a coefficient stays generic.
        /// </summary>
        private static (Entity, Entity) Pinned(Entity left, Entity right, Variable x)
        {
            var parameters = left.Vars.Concat(right.Vars).Where(v => v != x).Distinct().ToList();
            var pinned = 0;
            foreach (var parameter in parameters)
            {
                var fraction = (pinned % 3) switch { 0 => "1.37", 1 => "2.71", _ => "0.83" };
                var value = Real.Create(EDecimal.FromString(fraction).Add(EDecimal.FromInt32(pinned)));
                left = left.Substitute(parameter, value);
                right = right.Substitute(parameter, value);
                pinned++;
            }
            return (left, right);
        }

        /// <summary>
        /// Whether <paramref name="left"/> and <paramref name="right"/> agree, numerically, at a
        /// few points in <paramref name="x"/> with every other symbol pinned to a fixed value.
        /// A point where either has no value -- a pole, a cut, a condition that fails, a derivative
        /// left unevaluated -- or where the evaluation cannot tell is skipped, and at least two must
        /// compare. The points may
        /// be the caller's, for an identity that holds on a real domain only --
        /// <c>sqrt(1 - L^2) = sech(artanh(L))</c> inside <c>(-1, 1)</c> and off by a sign outside
        /// it -- where the default set would compare where it does not hold.
        /// </summary>
        /// <remarks>
        /// In intervals, which read no setting (https://github.com/asc-community/AngouriMath/issues/1019,
        /// item 10): doubles first, then forty and eighty digits where cancellation leaves the
        /// doubles too wide to tell.
        /// </remarks>
        internal static bool HoldsAtSampledPoints(Entity left, Entity right, Variable x, string[]? points = null)
        {
            (left, right) = Pinned(left, right, x);
            var compared = 0;
            // The decimal evaluations are made once for all the points, so that the subtrees free
            // of the point, which the substitutions share, are worked out once.
            var inDecimals = new System.Lazy<PreciseEvaluation>[] { new(() => PreciseEvaluation.In(40)), new(() => PreciseEvaluation.In(80)) };
            foreach (var at in points ?? DefaultSamplePoints)
            {
                var point = Real.Create(EDecimal.FromString(at));
                switch (Agree(left.Substitute(x, point), right.Substitute(x, point), 1e-9, inDecimals))
                {
                    case false:
                        return false;
                    case true:
                        compared++;
                        break;
                }
            }
            return compared >= 2;
        }

        /// <summary>
        /// Whether the two numbers agree to <paramref name="relativeTolerance"/> of their size, or
        /// of one where both are smaller: in double intervals, and where those cannot tell, in
        /// the decimal evaluations of <paramref name="inDecimals"/> in turn -- forty and then
        /// eighty digits; null where either has no value or none can tell.
        /// </summary>
        private static bool? Agree(Entity left, Entity right, double relativeTolerance, System.Lazy<PreciseEvaluation>[] inDecimals)
        {
            if (IntervalEvaluation.Of(left) is { } leftInterval && IntervalEvaluation.Of(right) is { } rightInterval
                && IntervalEvaluation.Agree(leftInterval, rightInterval, relativeTolerance) is { } inDoubles)
                return inDoubles;
            foreach (var precise in inDecimals)
                if (precise.Value.ValueOf(left) is { } leftPrecise && precise.Value.ValueOf(right) is { } rightPrecise
                    && PreciseEvaluation.Agree(leftPrecise, rightPrecise, relativeTolerance) is { } decided)
                    return decided;
            return null;
        }

        [ConstantField]
        private static readonly string[] DefaultSamplePoints = { "0.29", "1.43", "3.17", "-0.61" };

        /// <summary>
        /// Whether <paramref name="left"/> differs from <paramref name="right"/> at one of the
        /// points <see cref="HoldsAtSampledPoints"/> takes by default where
        /// <paramref name="right"/> is real, with the symbols pinned as it pins them. A point
        /// where <paramref name="right"/> has no value, or a value off the real line, says
        /// nothing; one where it is real and <paramref name="left"/> has no value is a
        /// difference.
        /// </summary>
        internal static bool DiffersWhereReal(Entity left, Entity right, Variable x)
        {
            using var _ = MathS.Settings.DowncastingEnabled.Set(false);
            (left, right) = Pinned(left, right, x);
            var tolerance = EDecimal.FromString("1e-9");
            foreach (var at in DefaultSamplePoints)
            {
                var point = Real.Create(EDecimal.FromString(at));
                if (right.Substitute(x, point).Evaled is not Complex r || r.IsNaN)
                    continue;
                var scale = EDecimal.Max(EDecimal.One, r.Abs().EDecimal);
                if (r.ImaginaryPart.EDecimal.Abs().CompareTo(scale.Multiply(tolerance)) > 0)
                    continue;
                if (left.Substitute(x, point).Evaled is not Complex l || l.IsNaN)
                    return true;
                if ((l - r).Abs().EDecimal.CompareTo(scale.Multiply(tolerance)) > 0)
                    return true;
            }
            return false;
        }
    }
}
