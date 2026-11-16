//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The Riemann zeta function <c>zeta(s)</c> and the Hurwitz zeta function <c>zeta(s, a)</c> of DLMF 25.11,
    /// undefined at <c>s = 1</c> and at <c>a = 0, -1, -2, ...</c>. The names were refused.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class ZetaTest
    {
        private static Number.Complex Point(string re, string im)
            => Number.Complex.Create(EDecimal.FromString(re), EDecimal.FromString(im));

        private static void AssertClose(Number.Complex expected, Entity actual, string tolerance, string what, string floor = "0")
        {
            var value = Assert.IsAssignableFrom<Number.Complex>(actual.EvalNumerical());
            var error = ((Number.Complex)(value - expected).EvalNumerical()).Abs().EDecimal;
            var scale = EDecimal.Max(expected.Abs().EDecimal, EDecimal.FromString(floor));
            Assert.True(error.CompareTo(scale.Multiply(EDecimal.FromString(tolerance))) <= 0,
                $"{what} = {value.Stringize()}, expected {expected.Stringize()}");
        }

        /// <summary>
        /// mpmath's <c>zeta(s, a)</c> at the decimals written, at 70 digits, and at 200 for the zero on the critical line:
        /// the Riemann function left of 0, where it is reflected, on the critical line and far up it, and the Hurwitz one
        /// with a negative and a complex <c>a</c>, a large <c>a</c>, and a large negative <c>s</c>.
        /// </summary>
        public static readonly TheoryData<string, string, string, string, string, string> Points = new()
        {
            { "3", "0", "1", "0", "1.20205690315959428539973816151144999076498629234049888179227", "0" },
            { "0.5", "0", "1", "0", "-1.46035450880958681288949915251529801246722933101258149054289", "0" },
            { "-2.5", "0", "1", "0", "0.00851692877785033054235856702834448693627599022007447776588885", "0" },
            { "-30.5", "0", "1", "0", "149774871.277934754838681857554689574764665020588700998123791", "0" },
            { "2", "3", "1", "0", "0.798021985146275720622294500724812686025220081608376128324805", "-0.113744308052938500215913365857315075570137806399665088964724" },
            { "0.5", "14.134725141734693790457251983562", "1", "0",
                "5.8642686732401224704147139300280540336314071857001765823911979e-32", "-3.6836146494157176893175447567032293783970498667487343122409982e-31" },
            { "-7.5", "20", "1", "0", "8899.57524069966774269699256796984192211541588308601192770482", "-9367.11252657321782202387408442966595683876298960334581042127" },
            { "0.5", "1000", "1", "0", "0.356334367194396055074402476711029641875046210906552513734106", "0.931997831232993665115060432737056074160354801664568016234414" },
            { "2.5", "0", "-1.5", "0", "6.24711063456881148528999055736586760935713119816423709645888", "-6.01974161879359176531005624123966511308845749864211379321527" },
            { "0.5", "0", "0.25", "0", "0.239963524495630955337574310160577225897864436801770042667629", "0" },
            { "-1.5", "0", "3", "0", "-3.853912326636023133552920435330100902608368735354869143188", "0" },
            { "-20.5", "0", "0.3", "0", "136.361935791831144171775099936398861428575875387817000609435", "0" },
            { "2", "0", "-0.5", "0.000001", "8.93480220044797476390088474438437691851342642624492692249518", "-0.000000828796644233740953958556752444575575372877908636915356748584" },
            { "-3.25", "2", "0.75", "-1.5", "-0.0776272421060595286457686221616168773491288036991489572040411", "0.0303671484781892175434412879020682483648718606849347385027133" },
            { "1.5", "-4", "-2.6", "0.4", "-0.0678455121508948491801743072183961714906127278469116621635844", "0.124007858786890837760811936572916589552779048538871001286881" },
            { "7", "0", "1000000", "0", "1.66667166667249999999999300000000001833333333326183333333333e-37", "0" },
        };

        [Theory]
        [MemberData(nameof(Points))]
        public void ItAgreesWithMpmath(string sRe, string sIm, string aRe, string aIm, string expectedRe, string expectedIm)
            => AssertClose(Point(expectedRe, expectedIm), MathS.Zeta(Point(sRe, sIm), Point(aRe, aIm)), "1e-55",
                $"zeta({sRe} + {sIm}i, {aRe} + {aIm}i)");

        /// <summary>At the working precision, here 200 digits.</summary>
        [Fact]
        public void ItIsEvaluatedAtTheWorkingPrecision()
        {
            using var _ = MathS.Settings.DecimalPrecisionContext.Set(new EContext(200, ERounding.HalfEven, -100000, 100000, false));
            AssertClose(Point("1.20205690315959428539973816151144999076498629234049888179227155534183820578631309018645587360933525814619915779526071941849199599867328321377639683720790016145394178294936006671919157552224249424396156391", "0"),
                MathS.Zeta(3), "1e-195", "zeta(3)");
        }

        /// <summary>
        /// DLMF 25.6.1, 25.6.2 and 25.6.3: <c>zeta(0) = -1/2</c>, <c>zeta(2m)</c> a rational times <c>pi^(2m)</c>, and
        /// <c>zeta(-n) = -B_(n + 1)/(n + 1)</c>, which is 0 at the negative even numbers. At a whole <c>s</c> and a whole or
        /// half-whole <c>a</c>, the Hurwitz function is the Riemann one less the terms that move <c>a</c> to <c>1</c> or to
        /// <c>1/2</c> (DLMF 25.11.3, 25.11.11), and at <c>s = -n</c> it is <c>-B_(n + 1)(a)/(n + 1)</c> for every rational
        /// <c>a</c> (25.11.14). Each is checked against the value evaluated from the sum, not against its printed form.
        /// </summary>
        [Theory]
        [InlineData("zeta(0)", "-1/2")]
        [InlineData("zeta(2)", "pi^2/6")]
        [InlineData("zeta(4)", "pi^4/90")]
        [InlineData("zeta(12)", "691 pi^12/638512875")]
        [InlineData("zeta(-1)", "-1/12")]
        [InlineData("zeta(-2)", "0")]
        [InlineData("zeta(-3)", "1/120")]
        [InlineData("zeta(-11)", "691/32760")]
        [InlineData("zeta(2, 1/2)", "pi^2/2")]
        [InlineData("zeta(2, 3)", "pi^2/6 - 5/4")]
        [InlineData("zeta(4, -3/2)", "15 pi^4/90 + 16 + 16/81")]
        [InlineData("zeta(3, 1/2)", "7 zeta(3)")]
        [InlineData("zeta(3, 2)", "zeta(3) - 1")]
        [InlineData("zeta(0, 1/3)", "1/6")]
        [InlineData("zeta(-2, 1/3)", "-1/81")]
        [InlineData("zeta(-1, -5/2)", "-(-5/2)^2/2 + (-5/2)/2 - 1/12")]
        [InlineData("riemannzeta(2)", "pi^2/6")]
        [InlineData("hurwitzzeta(2, 1)", "pi^2/6")]
        public void ItIsExactWhereTheValueIsKnown(string written, string expected)
        {
            var exact = written.ToEntity().Simplify();
            Assert.Equal(Number.Integer.Zero, (exact - expected.ToEntity()).Simplify());
            var parts = Assert.IsType<Zetaf>(written.ToEntity());
            var bySummation = Number.Complex.Create(
                ((Number.Complex)parts.Argument.EvalNumerical()).RealPart.EDecimal.Add(EDecimal.FromString("1e-60")),
                EDecimal.Zero);
            // Next to s, evaluated from the sum rather than from the closed form, agrees to the step.
            AssertClose((Number.Complex)exact.EvalNumerical(), MathS.Zeta(bySummation, parts.Parameter.EvalNumerical()),
                "1e-45", written, floor: "1");
        }

        /// <summary>The pole at <c>s = 1</c>, and <c>a = 0, -1, -2, ...</c>, where the sum has a term <c>1/0^s</c>.</summary>
        [Theory]
        [InlineData("zeta(1)")]
        [InlineData("zeta(1, 1/3)")]
        [InlineData("zeta(2, 0)")]
        [InlineData("zeta(-1, -3)")]
        [InlineData("zeta(1/2, -2)")]
        public void ThereIsNoValueAtThePoleOrAtANonPositiveWholeA(string written)
        {
            Assert.Equal(MathS.NaN, written.ToEntity().Simplify());
            Assert.Equal(MathS.NaN, written.ToEntity().Evaled);
        }

        [Fact]
        public void TheDomainExcludesThePoleAndTheNonPositiveWholeA()
        {
            var condition = "zeta(s, a)".ToEntity().DomainCondition;
            Assert.Equal(Boolean.False, condition.Substitute("s", 1).Substitute("a", 2).Simplify());
            Assert.Equal(Boolean.False, condition.Substitute("s", 2).Substitute("a", -3).Simplify());
            Assert.Equal(Boolean.True, condition.Substitute("s", 2).Substitute("a", MathS.FromString("-5/2")).Simplify());
            Assert.Equal(Boolean.True, condition.Substitute("s", -4).Substitute("a", 3).Simplify());
        }

        /// <summary>
        /// <c>zeta(s, a) = zeta(s, a + 1) + a^(-s)</c> (DLMF 25.11.3) and <c>zeta(s, 1/2) = (2^s - 1) zeta(s)</c> (25.11.11),
        /// off the real line, with a negative real part, and with a negative <c>a</c>.
        /// </summary>
        [Theory]
        [InlineData("2.5 + i", "0.3 - 0.2i")]
        [InlineData("-1.75 + 3i", "-0.6 + 0.1i")]
        [InlineData("0.25 - 9i", "-2.4")]
        public void ItSatisfiesTheShiftIdentity(string s, string a)
        {
            var (sv, av) = (s.ToEntity(), a.ToEntity());
            var difference = MathS.Zeta(sv, av) - MathS.Zeta(sv, av + 1) - MathS.Pow(av, -sv);
            Assert.True(((Number.Complex)difference.EvalNumerical()).Abs().EDecimal.CompareTo(EDecimal.FromString("1e-60")) < 0,
                difference.EvalNumerical().Stringize());
        }

        [Theory]
        [InlineData("0.7 + 3i")]
        [InlineData("-3.5 - 2i")]
        [InlineData("5")]
        public void AtAHalfItIsTheRiemannFunctionTimesTwoToTheSLessOne(string s)
        {
            var sv = s.ToEntity();
            var difference = MathS.Zeta(sv, MathS.FromString("1/2")) - (MathS.Pow(2, sv) - 1) * MathS.Zeta(sv);
            Assert.True(((Number.Complex)difference.EvalNumerical()).Abs().EDecimal.CompareTo(EDecimal.FromString("1e-60")) < 0,
                difference.EvalNumerical().Stringize());
        }

        /// <summary>
        /// <c>d/da zeta(s, a) = -s zeta(s + 1, a)</c> (DLMF 25.11.17), checked against a central difference. The
        /// derivative in <c>s</c> has no closed form and is left a derivative.
        /// </summary>
        [Fact]
        public void ItsDerivativeInAIsMinusSTimesZetaAtSPlusOne()
        {
            var derivative = "zeta(s, a)".ToEntity().Differentiate("a").Substitute("s", MathS.FromString("2.5 + i")).Substitute("a", MathS.FromString("0.7"));
            // Built rather than parsed: a parsed "1e-20" is the cached entity other tests evaluate under a widened tolerance.
            Entity h = Number.Rational.Create(1, PeterO.Numbers.EInteger.FromInt32(10).Pow(20));
            var s = MathS.FromString("2.5 + i");
            var difference = (MathS.Zeta(s, MathS.FromString("0.7") + h) - MathS.Zeta(s, MathS.FromString("0.7") - h)) / (2 * h);
            var error = ((Number.Complex)(derivative - difference).EvalNumerical()).Abs().EDecimal;
            Assert.True(error.CompareTo(EDecimal.FromString("1e-30")) < 0, error.ToString());
            Assert.IsType<Derivativef>("zeta(s)".ToEntity().Differentiate("s"));
        }

        /// <summary>The partial derivative over a variable is the same derivative.</summary>
        [Fact]
        public void ThePartialDerivativeInAIsTheSame()
            => Assert.Equal(Number.Integer.Zero,
                ("partialderivative(zeta(s, a^2), a)".ToEntity() - "-2 a s zeta(s + 1, a^2)".ToEntity()).Simplify() is Providedf p ? p.Expression
                    : ("partialderivative(zeta(s, a^2), a)".ToEntity() - "-2 a s zeta(s + 1, a^2)".ToEntity()).Simplify());

        /// <summary>
        /// <c>zeta</c> alone is a variable, and with a space before the bracket it is a product, as <c>gamma</c> is; the
        /// call takes one argument or two.
        /// </summary>
        [Theory]
        [InlineData("zeta", "zeta")]
        [InlineData("zeta (s)", "zeta * s")]
        [InlineData("gamma (s)", "gamma * s")]
        public void TheNameWithoutItsBracketIsAVariable(string written, string expected)
            => Assert.Equal(expected, written.ToEntity().Stringize());

        [Fact]
        public void ItTakesOneArgumentOrTwo()
        {
            Assert.Throws<FunctionArgumentCountException>(() => "zeta(s, a, b)".ToEntity());
            Assert.Throws<FunctionArgumentCountException>(() => "riemannzeta(s, a)".ToEntity());
            Assert.Throws<FunctionArgumentCountException>(() => "hurwitzzeta(s)".ToEntity());
        }

        [Fact]
        public void ASymbolKeepsTheNode()
            => Assert.IsType<Zetaf>("zeta(s, a)".ToEntity().Simplify());

        [Theory]
        [InlineData("zeta(s)", "zeta(s)", @"\operatorname{\zeta}\left(s\right)", "sympy.zeta(s)")]
        [InlineData("riemannzeta(s + 1)", "zeta(s + 1)", @"\operatorname{\zeta}\left(s+1\right)", "sympy.zeta(s + 1)")]
        [InlineData("zeta(s, 1)", "zeta(s)", @"\operatorname{\zeta}\left(s\right)", "sympy.zeta(s)")]
        [InlineData("hurwitzzeta(s, a)", "zeta(s, a)", @"\operatorname{\zeta}\left(s, a\right)", "sympy.zeta(s, a)")]
        public void ItIsWrittenAsZeta(string written, string stringized, string latex, string sympy)
        {
            var parsed = written.ToEntity();
            Assert.Equal(stringized, parsed.Stringize());
            Assert.Equal(parsed, parsed.Stringize().ToEntity());
            Assert.Equal(latex, parsed.Latexize());
            Assert.Equal(sympy, parsed.ToSymPy());
        }

        /// <summary>Neither compiler has a double-precision zeta function, so both refuse it, naming it.</summary>
        [Fact]
        public void ItIsNotCompiled()
        {
            Assert.Contains("zeta function", Assert.Throws<UncompilableNodeException>(() => "zeta(x)".ToEntity().Compile("x")).Message);
            Assert.Contains("zeta function", Assert.Throws<UncompilableNodeException>(() => "zeta(x, 2)".ToEntity().Compile<double, double>("x")).Message);
        }
    }
}
