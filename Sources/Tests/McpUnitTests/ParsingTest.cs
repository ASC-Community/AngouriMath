//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Text.RegularExpressions;
using AngouriMath.Core.Exceptions;
using Xunit;

namespace AngouriMath.Mcp.Tests;

/// <summary>
/// The server's knowledge of the grammar, checked against the grammar itself: every function
/// token in <c>Core/Antlr/AngouriMath.g</c>, so a function the grammar gains or loses fails
/// here until the server's warnings and its syntax document agree with it.
/// </summary>
public sealed class ParsingTest
{
    /// <summary>The names the grammar has a <c>'name('</c> token for.</summary>
    public static IEnumerable<object[]> GrammarFunctions()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AngouriMath", "Core", "Antlr", "AngouriMath.g")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var grammar = File.ReadAllText(Path.Combine(directory!.FullName, "AngouriMath", "Core", "Antlr", "AngouriMath.g"));
        return Regex.Matches(grammar, @"'([A-Za-z][A-Za-z0-9_]*)\('")
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .Select(name => new object[] { name });
    }

    [Fact]
    public void TheGrammarHasFunctions() => Assert.True(GrammarFunctions().Count() > 100);

    [Theory]
    [MemberData(nameof(GrammarFunctions))]
    public void EveryFunctionInTheGrammarIsReadAsOne(string name) =>
        Assert.True(Parsing.IsFunction(name), $"{name}(x) is read as a product");

    [Theory]
    [MemberData(nameof(GrammarFunctions))]
    public void TheSyntaxDocumentNamesEveryFunctionTheGrammarAccepts(string name)
    {
        try
        {
            MathS.FromString(name + "(x)");
        }
        catch (UnrecognizedFunctionParseException)
        {
            // Refused by name, so the grammar has it only to say why.
            return;
        }
        catch (FunctionArgumentCountException)
        {
            // A function of more arguments than one.
        }
        var syntax = Resources.Read("angourimath://syntax")!;
        Assert.True(Regex.IsMatch(syntax, $@"(?<![A-Za-z0-9_]){Regex.Escape(name)}(?![A-Za-z0-9_])"),
            $"angourimath://syntax does not name {name}, which the grammar reads as a function");
    }

    [Theory]
    [InlineData("im")]
    [InlineData("Γ")]
    [InlineData("e")]
    [InlineData("pi")]
    [InlineData("x2")]
    [InlineData("t_0")]
    public void ANameThatIsNoFunctionIsReadAsAProduct(string name) =>
        Assert.False(Parsing.IsFunction(name), $"{name}(x) is read as a call");

    [Fact]
    public void EveryMisspellingIsReadAsAProduct()
    {
        // A hint for a name the grammar calls would never be shown.
        foreach (var name in Parsing.Misspellings.Keys)
            Assert.False(Parsing.IsFunction(name), $"{name}(x) is read as a call, so its hint is never shown");
    }
}
