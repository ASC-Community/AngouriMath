//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AngouriMath.Core;
using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions.Boolean
{
    /// <summary>
    /// What the quantifiers around a statement establish while they decide it: each bound name
    /// is a member of the set it ranges over, and a claim made under a hypothesis is made where
    /// the hypothesis holds. A rule that needs one of those facts, such as that <c>p</c> is prime
    /// or that <c>0 &lt; k &lt; p</c>, asks here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A node has no parent, so a rule inside a body cannot walk up to the quantifier that binds
    /// its names. The quantifier hands the facts down instead, for as long as it takes to decide:
    /// the set its name ranges over, and for <c>forall x in S : H implies C</c> -- or
    /// <c>C or not H</c>, which says the same -- the conjuncts of <c>H</c> while <c>C</c> is
    /// simplified. They are never in scope while <c>H</c> itself is simplified, or
    /// <c>H implies C</c> would lose its hypothesis. For <c>exists</c> and
    /// <c>exists!</c> each conjunct of the body is simplified with the ones before it in scope.
    /// Scopes nest, so the inner quantifier of <c>forall p in PP : forall k in ZZ : ...</c> knows
    /// that <c>p</c> is prime. Nothing leaves a scope: a verdict reached with the facts is the
    /// verdict on the quantified statement, which states them, and it carries no condition.
    /// </para>
    /// <para>
    /// The bound name is renamed first, to a name no other expression has, and every fact is
    /// about a renamed name. That is what keeps a verdict from escaping through a cache. A node
    /// that caches its simplification can only have read a fact if it mentions a renamed name,
    /// and every such node is built during this decision and is reachable from nothing else. A
    /// cache keyed on the structure of an expression cannot serve it elsewhere either, since no
    /// other expression has the name. For the same reason a conjunct of the hypothesis about the
    /// free names alone is not recorded: a rule reading it could simplify a node that is shared.
    /// A substitution stops at a binder, so an inner quantifier that binds the same name again
    /// is not renamed through, and the outer facts about the name do not reach it.
    /// </para>
    /// <para>
    /// Only a body in which a rule reads the facts is renamed and decided this way. Every other
    /// statement is decided as it was, under its own names.
    /// </para>
    /// https://github.com/asc-community/AngouriMath/issues/1409
    /// </remarks>
    internal static class QuantifierFacts
    {
        /// <summary>One scope: the name it renamed, where it bound one, and what it establishes.</summary>
        private sealed class Frame
        {
            internal readonly Variable? Renamed;
            internal readonly Entity[] Facts;
            internal readonly Frame? Next;
            internal Frame(Variable? renamed, Entity[] facts, Frame? next) => (Renamed, Facts, Next) = (renamed, facts, next);
        }

        [System.ThreadStatic] private static Frame? top;

        /// <summary>Numbers the renamed names, which is what makes each one unlike any other.</summary>
        [ConcurrentField] private static long lastRenamed;

        /// <summary>
        /// Decides the quantified statement with the facts in scope, or <see langword="null"/>
        /// where no rule in the body reads them, for the statement to be decided as before. The
        /// verdict is <see langword="null"/> where it is not settled, beside the body simplified
        /// as far as it went, under the name it was given.
        /// </summary>
        internal static (Entity? Verdict, Entity Body)? Decide(Quantifiers.Kind kind, Entity var, Entity over, Entity body, bool isExact)
        {
            if (var is not Variable x || over is not Set set
                || !body.Nodes.Any(node => Reads(node) || set is SpecialSet whole && Quantifiers.IsIntegerSet(whole) && IsPeriodic(node)))
                return null;
            var renamed = Variable.CreateVariableUnchecked($"{x.Name}'{Interlocked.Increment(ref lastRenamed)}");
            var mark = ProofRecording.Mark();
            var enclosing = top;
            top = new Frame(renamed, MembershipFacts(renamed, set), enclosing);
            try
            {
                // One level down, so that what a rule records while the body is simplified is
                // written under the statement the quantifier records when it decides.
                var written = body.Substitute(x, renamed);
                Entity simplified;
                ProofRecording.Enter();
                try
                {
                    simplified = Simplified(kind, written, isExact);
                }
                finally
                {
                    ProofRecording.Leave();
                }
                var verdict = Quantifiers.Decide(kind, renamed, set, simplified, isExact);
                if (verdict is null)
                    ProofRecording.Rollback(mark);
                // A body the facts made True is recorded as it was written: the decision's own
                // step would otherwise read `forall k in ZZ : True`.
                else if (kind == Quantifiers.Kind.All && simplified == Entity.Boolean.True)
                    ProofRecording.Restate(mark, new Forallf(renamed, set, written),
                        written is Impliesf(var hypothesis, var claim)
                            ? $"{claim} holds for every {renamed} in {set} with {hypothesis}"
                            : $"the body holds for every {renamed} in {set}",
                        "intro");
                ProofRecording.Rename(mark, renamed, x);
                return (verdict is null ? null : Renamed(verdict, renamed, x), Renamed(simplified, renamed, x));
            }
            finally
            {
                top = enclosing;
            }
        }

        /// <summary>
        /// What membership in <paramref name="set"/> establishes about <paramref name="name"/>:
        /// the membership, and for a set builder <c>{ y in S : P }</c> membership in <c>S</c> and
        /// each conjunct of <c>P</c> as well, so that <c>x in RR \ ZZ</c>, which is
        /// <c>{ x in RR : not x in ZZ }</c>, says that <c>x</c> is real and not whole.
        /// </summary>
        private static Entity[] MembershipFacts(Variable name, Set set)
            => set is ConditionalSet { DeclaredMembership: (Set declared, var condition), Var: Variable y }
                // A conjunct about the free names alone is left out, as a hypothesis's is.
                ? new[] { name.In(set), name.In(declared) }.Concat(Andf.LinearChildren(condition.Substitute(y, name)).Where(fact => fact.ContainsNode(name))).ToArray()
                : new[] { name.In(set) };

        /// <summary>
        /// The body simplified with the facts in scope: a claim made under a hypothesis with the
        /// conjuncts of the hypothesis in scope as well, and the hypothesis itself with only the
        /// facts from outside it.
        /// </summary>
        private static Entity Simplified(Quantifiers.Kind kind, Entity body, bool isExact)
        {
            switch (kind, body)
            {
                // forall x in S : H implies C, and H1 implies (H2 implies C) with both in scope for C.
                case (Quantifiers.Kind.All, Impliesf(var hypothesis, var claim)):
                {
                    var assumed = hypothesis.InnerSimplified(isExact);
                    var enclosing = Establish(assumed);
                    Entity concluded;
                    try
                    {
                        concluded = Simplified(kind, claim, isExact);
                    }
                    finally
                    {
                        top = enclosing;
                    }
                    return new Impliesf(assumed, concluded).InnerSimplified(isExact);
                }
                // forall x in S : P or not Q is forall x in S : Q implies P, so P is simplified with
                // the conjuncts of Q in scope as a claim is, and Q with only the facts from outside
                // it: floor(x) + ceil(x) = 2 x or not x in ZZ reads floor(x) as x. Sullivan and
                // Mackey's Prob 1.5.6 written as a disjunction.
                // https://github.com/asc-community/AngouriMath/issues/1409
                case (Quantifiers.Kind.All, Orf(var either, var other)) when other is Notf || either is Notf:
                {
                    var (claim, hypothesis) = other is Notf(var negatedLast) ? (either, negatedLast) : (other, ((Notf)either).Argument);
                    var assumed = hypothesis.InnerSimplified(isExact);
                    var enclosing = Establish(assumed);
                    Entity concluded;
                    try
                    {
                        concluded = Simplified(kind, claim, isExact);
                    }
                    finally
                    {
                        top = enclosing;
                    }
                    return (other is Notf ? new Orf(concluded, new Notf(assumed)) : new Orf(new Notf(assumed), concluded)).InnerSimplified(isExact);
                }
                // exists x in S : A and B and C, each conjunct with the ones before it in scope.
                case (not Quantifiers.Kind.All, Andf):
                {
                    var conjuncts = new List<Entity>();
                    var enclosing = top;
                    try
                    {
                        foreach (var conjunct in Andf.LinearChildren(body))
                        {
                            var simplified = conjunct.InnerSimplified(isExact);
                            conjuncts.Add(simplified);
                            Establish(simplified);
                        }
                    }
                    finally
                    {
                        top = enclosing;
                    }
                    return conjuncts.Aggregate((left, right) => new Andf(left, right)).InnerSimplified(isExact);
                }
                default:
                    return body.InnerSimplified(isExact);
            }
        }

        /// <summary>
        /// Puts in scope the conjuncts of <paramref name="statement"/> that mention a renamed
        /// name, and returns what was in scope before.
        /// </summary>
        private static Frame? Establish(Entity statement)
        {
            var enclosing = top;
            var facts = Andf.LinearChildren(statement).Where(MentionsARenamedName).ToArray();
            if (facts.Length > 0)
                top = new Frame(null, facts, enclosing);
            return enclosing;
        }

        /// <summary>
        /// The expression with <paramref name="from"/> read as <paramref name="to"/> everywhere,
        /// the name a quantifier binds included, which a substitution leaves alone.
        /// </summary>
        internal static Entity Renamed(Entity expression, Variable from, Variable to)
            => expression.Replace(node => node switch
            {
                Variable name when name == from => to,
                Quantifier quantifier when quantifier.Var == from => quantifier.New(to, quantifier.Over, quantifier.Body),
                _ => node
            });

        /// <summary>
        /// Whether a rule reads the facts at <paramref name="node"/>, which is what a body is
        /// renamed and decided with the facts in scope for: a divisibility, a congruence or an
        /// equation of residues by a modulus that is not a number, which the rules modulo a
        /// prime read; and a floor or a ceiling of anything but a number, which
        /// <see cref="Rounded"/> reads.
        /// </summary>
        private static bool Reads(Entity node) => node switch
        {
            // A membership in a set of whole numbers, which MembershipAsComparison reads, and a
            // quantifier over one, whose listed witnesses are asked about such memberships.
            Inf(not Number, SpecialSet whole) => Quantifiers.IsIntegerSet(whole),
            Quantifier(_, SpecialSet over, _) => Quantifiers.IsIntegerSet(over),
            Dividesf(not Number, _) or Congruentf(_, _, not Number) => true,
            Equalsf(Modf(_, var modulus), Modf(_, var other)) => modulus is not Number && modulus == other,
            Floorf(not Number) or Ceilf(not Number) => true,
            // A principal root of a whole power, which PrincipalPowerOfAPower reads.
            Powf(Powf(not Number, Integer), Rational) => true,
            _ => false
        };

        private static bool MentionsARenamedName(Entity expression)
        {
            if (top is null)
                return false;
            foreach (var name in expression.Vars)
                for (var frame = top; frame is not null; frame = frame.Next)
                    if (frame.Renamed is { } renamed && renamed == name)
                        return true;
            return false;
        }

        private static IEnumerable<Entity> InScope()
        {
            for (var frame = top; frame is not null; frame = frame.Next)
                foreach (var fact in frame.Facts)
                    yield return fact;
        }

        /// <summary>
        /// Whether <c>p divides binomial(p, k)</c> follows from the facts in scope: <c>p</c> is
        /// prime and <c>k</c> is a whole number with <c>0 &lt; k &lt; p</c>. Then <c>p</c> divides
        /// <c>p! = binomial(p, k) k! (p - k)!</c> and none of the factors of <c>k! (p - k)!</c>,
        /// each of which is below the prime <c>p</c>. Sullivan and Mackey's Prob 8.9.24.
        /// </summary>
        internal static bool PrimeDividesItsBinomial(Entity p, Entity k)
        {
            if (top is null || !IsPrime(p) || !IsWhole(k) || !IsPositive(k) || !IsPositive(p - k))
                return false;
            if (ProofRecording.Recording)
                ProofRecording.AddBelow(new Dividesf(p, new Binomialf(p, k)),
                    $"{p} is prime and 0 < {k} < {p}, so {p} divides {p}! = binomial({p}, {k}) {k}! ({p} - {k})! and none of the factors of {k}! ({p} - {k})!",
                    "Nat.Prime.dvd_choose_self", Entity.Boolean.True);
            return true;
        }

        /// <summary>
        /// <c>s in S</c> for a set of whole numbers, as the comparison it is where the facts in scope
        /// make <c>s</c> a whole number: <c>s &gt;= 0</c> in <c>ZZ*</c>, <c>s &gt;= 1</c> in <c>ZZ+</c>, and
        /// <c>True</c> in <c>ZZ</c>; and <c>|s|</c> as <c>not s = 0</c> in <c>ZZ+</c> and <c>True</c> in the
        /// other two. <see langword="null"/> otherwise. Sullivan and Mackey's Prob
        /// 4.11.6 asks for a <c>z</c> in <c>ZZ*</c> with <c>x - y = z</c> or <c>y - x = z</c>, for whole
        /// <c>x</c> and <c>y</c>: the witnesses are <c>x - y</c> and <c>y - x</c>, one of which is
        /// not negative.
        /// </summary>
        internal static Entity? MembershipAsComparison(Entity element, Set set)
        {
            if (top is null || element is Number || !MentionsARenamedName(element))
                return null;
            if (IsWholePolynomial(element))
                return set switch
                {
                    SpecialSet.Integers => Entity.Boolean.True,
                    SpecialSet.NonNegativeIntegers => element >= Integer.Zero,
                    SpecialSet.PositiveIntegers => element >= Integer.One,
                    _ => null,
                };
            // The modulus of a whole quantity is whole and not negative, and positive where the
            // quantity is not zero: |2 z + 1| is in ZZ+ for every whole z, since 2 z + 1 = 0 has
            // no whole root. Sullivan and Mackey's Ex 7.2.6, where f(z) = |2 z + 1| is a function
            // from ZZ to their N. https://github.com/asc-community/AngouriMath/issues/1409
            if (element is Absf(var quantity) && IsWholePolynomial(quantity))
                return set switch
                {
                    SpecialSet.Integers or SpecialSet.NonNegativeIntegers => Entity.Boolean.True,
                    SpecialSet.PositiveIntegers => !quantity.Equalizes(Integer.Zero),
                    _ => null,
                };
            // A whole quantity over a whole number m is whole where m divides it, and then has
            // the sign of the quantity: (13 - 3 a)/8 in ZZ* is 8 divides 13 - 3 a and
            // 13 - 3 a >= 0. That is what an equation 13 = 3 a + 8 b leaves of b, so the
            // quantifier over a then ranges over the a with 3 a <= 13 -- Sullivan and Mackey's
            // coins, Prob 2.7.8. https://github.com/asc-community/AngouriMath/issues/1409
            if (WholeOverAWholeNumber(element) is not (var numerator, var denominator))
                return null;
            var divides = new Dividesf(denominator, numerator);
            return set switch
            {
                SpecialSet.Integers => divides,
                SpecialSet.NonNegativeIntegers => divides & (numerator >= Integer.Zero),
                SpecialSet.PositiveIntegers => divides & (numerator >= denominator),
                _ => null,
            };
        }

        /// <summary>
        /// <paramref name="element"/> as a whole quantity over a whole number above one, with the
        /// facts in scope: <c>(13 - 3 a)/8</c> or <c>13/8 - 3/8 a</c> for a whole <c>a</c> is
        /// <c>13 - 3 a</c> over <c>8</c>. <see langword="null"/> where it is not one.
        /// </summary>
        private static (Entity Numerator, Integer Denominator)? WholeOverAWholeNumber(Entity element)
        {
            var denominator = EInteger.One;
            foreach (var node in element.Nodes)
            {
                var divisor = node switch
                {
                    Rational { ERational.Denominator: var below } and not Integer => below.Abs(),
                    Divf(_, Integer { EInteger: var by }) when !by.IsZero => by.Abs(),
                    _ => EInteger.One,
                };
                denominator = denominator.Divide(denominator.GreatestCommonDivisor(divisor)).Multiply(divisor);
            }
            if (denominator.Equals(EInteger.One))
                return null;
            var over = Integer.Create(denominator);
            var numerator = (element * over).Expand().InnerSimplified;
            return IsWholePolynomial(numerator) ? (numerator, over) : null;
        }

        /// <summary>
        /// Whether <c>p divides d</c> by the binomial theorem, with the facts in scope: <c>p</c> is
        /// prime, <c>d</c> is a polynomial with whole coefficients in whole quantities, and
        /// writing each power <c>(u + v)^p</c> in it as <c>u^p + v^p</c> leaves nothing, or only
        /// multiples of <c>p</c>. What the binomial theorem adds to <c>u^p + v^p</c> is
        /// <c>binomial(p, k) u^k v^(p - k)</c> for <c>0 &lt; k &lt; p</c>, each a multiple of
        /// <c>p</c> by <see cref="PrimeDividesItsBinomial"/>. That is asked of the quantifiers
        /// first, and recorded as the step this one rests on. A difference is read the same way,
        /// since <c>(-v)^p</c> is <c>-v^p</c> modulo every prime, 2 included. Sullivan and
        /// Mackey's Prob 8.9.25, the freshman's dream.
        /// </summary>
        internal static bool PrimeDividesByTheBinomialTheorem(Entity p, Entity d)
        {
            if (top is null || !IsPrime(p) || !d.Nodes.Any(node => node is Powf(Sumf or Minusf, var exponent) && exponent == p))
                return false;
            var dreamt = d.Replace(node => node is Powf(Sumf or Minusf, var exponent) && exponent == p ? PowersOfTheTerms(((Powf)node).Base, p) : node);
            // Read as polynomials over the same atoms, each of which must be the p-th power of a
            // whole quantity for a congruence modulo p to carry through a product with it.
            var atoms = new Dictionary<Entity, Variable>();
            var unfolded = new Dictionary<Entity, Entity>();
            var before = Quantifiers.Atomized(d, atoms, unfolded);
            var after = Quantifiers.Atomized(dreamt, atoms, unfolded);
            foreach (var atom in atoms.Keys)
                if (atom is not Powf(var @base, var exponent) || exponent != p || !IsWholePolynomial(@base))
                    return false;
            var names = before.Vars.Concat(after.Vars).Distinct().ToArray();
            if (names.Length > MultivariatePolynomial.MaxVariables || names.Any(name => !atoms.ContainsValue(name) && !IsWhole(name)))
                return false;
            var indices = new Dictionary<Variable, int>();
            for (var i = 0; i < names.Length; i++)
                indices[names[i]] = i;
            if (MultivariatePolynomial.TryParse(before, indices) is not { HasIntegerCoefficients: true }
                || MultivariatePolynomial.TryParse(after, indices) is not { } left
                || !(left.IsZero || p is Variable modulus && indices.TryGetValue(modulus, out var at) && left.HasIntegerCoefficients && left.Terms.All(term => MultivariatePolynomial.PowerOf(term.Key, at) > 0)))
                return false;
            // The step this rests on, p divides binomial(p, k) for 0 < k < p, asked of the
            // quantifiers with the facts that p is prime already in scope.
            var k = (d + p).Vars.Any(name => name.Name == "k") ? Variable.CreateUnique(d + p, "k") : MathS.Var("k");
            ProofRecording.Enter();
            Entity binomials;
            try
            {
                binomials = new Forallf(k, MathS.Sets.Z, (Integer.Zero < k & k < p).Implies(new Dividesf(p, new Binomialf(p, k)))).InnerSimplified;
            }
            finally
            {
                ProofRecording.Leave();
            }
            if (binomials != Entity.Boolean.True)
                return false;
            if (ProofRecording.Recording)
                ProofRecording.AddBelow(new Dividesf(p, d),
                    $"by the binomial theorem each ({string.Join("), (", d.Nodes.OfType<Powf>().Where(power => power.Base is Sumf or Minusf && power.Exponent == p).Select(power => power.Base).Distinct())})^{p} is the sum of the {p}-th powers of its terms and multiples of {p}, and what is left of {d} is a multiple of {p}",
                    "add_pow_char", Entity.Boolean.True);
            return true;
        }

        /// <summary>
        /// The floor of <paramref name="argument"/>, or with <paramref name="up"/> its ceiling, as
        /// the facts in scope write it, or <see langword="null"/> where they say nothing about it.
        /// A whole argument is its own floor and ceiling. A whole term comes out of either,
        /// <c>floor(y + n) = floor(y) + n</c>. A negated argument turns one into the other,
        /// <c>floor(-y) = -ceil(y)</c>. And the ceiling of a real argument that is not whole is one
        /// above its floor, which is the form the others end in. The nodes are taken
        /// componentwise on the complex plane, where the first three hold as they do on the real
        /// line and the last does not, so it asks that the argument be real: the ceiling of
        /// <c>i/2</c> is <c>i</c> and its floor is <c>0</c>. Sullivan and Mackey's Prob 1.5.6,
        /// <c>floor(x) + floor(1 - x)</c>, is <c>1</c> for a whole <c>x</c> and <c>0</c> for any
        /// other real one.
        /// </summary>
        internal static Entity? Rounded(Entity argument, bool up, bool isExact)
        {
            if (top is null || argument is Number || !MentionsARenamedName(argument))
                return null;
            Entity Round(Entity of, bool ceiling) => ceiling ? new Ceilf(of) : new Floorf(of);
            if (IsWholeNumber(argument))
                return Recorded(Round(argument, up), argument, $"{argument} is a whole number", up ? "Int.ceil_intCast" : "Int.floor_intCast");
            var terms = Sumf.LinearChildren(argument);
            if (terms.Count > 1 && terms.Any(IsWholeNumber) && !terms.All(IsWholeNumber))
            {
                var whole = terms.Where(IsWholeNumber).Aggregate((left, right) => left + right);
                var rest = terms.Where(term => !IsWholeNumber(term)).Aggregate((left, right) => left + right);
                return Recorded(Round(argument, up), Round(rest, up) + whole, $"{whole} is a whole number",
                    up ? "Int.ceil_add_intCast" : "Int.floor_add_intCast").InnerSimplified(isExact);
            }
            if (argument is Mulf(Integer { IsNegative: true } coefficient, var factor))
            {
                var negated = coefficient.EInteger.Equals(EInteger.FromInt32(-1)) ? factor : Integer.Create(coefficient.EInteger.Negate()) * factor;
                return Recorded(Round(argument, up), -Round(negated, !up), $"{argument} is the negation of {negated}",
                    up ? "Int.ceil_neg" : "Int.floor_neg").InnerSimplified(isExact);
            }
            if (up && IsRealNumber(argument) && IsNotWholeNumber(argument))
                return Recorded(Round(argument, up), Round(argument, false) + Integer.One, $"{argument} is real and not a whole number",
                    "Int.ceil_eq_floor_add_one_iff_notMem").InnerSimplified(isExact);
            return null;
        }

        /// <summary>
        /// The function of <paramref name="argument"/> with each term of it that is a whole number of
        /// periods by the facts in scope taken out, or <see langword="null"/>: <c>sin(x + 2 pi n)</c> is
        /// <c>sin(x)</c> for a whole <c>n</c>, as <c>Complex.sin_add_int_mul_two_pi</c> has it. The period is
        /// <paramref name="halfTurns"/> times <c>pi</c>: two for the sine, the cosine, the secant and the
        /// cosecant, and one for the tangent and the cotangent, as <c>Complex.tan_add_int_mul_pi</c> has it.
        /// https://github.com/asc-community/AngouriMath/issues/212
        /// </summary>
        internal static Entity? WithoutWholePeriods(Entity argument, int halfTurns, System.Func<Entity, Entity> function, string lemma)
        {
            if (top is null || argument is Number || !MentionsARenamedName(argument))
                return null;
            // A term of the argument, or of its multiple of pi where it is gathered into one,
            // such as pi * (2 n + 1/6).
            var kept = new List<Entity>();
            Entity? periods = null;
            foreach (var term in Sumf.LinearChildren(argument))
            {
                if (!MentionsARenamedName(term) || MathS.UnsafeAndInternal.DivideByEntityStrict(term, MathS.pi) is not { } multiple)
                {
                    kept.Add(term);
                    continue;
                }
                var parts = Sumf.LinearChildren(multiple);
                foreach (var part in parts)
                    if (MentionsARenamedName(part) && IsWholeNumber(Rational.Create(1, halfTurns) * part))
                        periods = periods is null ? part * MathS.pi : periods + part * MathS.pi;
                    else
                        kept.Add(parts.Count == 1 ? term : part * MathS.pi);
            }
            if (periods is null)
                return null;
            var rest = kept.Count == 0 ? Integer.Zero : kept.Aggregate((left, right) => left + right);
            return Recorded(function(argument), function(rest), $"{periods} is a whole number of periods", lemma);
        }

        /// <summary>
        /// <c>z</c> for the principal root <c>(z^n)^(1/n)</c> of a whole <c>n &gt;= 2</c>, where the facts in
        /// scope put <c>z</c> where that root takes its values, or <see langword="null"/>: at <c>0</c> or in
        /// the sector <c>-pi/n &lt; arg(z) &lt;= pi/n</c>, which is how the power's inversion states it, or on
        /// the half-line <c>z &gt;= 0</c>, as <c>Complex.pow_cpow_nat_inv</c> has it. So <c>sqrt(t^2)</c> is
        /// <c>t</c> where <c>t = 0 or -pi/2 &lt; arg(t) &lt;= pi/2</c>.
        /// https://github.com/asc-community/AngouriMath/issues/212
        /// </summary>
        internal static Entity? PrincipalPowerOfAPower(Entity z, Entity inner, Entity exponent)
        {
            if (top is null || !MentionsARenamedName(z)
                || inner is not Integer { EInteger: var n } || n.CompareTo(2) < 0
                || exponent is not Rational { ERational: var p } || p.CompareTo(ERational.Create(EInteger.One, n)) != 0)
                return null;
            foreach (var fact in InScope())
                if (IsOnTheHalfLine(fact, z) || IsInTheSector(fact, z, exponent))
                    return Recorded(MathS.Pow(MathS.Pow(z, inner), exponent), z,
                        $"{z} is where the principal {inner}-th root takes its values", "Complex.pow_cpow_nat_inv");
            return null;

            static bool IsOnTheHalfLine(Entity fact, Entity z)
                => fact switch
                {
                    GreaterOrEqualf(var left, Integer { IsZero: true }) => left == z,
                    Greaterf(var left, Integer { IsZero: true }) => left == z,
                    LessOrEqualf(Integer { IsZero: true }, var right) => right == z,
                    Lessf(Integer { IsZero: true }, var right) => right == z,
                    _ => false
                };

            // z = 0 or (-p pi < arg(z) and arg(z) <= p pi), the two sides of each in either order.
            static bool IsInTheSector(Entity fact, Entity z, Entity p)
            {
                if (fact is not Orf(var either, var other))
                    return false;
                var (zero, sector) = IsZero(either, z) ? (either, other) : (other, either);
                if (!IsZero(zero, z) || sector is not Andf(var first, var second))
                    return false;
                var bound = p * MathS.pi;
                return Below(first, -bound) && Above(second, bound) || Below(second, -bound) && Above(first, bound);

                bool Below(Entity side, Entity lower)
                    => side is Lessf(var low, Argf(var of)) && of == z && Same(low, lower)
                       || side is Greaterf(Argf(var on), var low2) && on == z && Same(low2, lower);
                bool Above(Entity side, Entity upper)
                    => side is LessOrEqualf(Argf(var of), var high) && of == z && Same(high, upper)
                       || side is GreaterOrEqualf(var high2, Argf(var on)) && on == z && Same(high2, upper);
            }
            static bool IsZero(Entity statement, Entity z)
                => statement is Equalsf(var left, var right) && (left == z && right == 0 || right == z && left == 0);
            // The bounds are multiples of pi, so they are compared by value.
            static bool Same(Entity a, Entity b)
                => !a.Vars.Any() && (a - b).Evaled is Complex difference && ((Real)difference.Abs()).EDecimal.ToDouble() < 1e-12;
        }

        /// <summary>A function <see cref="WithoutWholePeriods"/> takes whole periods out of, of anything but a number.</summary>
        private static bool IsPeriodic(Entity node)
            => node is Sinf(not Number) or Cosf(not Number) or Tanf(not Number) or Cotanf(not Number) or Secantf(not Number) or Cosecantf(not Number);

        /// <summary><paramref name="to"/>, with the step from <paramref name="from"/> recorded where a proof is.</summary>
        private static Entity Recorded(Entity from, Entity to, string rule, string lemma)
        {
            if (ProofRecording.Recording)
                ProofRecording.AddBelow(new Equalsf(from, to), rule, lemma, Entity.Boolean.True);
            return to;
        }

        /// <summary>Whether the facts in scope make <paramref name="expression"/> a whole number.</summary>
        private static bool IsWholeNumber(Entity expression)
            => expression is Integer || MentionsARenamedName(expression) && IsWholePolynomial(expression);

        /// <summary>
        /// Whether the facts in scope make <paramref name="expression"/> real: a polynomial with
        /// rational coefficients in names each of which is in <c>RR</c> or a set inside it.
        /// </summary>
        private static bool IsRealNumber(Entity expression)
        {
            var names = expression.Vars.Distinct().ToArray();
            if (names.Length == 0 || names.Length > MultivariatePolynomial.MaxVariables || names.Any(name => !IsReal(name)))
                return false;
            var indices = new Dictionary<Variable, int>();
            for (var i = 0; i < names.Length; i++)
                indices[names[i]] = i;
            return MultivariatePolynomial.TryParse(expression, indices) is not null;
        }

        /// <summary>A name in <c>RR</c> or a set of numbers inside it, rank 5 by <see cref="Rank"/>.</summary>
        private static bool IsReal(Entity name) => IsIn(name, 5);

        /// <summary>Whether a fact in scope says that <paramref name="expression"/> is not a whole number: <c>not x in ZZ</c>.</summary>
        private static bool IsNotWholeNumber(Entity expression)
            => MentionsARenamedName(expression) && InScope().Any(fact => fact is Notf(Inf(var member, SpecialSet.Integers)) && member == expression);

        /// <summary>The p-th powers of the terms of a sum, added: <c>(u - v)^p</c> is read as <c>u^p - v^p</c>.</summary>
        private static Entity PowersOfTheTerms(Entity sum, Entity p)
            => Sumf.LinearChildren(sum)
                .Select(term => term switch
                {
                    Mulf(Integer { EInteger: var minusOne }, var rest) when minusOne.Equals(EInteger.FromInt32(-1)) => -rest.Pow(p).InnerSimplified,
                    Integer { IsNegative: true } negative => -((Entity)(-negative)).Pow(p).InnerSimplified,
                    _ => term.Pow(p).InnerSimplified
                })
                .Aggregate((left, right) => left + right);

        /// <summary>Whether the expression is a polynomial with whole coefficients in names that are whole by the facts in scope.</summary>
        private static bool IsWholePolynomial(Entity expression)
        {
            var names = expression.Vars.Distinct().ToArray();
            if (names.Length > MultivariatePolynomial.MaxVariables || names.Any(name => !IsWhole(name)))
                return false;
            var indices = new Dictionary<Variable, int>();
            for (var i = 0; i < names.Length; i++)
                indices[names[i]] = i;
            return MultivariatePolynomial.TryParse(expression, indices) is { HasIntegerCoefficients: true };
        }

        private static bool IsPrime(Entity expression)
            => expression is Integer number ? Functions.Primes.IsPrime(number.EInteger) == true : IsIn(expression, 0);

        private static bool IsWhole(Entity expression)
            => expression is Integer || IsIn(expression, 3);

        /// <summary>
        /// Whether the facts in scope put <paramref name="element"/> in the set of numbers ranked
        /// <paramref name="rank"/> by <see cref="Rank"/>, or in one inside it.
        /// </summary>
        private static bool IsIn(Entity element, int rank)
        {
            if (!MentionsARenamedName(element))
                return false;
            foreach (var fact in InScope())
                if (fact is Inf(var member, SpecialSet within) && member == element && Rank(within) is var inner and >= 0 && inner <= rank)
                    return true;
            return false;
        }

        /// <summary>The sets of numbers, each inside the next: PP, ZZ+, ZZ*, ZZ, QQ, RR, CC.</summary>
        private static int Rank(SpecialSet set) => set switch
        {
            SpecialSet.Primes => 0,
            SpecialSet.PositiveIntegers => 1,
            SpecialSet.NonNegativeIntegers => 2,
            SpecialSet.Integers => 3,
            SpecialSet.Rationals => 4,
            SpecialSet.Reals => 5,
            SpecialSet.Complexes => 6,
            _ => -1
        };

        /// <summary>
        /// Whether <paramref name="expression"/> is positive by the facts in scope. Each fact bounds
        /// a quantity below: <c>a &gt; b</c> and <c>b &lt; a</c> say <c>a - b &gt; 0</c>, and a
        /// member of the primes, the positive or the non-negative whole numbers is at least 2, 1
        /// or 0. The expression is positive where it is such a quantity plus a constant that is
        /// not negative, or plus a positive one where the bound is not strict: <c>p - 1</c> for a
        /// prime <c>p</c> is <c>p - 2</c> plus 1.
        /// </summary>
        private static bool IsPositive(Entity expression)
        {
            if (expression is Real number)
                return number.IsPositive;
            if (!MentionsARenamedName(expression))
                return false;
            foreach (var fact in InScope())
            {
                var bound = fact switch
                {
                    Lessf(var smaller, var greater) => (Difference: greater - smaller, Strict: true),
                    Greaterf(var greater, var smaller) => (greater - smaller, true),
                    LessOrEqualf(var smaller, var greater) => (greater - smaller, false),
                    GreaterOrEqualf(var greater, var smaller) => (greater - smaller, false),
                    Inf(var member, SpecialSet.Primes) => (member - 2, false),
                    Inf(var member, SpecialSet.PositiveIntegers) => (member - 1, false),
                    Inf(var member, SpecialSet.NonNegativeIntegers) => (member, false),
                    _ => ((Entity Difference, bool Strict)?)null
                };
                if (bound is var (difference, strict) && Gap(expression, difference) is { } gap
                    && (strict ? gap.Sign >= 0 : gap.Sign > 0))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// The constant <c>a - b</c>, where the two differ by one as polynomials, and
        /// <see langword="null"/> otherwise.
        /// </summary>
        private static ERational? Gap(Entity a, Entity b)
        {
            var difference = a - b;
            var names = difference.Vars.Distinct().ToArray();
            if (names.Length > MultivariatePolynomial.MaxVariables)
                return null;
            var indices = new Dictionary<Variable, int>();
            for (var i = 0; i < names.Length; i++)
                indices[names[i]] = i;
            if (MultivariatePolynomial.TryParse(difference, indices) is not { IsConstant: true } constant)
                return null;
            return constant.IsZero ? ERational.Zero : constant.Terms.Single().Value;
        }
    }
}
