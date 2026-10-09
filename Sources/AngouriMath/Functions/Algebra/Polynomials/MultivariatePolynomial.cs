//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;
using System;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions
{
    /// <summary>
    /// A polynomial in several variables over the rationals, stored sparsely.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The coefficient domain is deliberately only <c>Q</c>. A polynomial whose
    /// coefficients are arbitrary expressions is not one this can reason about: deciding
    /// whether <c>sqrt(2) - a</c> is zero is as hard as the problem that sent us here, and
    /// a divisor wrongly believed nonzero produces a wrong cancellation rather than a
    /// missing one. Anything outside <c>Q</c> is refused at the door by
    /// <see cref="TryParse"/>.
    /// </para>
    /// <para>
    /// An exponent vector is packed into one <see cref="ulong"/>, a byte to a variable,
    /// the first variable in the most significant byte. Comparing packed monomials is then
    /// exactly the lexicographic order on exponents that <see cref="DivideExact(MultivariatePolynomial, int)"/> needs,
    /// and looking a monomial up costs one hash.
    /// </para>
    /// <para>
    /// This and its neighbours in <c>Functions/Algebra/Polynomials</c> are kernel algebra:
    /// the solvers, the evaluator and the simplifier all depend on them, and they depend on
    /// none of those. That direction is the point of the folder. <see cref="PolynomialGcd"/>
    /// in particular is reached from simplification, from <c>Core/Transformations</c> and
    /// from evaluation, which is why it lived under <c>Functions/Simplification</c> for as
    /// long as simplification was the only caller anybody had counted.
    /// </para>
    /// <para>
    /// Partial so that the operations only a Gröbner basis needs — monomial divisibility, an
    /// order other than lexicographic, reduction against a set — live beside the solver that
    /// wants them, in <c>Functions/Algebra/Groebner</c>, rather than here where every other
    /// caller would have to read past them. See <c>MultivariatePolynomial.Groebner.cs</c>.
    /// </para>
    /// </remarks>
    internal sealed partial class MultivariatePolynomial
    {
        /// <summary>One byte of the packed monomial each, so eight of them fit.</summary>
        internal const int MaxVariables = 8;

        /// <summary>
        /// Multiplying two monomials adds their exponents, so half of what a byte holds is
        /// the most a single exponent may carry if the sum is to stay in its byte.
        /// </summary>
        internal const int MaxDegree = 127;

        /// <summary>
        /// Past this an intermediate result is refused rather than paid for. Nothing here
        /// is asked a question worth thousands of monomials, and the simplifier runs it on
        /// every quotient it passes.
        /// </summary>
        internal const int MaxTerms = 512;

        /// <summary>
        /// The ceiling for an intermediate of a calculation whose input and answer are both
        /// already inside <see cref="MaxTerms"/>.
        /// </summary>
        /// <remarks>
        /// A multivariate pseudo-remainder multiplies through by a leading coefficient that is
        /// itself a polynomial, so its intermediates grow in monomial count even when neither
        /// side is anywhere near the bound — the subresultant divisions bound how big the
        /// coefficients get, not how many terms there are. Holding those intermediates to the
        /// input bound refused a gcd of a 19-term and a 29-term pair.
        /// https://github.com/asc-community/AngouriMath/issues/920
        ///
        /// It is deliberately a second constant rather than a larger <see cref="MaxTerms"/>.
        /// The input bound is what protects the hot path — <c>TryCancel</c> runs on every
        /// quotient the simplifier builds — and raising it turned three documented refusals
        /// into answers, including a direct product of two 495-term inputs. What was too small
        /// was never the bound on what may be asked, only the bound on what may be passed
        /// through on the way to an answer that is itself small.
        /// </remarks>
        internal const int MaxIntermediateTerms = 4096;

        private const int BitsPerVariable = 8;
        private const ulong PowerMask = 0xFF;

        /// <summary>Monomial to its coefficient. A zero coefficient is never stored.</summary>
        private readonly Dictionary<ulong, ERational> terms;

        internal int VariableCount { get; }

        private MultivariatePolynomial(int variableCount, Dictionary<ulong, ERational> terms)
        {
            VariableCount = variableCount;
            this.terms = terms;
        }

        internal static MultivariatePolynomial Zero(int variableCount)
            => new(variableCount, new Dictionary<ulong, ERational>());

        internal static MultivariatePolynomial Constant(int variableCount, ERational value)
        {
            var terms = new Dictionary<ulong, ERational>();
            if (!value.IsZero)
                terms[0] = value.ToLowestTerms();
            return new(variableCount, terms);
        }

        internal static MultivariatePolynomial One(int variableCount)
            => Constant(variableCount, ERational.One);

        internal static MultivariatePolynomial Monomial(int variableCount, int variable)
            => new(variableCount, new Dictionary<ulong, ERational> { [Pack(variable, 1)] = ERational.One });

        internal bool IsZero => terms.Count == 0;

        internal bool IsConstant => terms.Count == 0 || (terms.Count == 1 && terms.ContainsKey(0));

        internal int TermCount => terms.Count;

        private static int ShiftOf(int variable) => (MaxVariables - 1 - variable) * BitsPerVariable;

        internal static int PowerOf(ulong monomial, int variable)
            => (int)((monomial >> ShiftOf(variable)) & PowerMask);

        private static ulong Pack(int variable, int power) => (ulong)power << ShiftOf(variable);

        private static ulong Without(ulong monomial, int variable) => monomial & ~Pack(variable, (int)PowerMask);

        /// <summary>
        /// The highest power of <paramref name="variable"/> occurring. Zero both for a
        /// polynomial free of it and for the zero polynomial, so callers to which the
        /// difference matters test <see cref="IsZero"/> first.
        /// </summary>
        internal int DegreeIn(int variable)
        {
            var degree = 0;
            foreach (var monomial in terms.Keys)
            {
                var power = PowerOf(monomial, variable);
                if (power > degree)
                    degree = power;
            }
            return degree;
        }

        /// <summary>The greatest monomial in lexicographic order; zero if there is none.</summary>
        private ulong LeadingMonomial()
        {
            ulong leading = 0;
            foreach (var monomial in terms.Keys)
                if (monomial > leading)
                    leading = monomial;
            return leading;
        }

        internal MultivariatePolynomial Add(MultivariatePolynomial other) => Combine(other, subtract: false);

        internal MultivariatePolynomial Subtract(MultivariatePolynomial other) => Combine(other, subtract: true);

        private MultivariatePolynomial Combine(MultivariatePolynomial other, bool subtract)
        {
            var result = new Dictionary<ulong, ERational>(terms);
            foreach (var term in other.terms)
                Accumulate(result, term.Key, subtract ? term.Value.Negate() : term.Value);
            return new(VariableCount, result);
        }

        private static void Accumulate(Dictionary<ulong, ERational> into, ulong monomial, ERational value)
        {
            if (!into.TryGetValue(monomial, out var existing))
            {
                if (!value.IsZero)
                    into[monomial] = value;
                return;
            }
            var sum = existing.Add(value).ToLowestTerms();
            if (sum.IsZero)
                into.Remove(monomial);
            else
                into[monomial] = sum;
        }

        internal MultivariatePolynomial? Multiply(MultivariatePolynomial other, int maxTerms = MaxTerms)
        {
            if (IsZero || other.IsZero)
                return Zero(VariableCount);
            var result = new Dictionary<ulong, ERational>();
            foreach (var left in terms)
                foreach (var right in other.terms)
                {
                    if (!TryMultiplyMonomials(left.Key, right.Key, VariableCount, out var monomial))
                        return null;
                    Accumulate(result, monomial, left.Value.Multiply(right.Value).ToLowestTerms());
                    if (result.Count > maxTerms)
                        return null;
                }
            return new(VariableCount, result);
        }

        internal MultivariatePolynomial? Power(int exponent, int maxTerms = MaxTerms)
        {
            if (exponent < 0 || exponent > MaxDegree)
                return null;
            var result = One(VariableCount);
            var square = this;
            while (exponent > 0)
            {
                if ((exponent & 1) == 1)
                {
                    if (result.Multiply(square, maxTerms) is not { } multiplied)
                        return null;
                    result = multiplied;
                }
                exponent >>= 1;
                if (exponent == 0)
                    break;
                if (square.Multiply(square, maxTerms) is not { } squared)
                    return null;
                square = squared;
            }
            return result;
        }

        internal MultivariatePolynomial ScaleBy(ERational factor)
        {
            if (factor.IsZero)
                return Zero(VariableCount);
            var result = new Dictionary<ulong, ERational>(terms.Count);
            foreach (var term in terms)
                result[term.Key] = term.Value.Multiply(factor).ToLowestTerms();
            return new(VariableCount, result);
        }

        /// <summary>Multiplied by <c>variable ^ power</c>.</summary>
        internal MultivariatePolynomial? ShiftedBy(int variable, int power)
        {
            if (power == 0)
                return this;
            var result = new Dictionary<ulong, ERational>(terms.Count);
            foreach (var term in terms)
            {
                var raised = PowerOf(term.Key, variable) + power;
                if (raised > MaxDegree)
                    return null;
                result[Without(term.Key, variable) | Pack(variable, raised)] = term.Value;
            }
            return new(VariableCount, result);
        }

        /// <summary>The terms, each as its packed monomial and its coefficient.</summary>
        internal IEnumerable<KeyValuePair<ulong, ERational>> Terms => terms;

        /// <summary>Whether every coefficient is a whole number.</summary>
        internal bool HasIntegerCoefficients
        {
            get
            {
                foreach (var term in terms)
                    if (!term.Value.IsInteger())
                        return false;
                return true;
            }
        }

        /// <summary>
        /// The value at whole-number arguments, reduced modulo <paramref name="modulus"/> at
        /// every step so that the numbers stay small; <see langword="null"/> where a coefficient
        /// is not whole, since then the value modulo anything is not defined.
        /// </summary>
        internal EInteger? ValueModulo(IReadOnlyList<EInteger> values, EInteger modulus)
        {
            var total = EInteger.Zero;
            foreach (var term in terms)
            {
                if (!term.Value.IsInteger())
                    return null;
                var product = term.Value.Numerator.Mod(modulus);
                for (var variable = 0; variable < VariableCount && variable < values.Count; variable++)
                {
                    var power = PowerOf(term.Key, variable);
                    if (power > 0)
                        product = product * values[variable].Mod(modulus).ModPow(EInteger.FromInt32(power), modulus) % modulus;
                }
                total = (total + product) % modulus;
            }
            return total.Mod(modulus);
        }

        /// <summary>
        /// Read as a polynomial in <paramref name="variable"/> alone: the power of that
        /// variable mapped to the coefficient, itself a polynomial in the others.
        /// </summary>
        internal Dictionary<int, MultivariatePolynomial> CoefficientsIn(int variable)
        {
            var buckets = new Dictionary<int, Dictionary<ulong, ERational>>();
            foreach (var term in terms)
            {
                var power = PowerOf(term.Key, variable);
                if (!buckets.TryGetValue(power, out var bucket))
                    buckets[power] = bucket = new Dictionary<ulong, ERational>();
                bucket[Without(term.Key, variable)] = term.Value;
            }
            var result = new Dictionary<int, MultivariatePolynomial>(buckets.Count);
            foreach (var bucket in buckets)
                result[bucket.Key] = new(VariableCount, bucket.Value);
            return result;
        }

        /// <summary>
        /// The partial derivative with respect to <paramref name="variable"/>. Nothing here
        /// can leave the type's bounds: every exponent falls by one and no monomial is
        /// created that was not already present.
        /// </summary>
        internal MultivariatePolynomial DerivativeIn(int variable)
        {
            var result = new Dictionary<ulong, ERational>();
            foreach (var term in terms)
            {
                var power = PowerOf(term.Key, variable);
                if (power == 0)
                    continue;
                Accumulate(result, Without(term.Key, variable) | Pack(variable, power - 1),
                    term.Value.Multiply(ERational.FromInt32(power)).ToLowestTerms());
            }
            return new(VariableCount, result);
        }

        /// <summary>
        /// The polynomial whose square this is, or <see langword="null"/> where it is not the
        /// square of one over <c>Q</c>. Of the two roots, the one whose leading coefficient in
        /// each variable, read in turn, is positive.
        /// </summary>
        /// <remarks>
        /// In the first variable that occurs, as for one variable: the root's leading coefficient
        /// is the root of this one's, found the same way in the variables left, and each lower
        /// coefficient is what the square of the root so far leaves at the next power down,
        /// divided exactly by twice that leading coefficient. Whatever is not a square fails
        /// one of those divisions or the check at the end.
        /// </remarks>
        internal MultivariatePolynomial? TrySquareRoot()
        {
            if (IsZero)
                return this;
            if (IsConstant)
            {
                var value = ConstantValue.ToLowestTerms();
                if (value.Sign < 0)
                    return null;
                var (above, below) = (value.Numerator.Sqrt(), value.Denominator.Sqrt());
                return above.Multiply(above).Equals(value.Numerator) && below.Multiply(below).Equals(value.Denominator)
                    ? Constant(VariableCount, ERational.Create(above, below))
                    : null;
            }
            var variable = 0;
            while (DegreeIn(variable) == 0)
                variable++;
            var degree = DegreeIn(variable);
            if (degree % 2 != 0)
                return null;
            if (LeadingCoefficientIn(variable).TrySquareRoot() is not { } leading
                || leading.ShiftedBy(variable, degree / 2) is not { } root)
                return null;
            var twice = leading.ScaleBy(ERational.FromInt32(2));
            for (var step = 1; step <= degree / 2; step++)
            {
                if (root.Multiply(root) is not { } square)
                    return null;
                var left = Subtract(square);
                if (left.IsZero)
                    return root;
                if (!left.CoefficientsIn(variable).TryGetValue(degree - step, out var next))
                    continue;
                if (next.DivideExact(twice) is not { } term || term.ShiftedBy(variable, degree / 2 - step) is not { } shifted)
                    return null;
                root = root.Add(shifted);
            }
            return root.Multiply(root) is { } last && Subtract(last).IsZero ? root : null;
        }

        internal MultivariatePolynomial LeadingCoefficientIn(int variable)
        {
            var degree = DegreeIn(variable);
            var result = new Dictionary<ulong, ERational>();
            foreach (var term in terms)
                if (PowerOf(term.Key, variable) == degree)
                    result[Without(term.Key, variable)] = term.Value;
            return new(VariableCount, result);
        }

        /// <summary>
        /// <paramref name="divisor"/> divided out, or <see langword="null"/> when it does
        /// not divide exactly.
        /// </summary>
        /// <remarks>
        /// The leading term in lexicographic order is cancelled at each step, which lowers
        /// it strictly, so the loop terminates; when the divisor really divides, its
        /// leading term divides the leading term of what is left every time, and what
        /// remains at the end is zero. Anything else — a leading term that will not divide,
        /// or a remainder that never reaches zero — is the answer that it does not divide.
        /// That is the check the caller relies on: nothing is cancelled that has not been
        /// divided out and seen to leave nothing behind.
        /// </remarks>
        internal MultivariatePolynomial? DivideExact(MultivariatePolynomial divisor, int maxTerms = MaxTerms)
            => DivideExact(divisor, out _, maxTerms);

        /// <summary>
        /// The same division, saying <b>why</b> there is no quotient. <paramref name="settled"/>
        /// is <see langword="false"/> where the division ran out of room rather than finishing:
        /// the term count grew past <paramref name="maxTerms"/>, a monomial went past
        /// <see cref="MaxDegree"/>, or the loop reached its step limit.
        /// </summary>
        /// <remarks>
        /// A caller that reads a <see langword="null"/> as "does not divide" is right only where
        /// the division was settled. Where it was not, the same <see langword="null"/> means "no
        /// answer", and a caller drawing a mathematical conclusion from it — that a polynomial is
        /// irreducible, say — would be stating exhaustion as a fact.
        /// </remarks>
        internal MultivariatePolynomial? DivideExact(
            MultivariatePolynomial divisor, out bool settled, int maxTerms = MaxTerms)
        {
            settled = true;
            if (divisor.IsZero)
                return null;
            if (IsZero)
                return Zero(VariableCount);
            if (divisor.IsConstant)
                return ScaleBy(ERational.One.Divide(divisor.terms[0]));

            var divisorLead = divisor.LeadingMonomial();
            var divisorValue = divisor.terms[divisorLead];
            var quotient = new Dictionary<ulong, ERational>();
            var rest = this;
            for (var step = 0; step <= maxTerms; step++)
            {
                if (rest.IsZero)
                    return new(VariableCount, quotient);
                var lead = rest.LeadingMonomial();
                // The leading monomial of what is left is not a multiple of the divisor's, and
                // no later step can make it one -- so this is a "no" and not a "gave up".
                if (!TryDivideMonomials(lead, divisorLead, VariableCount, out var monomial))
                    return null;
                var value = rest.terms[lead].Divide(divisorValue).ToLowestTerms();
                quotient[monomial] = value;
                if (divisor.MultiplyByTerm(monomial, value) is not { } product)
                {
                    settled = false;
                    return null;
                }
                rest = rest.Subtract(product);
                if (rest.TermCount > maxTerms)
                {
                    settled = false;
                    return null;
                }
            }
            settled = false;
            return null;
        }

        private MultivariatePolynomial? MultiplyByTerm(ulong monomial, ERational value)
        {
            var result = new Dictionary<ulong, ERational>(terms.Count);
            foreach (var term in terms)
            {
                if (!TryMultiplyMonomials(term.Key, monomial, VariableCount, out var product))
                    return null;
                result[product] = term.Value.Multiply(value).ToLowestTerms();
            }
            return new(VariableCount, result);
        }

        private static bool TryMultiplyMonomials(ulong left, ulong right, int variableCount, out ulong product)
        {
            product = 0;
            for (var i = 0; i < variableCount; i++)
            {
                var power = PowerOf(left, i) + PowerOf(right, i);
                if (power > MaxDegree)
                    return false;
                product |= Pack(i, power);
            }
            return true;
        }

        private static bool TryDivideMonomials(ulong dividend, ulong divisor, int variableCount, out ulong quotient)
        {
            quotient = 0;
            for (var i = 0; i < variableCount; i++)
            {
                var power = PowerOf(dividend, i) - PowerOf(divisor, i);
                if (power < 0)
                    return false;
                quotient |= Pack(i, power);
            }
            return true;
        }

        /// <summary>
        /// The same polynomial up to a rational factor, with whole coprime coefficients and
        /// a positive leading one.
        /// </summary>
        /// <remarks>
        /// A greatest common divisor is only defined up to a unit, and over <c>Q</c> every
        /// nonzero rational is one, so a representative has to be chosen. This is the one
        /// that reads the way the answer is usually written: <c>x + y</c>, not
        /// <c>-x/2 - y/2</c>.
        /// </remarks>
        internal MultivariatePolynomial Normalized() => Normalized(out _);

        /// <summary>
        /// <see cref="Normalized()"/>, with the factor it was scaled by: the normalized
        /// polynomial is this one times <paramref name="scale"/>, and this one is the
        /// normalized one over it.
        /// </summary>
        internal MultivariatePolynomial Normalized(out ERational scale)
        {
            scale = ERational.One;
            if (IsZero)
                return this;
            var denominators = EInteger.One;
            foreach (var term in terms)
                denominators = Lcm(denominators, term.Value.Denominator);
            var numerators = EInteger.Zero;
            foreach (var term in terms)
                numerators = numerators.Gcd(
                    term.Value.Numerator.Multiply(denominators.Divide(term.Value.Denominator)));
            if (numerators.IsZero)
                return this;
            scale = ERational.Create(denominators, numerators);
            if (terms[LeadingMonomial()].Sign < 0)
                scale = scale.Negate();
            return ScaleBy(scale);
        }

        private static EInteger Lcm(EInteger a, EInteger b) => a.Divide(a.Gcd(b)).Multiply(b);

        internal bool SameAs(MultivariatePolynomial other)
        {
            if (terms.Count != other.terms.Count)
                return false;
            foreach (var term in terms)
                if (!other.terms.TryGetValue(term.Key, out var value) || value.CompareTo(term.Value) != 0)
                    return false;
            return true;
        }

        /// <summary>
        /// Reads <paramref name="expr"/> as a polynomial over <c>Q</c> in the given
        /// variables, or answers <see langword="null"/> where it is not one.
        /// </summary>
        /// <remarks>
        /// Products and powers are multiplied out as they are read rather than by expanding
        /// the expression first, so that the term ceiling stops a <c>(x + y) ^ 100</c>
        /// before it is built rather than after.
        /// </remarks>
        internal static MultivariatePolynomial? TryParse(Entity expr, IReadOnlyDictionary<Variable, int> variables)
            // A ninth variable has no byte of the packed monomial to live in, and the shift
            // that would address it is negative, which the language turns into a shift by
            // 56 -- the first variable's byte. So it would not fail, it would silently be
            // the first variable, and x_1 - x_9 would read as the zero polynomial. Refused
            // here rather than guarded at each caller, since the packing is this type's.
            => variables.Count > MaxVariables ? null : expr switch
            {
                // Integer is a Rational, so this arm takes both.
                Rational rational => Constant(variables.Count, rational.ERational),
                Variable variable => variables.TryGetValue(variable, out var index)
                    ? Monomial(variables.Count, index)
                    : null,
                Sumf(var augend, var addend) =>
                    TryParse(augend, variables) is { } left && TryParse(addend, variables) is { } right
                    ? left.Add(right) : null,
                Minusf(var subtrahend, var minuend) =>
                    TryParse(subtrahend, variables) is { } left && TryParse(minuend, variables) is { } right
                    ? left.Subtract(right) : null,
                Mulf(var multiplier, var multiplicand) =>
                    TryParse(multiplier, variables) is { } left && TryParse(multiplicand, variables) is { } right
                    ? left.Multiply(right) : null,
                Divf(var dividend, var divisor) =>
                    TryParse(divisor, variables) is { IsConstant: true, IsZero: false } bottom
                    && TryParse(dividend, variables) is { } top
                    ? top.ScaleBy(ERational.One.Divide(bottom.ConstantValue)) : null,
                Powf(var @base, Integer exponent) => ParsePower(@base, exponent, variables),
                _ => null
            };

        private static MultivariatePolynomial? ParsePower(
            Entity @base, Integer exponent, IReadOnlyDictionary<Variable, int> variables)
        {
            if (!exponent.EInteger.CanFitInInt32())
                return null;
            var power = exponent.EInteger.ToInt32Checked();
            if (power > MaxDegree || power < -MaxDegree)
                return null;
            if (TryParse(@base, variables) is not { } parsed)
                return null;
            if (power >= 0)
                return parsed.Power(power);
            // A negative power is only a polynomial when what it inverts is a nonzero
            // number; a genuine 1/x is not one and is refused.
            if (!parsed.IsConstant || parsed.IsZero)
                return null;
            var inverse = ERational.One.Divide(parsed.ConstantValue);
            var result = ERational.One;
            for (var i = 0; i < -power; i++)
                result = result.Multiply(inverse).ToLowestTerms();
            return Constant(variables.Count, result);
        }

        private ERational ConstantValue => terms.Count == 0 ? ERational.Zero : terms[0];

        internal Entity ToEntity(IReadOnlyList<Variable> variables)
        {
            Entity? result = null;
            foreach (var monomial in terms.Keys.OrderByDescending(key => key))
            {
                var coefficient = terms[monomial];
                var negative = coefficient.Sign < 0;
                var magnitude = negative ? coefficient.Negate() : coefficient;
                Entity? term = null;
                for (var i = 0; i < VariableCount; i++)
                {
                    var power = PowerOf(monomial, i);
                    if (power == 0)
                        continue;
                    Entity factor = power == 1 ? variables[i] : MathS.Pow(variables[i], power);
                    term = term is null ? factor : term * factor;
                }
                if (term is null)
                    term = Rational.Create(magnitude);
                else if (magnitude.CompareTo(ERational.One) != 0)
                    term = Rational.Create(magnitude) * term;
                result = result is null
                    ? negative ? -term : term
                    : negative ? result - term : result + term;
            }
            return result ?? Integer.Create(0);
        }
    }
}
