//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using Xunit;

namespace AngouriMath.Mcp.Tests;

/// <summary>
/// One call to each tool, on the behaviour its design rests on: the two silent misparses and
/// the correct spellings that must not be warned about, a domain condition kept, a decline
/// reported as one, and an answer verified rather than trusted.
/// </summary>
public sealed class ToolsTest
{
    private const string ImplicitPower = "implicit-power";
    private const string UnknownFunction = "unknown-function";

    [Theory]
    [InlineData("x2 + 1", "x ^ 2 + 1")]
    // VARIABLE accepts Greek, so `α2` is the same trap as `x2`.
    [InlineData("α2 + β", "α ^ 2 + β")]
    // No numeric literal precedes this `e`, so it is the constant squared... cubed.
    [InlineData("e3", "e ^ 3")]
    [InlineData("a*(t - t0)^2 + c", "a * (t - t ^ 0) ^ 2 + c")]
    public void ADigitAfterANameIsWarnedAboutAsAPower(string expression, string parsed)
    {
        var answer = Mcp.Call("am_parse", $$"""{"expression":"{{expression}}"}""");
        Assert.Equal(parsed, answer.Field("parsed"));
        Assert.Contains(answer.Warnings(), w => w.StartsWith(ImplicitPower));
    }

    [Theory]
    [InlineData("exp(x)", "e ^ x")]
    // The `g2` inside the function's own name is not the `x2` trap.
    [InlineData("log2(8)", "log(2, 8)")]
    // EXPONENT is a fragment of NUMBER, so the `e3` here belongs to the literal.
    [InlineData("1.5e3 + 2", "1500 + 2")]
    [InlineData("erf(x) + Si(x)", "erf(x) + Si(x)")]
    [InlineData("t_0 + x", "t_0 + x")]
    public void ACorrectSpellingIsNotWarnedAbout(string expression, string parsed)
    {
        var answer = Mcp.Call("am_parse", $$"""{"expression":"{{expression}}"}""");
        Assert.Equal(parsed, answer.Field("parsed"));
        Assert.Empty(answer.Warnings());
    }

    [Theory]
    [InlineData("Γ(x+1)", "Γ * (x + 1)")]
    [InlineData("im(z)", "im * z")]
    public void AnUnknownNameBeforeABracketIsWarnedAboutAsAProduct(string expression, string parsed)
    {
        var answer = Mcp.Call("am_parse", $$"""{"expression":"{{expression}}"}""");
        Assert.Equal(parsed, answer.Field("parsed"));
        Assert.Contains(answer.Warnings(), w => w.StartsWith(UnknownFunction));
    }

    [Fact]
    public void StrictParsingRefusesAnImplicitProduct() =>
        Assert.Equal("failed", Mcp.Call("am_parse", """{"expression":"2x + 1","strict":true}""").Field("status"));

    [Fact]
    public void SimplifyKeepsTheDomainCondition() =>
        Assert.Equal("x + 1 provided not x - 1 = 0",
            Mcp.Call("am_simplify", """{"expression":"(x^2-1)/(x-1)"}""").Field("result"));

    [Fact]
    public void SimplifyCancelsAMultivariateRationalFunction() =>
        Assert.Equal("(x + y) / (x - y) provided not x + y = 0",
            Mcp.Call("am_simplify", """{"expression":"(x^2+2*x*y+y^2)/(x^2-y^2)"}""").Field("result"));

    [Fact]
    public void SolveFindsBothRoots() =>
        Assert.Equal("""["2","-2"]""", Mcp.Call("am_solve", """{"constraints":["x^2 = 4"],"variable":"x"}""")["solutions"]!.ToJsonString());

    [Fact]
    public void AConstraintNarrowsTheRoots() =>
        Assert.Equal("""["2"]""", Mcp.Call("am_solve", """{"constraints":["x^2 = 4","x > 0"],"variable":"x"}""")["solutions"]!.ToJsonString());

    [Fact]
    public void Differentiate() =>
        Assert.Equal("cos(2 * x)", Mcp.Call("am_differentiate", """{"expression":"sin(x)*cos(x)","variable":"x"}""").Field("result"));

    [Theory]
    [InlineData("x*ln(x)")]
    // No elementary antiderivative, and one in erfi.
    [InlineData("e^(x^2)")]
    [InlineData("e^x/x")]
    public void AnAntiderivativeIsVerifiedByDifferentiatingItBack(string integrand)
    {
        var answer = Mcp.Call("am_integrate", $$"""{"expression":"{{integrand}}","variable":"x"}""");
        Assert.Equal("solved", answer.Field("status"));
        Assert.Equal("true", answer.Field("verified"));
    }

    [Fact]
    public void AnIntegralWithoutARuleIsDeclined()
    {
        var answer = Mcp.Call("am_integrate", """{"expression":"x^x","variable":"x","from":"1","to":"2"}""");
        Assert.Equal("declined", answer.Field("status"));
        // Quadrature for the one interval, to the digits two step counts agree on.
        Assert.StartsWith("2.05", answer.Field("numeric_definite_value"));
    }

    [Fact]
    public void ADefiniteIntegralIsExact() =>
        Assert.Equal("22/7 - pi", Mcp.Call("am_integrate",
            """{"expression":"x^6-4*x^5+5*x^4-4*x^2+4-4/(1+x^2)","variable":"x","from":"0","to":"1"}""").Field("definite_value"));

    [Theory]
    [InlineData("sin(x)/x", "0", "1")]
    [InlineData("(1+x)^(1/x)", "0", "e")]
    public void Limit(string expression, string to, string limit) =>
        Assert.Equal(limit, Mcp.Call("am_limit", $$"""{"expression":"{{expression}}","variable":"x","to":"{{to}}"}""").Field("result"));

    [Fact]
    public void EvaluateIsExactWithADecimalBeside()
    {
        var answer = Mcp.Call("am_evaluate", """{"expression":"sqrt(2)+sqrt(8)"}""");
        Assert.Equal("sqrt(2) * 3", answer.Field("result"));
        Assert.StartsWith("4.24264068711928", answer.Field("approximate"));
    }

    [Theory]
    [InlineData("sin(x)^2+cos(x)^2", "1", true, "0")]
    [InlineData("(x+1)^2", "x^2+1", false, "2 * x")]
    public void VerifyEqualNamesTheDifference(string left, string right, bool equal, string difference)
    {
        var answer = Mcp.Call("am_verify_equal", $$"""{"left":"{{left}}","right":"{{right}}"}""");
        Assert.Equal(equal, answer["equal"]!.GetValue<bool>());
        Assert.Equal(difference, answer.Field("difference"));
    }

    [Fact]
    public void TruthTableListsTheSatisfyingAssignments()
    {
        var answer = Mcp.Call("am_truth_table", """{"expression":"a and (b or not c)"}""");
        Assert.True(answer["satisfiable"]!.GetValue<bool>());
        Assert.StartsWith("Matrix[3 x 3]", answer.Field("satisfying_assignments"));
    }

    [Fact]
    public void ASystemWrittenAsEqualitiesIsSolved()
    {
        var answer = Mcp.Call("am_solve_system", """{"equations":["x + y = 3","x - y = 1"],"variables":["x","y"]}""");
        Assert.Equal(1, answer["solution_count"]!.GetValue<int>());
        Assert.Equal("2 1", string.Join(" ", answer.Field("solutions").Split('\n')[1].Split(' ', StringSplitOptions.RemoveEmptyEntries)));
    }

    [Fact]
    public void ToSympyWritesAProgram() =>
        Assert.Contains("sympy", Mcp.Call("am_to_sympy", """{"expression":"sin(x)/x"}""").Field("sympy"));

    [Fact]
    public void ASymbolicDeterminantHasNoPivotGuard() =>
        Assert.Equal("a * d - b * c", Mcp.Call("am_matrix", """{"operation":"determinant","matrix":[["a","b"],["c","d"]]}""").Field("result"));

    [Fact]
    public void TensorProductOfHadamardAndIdentity()
    {
        var answer = Mcp.Call("am_matrix",
            """{"operation":"tensor_product","matrix":[["1/sqrt(2)","1/sqrt(2)"],["1/sqrt(2)","-1/sqrt(2)"]],"matrix_b":[["1","0"],["0","1"]]}""");
        Assert.Equal("4x4", answer.Field("shape_out"));
        Assert.Equal("sqrt(2) / 2", answer["result"]![0]![0]!.GetValue<string>());
    }

    [Fact]
    public void EigenvaluesHoldAtZeroToo()
    {
        var answer = Mcp.Call("am_eigenvalues", """{"matrix":[["0","J"],["J","0"]]}""");
        Assert.Equal("lambda ^ 2 - J ^ 2", answer.Field("characteristic_polynomial"));
        Assert.DoesNotContain("provided", answer.Field("eigenvalues"));
    }

    [Fact]
    public void Series() =>
        Assert.Equal("x ^ 5 / 120 - x ^ 3 / 6 + x", Mcp.Call("am_series", """{"expression":"sin(x)","variable":"x","degree":7}""").Field("result"));

    [Fact]
    public void Factorize() =>
        Assert.Equal("2^4 * 3^2 * 5 * 7", Mcp.Call("am_number_theory", """{"operation":"factorize","value":"5040"}""").Field("result"));

    [Fact]
    public void SubstituteIsStructural() =>
        Assert.Equal("a * (t - t_0) ^ 2 + b * (t - t_0) + c",
            Mcp.Call("am_substitute", """{"expression":"a*x^2 + b*x + c","substitutions":{"x":"t - t_0"}}""").Field("result"));

    [Fact]
    public void CompareNumericFindsTheWorstPoint()
    {
        var answer = Mcp.Call("am_compare_numeric",
            """{"reference":"sin(x)","approximation":"x - x^3/6","variable":"x","from":0,"to":1,"samples":50}""");
        Assert.InRange(answer["max_absolute_error"]!.GetValue<double>(), 0.0081, 0.0082);
    }

    [Fact]
    public void FixedPointIsExact()
    {
        var answer = Mcp.Call("am_represent", """{"operation":"fixed_point","value":"1/sqrt(2)","fraction_bits":15,"total_bits":16}""");
        Assert.Equal("23170", answer.Field("raw"));
        Assert.Equal("11585/16384", answer.Field("represented_value"));
    }

    [Fact]
    public void Ieee754StoresMoreThanATenth()
    {
        var answer = Mcp.Call("am_represent", """{"operation":"ieee754","value":"0.1"}""");
        Assert.Equal("0x3FB999999999999A", answer.Field("hex"));
        Assert.Equal("0.1000000000000000055511151231257827021181583404541015625", answer.Field("exact_value"));
    }

    [Fact]
    public void PolarIsQuadrantCorrected()
    {
        var answer = Mcp.Call("am_represent", """{"operation":"polar","value":"-1 - i"}""");
        Assert.Equal("sqrt(2)", answer.Field("magnitude"));
        Assert.Equal("-3/4 * pi", answer.Field("phase_radians"));
    }
}
