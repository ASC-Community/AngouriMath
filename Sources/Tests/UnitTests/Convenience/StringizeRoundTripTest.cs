//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// What <see cref="Entity.Stringize()"/> is for: parsing what it prints gives back the
    /// expression it printed. Anything else makes the printed form a lie, and a silent one,
    /// since a wrong reading is still a valid expression.
    /// </summary>
    [Trait("Area", "Convenience")]
    public class StringizeRoundTripTest
    {
        /// <summary>
        /// Whether to print under <see cref="MathS.Settings.UnicodeOutput"/>, which
        /// <see cref="UnicodeStringizeRoundTripTest"/> turns on to run every case here again.
        /// </summary>
        protected virtual bool Unicode => false;

        private protected string Print(Entity expression)
        {
            using var _ = MathS.Settings.UnicodeOutput.Set(Unicode);
            return expression.Stringize();
        }

        private void AssertRoundTrip(string source)
        {
            var original = source.ToEntity();
            var printed = Print(original);
            Assert.Equal(original, printed.ToEntity());
        }

        /// <summary>
        /// Powers group to the right, so it is the base that needs bracketing when it is a power
        /// of its own -- the mirror of what the left-associative operators need. Printing
        /// (2 ^ 3) ^ 2 as 2 ^ 3 ^ 2 did not merely look wrong: the first is 64 and the second is
        /// 512, so the printed form was a different expression.
        /// </summary>
        [Theory]
        [InlineData("(x ^ y) ^ z")]
        [InlineData("((x ^ y) ^ z) ^ w")]
        [InlineData("x ^ (y ^ z)")]
        [InlineData("x ^ y ^ z")]
        [InlineData("(2 ^ 2) ^ 3")]
        [InlineData("2 ^ 2 ^ 3")]
        [InlineData("(x + 1) ^ (y + 1)")]
        [InlineData("(-x) ^ 2")]
        [InlineData("2 ^ (-x)")]
        public void PowersKeepTheirGrouping(string source) => AssertRoundTrip(source);

        [Fact]
        public void ANestedPowerKeepsItsValue()
        {
            var original = "(2 ^ 3) ^ 2".ToEntity();
            Assert.Equal(original.Evaled, Print(original).ToEntity().Evaled);
        }

        /// <summary>
        /// These three used to be printed in spellings the parser does not have. A lambda came
        /// back as an implication, an application as a power, and a piecewise as a product with
        /// "if" read as an undeclared variable -- or, with more than one case, as a parse error.
        /// </summary>
        [Theory]
        [InlineData("lambda(x, x + 1)")]
        [InlineData("lambda(x, lambda(y, x + y))")]
        [InlineData("apply(lambda(x, x + 1), 2)")]
        [InlineData("apply(lambda(x, lambda(y, x + y)), 1, 2)")]
        [InlineData("piecewise(1 provided x > 0)")]
        [InlineData("piecewise(1 provided x > 0, 2 provided x < 0)")]
        [InlineData("piecewise(x + 1 provided x > 0, x - 1 provided x < 0)")]
        public void TheNodesWithoutAnOperatorSpellingPrintAsTheirFunction(string source) =>
            AssertRoundTrip(source);

        [Theory]
        [InlineData("x + y")]
        [InlineData("x - y")]
        [InlineData("x * y")]
        [InlineData("x / y")]
        [InlineData("-x")]
        [InlineData("x!")]
        [InlineData("(x + y) * z")]
        [InlineData("x + y * z")]
        [InlineData("(x - y) - z")]
        [InlineData("x - (y - z)")]
        [InlineData("x / y / z")]
        [InlineData("x / (y / z)")]
        [InlineData("-(x + y)")]
        public void ArithmeticKeepsItsGrouping(string source) => AssertRoundTrip(source);

        [Theory]
        [InlineData("sin(x)")]
        [InlineData("cos(x)")]
        [InlineData("tan(x)")]
        [InlineData("cotan(x)")]
        [InlineData("sec(x)")]
        [InlineData("cosec(x)")]
        [InlineData("arcsin(x)")]
        [InlineData("arccos(x)")]
        [InlineData("arctan(x)")]
        [InlineData("arccotan(x)")]
        [InlineData("arcsec(x)")]
        [InlineData("arccosec(x)")]
        [InlineData("sinh(x)")]
        [InlineData("cosh(x)")]
        [InlineData("tanh(x)")]
        [InlineData("cotanh(x)")]
        [InlineData("sech(x)")]
        [InlineData("cosech(x)")]
        [InlineData("ln(x)")]
        [InlineData("log(2, x)")]
        [InlineData("sqrt(x)")]
        [InlineData("abs(x)")]
        [InlineData("signum(x)")]
        [InlineData("phi(x)")]
        [InlineData("gamma(x)")]
        [InlineData("derivative(x ^ 2, x, 1)")]
        [InlineData("limit(x, x, 0)")]
        [InlineData("limitleft(x, x, 0)")]
        [InlineData("limitright(x, x, 0)")]
        [InlineData("dot(a, b)")]
        [InlineData("cross(a, b)")]
        [InlineData("sum(x ^ 2, x in S)")]
        [InlineData("product(x ^ 2, x in S)")]
        [InlineData("product(1 - 1 / p ^ 2, p in { 2, 3, 5 })")]
        [InlineData("factorization(n)")]
        [InlineData("primefactors(n)")]
        [InlineData("product(p ^ valuation(n, p), p in primefactors(n))")]
        public void FunctionsRoundTrip(string source) => AssertRoundTrip(source);

        [Theory]
        [InlineData("a and b")]
        [InlineData("a or b")]
        [InlineData("a xor b")]
        [InlineData("not a")]
        [InlineData("a implies b")]
        [InlineData("a and b or c")]
        [InlineData("a or b and c")]
        [InlineData("not (a and b)")]
        [InlineData("x > y")]
        [InlineData("x < y")]
        [InlineData("x >= y")]
        [InlineData("x <= y")]
        [InlineData("x = y")]
        [InlineData("x <> y")]
        [InlineData("x provided y")]
        public void BooleansRoundTrip(string source) => AssertRoundTrip(source);

        /// <summary>
        /// An operator that is <em>not</em> associative has to print the bracketing it has, because
        /// the flat form is read the way the grammar folds -- to the left for everything but
        /// <c>^</c> and <c>provided</c>. Printing <c>a implies (b implies c)</c> as
        /// <c>a implies b implies c</c> did not merely look wrong: it comes back as
        /// <c>(a implies b) implies c</c>, and the two do not agree
        /// (<c>false implies (true implies false)</c> is true, the other is false).
        /// </summary>
        /// <remarks>
        /// <c>\/</c> and <c>\</c> share one precedence level and are folded by the same loop, so a
        /// <c>\</c> on the right of a <c>\/</c> mis-associates too even though union on its own is
        /// associative; and <c>mod</c> shares a level with <c>*</c> and <c>/</c>, so a <c>mod</c> on
        /// the right of a <c>*</c> does the same.
        /// </remarks>
        [Theory]
        [InlineData("a implies (b implies c)")]
        [InlineData("(a implies b) implies c")]
        [InlineData("a implies b implies c")]
        [InlineData("a implies (b implies (c implies d))")]
        [InlineData("(a implies (b implies c)) implies d")]
        [InlineData("a implies (b or c)")]
        [InlineData(@"A \ (B \ C)")]
        [InlineData(@"(A \ B) \ C")]
        [InlineData(@"A \/ (B \ C)")]
        [InlineData(@"A \ (B \/ C)")]
        [InlineData(@"(A \/ B) \ C")]
        [InlineData(@"A \ (B /\ C)")]
        [InlineData(@"A /\ (B \ C)")]
        [InlineData("a in (b in c)")]
        [InlineData("(a in b) in c")]
        [InlineData("x * (y mod z)")]
        [InlineData("(x * y) mod z")]
        [InlineData("x mod (y * z)")]
        [InlineData("x / (y mod z)")]
        [InlineData("x mod (y mod z)")]
        // `provided` is the mirror case: it folds to the *right*, so it is the left operand that
        // mis-associates. Its value survives either reading -- `(x provided p) provided q` and
        // `x provided (p provided q)` are both `x` exactly when `p` and `q` hold -- which is why
        // a check on the value alone passes it and a check on the expression does not.
        [InlineData("(x provided p) provided q")]
        [InlineData("x provided (p provided q)")]
        [InlineData("x provided p provided q")]
        [InlineData("((x provided p) provided q) provided r")]
        public void ANonAssociativeOperatorKeepsItsGrouping(string source) => AssertRoundTrip(source);

        /// <summary>
        /// The same defect stated in values rather than in trees: each of these is a case where
        /// reading the printed form back the way the grammar folds gives a different answer.
        /// </summary>
        [Theory]
        [InlineData("false implies (true implies false)")]
        [InlineData(@"{ 1, 2, 3 } \ ({ 2, 3 } \ { 3 })")]
        [InlineData(@"{ 1, 2 } \ ({ 2 } \/ { 3 })")]
        [InlineData(@"{ 1, 2 } \/ ({ 3 } \ { 1, 2 })")]
        [InlineData("2 * (3 mod 2)")]
        [InlineData("(3 in { 3 }) in BB")]
        public void AMisreadGroupingWouldChangeTheValue(string source)
        {
            var original = source.ToEntity();
            Assert.Equal(original.Evaled, Print(original).ToEntity().Evaled);
        }

        /// <summary>
        /// The other half of the rule, pinned so that it is a decision rather than an oversight:
        /// an operator that <em>is</em> associative prints flat, because the bracketing then
        /// carries no mathematics. <c>x + (y + z)</c> comes back as <c>(x + y) + z</c>, which is a
        /// different <see cref="Entity"/> and the same number -- and bracketing it instead would
        /// turn every expanded polynomial into a right-nested pile of parentheses.
        /// </summary>
        [Theory]
        [InlineData("1 + (2 + 3)", "1 + 2 + 3")]
        [InlineData("1 + (2 - 3)", "1 + 2 - 3")]
        [InlineData("2 * (3 * 4)", "2 * 3 * 4")]
        [InlineData("2 * (6 / 3)", "2 * 6 / 3")]
        [InlineData("true and (false and true)", "True and False and True")]
        [InlineData("true or (false or true)", "True or False or True")]
        [InlineData("true xor (false xor true)", "True xor False xor True")]
        [InlineData(@"{ 1 } \/ ({ 2 } \/ { 3 })", @"{ 1 } \/ { 2 } \/ { 3 }")]
        [InlineData(@"{ 1, 2 } /\ ({ 2, 3 } /\ { 2 })", @"{ 1, 2 } /\ { 2, 3 } /\ { 2 }")]
        public void AnAssociativeOperatorPrintsFlatAndKeepsItsValue(string source, string expectedFlat)
        {
            var original = source.ToEntity();
            Assert.Equal(expectedFlat, original.Stringize());
            Assert.Equal(original.Evaled, Print(original).ToEntity().Evaled);
        }

        [Theory]
        [InlineData("{ 1, 2, 3 }")]
        [InlineData("[1; 2]")]
        [InlineData("(1; 2)")]
        [InlineData("[1; 2)")]
        [InlineData("A unite B")]
        [InlineData("A intersect B")]
        [InlineData("A setsubtract B")]
        [InlineData("x in A")]
        [InlineData("RR")]
        [InlineData("CC")]
        [InlineData("ZZ")]
        [InlineData("QQ")]
        [InlineData("BB")]
        public void SetsRoundTrip(string source) => AssertRoundTrip(source);

        [Theory]
        [InlineData("[[1, 2], [3, 4]]")]
        [InlineData("[1, 2, 3]")]
        [InlineData("1/2")]
        [InlineData("-1/2")]
        [InlineData("i")]
        [InlineData("-i")]
        [InlineData("2 + 3 * i")]
        [InlineData("+oo")]
        [InlineData("-oo")]
        [InlineData("e")]
        [InlineData("pi")]
        public void MatricesAndNumbersRoundTrip(string source) => AssertRoundTrip(source);

        /// <summary>
        /// The cases above round-trip the expression as written, so <c>i / 2</c> stays a
        /// division and never reaches the printer for a complex <em>number</em>. Evaluating
        /// first is what exercises that printer, and it printed a pure imaginary with a
        /// fractional part as <c>1/2i</c> -- which the parser reads as <c>1/(2i)</c>, the
        /// negation of what was printed. The mixed case was already right, bracketing it as
        /// <c>2 + (3/4)i</c>; the pure-imaginary arm returned before that bracketing was
        /// applied.
        ///
        /// Silent, and it misleads a reader as much as a parser: every root of a
        /// trigonometric equation is printed through this.
        /// </summary>
        [Theory]
        [InlineData("i / 2")]
        [InlineData("-i / 2")]
        [InlineData("3 * i / 4")]
        [InlineData("-5 * i / 3")]
        [InlineData("2 + 3 * i / 4")]
        [InlineData("2 - 3 * i / 4")]
        [InlineData("i")]
        [InlineData("-i")]
        [InlineData("2 * i")]
        [InlineData("-2 * i")]
        [InlineData("1/2")]
        [InlineData("2 + 3 * i")]
        public void AnEvaluatedComplexNumberRoundTrips(string source)
        {
            var value = source.ToEntity().Evaled;
            Assert.Equal(value, Print(value).ToEntity().Evaled);
        }

        /// <summary>
        /// Every value the library hands out as a named constant, printed and read back.
        /// </summary>
        /// <remarks>
        /// This is the round trip taken from the other end, and the direction the rest of this
        /// class structurally cannot see: every case above starts from a **string**, so it can only
        /// reach expressions the parser already produces. A value with no source form is invisible
        /// to all of them, however many cases are added.
        /// <para/>
        /// That is how <c>NaN</c> went unnoticed. It printed as <c>NaN</c>, the grammar had no such
        /// token, so reading it back gave a <em>variable</em> of that name -- which then cancelled
        /// and collected like any symbol, making <c>NaN - NaN</c> into <c>0</c> and
        /// <c>NaN / NaN</c> into <c>1 provided not NaN = 0</c>. Nothing on the page distinguished
        /// the two, since a variable named <c>NaN</c> prints as <c>NaN</c> as well.
        /// https://github.com/asc-community/AngouriMath/issues/906
        /// <para/>
        /// Enumerated by reflection rather than written out, so that a constant added to
        /// <see cref="MathS"/> or to <see cref="Entity.Number.Real"/> later is covered without
        /// anyone remembering this file exists. The names are the test data rather than the values,
        /// because xUnit wants its data serializable and an <see cref="Entity"/> is not.
        /// </remarks>
        public static IEnumerable<object[]> NamedConstants() =>
            ConstantsByName.Keys.Select(name => new object[] { name });

        private static readonly IReadOnlyDictionary<string, Entity> ConstantsByName =
            new[] { typeof(MathS), typeof(Entity.Number.Real) }
                .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
                .Where(field => typeof(Entity).IsAssignableFrom(field.FieldType))
                .ToDictionary(field => field.DeclaringType!.Name + "." + field.Name,
                              field => (Entity)field.GetValue(null)!);

        [Theory]
        [MemberData(nameof(NamedConstants))]
        public void ANamedConstantRoundTrips(string name)
        {
            var constant = ConstantsByName[name];
            var printed = Print(constant);
            Assert.Equal(constant, printed.ToEntity());
        }

        /// <summary>
        /// And the same for what those constants evaluate to, since a constant may print one way
        /// and its value another -- <c>MathS.oo</c> is a <see cref="Entity.Number.Real"/> already,
        /// but <c>pi</c> and <c>e</c> are variables that carry a numeric value.
        /// </summary>
        [Theory]
        [MemberData(nameof(NamedConstants))]
        public void AnEvaluatedNamedConstantRoundTrips(string name)
        {
            var value = ConstantsByName[name].Evaled;
            Assert.Equal(value, Print(value).ToEntity().Evaled);
        }

        /// <summary>
        /// The three non-finite reals together, since they are one family and only one of them was
        /// broken. Written out as well as enumerated above, because these are the values a caller
        /// meets by computing rather than by naming: <c>0/0</c> and <c>1/0</c> reach NaN, and a
        /// divergent limit reaches an infinity.
        /// </summary>
        [Theory]
        [InlineData("0/0")]
        [InlineData("1/0")]
        [InlineData("-1/0")]
        [InlineData("+oo")]
        [InlineData("-oo")]
        [InlineData("+oo - +oo")]
        [InlineData("1/0 + 1")]
        public void ANonFiniteValueRoundTrips(string source)
        {
            var value = source.ToEntity().Evaled;
            Assert.Equal(value, Print(value).ToEntity().Evaled);
        }

        /// <summary>
        /// Reserving the word costs the identifier, and only the exact spelling: a longer name that
        /// merely starts with it is still a variable, because the lexer takes the longest match.
        /// </summary>
        [Theory]
        [InlineData("NaNx")]
        [InlineData("NaN_1")]
        [InlineData("aNaN")]
        public void AWordContainingTheTokenIsStillAVariable(string source) =>
            Assert.IsType<Entity.Variable>(source.ToEntity());

        /// <summary>
        /// A narrowed codomain is part of the expression, so it is part of what has to come back.
        /// Nothing printed it, so <c>domain(x, ZZ)</c> printed as <c>x</c> and the annotation was
        /// gone -- silently, since <c>x</c> is a perfectly good expression.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1022">#1022</a>.
        /// <see cref="AngouriMath.Tests.Common.CodomainSurvivesPrintingTest"/> takes it across
        /// every node type; these are the shapes the issue names.
        /// </summary>
        [Theory]
        [InlineData("domain(x, ZZ)")]
        [InlineData("domain(x + 1, RR)")]
        [InlineData("domain(sqrt(-1), RR)")]
        [InlineData("domain([1, 2], RR)")]
        [InlineData("domain(a and b, BB)")]
        [InlineData("sin(domain(x, ZZ)) + domain(y, RR)")]
        [InlineData("2 * domain(x + 1, RR)")]
        [InlineData("domain(x, ZZ) ^ domain(y, RR)")]
        [InlineData("integral(domain(x, ZZ), x)")]
        [InlineData("{ domain(x, ZZ) : x > 0 }")]
        public void ANarrowedCodomainRoundTrips(string source) => AssertRoundTrip(source);

        /// <summary>
        /// A number prints as it is, whatever the downcasting setting when it is printed. A
        /// negative imaginary part was negated to be printed, which made a new number under the
        /// setting in force, so a number made with downcasting off printed as another one once it
        /// was back on: <c>1 - 0.333...i</c> as <c>1 - 1/3i</c>, which reads back as <c>1 + i/3</c>,
        /// and the imaginary part of <c>erfc(16 - 0.5i)</c> as 0.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1610">#1610</a>
        /// </summary>
        [Theory]
        [InlineData("1", "-0.3333333333333333333333333333333333333333")]
        [InlineData("2", "-0.5000000000000000000000000000001")]
        [InlineData("-2.8337075375928474951E-113", "-9.4899608506981397569E-114")]
        public void ANumberMadeWithoutDowncastingPrintsAsItIs(string real, string imaginary)
        {
            Entity.Number.Complex number;
            using (MathS.Settings.DowncastingEnabled.Set(false))
                number = Entity.Number.Complex.Create(PeterO.Numbers.EDecimal.FromString(real), PeterO.Numbers.EDecimal.FromString(imaginary));
            var printed = Print(number);
            var latex = number.Latexize();
            using (MathS.Settings.DowncastingEnabled.Set(false))
            {
                var read = MathS.FromString(printed, useCache: false).EvalNumerical();
                Assert.True(read.RealPart.EDecimal.CompareTo(number.RealPart.EDecimal) == 0
                    && read.ImaginaryPart.EDecimal.CompareTo(number.ImaginaryPart.EDecimal) == 0,
                    $"{printed} reads back as {read}");
            }
            Assert.Contains(PeterO.Numbers.EDecimal.FromString(imaginary).Negate() + @"\mathrm{i}", latex);
        }

        /// <summary>
        /// The shapes <see cref="MathS.Settings.UnicodeOutput"/> respells: an integer exponent,
        /// which it writes in superscript, beside the exponents it must not -- another power, a
        /// factorial, a non-integer -- and every operator it gives a glyph.
        /// https://github.com/asc-community/AngouriMath/issues/1242
        /// </summary>
        [Theory]
        [InlineData("x ^ 2")]
        [InlineData("x ^ (-1)")]
        [InlineData("x ^ 23")]
        [InlineData("x ^ (-10)")]
        [InlineData("2 * x ^ 2")]
        [InlineData("2 ^ 3")]
        [InlineData("(x + 1) ^ 2")]
        [InlineData("(x ^ 2) ^ 3")]
        [InlineData("x ^ 2 ^ 3")]
        [InlineData("x ^ 3!")]
        [InlineData("x ^ (-3)!")]
        [InlineData("x ^ (1/2)")]
        [InlineData("x ^ 2.5")]
        [InlineData("x ^ (2i)")]
        [InlineData("-x ^ 2")]
        [InlineData("(-x) ^ 2")]
        [InlineData("(-2) ^ 2")]
        [InlineData("x ^ 2 * y ^ 3")]
        [InlineData("domain(x, ZZ) ^ 2")]
        [InlineData("a - b")]
        [InlineData("-a")]
        [InlineData("2 - -3")]
        [InlineData("-(-2/3)")]
        [InlineData("1 - 2i")]
        [InlineData("-i")]
        [InlineData("1E-20")]
        [InlineData("+oo")]
        [InlineData("-oo")]
        [InlineData("x + +oo")]
        [InlineData("x - -oo")]
        [InlineData("oo")]
        [InlineData("pi + e")]
        [InlineData("e ^ x")]
        [InlineData("x <= 1")]
        [InlineData("x >= 1")]
        [InlineData("x < y <= z")]
        [InlineData("not x = 1")]
        [InlineData("not (a = b) = c")]
        [InlineData("a = (not b = c)")]
        [InlineData("not domain(a = b, BB)")]
        [InlineData("a = b (mod 3)")]
        [InlineData("a = b and c = d (mod 3)")]
        [InlineData("x in RR")]
        [InlineData("x subset y")]
        [InlineData(@"{ 1 } \/ { 2 } /\ { 3 }")]
        [InlineData(@"({ 1 } \/ { 2 }) /\ { 3 }")]
        [InlineData("not a and b or c")]
        [InlineData("not (a and b)")]
        [InlineData("forall x in RR : x ^ 2 >= 0")]
        [InlineData("exists x in ZZ : x = 0")]
        [InlineData("exists! x in ZZ : x = 0")]
        [InlineData("sum(x ^ 2, x in ZZ)")]
        [InlineData("{ x in RR : x ^ 2 <= 1 }")]
        [InlineData("notable + inner + order + pie")]
        public void TheUnicodeSpelledShapesRoundTrip(string source) => AssertRoundTrip(source);
    }

    /// <summary>
    /// Every case of <see cref="StringizeRoundTripTest"/> again, printed under
    /// <see cref="MathS.Settings.UnicodeOutput"/>: the Unicode form is held to the same contract,
    /// that parsing it gives back the expression it printed.
    /// https://github.com/asc-community/AngouriMath/issues/1242
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class UnicodeStringizeRoundTripTest : StringizeRoundTripTest
    {
        /// <inheritdoc/>
        protected override bool Unicode => true;

        [Theory]
        [InlineData("x ^ 2 - 1 <= pi", "x² − 1 ≤ π")]
        [InlineData("x ^ (-1) + e", "x⁻¹ + ℯ")]
        [InlineData("x ^ 23 * 2 ^ 10", "x²³ * 2¹⁰")]
        [InlineData("(x + 1) ^ 2", "(x + 1)²")]
        [InlineData("x ^ 2 ^ 3", "x ^ 2³")]
        [InlineData("x ^ 3!", "x ^ 3!")]
        [InlineData("x ^ (1/2) + x ^ 2.5", "sqrt(x) + x ^ (5/2)")]
        [InlineData("2 - -3", "2 − (−3)")]
        [InlineData("x + +oo", "x + +∞")]
        [InlineData("-oo", "−∞")]
        [InlineData("x >= 1 and not x = 2", "x ≥ 1 ∧ x ≠ 2")]
        [InlineData("not a or b", "¬a ∨ b")]
        [InlineData("a = b (mod 3)", "a ≡ b (mod 3)")]
        [InlineData(@"x in A \/ B /\ C", "x ∈ A ∪ B ∩ C")]
        [InlineData("x subset y", "x ⊆ y")]
        [InlineData("forall x in RR : exists y in RR : y > x", "∀ x ∈ RR : ∃ y ∈ RR : y > x")]
        [InlineData("exists! x in ZZ : x = 0", "∃! x ∈ ZZ : x = 0")]
        [InlineData("notable + inner + pie", "notable + inner + pie")]
        public void PrintsTheGlyphs(string source, string expected) => Assert.Equal(expected, Print(source.ToEntity()));

        /// <summary>
        /// Each glyph is read as the ASCII token it stands for, so the two spellings are the same
        /// expression. A bare ∞ is not read, as a bare oo is not an infinity.
        /// </summary>
        [Theory]
        [InlineData("x ≤ 1", "x <= 1")]
        [InlineData("x ≥ 1", "x >= 1")]
        [InlineData("x ≠ 1", "x <> 1")]
        [InlineData("x ∈ RR", "x in RR")]
        [InlineData("A ∪ B", @"A \/ B")]
        [InlineData("A ∩ B", @"A /\ B")]
        [InlineData("¬a", "not a")]
        [InlineData("a ∧ b", "a and b")]
        [InlineData("a ∨ b", "a or b")]
        [InlineData("a − b", "a - b")]
        [InlineData("−x", "-x")]
        [InlineData("2 − −3", "2 - -3")]
        [InlineData("+∞", "+oo")]
        [InlineData("−∞", "-oo")]
        [InlineData("-∞", "-oo")]
        [InlineData("x²", "x ^ 2")]
        [InlineData("2x²", "2 * x ^ 2")]
        [InlineData("x²³", "x ^ 23")]
        [InlineData("x²y", "x ^ 2 * y")]
        [InlineData("x⁻¹", "x ^ (-1)")]
        [InlineData("−x²", "-(x ^ 2)")]
        [InlineData("(x + 1)²", "(x + 1) ^ 2")]
        [InlineData("x ^ 2³", "x ^ (2 ^ 3)")]
        [InlineData("(x²)³", "(x ^ 2) ^ 3")]
        [InlineData("x⁰ + x¹ + x⁴⁵⁶⁷⁸⁹", "x ^ 0 + x ^ 1 + x ^ 456789")]
        public void TheGlyphsParseAsTheirAsciiTokens(string unicode, string ascii)
            => Assert.Equal(ascii.ToEntity(), MathS.FromString(unicode, useCache: false));

        [Fact]
        public void ASuperscriptIsAWrittenOperatorUnderExplicitParsing()
        {
            using var _ = MathS.Settings.ExplicitParsingOnly.Set(true);
            Assert.Equal("x ^ 2".ToEntity(), MathS.FromString("x²", useCache: false));
        }

        [Theory]
        [InlineData("∞")]
        [InlineData("x⁻")]
        [InlineData("√2")]
        public void WhatIsNotAGlyphStaysAParseError(string source)
            => Assert.ThrowsAny<AngouriMath.Core.Exceptions.ParseException>(() => MathS.FromString(source, useCache: false));

        /// <summary>
        /// The setting changes what is shown and nothing else: the default stays ASCII, LaTeX does
        /// not change, and an ordering the library takes by printed form -- which decides the
        /// order of an answer's terms -- is the same with it on.
        /// </summary>
        [Fact]
        public void OnlyWhatIsShownChanges()
        {
            var expression = "x ^ 2 - pi <= -oo or not a = b".ToEntity();
            Assert.Equal("x ^ 2 - pi <= -oo or not a = b", expression.Stringize());
            var latex = expression.Latexize();
            var sympy = MathS.ToSympyCode("x ^ (-2) - 3".ToEntity());
            var simplified = "(b - 3) * (-2 + a) + x * pi - e".ToEntity().Simplify();
            using var _ = MathS.Settings.UnicodeOutput.Set(true);
            Assert.Equal(latex, expression.Latexize());
            Assert.Equal(sympy, MathS.ToSympyCode("x ^ (-2) - 3".ToEntity()));
            Assert.Equal(simplified, "(b - 3) * (-2 + a) + x * pi - e".ToEntity().Simplify());
            Assert.Equal("x² − π ≤ −∞ ∨ a ≠ b", expression.ToString());
        }
    }
}
