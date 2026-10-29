//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The Lambert W function, the solution w of w e^w = z on each branch, with the branches of
    /// Corless et al. (1996) that Mathematica's ProductLog and mpmath's lambertw have. The name was
    /// refused. <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class LambertWTest
    {
        /// <summary>
        /// mpmath's lambertw at 60 digits, at the decimals written: on the real axis where each branch is real, across the cut
        /// below -1/e where W_0 and W_(-1) are conjugates, about the branch point from above and below,
        /// and far out. W_0 on the axis from -1 to -2/e, about -1, and just left of -1 was real, or on
        /// another branch: its start was ln(1 + z), which is real there and is ln 0 at -1.
        /// </summary>
        public static readonly TheoryData<int, string, string, string, string> Points = new()
        {
            { 0, "1", "0", "0.56714329040978387299996866221035555", "0.0" },
            { 0, "2.5", "0", "0.95858635672870291216986678133245214", "0.0" },
            { 0, "10", "0", "1.7455280027406993830743012648753899", "0.0" },
            { 0, "1000000", "0", "11.383358086140052622000156781585004", "0.0" },
            { 0, "0.1", "0", "0.091276527160862264299895721423179569", "0.0" },
            { 0, "-0.1", "0", "-0.11183255915896296483356945682026584", "0.0" },
            { 0, "-0.3", "0", "-0.48940222718021496903623125199629337", "0.0" },
            { 0, "-0.36", "0", "-0.80608431597081777828552136162099200", "0.0" },
            { 0, "-0.5", "0", "-0.79402363234468936796301532190058981", "0.77011175051037910968131307740502893" },
            { 0, "-2", "0", "0.17281600283999997574575914578045636", "1.6736864137408426771888017779670810" },
            { 0, "-10", "0", "1.3699809685212708156194228571987809", "2.1401945270747131960165362216531737" },
            { 0, "1", "1", "0.65696606923043640587393518961502951", "0.32545033941341502998928195138862240" },
            { 0, "-1", "2", "0.62712187862672106128482581800205043", "1.0164525226857943480329954988558366" },
            { 0, "-3", "-4", "1.0750730665692549275718182498895184", "-1.3251023817343588822606049852999208" },
            { 0, "0", "0.5", "0.16259964821886692670376696666293535", "0.39262857399163941534774301910986139" },
            { 0, "0", "100", "3.3372744790013778465375635510033457", "1.2202535275983894290305770906784596" },
            { 0, "-0.36", "0.01", "-0.77692593162589352548613077434798300", "0.099106539538801071982698823506270403" },
            { 0, "-0.36", "-0.01", "-0.77692593162589352548613077434798300", "-0.099106539538801071982698823506270403" },
            { 0, "-0.367", "0", "-0.93239918474792848371646619087935721", "0.0" },
            { 0, "0.5", "0.5", "0.40431612353121278901924224143642299", "0.24343775688425395570266928474288716" },
            { 0, "-0.2", "0.3", "-0.079775717127478451154646095753164503", "0.38226147065890346680498485830193446" },
            { 0, "2", "-1.5", "0.93210073009396256291394339557696040", "-0.31632723076474133978590653951403091" },
            { 0, "-0.8", "0", "-0.47296437953376993647110619289081", "1.1934970731363015053999934286237" },
            { 0, "-0.975", "0", "-0.33577732103000080461145009761745", "1.3220782205206208727604624558714" },
            { 0, "-1", "0", "-0.31813150520476413531265425158766", "1.3372357014306894089011621431937" },
            { 0, "-1.02", "0", "-0.30431558075585315366696810619974", "1.3489097881249077058064917902432" },
            { 0, "-1.349", "-0.358", "0.039601766331215229042972440561754", "-1.3409186442644250944627990020702" },
            { -1, "-0.1", "0", "-3.5771520639572972184093919635119949", "0.0" },
            { -1, "-0.3", "0", "-1.7813370234216276119741702815127453", "0.0" },
            { -1, "-0.36", "0", "-1.2227701339785059531429380734238623", "0.0" },
            { -1, "-0.5", "0", "-0.79402363234468936796301532190058981", "-0.77011175051037910968131307740502893" },
            { -1, "-2", "0", "0.17281600283999997574575914578045636", "-1.6736864137408426771888017779670810" },
            { -1, "1", "0", "-1.5339133197935745079197410820727338", "-4.3751851530618983854709065648525843" },
            { -1, "1", "1", "-0.98696957322127523414822457230314696", "-3.6638570032847919874683044706668667" },
            { -1, "-1", "-1", "-1.6027125327569588280097149491309221", "-6.8383680385915972761916759143574442" },
            { -1, "-0.36", "0.01", "-1.2514376073860293427616444184098277", "-0.13609907407909009200297557730125840" },
            { -1, "-0.36", "-0.01", "-3.1073988682641031993153486904823598", "-7.4300990216044500554595691973401438" },
            { -1, "-0.00001", "0", "-14.163600815810183009109556303610900", "0.0" },
            { 1, "1", "0", "-1.5339133197935745079197410820727338", "4.3751851530618983854709065648525843" },
            { 1, "-1", "0", "-2.0622777295982838849784867200080460", "7.5886311784725126225689239541075844" },
            { 1, "-0.36", "-0.01", "-1.2514376073860293427616444184098277", "0.13609907407909009200297557730125840" },
            { 1, "-0.36", "0.01", "-3.1073988682641031993153486904823598", "7.4300990216044500554595691973401438" },
            { 1, "1", "1", "-1.3428489407008043011797745967257447", "5.2472493742914012056555318165948516" },
            { 1, "-2", "0", "-1.3607494244085734046050529099678950", "7.6785890798165936510026544586454389" },
            { 2, "1", "0", "-2.4015851048680028841741397746840765", "10.776299516115070898497103346391148" },
            { 2, "-1", "1", "-2.2466670775452915909711722757930199", "13.182968560741182863737480091832091" },
            { 2, "10", "0", "-0.094164747214147723021593974923742271", "10.987003940068022849033192477402823" },
            { -2, "1", "0", "-2.4015851048680028841741397746840765", "-10.776299516115070898497103346391148" },
            { -2, "-1", "1", "-1.8074833801113711199505175939657967", "-8.4281211380731151670788198279353834" },
            { -2, "10", "0", "-0.094164747214147723021593974923742271", "-10.987003940068022849033192477402823" },
        };

        [Theory]
        [MemberData(nameof(Points))]
        public void ItAgreesWithMpmath(int branch, string re, string im, string expectedRe, string expectedIm)
        {
            var z = Number.Complex.Create(EDecimal.FromString(re), EDecimal.FromString(im));
            var w = Assert.IsAssignableFrom<Number.Complex>(MathS.LambertW(branch, z).EvalNumerical());
            var expected = Number.Complex.Create(EDecimal.FromString(expectedRe), EDecimal.FromString(expectedIm));
            var error = ((Number.Complex)(w - expected).EvalNumerical()).Abs().EDecimal;
            var scale = EDecimal.Max(expected.Abs().EDecimal, EDecimal.One);
            Assert.True(error.CompareTo(scale.Multiply(EDecimal.FromString("1e-25"))) < 0, $"W_{branch}({re} + {im}i) = {w.Stringize()}, expected {expected.Stringize()}");
        }

        [Theory]
        [InlineData("lambertw(0)", "0")]
        [InlineData("lambertw(e)", "1")]
        [InlineData("lambertw(-1/e)", "-1")]
        [InlineData("lambertw(-1, -1/e)", "-1")]
        [InlineData("lambertw(2 e^2)", "2")]
        [InlineData("lambertw(-1, -2 e^(-2))", "-2")]
        [InlineData("productlog(e)", "1")]
        public void ItIsExactWhereZIsANumberTimesItsExponential(string written, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified, written.ToEntity().InnerSimplified);

        /// <summary>Off the principal branch W_k(z) goes to -oo as z goes to 0, as ln z does.</summary>
        [Fact]
        public void OffThePrincipalBranchZeroIsMinusInfinity()
            => Assert.Equal(Number.Real.NegativeInfinity, "lambertw(1, 0)".ToEntity().InnerSimplified);

        /// <summary>The branch is a whole number.</summary>
        [Fact]
        public void ABranchThatIsNotWholeHasNoValue()
            => Assert.Equal(MathS.NaN, "lambertw(1/2, 1)".ToEntity().InnerSimplified);

        /// <summary>W(z) e^(W(z)) = z.</summary>
        [Theory]
        [InlineData(0, "3.5", "0")]
        [InlineData(0, "-2", "1")]
        [InlineData(-1, "-0.2", "0")]
        [InlineData(3, "5", "-7")]
        public void ItSolvesWTimesItsExponentialEqualsZ(int branch, string re, string im)
        {
            var z = Number.Complex.Create(EDecimal.FromString(re), EDecimal.FromString(im));
            var w = MathS.LambertW(branch, z);
            var residual = ((Number.Complex)(w * MathS.Pow(MathS.e, w) - z).EvalNumerical()).Abs().EDecimal;
            Assert.True(residual.CompareTo(EDecimal.FromString("1e-25")) < 0, residual.ToString());
        }

        /// <summary>W'(z) = W(z) / (z (1 + W(z))), and 1 at 0.</summary>
        [Fact]
        public void ItsDerivativeIsWOverZOnePlusW()
        {
            var derivative = MathS.LambertW("x").Differentiate("x");
            var atOne = ((Number.Real)derivative.Substitute("x", 1).EvalNumerical()).EDecimal;
            var w = ((Number.Real)MathS.LambertW(1).EvalNumerical()).EDecimal;
            var expected = w.Divide(w.Add(EDecimal.One), EContext.ForPrecision(40));
            Assert.True(atOne.Subtract(expected).Abs().CompareTo(EDecimal.FromString("1e-25")) < 0, atOne.ToString());
            Assert.Equal(Number.Integer.One, derivative.Substitute("x", 0).Evaled);
        }

        [Fact]
        public void ASymbolKeepsTheNode()
            => Assert.IsType<LambertWf>("lambertw(x)".ToEntity().InnerSimplified);

        [Theory]
        [InlineData("lambertw(z)", "lambertw(z)", @"\operatorname{W}\left(z\right)", "sympy.LambertW(z)")]
        [InlineData("lambertw(-1, z)", "lambertw(-1, z)", @"\operatorname{W}_{-1}\left(z\right)", "sympy.LambertW(z, -1)")]
        [InlineData("productlog(1, z + 1)", "lambertw(1, z + 1)", @"\operatorname{W}_{1}\left(z+1\right)", "sympy.LambertW(z + 1, 1)")]
        public void ItIsWrittenAsW(string written, string stringized, string latex, string sympy)
        {
            var parsed = written.ToEntity();
            Assert.Equal(stringized, parsed.Stringize());
            Assert.Equal(parsed, parsed.Stringize().ToEntity());
            Assert.Equal(latex, parsed.Latexize());
            Assert.Equal(sympy, parsed.ToSymPy());
        }
    }
}
