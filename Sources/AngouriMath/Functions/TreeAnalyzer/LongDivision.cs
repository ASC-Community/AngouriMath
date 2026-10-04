//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using Antlr4.Runtime.Misc;
using PeterO.Numbers;

namespace AngouriMath.Functions
{
    using static Entity;
    using static Entity.Number;
    internal static partial class TreeAnalyzer
    {
        internal sealed class PolynomialInformation
        {
            private readonly Dictionary<Variable, Dictionary<EDecimal, Entity>> monoInfo = new();
            private readonly Dictionary<Entity, Variable> replacements = new();
            private readonly Dictionary<Variable, Entity> revertReplacements = new();
            public IReadOnlyDictionary<Variable, Dictionary<EDecimal, Entity>> MonoInfo => monoInfo;
            public IReadOnlyDictionary<Entity, Variable> Replacements => replacements;
            public IReadOnlyDictionary<Variable, Entity> RevertReplacements => revertReplacements;
            public void AddReplacement(IEnumerable<Variable> existingVars, Entity value)
            {
                var variable = Variable.CreateTemp(existingVars.Concat(revertReplacements.Keys));
                replacements[value] = variable;
                revertReplacements[variable] = value;
            }
            public void AddMonoInfo(Variable variable, Dictionary<EDecimal, Entity>? powers)
            {
                if (powers is { }) monoInfo.Add(variable, powers);
            }
        }

        internal static PolynomialInformation GatherAllPossiblePolynomials(Entity expr, bool replaceVars)
        {
            // Init
            var res = new PolynomialInformation();

            if (replaceVars)
            {
                // Replace all variables we can
                foreach (var varMentioned in expr.Vars)
                    res.AddReplacement(expr.Vars, GetMinimumSubtree(expr, varMentioned));
                expr = expr.Substitute(res.Replacements);
            }

            // Gather info about each var as if this var was the only argument of the polynomial P(x)
            var children = Sumf.LinearChildren(expr);
            foreach (var varMentioned in expr.Vars)
                res.AddMonoInfo(varMentioned, Algebra.AnalyticalSolving.PolynomialSolver.GatherMonomialInformation
                   <EDecimal, PrimitiveDecimal>(children, varMentioned));
            return res;
        }

        /// <summary>
        /// A power in the one form that equal powers share. <see cref="EDecimal"/> keeps the
        /// scale it was written or computed at, and is equal only to an <see cref="EDecimal"/>
        /// of that same scale -- so <c>2</c> and <c>2.0</c> are the same number and different
        /// dictionary keys. Removing the trailing zeros is what makes them one key.
        /// </summary>
        /// <remarks>
        /// The unlimited context, so that reducing a power never rounds it. Rounding here
        /// would merge two powers that are genuinely different, which is the opposite
        /// mistake and a worse one.
        /// </remarks>
        private static EDecimal Canonical(EDecimal power) => power.Reduce(EContext.Unlimited);

        private static Dictionary<EDecimal, Entity> Canonicalize(Dictionary<EDecimal, Entity> powers)
        {
            var canonical = new Dictionary<EDecimal, Entity>();
            foreach (var pair in powers)
            {
                var power = Canonical(pair.Key);
                // Two powers of the source may reduce to the same one, and then they are one
                // monomial and their coefficients add. Overwriting instead would silently
                // drop a term.
                canonical[power] = canonical.TryGetValue(power, out var already)
                    ? already + pair.Value : pair.Value;
            }
            return canonical;
        }

        /// <summary>
        /// Divides one polynomial over another one:
        /// <a href="https://en.wikipedia.org/wiki/Polynomial_long_division"/>
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>As written first, and only then with the variables replaced.</b> The replacement
        /// exists for a base that is not a variable — <c>sin(x)^2 / sin(x)</c>, where the thing
        /// the division is in is <c>sin(x)</c> — and it works by swapping each variable's
        /// smallest enclosing subtree for a fresh symbol. With one variable that is harmless,
        /// because nothing in the pair contains the whole expression. With <b>two</b> it is
        /// destructive: for <c>x / (a + b x)</c> the smallest subtree holding <c>a</c> is
        /// <c>a + b x</c>, so the divisor is replaced wholesale by one opaque symbol, the
        /// dividend keeps its <c>x</c>, and the two no longer share a variable to divide in.
        /// The division then reports that it cannot be done.
        /// </para>
        /// <para>
        /// What that cost: <c>int x/(a + b x) dx</c> was unanswered while
        /// <c>int x/(2 + 3 x) dx</c> came out, and the same for every improper fraction with a
        /// symbolic coefficient — including the one <c>int x ln(b + a x) dx</c> is left with
        /// after integration by parts. A numeric coefficient hid the defect, which is the usual
        /// way this one hides.
        /// https://github.com/asc-community/AngouriMath/issues/718
        /// </para>
        /// <para>
        /// Trying the unreplaced form first keeps both: a pair that divides as written divides
        /// the same way it always did, and one that does not falls through to the replacement,
        /// which is the only thing that ever answered <c>sin(x)^2 / sin(x)</c>.
        /// </para>
        /// <para>
        /// <b><paramref name="genericCase"/> says which of two callers is asking.</b> Dividing by
        /// the divisor's leading coefficient loses the value of the parameter that makes it zero:
        /// <c>x / (a + b x)</c> divided out is undefined at <c>b = 0</c>, where the quotient is
        /// <c>x / a</c> and perfectly ordinary. For the simplifier that is a rewrite which is not
        /// an equivalence, so it is declined; for the integrator it is the answer every
        /// neighbouring rule already gives, since <c>int 1/(a x + b) dx</c> is
        /// <c>ln(a x + b) / a</c> and loses <c>a = 0</c> on every call. The default is the
        /// simplifier's, and nothing about the general pass changes by adding this.
        /// </para>
        /// </remarks>
        internal static (Entity Divided, Entity Remainder)? PolynomialLongDivision(
            Entity p, Entity q, bool genericCase = false, Variable? inTermsOf = null)
        {
            if (!p.Vars.Any() || !q.Vars.Any())
                return null; // There are no variables to find polynomial as
            return DivideOnePolynomial(p, q, replaceVars: false, genericCase, inTermsOf)
                ?? DivideOnePolynomial(p, q, replaceVars: true, genericCase, inTermsOf);
        }

        private static (Entity Divided, Entity Remainder)? DivideOnePolynomial(
            Entity p, Entity q, bool replaceVars, bool genericCase, Variable? inTermsOf)
        {
            // ---> (x^0.6 + 2x^0.3 + 1) / (x^0.3 + 1)
            var replacementInfo = GatherAllPossiblePolynomials(p + q, replaceVars);

            var originalP = p;
            var originalQ = q;

            // TODO: remove extra call to GatherAllPossiblePolynomials above with p + q
            // by making GatherAllPossiblePolynomials accept multiple polynomials
            p = p.Substitute(replacementInfo.Replacements);
            q = q.Substitute(replacementInfo.Replacements);

            var monoinfoP = GatherAllPossiblePolynomials(p.Expand(), replaceVars: false).MonoInfo;
            var monoinfoQ = GatherAllPossiblePolynomials(q.Expand(), replaceVars: false).MonoInfo;

            // First attempt to find polynoms.
            //
            // **Which variable the division is in is the caller's to say, where it knows.** The
            // choice was whichever variable came first, and with two symbols in play that is as
            // likely to be a parameter as the one the caller cares about: `a x^2 / (b + a x)`
            // divides perfectly well in `a`, giving `x - b x / (b + a x)`, which is true and
            // useless to an integrator working in `x` — and it consumes the division, so the
            // split that would have answered is never tried. Taking the first candidate made the
            // verdict depend on the order `Vars` happens to report, which is not a property of
            // the division. A caller that does not care still gets the old choice.
            //
            // In the replaced pass the variables are gone, swapped for temporaries standing for
            // a subtree — `sin(x)^2 / sin(x)` divides in a symbol standing for `sin(x)` — so the
            // test there is that the subtree behind the temporary is one the caller's variable
            // occurs in. https://github.com/asc-community/AngouriMath/issues/718
            var candidates = monoinfoP.Keys.Where(monoinfoQ.ContainsKey);
            var polyvar = inTermsOf is null
                ? candidates.FirstOrDefault()
                : candidates.FirstOrDefault(v => v == inTermsOf
                    || (replacementInfo.RevertReplacements.TryGetValue(v, out var behind)
                        && behind.ContainsNode(inTermsOf)));
            // cannot divide, return unchanged
            if (polyvar is null) return null;

            // The powers are dictionary keys, and EDecimal is equal only to an EDecimal of the
            // same scale: 2 and 2.0 are the same number and not the same key. Every power
            // arrived at by arithmetic below carries the scale of that arithmetic, so
            // 1.5 + 0.5 comes out as 2.0 and misses the 2 already in the dictionary. Reduced
            // once, here, so that every key in play is in the one canonical form and the
            // lookups below mean what they say. https://github.com/asc-community/AngouriMath/issues/751
            var powersOfP = Canonicalize(monoinfoP[polyvar]);
            var powersOfQ = Canonicalize(monoinfoQ[polyvar]);
            var maxpowP = powersOfP.Keys.Max() ?? throw new AngouriBugException("No null expected");
            var maxpowQ = powersOfQ.Keys.Max() ?? throw new AngouriBugException("No null expected");
            var maxvalP = powersOfP[maxpowP];
            var maxvalQ = powersOfQ[maxpowQ];

            // TODO: add case where all powers are non-positive
            // for now just return polynomials unchanged
            if (maxpowP.LessThan(maxpowQ)) return null;

            // The unreplaced attempt divides by the divisor's leading coefficient, so for the
            // simplifier it runs only where that coefficient is a number other than zero — which
            // is to say, where the quotient it produces is valid for every value of every symbol
            // in it. With a symbolic leading coefficient it would not be: `x^2/(a + b x)` divided
            // out is undefined at `b = 0`, where the integrand is `x^2/a` and perfectly ordinary,
            // and a rewrite that silently loses a value of a parameter is not an equivalence.
            // `x^2/(x + a)` has leading coefficient 1 and is not that case, which is why it is
            // answered even for the simplifier.
            //
            // **The integrator asks for the generic case, and is right to.** It is not rewriting
            // an expression into an equal one; it is naming an antiderivative, and the rest of
            // the integrator already names the generic one. `int 1/(a x + b) dx` comes back as
            // `ln(a x + b) / a`, `int sin(a x) dx` as `-cos(a x) / a`, `int x^n dx` as
            // `x^(n+1)/(n+1)` — each undefined at one value of its parameter, each given anyway.
            // The first of those divides by the very coefficient this guard was refusing to
            // divide by, so declining here made long division the one rule in the integrator
            // holding out for a condition none of its neighbours carry. That cost `x/(a + b x)`,
            // `x ln(b + a x)` and every improper fraction with a symbolic coefficient, all of
            // which come out with the coefficients made numeric.
            // https://github.com/asc-community/AngouriMath/issues/180
            if (!replaceVars && ((maxvalQ.Vars.Any() && !genericCase) || maxvalQ.Evaled == Integer.Create(0)))
                return null;

            // A leading coefficient with a symbol in it can be zero as a value without being zero
            // as written: `(-(a - a u^2) - (u^2 + 1) a)^3` is `-8 a^3`, and expanded it leaves
            // `-3 a^3 + 3 a^3` at `u^6`, which nothing above collects. Divided by that, the
            // quotient was NaN. The divisor's degree is that of its first coefficient that is
            // not zero. https://github.com/asc-community/AngouriMath/issues/1665
            while (maxvalQ.Vars.Any() && PartialFractions.IsZeroAsAValue(maxvalQ))
            {
                _ = powersOfQ.Remove(maxpowQ);
                if (powersOfQ.Count == 0)
                    return null;
                maxpowQ = powersOfQ.Keys.Max() ?? throw new AngouriBugException("No null expected");
                maxvalQ = powersOfQ[maxpowQ];
                if (maxvalQ.Evaled == Integer.Create(0))
                    return null;
            }

            var result = new Dictionary<EDecimal, Entity>();
            // possibly very long process
            while (maxpowP.GreaterThanOrEquals(maxpowQ))
            {
                // KeyPair is ax^n with Key=n, Value=a
                var deltapow = Canonical(maxpowP - maxpowQ);
                var deltamul = maxvalP / maxvalQ;
                result[deltapow] = deltamul;

                foreach (var n in powersOfQ)
                {
                    var newpow = Canonical(deltapow + n.Key);
                    if (powersOfP.TryGetValue(newpow, out var existing))
                        powersOfP[newpow] = existing - deltamul * n.Value;
                    else
                        powersOfP[newpow] = -deltamul * n.Value;
                }
                _ = powersOfP.Remove(maxpowP);
                if (powersOfP.Count == 0)
                    break;

                maxpowP = powersOfP.Keys.Max() ?? throw new AngouriBugException("No null expected");
                maxvalP = powersOfP[maxpowP];
            }

            // check if all left in P is zero. If something left, division is impossible => return P / Q
            Entity rest = 0;
            foreach (var coef in powersOfP)
                if (coef.Value.Simplify() is not Integer(0) and var simplified)
                    if (coef.Key.IsZero) // Don't insert unnecessary x^0 because it's undefined for x=0
                        rest += simplified;
                    else
                        rest += simplified * MathS.Pow(polyvar, coef.Key);
            rest /= q;

            Entity res = 0;
            foreach (var pair in result)
                if (pair.Key.IsZero) // Don't insert unnecessary x^0 because it's undefined for x=0
                    res += pair.Value.Simplify(5);
                else
                    res += pair.Value.Simplify(5) * MathS.Pow(polyvar, pair.Key);
            return (res.Substitute(replacementInfo.RevertReplacements),
                    rest.Substitute(replacementInfo.RevertReplacements));
        }
    }
}
