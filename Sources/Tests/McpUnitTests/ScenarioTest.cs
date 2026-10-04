//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using Xunit;

namespace AngouriMath.Mcp.Tests;

/// <summary>
/// The use cases Sources/MCP/README.md describes, each as the question an agent would actually be
/// asked, asserting what the README says the server contributes at that moment.
/// </summary>
public sealed class ScenarioTest
{
    [Fact]
    public void AnIntegrationDoneByHandIsConfirmed() =>
        Assert.True(Mcp.Call("am_verify_equal", """{"left":"derivative(e^x*(x-1), x)","right":"x*e^x"}""")["equal"]!.GetValue<bool>());

    [Fact]
    public void ASignSlipIsNamedByTheDifference()
    {
        var answer = Mcp.Call("am_verify_equal", """{"left":"derivative(e^x*(x+1), x)","right":"x*e^x"}""");
        Assert.False(answer["equal"]!.GetValue<bool>());
        Assert.Equal("2 * e ^ x", answer.Field("difference"));
    }

    [Fact]
    public void TheStepThatBrokeAWorkingIsNamed()
    {
        var answer = Mcp.Call("am_check_steps", """{"steps":["(x+1)^2 - 1","x^2 + 1 - 1","x^2"]}""");
        Assert.Equal(1, answer["first_invalid_step"]!.GetValue<int>());
        Assert.Equal("2 * x", answer["steps"]![0]!["difference"]!.GetValue<string>());
        Assert.True(answer["steps"]![1]!["equal"]!.GetValue<bool>());
    }

    [Fact]
    public void AnExpansionInTheCodeAgreesWithTheCommentAboveIt() =>
        Assert.True(Mcp.Call("am_verify_equal",
            """{"left":"(a*(t - tref))^2 + b*(t - tref) + c","right":"a^2*t^2 - 2*a^2*t*tref + a^2*tref^2 + b*t - b*tref + c"}""")["equal"]!.GetValue<bool>());

    [Fact]
    public void ASignErrorInTheCodeIsPointedAt()
    {
        var answer = Mcp.Call("am_verify_equal",
            """{"left":"(a*(t - tref))^2 + b*(t - tref) + c","right":"a^2*t^2 - 2*a^2*t*tref + a^2*tref^2 + b*t + b*tref + c"}""");
        Assert.False(answer["equal"]!.GetValue<bool>());
        Assert.Equal("(-2) * b * tref", answer.Field("difference"));
    }

    [Fact]
    public void TheHazardsBeforeHardwareAreListed()
    {
        var risks = Mcp.Call("am_domain_check", """{"expression":"sqrt(v - vref) / (t - tref)"}""")["hazards"]!.AsArray()
            .Select(hazard => hazard!["risk"]!.GetValue<string>()).ToList();
        Assert.Contains(risks, risk => risk.StartsWith("division"));
        Assert.Contains(risks, risk => risk.StartsWith("fractional or symbolic power"));
    }

    [Fact]
    public void AJacobianRow() =>
        Assert.Equal("x * (x ^ 2 + y ^ 2) ^ (-1/2)",
            Mcp.Call("am_differentiate", """{"expression":"sqrt(x^2 + y^2)","variable":"x"}""").Field("result"));

    [Fact]
    public void ADesignFormulaIsSolvedForAPart() =>
        Assert.Equal("""["1/2 / (C * f * pi)"]""",
            Mcp.Call("am_solve", """{"constraints":["f = 1/(2*pi*R*C)"],"variable":"R"}""")["solutions"]!.ToJsonString());

    [Fact]
    public void AnExactAreaComesWithItsAntiderivativeVerified()
    {
        var answer = Mcp.Call("am_integrate",
            """{"expression":"x^6-4*x^5+5*x^4-4*x^2+4-4/(1+x^2)","variable":"x","from":"0","to":"1"}""");
        Assert.Equal("22/7 - pi", answer.Field("definite_value"));
        Assert.Equal("true", answer.Field("verified"));
    }

    [Fact]
    public void WhatTruncatingASeriesCostsAndWhere()
    {
        var answer = Mcp.Call("am_compare_numeric",
            """{"reference":"sin(x)","approximation":"x - x^3/6 + x^5/120","variable":"x","from":0,"to":1.5,"samples":200}""");
        // The first term left out, x^7/5040, is 3.4e-3 at 1.5.
        Assert.InRange(answer["max_absolute_error"]!.GetValue<double>(), 3.2e-3, 3.4e-3);
        Assert.Equal("1.5", answer.Field("max_absolute_error_at"));
    }

    [Fact]
    public void ACoefficientAsAFixedPointWord()
    {
        var answer = Mcp.Call("am_represent", """{"operation":"fixed_point","value":"1/6","fraction_bits":15,"total_bits":16}""");
        Assert.Equal("5461", answer.Field("raw"));
        Assert.Equal("5461/32768", answer.Field("represented_value"));
        Assert.Equal("false", answer.Field("saturated"));
    }

    [Fact]
    public void AValueTheFormatCannotHoldIsSuspect()
    {
        var answer = Mcp.Call("am_represent", """{"operation":"fixed_point","value":"1.5","fraction_bits":15,"total_bits":16}""");
        Assert.Equal("suspect", answer.Field("status"));
        Assert.Equal("true", answer.Field("saturated"));
    }

    [Fact]
    public void TheBellStateIsExact()
    {
        // H (x) I, then CNOT times that, then times |00>: three calls, amplitudes 1/sqrt(2).
        var hadamardTimesIdentity = Mcp.Call("am_matrix",
            """{"operation":"tensor_product","matrix":[["1/sqrt(2)","1/sqrt(2)"],["1/sqrt(2)","-1/sqrt(2)"]],"matrix_b":[["1","0"],["0","1"]]}""")["result"]!;
        var entangled = Mcp.Call("am_matrix",
            $$"""{"operation":"multiply","matrix":[["1","0","0","0"],["0","1","0","0"],["0","0","0","1"],["0","0","1","0"]],"matrix_b":{{hadamardTimesIdentity.ToJsonString()}}}""")["result"]!;
        var bell = Mcp.Call("am_matrix",
            $$"""{"operation":"multiply","matrix":{{entangled.ToJsonString()}},"matrix_b":[["1"],["0"],["0"],["0"]]}""")["result"]!;
        Assert.Equal("""[["sqrt(2) / 2"],["0"],["0"],["sqrt(2) / 2"]]""", bell.ToJsonString());
    }

    [Fact]
    public void EveryCaseAGuardHasToHandle()
    {
        var answer = Mcp.Call("am_truth_table",
            """{"expression":"(ready and not fault) or override","variables":["ready","fault","override"]}""");
        Assert.StartsWith("Matrix[5 x 3]", answer.Field("satisfying_assignments"));
    }

    [Fact]
    public void AnExactValueForATest() =>
        Assert.Equal("sqrt(3)", Mcp.Call("am_evaluate", """{"expression":"sin(pi/3) + cos(pi/6)"}""").Field("result"));
}
