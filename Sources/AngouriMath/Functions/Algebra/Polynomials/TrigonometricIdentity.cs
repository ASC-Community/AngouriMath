//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions
{
    /// <summary>
    /// Decides whether a rational function of sines, cosines, tangents, cotangents, secants and
    /// cosecants is a constant where it has a value, and which constant. Proving a trigonometric
    /// identity written as a difference is asking whether that constant is zero.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every angle has to be a combination of the variables with rational coefficients and no
    /// constant term. Each variable <c>v</c> then has a base angle <c>u = v / L</c>, where <c>L</c>
    /// is the least common denominator of its coefficients, and every angle is a whole combination
    /// of base angles. A cosine of <c>n u</c> is the Chebyshev polynomial <c>T_n(cos u)</c> and a sine
    /// is <c>sin(u) U_(n-1)(cos u)</c>, and a sum of base angles is opened by the addition theorem.
    /// So the expression is a quotient of two polynomials over <c>Q</c> in its variables and in a
    /// sine and a cosine of each base angle.
    /// </para>
    /// <para>
    /// The only relation among those is <c>sin(u)^2 + cos(u)^2 = 1</c>, since <c>x</c> and
    /// <c>e^(ix)</c> are algebraically independent, and so are different variables. Writing each
    /// <c>sin(u)^2</c> as <c>1 - cos(u)^2</c> leaves every sine to the first power at most, and that
    /// form is unique: the relations' leading monomials are coprime, so they are a Gröbner basis. A
    /// polynomial therefore vanishes identically exactly when its form is zero, and the quotient is
    /// the constant <c>k</c> exactly when the numerator's form is <c>k</c> times the denominator's.
    /// </para>
    /// <para>
    /// Both theorems hold for every complex angle, and the denominator is the product of the ones
    /// the expression divides by, so it vanishes only where the expression has no value. The
    /// constant is the expression's value everywhere else, and a caller states where that is.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2142">#2142</a>
    /// </para>
    /// </remarks>
    internal static class TrigonometricIdentity
    {
        /// <summary>
        /// The largest multiple of a base angle written out, and the largest base denominator. A
        /// cosine of <c>n u</c> is a polynomial of degree <c>n</c>, so this bounds the degrees before
        /// <see cref="MultivariatePolynomial.MaxTerms"/> bounds the products.
        /// </summary>
        private const int MaxMultiple = 16;

        /// <summary>The largest power of a subexpression multiplied out.</summary>
        private const int MaxExponent = 16;

        /// <summary>
        /// The constant <paramref name="expr"/> equals wherever it has a value, or
        /// <see langword="false"/> where it is not a constant, is not a rational function of
        /// trigonometric functions of the kind described, or where a bound is reached.
        /// </summary>
        internal static bool TryConstant(Entity expr, [NotNullWhen(true)] out Entity? constant)
        {
            constant = null;
            var polynomialVariables = new HashSet<Variable>();
            var angles = new List<Entity>();
            if (!Collect(expr, polynomialVariables, angles) || angles.Count == 0)
                return false;

            // Each angle as whole multiples of base angles, and each variable's base denominator.
            var coefficients = new List<List<(Variable Variable, ERational Coefficient)>>(angles.Count);
            var denominators = new Dictionary<Variable, EInteger>();
            foreach (var angle in angles)
            {
                if (LinearCombination(angle) is not { } combination)
                    return false;
                foreach (var (variable, coefficient) in combination)
                {
                    var denominator = coefficient.Denominator;
                    denominators[variable] = denominators.TryGetValue(variable, out var known)
                        ? known.Divide(known.GreatestCommonDivisor(denominator)).Multiply(denominator)
                        : denominator;
                    if (denominators[variable].CompareTo(EInteger.FromInt32(MaxMultiple)) > 0)
                        return false;
                }
                coefficients.Add(combination);
            }

            var baseAngles = denominators.Keys.OrderBy(variable => variable.Name, StringComparer.Ordinal).ToArray();
            var polynomial = polynomialVariables.OrderBy(variable => variable.Name, StringComparer.Ordinal).ToArray();
            var variableCount = polynomial.Length + 2 * baseAngles.Length;
            if (variableCount > MultivariatePolynomial.MaxVariables)
                return false;
            var indices = new Dictionary<Variable, int>(polynomial.Length);
            for (var i = 0; i < polynomial.Length; i++)
                indices[polynomial[i]] = i;
            var pairs = new Dictionary<Variable, (int Sine, int Cosine)>(baseAngles.Length);
            for (var i = 0; i < baseAngles.Length; i++)
                pairs[baseAngles[i]] = (polynomial.Length + 2 * i, polynomial.Length + 2 * i + 1);

            var opened = new Dictionary<Entity, (MultivariatePolynomial Cosine, MultivariatePolynomial Sine)>(angles.Count);
            for (var i = 0; i < angles.Count; i++)
            {
                var cosine = MultivariatePolynomial.One(variableCount);
                var sine = MultivariatePolynomial.Zero(variableCount);
                var total = 0;
                foreach (var (variable, coefficient) in coefficients[i])
                {
                    var multiple = coefficient.Multiply(ERational.FromEInteger(denominators[variable])).ToEInteger();
                    if (!multiple.CanFitInInt32())
                        return false;
                    var n = multiple.ToInt32Checked();
                    total += Math.Abs(n);
                    if (total > MaxMultiple)
                        return false;
                    var (cosineOfIt, sineOfIt) = OfAMultiple(Math.Abs(n), pairs[variable], variableCount);
                    if (cosineOfIt is null || sineOfIt is null)
                        return false;
                    if (n < 0)
                        sineOfIt = sineOfIt.ScaleBy(ERational.FromInt32(-1));
                    // The addition theorem: cos(A + B) = cos A cos B - sin A sin B, and
                    // sin(A + B) = sin A cos B + cos A sin B.
                    if (cosine.Multiply(cosineOfIt) is not { } cc || sine.Multiply(sineOfIt) is not { } ss
                        || sine.Multiply(cosineOfIt) is not { } sc || cosine.Multiply(sineOfIt) is not { } cs)
                        return false;
                    cosine = cc.Subtract(ss);
                    sine = sc.Add(cs);
                }
                opened[angles[i]] = (cosine, sine);
            }

            if (Gathered(expr, indices, opened, variableCount) is not ({ } top, { } bottom))
                return false;
            var cosines = pairs.Values.ToArray();
            if (Reduced(top, cosines) is not { } reducedTop || Reduced(bottom, cosines) is not { } reducedBottom
                || reducedBottom.IsZero)
                return false;
            if (reducedTop.IsZero)
            {
                constant = Integer.Create(0);
                return true;
            }
            var leading = reducedBottom.LeadingMonomial(MonomialOrder.Lexicographic);
            var ratio = reducedTop.CoefficientOf(leading).Divide(reducedBottom.CoefficientOf(leading)).InLowestTerms();
            if (ratio.IsZero || !reducedTop.Subtract(reducedBottom.ScaleBy(ratio)).IsZero)
                return false;
            constant = Rational.Create(ratio);
            return true;
        }

        /// <summary>
        /// Whether <paramref name="node"/> is built from rational numbers, variables, the four
        /// operations, whole powers and the six trigonometric functions, collecting the variables
        /// outside the functions and the functions' arguments.
        /// </summary>
        private static bool Collect(Entity node, HashSet<Variable> polynomialVariables, List<Entity> angles)
        {
            switch (node)
            {
                case Rational:
                    return true;
                case Variable variable:
                    if (variable.IsConstant)
                        return false;
                    polynomialVariables.Add(variable);
                    return true;
                case Sumf(var left, var right):
                    return Collect(left, polynomialVariables, angles) && Collect(right, polynomialVariables, angles);
                case Minusf(var left, var right):
                    return Collect(left, polynomialVariables, angles) && Collect(right, polynomialVariables, angles);
                case Mulf(var left, var right):
                    return Collect(left, polynomialVariables, angles) && Collect(right, polynomialVariables, angles);
                case Divf(var left, var right):
                    return Collect(left, polynomialVariables, angles) && Collect(right, polynomialVariables, angles);
                case Powf(var @base, Integer exponent):
                    return exponent.EInteger.Abs().CompareTo(EInteger.FromInt32(MaxExponent)) <= 0
                        && Collect(@base, polynomialVariables, angles);
                default:
                    if (AngleOf(node) is not { } angle)
                        return false;
                    if (!angles.Contains(angle))
                        angles.Add(angle);
                    return true;
            }
        }

        private static Entity? AngleOf(Entity node) => node switch
        {
            Sinf(var angle) => angle,
            Cosf(var angle) => angle,
            Tanf(var angle) => angle,
            Cotanf(var angle) => angle,
            Secantf(var angle) => angle,
            Cosecantf(var angle) => angle,
            _ => null
        };

        /// <summary>
        /// <paramref name="angle"/> as a sum of its variables times rational coefficients, or
        /// <see langword="null"/> where it is not one, or has a constant term.
        /// </summary>
        private static List<(Variable Variable, ERational Coefficient)>? LinearCombination(Entity angle)
        {
            var variables = angle.VarsAndConsts.ToArray();
            if (variables.Length == 0 || variables.Length > MultivariatePolynomial.MaxVariables || variables.Any(variable => variable.IsConstant))
                return null;
            var indices = new Dictionary<Variable, int>(variables.Length);
            for (var i = 0; i < variables.Length; i++)
                indices[variables[i]] = i;
            if (MultivariatePolynomial.TryParse(angle, indices) is not { IsZero: false } parsed)
                return null;
            var combination = new List<(Variable, ERational)>(variables.Length);
            foreach (var monomial in parsed.Monomials)
            {
                if (MultivariatePolynomial.TotalDegree(monomial) != 1)
                    return null;
                for (var i = 0; i < variables.Length; i++)
                    if (MultivariatePolynomial.PowerOfMonomial(monomial, i) == 1)
                        combination.Add((variables[i], parsed.CoefficientOf(monomial)));
            }
            return combination;
        }

        /// <summary>
        /// <c>cos(n u)</c> and <c>sin(n u)</c> for a whole <c>n &gt;= 0</c>, as polynomials in the sine
        /// and the cosine of <c>u</c>: <c>T_n(cos u)</c> and <c>sin(u) U_(n-1)(cos u)</c>, by
        /// <c>T_(k+1) = 2 c T_k - T_(k-1)</c> and the same recurrence for <c>U</c>.
        /// </summary>
        private static (MultivariatePolynomial? Cosine, MultivariatePolynomial? Sine) OfAMultiple(
            int n, (int Sine, int Cosine) pair, int variableCount)
        {
            var one = MultivariatePolynomial.One(variableCount);
            if (n == 0)
                return (one, MultivariatePolynomial.Zero(variableCount));
            var c = MultivariatePolynomial.Monomial(variableCount, pair.Cosine);
            var twoC = c.ScaleBy(ERational.FromInt32(2));
            var (previousT, t) = (one, c);
            var (previousU, u) = (one, twoC);
            for (var k = 1; k < n; k++)
            {
                if (twoC.Multiply(t) is not { } nextT || twoC.Multiply(u) is not { } nextU)
                    return (null, null);
                (previousT, t) = (t, nextT.Subtract(previousT));
                (previousU, u) = (u, nextU.Subtract(previousU));
            }
            // u is now U_n and previousU is U_(n-1).
            return (t, MultivariatePolynomial.Monomial(variableCount, pair.Sine).Multiply(previousU));
        }

        /// <summary>
        /// <paramref name="expr"/> as a numerator and a denominator, the denominator the product of
        /// the denominators of the quotients it is built from.
        /// </summary>
        private static (MultivariatePolynomial? Top, MultivariatePolynomial? Bottom) Gathered(
            Entity expr, IReadOnlyDictionary<Variable, int> indices,
            IReadOnlyDictionary<Entity, (MultivariatePolynomial Cosine, MultivariatePolynomial Sine)> opened,
            int variableCount)
        {
            var one = MultivariatePolynomial.One(variableCount);
            switch (expr)
            {
                case Rational rational:
                    return (MultivariatePolynomial.Constant(variableCount, rational.ERational), one);
                case Variable variable:
                    return indices.TryGetValue(variable, out var index)
                        ? (MultivariatePolynomial.Monomial(variableCount, index), one)
                        : (null, null);
                case Sumf(var left, var right):
                    return Combined(left, right, subtract: false);
                case Minusf(var left, var right):
                    return Combined(left, right, subtract: true);
                case Mulf(var left, var right):
                {
                    if (Gathered(left, indices, opened, variableCount) is not ({ } leftTop, { } leftBottom)
                        || Gathered(right, indices, opened, variableCount) is not ({ } rightTop, { } rightBottom))
                        return (null, null);
                    return (leftTop.Multiply(rightTop), leftBottom.Multiply(rightBottom));
                }
                case Divf(var left, var right):
                {
                    if (Gathered(left, indices, opened, variableCount) is not ({ } leftTop, { } leftBottom)
                        || Gathered(right, indices, opened, variableCount) is not ({ } rightTop, { } rightBottom))
                        return (null, null);
                    return (leftTop.Multiply(rightBottom), leftBottom.Multiply(rightTop));
                }
                case Powf(var @base, Integer exponent):
                {
                    if (Gathered(@base, indices, opened, variableCount) is not ({ } baseTop, { } baseBottom))
                        return (null, null);
                    var power = exponent.EInteger.ToInt32Checked();
                    return power >= 0
                        ? (baseTop.Power(power), baseBottom.Power(power))
                        : (baseBottom.Power(-power), baseTop.Power(-power));
                }
                default:
                    if (AngleOf(expr) is not { } angle || !opened.TryGetValue(angle, out var trigonometric))
                        return (null, null);
                    var (cosine, sine) = trigonometric;
                    return expr switch
                    {
                        Sinf => (sine, one),
                        Cosf => (cosine, one),
                        Tanf => (sine, cosine),
                        Cotanf => (cosine, sine),
                        Secantf => (one, cosine),
                        _ => (one, sine),
                    };
            }

            (MultivariatePolynomial?, MultivariatePolynomial?) Combined(Entity left, Entity right, bool subtract)
            {
                if (Gathered(left, indices, opened, variableCount) is not ({ } leftTop, { } leftBottom)
                    || Gathered(right, indices, opened, variableCount) is not ({ } rightTop, { } rightBottom))
                    return (null, null);
                // Over one denominator the sum keeps it, so that a sum of n terms over d is not
                // carried over d^n.
                if (leftBottom.SameAs(rightBottom))
                    return (subtract ? leftTop.Subtract(rightTop) : leftTop.Add(rightTop), leftBottom);
                if (leftTop.Multiply(rightBottom) is not { } first || rightTop.Multiply(leftBottom) is not { } second)
                    return (null, null);
                return (subtract ? first.Subtract(second) : first.Add(second), leftBottom.Multiply(rightBottom));
            }
        }

        /// <summary>
        /// <paramref name="polynomial"/> with every <c>sin(u)^2</c> written as <c>1 - cos(u)^2</c>,
        /// which is its unique form modulo <c>sin(u)^2 + cos(u)^2 - 1</c>.
        /// </summary>
        private static MultivariatePolynomial? Reduced(MultivariatePolynomial polynomial, IReadOnlyList<(int Sine, int Cosine)> pairs)
        {
            var variableCount = polynomial.VariableCount;
            var one = MultivariatePolynomial.One(variableCount);
            var result = MultivariatePolynomial.Zero(variableCount);
            foreach (var monomial in polynomial.Monomials)
            {
                var kept = monomial;
                var factor = one;
                foreach (var (sine, cosine) in pairs)
                {
                    var half = MultivariatePolynomial.PowerOfMonomial(monomial, sine) / 2;
                    if (half == 0)
                        continue;
                    kept -= MultivariatePolynomial.PackMonomial(sine, 2 * half);
                    var cosineSquared = MultivariatePolynomial.Term(variableCount, MultivariatePolynomial.PackMonomial(cosine, 2), ERational.One);
                    if (one.Subtract(cosineSquared).Power(half) is not { } replaced || factor.Multiply(replaced) is not { } next)
                        return null;
                    factor = next;
                }
                if (MultivariatePolynomial.Term(variableCount, kept, polynomial.CoefficientOf(monomial)).Multiply(factor) is not { } term)
                    return null;
                result = result.Add(term);
                if (result.TermCount > MultivariatePolynomial.MaxTerms)
                    return null;
            }
            return result;
        }
    }
}
