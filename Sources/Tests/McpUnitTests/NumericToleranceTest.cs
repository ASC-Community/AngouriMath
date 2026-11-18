//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using Xunit;

namespace AngouriMath.Mcp.Tests;

/// <summary>
/// A small value is not zero. The numeric checks compared a difference against an absolute
/// 1e-6 below a magnitude of 1, so two expressions of size 1e-9 agreed whatever they were, and
/// <c>am_compare_numeric</c> scored no relative error where the reference was below 1e-12 and
/// subtracted the two sides after rounding each to a double.
/// </summary>
public sealed class NumericToleranceTest
{
    [Theory]
    [InlineData("x / 10^9", "2 * x / 10^9")]
    [InlineData("e^(-30) * x", "0")]
    public void VerifyEqualTellsSmallValuesApart(string left, string right)
    {
        var answer = Mcp.Call("am_verify_equal", $$"""{"left":"{{left}}","right":"{{right}}"}""");
        Assert.Equal("solved", answer.Field("status"));
        Assert.False(answer["equal"]!.GetValue<bool>());
    }

    // The tolerance is still relative: a decimal that agrees to eight digits is equal.
    [Theory]
    [InlineData("x / 3", "0.33333333 * x")]
    [InlineData("x / (3 * 10^9)", "0.33333333 * x / 10^9")]
    public void VerifyEqualStillAcceptsARelativelyCloseValue(string left, string right) =>
        Assert.True(Mcp.Call("am_verify_equal", $$"""{"left":"{{left}}","right":"{{right}}"}""")["equal"]!.GetValue<bool>());

    [Fact]
    public void CheckStepsTellsSmallValuesApart()
    {
        var answer = Mcp.Call("am_check_steps", """{"steps":["x / 10^9","2 * x / 10^9"]}""");
        Assert.Equal(1, answer["first_invalid_step"]!.GetValue<int>());
        Assert.False(answer["steps"]![0]!["equal"]!.GetValue<bool>());
    }

    [Fact]
    public void CheckStepsAcceptsAStepThatCancelsToZero()
    {
        var answer = Mcp.Call("am_check_steps", """{"steps":["sin(x)^2 + cos(x)^2 - 1","0"]}""");
        Assert.True(answer["steps"]![0]!["equal"]!.GetValue<bool>());
    }

    // e^(-x) is below 1e-12 across the whole interval, and 0 is wrong by all of it.
    [Fact]
    public void CompareNumericScoresTheRelativeErrorOfASmallReference()
    {
        var answer = Mcp.Call("am_compare_numeric",
            """{"reference":"e^(-x)","approximation":"0","variable":"x","from":30,"to":40,"samples":5}""");
        Assert.Equal(1.0, answer["max_relative_error"]!.GetValue<double>(), 12);
    }

    // Both sides round to the double 1, so subtracting the doubles gave an error of 0.
    [Fact]
    public void CompareNumericSubtractsBeforeRounding()
    {
        var answer = Mcp.Call("am_compare_numeric",
            """{"reference":"1 + e^(-40) * x","approximation":"1","variable":"x","from":1,"to":2,"samples":3}""");
        Assert.InRange(answer["max_absolute_error"]!.GetValue<double>(), 8.49e-18, 8.51e-18);
        Assert.Equal("2", answer.Field("max_absolute_error_at"));
    }

    // e^1000 is past the range of a double. Such a point is skipped, as it was, rather than
    // giving the tolerance an infinite scale that every difference fits inside.
    [Fact]
    public void AValuePastADoubleIsNotEqualToTwiceItself()
    {
        var answer = Mcp.Call("am_verify_equal", """{"left":"e^1000 * x","right":"2 * e^1000 * x"}""");
        Assert.Equal("inconclusive", answer.Field("status"));
    }
}
