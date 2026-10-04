//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions
{
    /// <summary>
    /// A canonical form for rational functions over <c>Q</c>: two expressions denoting the
    /// same quotient of polynomials become the identical tree, so that deciding whether they
    /// are equal is a structural comparison rather than a search.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the part of the language where a canonical form is <i>possible</i>. There is
    /// none for the whole of it — zero-equivalence is undecidable once <c>pi</c>, the
    /// exponential, the trigonometric functions and <c>abs</c> are in play (Richardson, 1968)
    /// — so the boundary is in the signature: a refusal means "not a rational function over
    /// <c>Q</c>, and no canonical form is claimed", never a normalisation that resembles one.
    /// <c>Docs/Contributing/CanonicalForm.md</c> is the specification.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/934">#934</a>.
    /// </para>
    /// <para>
    /// Four steps. The expression is written as a single fraction, by the step
    /// <see cref="Entity.AsSingleFraction"/> takes -- without it <c>1/x + 1/y</c> and
    /// <c>(x + y)/(x*y)</c> could never meet. Then the numerator and denominator are divided
    /// by their multivariate greatest common divisor, which
    /// <see cref="PolynomialGcd"/> computes and verifies. Then both are scaled so that the
    /// denominator's leading coefficient is one, which is what makes <c>2x/(4y)</c> and
    /// <c>x/(2y)</c> the same tree. Coefficients are in lowest terms throughout.
    /// </para>
    /// <para>
    /// <b>The domain is preserved rather than assumed away.</b> Cancelling a common factor
    /// widens the domain — <c>x/x</c> is not <c>1</c> — and so does dividing by a quotient,
    /// which moves its denominator into the numerator: <c>1/(1/x)</c> is not <c>x</c>. So the
    /// answer carries the condition that neither vanishes, which is what the library already
    /// does elsewhere and what keeps "equal trees means equal expressions" true rather than
    /// nearly true; the condition is written canonically too, square-free and without what
    /// the denominator already excludes. A sum is defined exactly where its terms are, and the
    /// common denominator vanishes exactly where one of theirs does, so gathering one widens
    /// nothing. <a href="https://github.com/asc-community/AngouriMath/issues/1618">#1618</a>
    /// </para>
    /// </remarks>
    internal static class RationalFunction
    {
        /// <summary>
        /// A quotient larger than this is left alone. On node count, and a refusal rather
        /// than an approximation.
        /// </summary>
        private const int MaxComplexity = 256;

        /// <summary>
        /// Raising a quotient to a power is where a gathered quotient explodes, so a power of
        /// anything but a polynomial is bounded before anything is multiplied out; a polynomial's
        /// power has <see cref="MultivariatePolynomial.Power"/>'s own bound on the number of
        /// terms.
        /// </summary>
        private const int MaxExponent = 32;

        /// <summary>
        /// <paramref name="expr"/> as a canonical quotient of polynomials over <c>Q</c>, or
        /// <see langword="false"/> where it is not a rational function over <c>Q</c> in its
        /// free variables, or where a bound is reached.
        /// </summary>
        internal static bool TryCanonicalize(Entity expr, [NotNullWhen(true)] out Entity? canonical)
        {
            canonical = null;
            if (expr.Complexity > MaxComplexity)
                return false;

            var variables = expr.Vars
                .OrderBy(variable => variable.Name, StringComparer.Ordinal)
                .ToArray();
            if (variables.Length > MultivariatePolynomial.MaxVariables)
                return false;
            var indices = new Dictionary<Variable, int>(variables.Length);
            for (var i = 0; i < variables.Length; i++)
                indices[variables[i]] = i;
            var variableCount = variables.Length;

            if (!TryGather(expr, indices, variableCount, out var numerator, out var denominator, out var excluded))
                return false;
            // A vanishing denominator is not a rational function. A vanishing numerator is zero
            // wherever the expression has a value: everywhere its denominator does not vanish.
            if (denominator.IsZero)
                return false;
            var order = new int[variableCount];
            for (var i = 0; i < order.Length; i++)
                order[i] = i;
            if (numerator.IsZero)
            {
                if (excluded.Multiply(denominator) is not { } wholeDenominator
                    || Conditioned(Integer.Create(0), wholeDenominator, MultivariatePolynomial.One(variableCount), order, variables) is not { } zero)
                    return false;
                canonical = zero;
                return true;
            }

            if (variableCount > 0
                && PolynomialGcd.Gcd(numerator, denominator, order, 0) is { } divisor
                && !divisor.IsConstant)
            {
                if (numerator.DivideExact(divisor) is not { } reducedTop
                    || denominator.DivideExact(divisor) is not { } reducedBottom)
                    return false;
                // Multiplied back independently of the division that produced them, as
                // PolynomialGcd does for the same reason: an incomplete cancellation is a
                // tolerable answer and a wrong one is not.
                if (reducedTop.Multiply(divisor) is not { } checkedTop || !checkedTop.SameAs(numerator)
                    || reducedBottom.Multiply(divisor) is not { } checkedBottom
                    || !checkedBottom.SameAs(denominator))
                    return false;
                if (excluded.Multiply(divisor) is not { } withTheCancelled)
                    return false;
                numerator = reducedTop;
                denominator = reducedBottom;
                excluded = withTheCancelled;
            }

            // Scaled so the denominator leads with one, under the same lexicographic monomial
            // order the Gröbner solver uses. Without this, 2x/(4y) and x/(2y) are different
            // trees for one function: their greatest common divisor is fixed only up to a
            // unit, and nothing obliges the machinery to take the 2 out.
            var leading = denominator.LeadingCoefficient(MonomialOrder.Lexicographic);
            if (leading.IsZero)
                return false;
            if (leading.CompareTo(ERational.One) != 0)
            {
                var inverse = ERational.One.Divide(leading).ToLowestTerms();
                numerator = numerator.ScaleBy(inverse);
                denominator = denominator.ScaleBy(inverse);
            }

            // The denominator is now monic, so a constant one is exactly 1 and is dropped.
            var quotient = denominator.IsConstant
                ? numerator.ToEntity(variables)
                : numerator.ToEntity(variables) / denominator.ToEntity(variables);
            canonical = Conditioned(quotient, excluded, denominator, order, variables);
            return canonical is not null;
        }

        /// <summary>
        /// <paramref name="quotient"/> with the condition that it is not at a zero of
        /// <paramref name="excluded"/>, where the expression it came from had no value, except
        /// where <paramref name="denominator"/> vanishing already says so; <see langword="null"/>
        /// where a step of the arithmetic declined.
        /// </summary>
        /// <remarks>
        /// The condition is part of the form, so it is canonical too: two expressions with the same
        /// value and the same points where they have none have to meet. So the polynomial written
        /// is the square-free part of <paramref name="excluded"/>, which vanishes exactly where it
        /// does -- <c>x^2/x^2</c> and <c>x/x</c> are both <c>1 provided not x = 0</c> -- without the
        /// factors it shares with the denominator, and with whole coprime coefficients and a
        /// positive leading one. The square-free part is <c>E / gcd(E, dE/dx_1, …, dE/dx_n)</c>:
        /// over the rationals a factor to the power <c>k</c> divides each derivative to the power
        /// <c>k - 1</c>, and one of them no further.
        /// </remarks>
        private static Entity? Conditioned(Entity quotient, MultivariatePolynomial excluded, MultivariatePolynomial denominator,
            IReadOnlyList<int> order, IReadOnlyList<Variable> variables)
        {
            if (excluded.IsConstant)
                return excluded.IsZero ? null : quotient;
            var repeated = excluded;
            for (var i = 0; i < excluded.VariableCount; i++)
                if (excluded.DegreeIn(i) > 0)
                {
                    if (PolynomialGcd.Gcd(repeated, excluded.DerivativeIn(i), order, 0) is not { } common)
                        return null;
                    repeated = common;
                }
            if (excluded.DivideExact(repeated) is not { } squareFree
                || PolynomialGcd.Gcd(squareFree, denominator, order, 0) is not { } shared
                || squareFree.DivideExact(shared) is not { } left)
                return null;
            return left.IsConstant
                ? quotient
                : new Providedf(quotient, !left.Normalized().ToEntity(variables).EqualTo(0));
        }

        /// <summary>
        /// <paramref name="expr"/> as a single quotient of polynomials, and the polynomial whose
        /// zeros are the other points where <paramref name="expr"/> has no value: the product of
        /// the denominators that dividing by a quotient turned over. The denominator is never
        /// zero and never simplified away; it is <c>1</c> for a polynomial.
        /// </summary>
        /// <remarks>
        /// Gathered by <see cref="SingleQuotient.OverLeastCommonDenominator"/>, the step
        /// <see cref="Entity.AsSingleFraction"/> takes, so that the two agree on what a single
        /// quotient of an expression is and on where it has a value; this form is that quotient
        /// reduced and normalised.
        /// </remarks>
        private static bool TryGather(
            Entity expr, IReadOnlyDictionary<Variable, int> indices, int variableCount,
            [NotNullWhen(true)] out MultivariatePolynomial? numerator,
            [NotNullWhen(true)] out MultivariatePolynomial? denominator,
            [NotNullWhen(true)] out MultivariatePolynomial? excluded)
        {
            numerator = denominator = excluded = null;
            foreach (var node in expr.Nodes)
                if (node is Powf(var @base, Integer power)
                    && power.EInteger.Abs().CompareTo(EInteger.FromInt32(MaxExponent)) > 0
                    && MultivariatePolynomial.TryParse(@base, indices) is null)
                    return false;
            var carried = new List<Entity>();
            var (top, bottom) = SingleQuotient.OverLeastCommonDenominator(expr, carried);
            if (MultivariatePolynomial.TryParse(top, indices) is not { } parsedTop
                || MultivariatePolynomial.TryParse(bottom, indices) is not { } parsedBottom)
                return false;
            var product = MultivariatePolynomial.One(variableCount);
            foreach (var turnedOver in carried)
            {
                if (MultivariatePolynomial.TryParse(turnedOver, indices) is not { } parsed
                    || product.Multiply(parsed) is not { } next)
                    return false;
                product = next;
            }
            (numerator, denominator, excluded) = (parsedTop, parsedBottom, product);
            return true;
        }
    }
}
