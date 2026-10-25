//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using Xunit;

namespace AngouriMath.Mcp.Tests;

/// <summary>
/// The tools against the library's catalogue of operations: every tool offers an entry or says
/// what it computes itself, and every entry is offered by a tool or is listed with the reason
/// none offers it. An operation added to the catalogue fails here until the server takes it.
/// https://github.com/asc-community/AngouriMath/issues/1684
/// </summary>
public sealed class CatalogueTest
{
    private static readonly HashSet<string> ToolNames =
        Tools.List(preliminary: true).Select(tool => tool!["name"]!.GetValue<string>()).ToHashSet();

    [Fact]
    public void EveryToolOffersAnEntryOrSaysWhatItComputes()
    {
        var unaccounted = ToolNames.Where(tool => !Tools.OfferedNames.ContainsKey(tool) && !Tools.OwnComputations.ContainsKey(tool));
        Assert.Empty(unaccounted);
        Assert.Empty(Tools.OfferedNames.Keys.Intersect(Tools.OwnComputations.Keys));
        Assert.Empty(Tools.OfferedNames.Keys.Concat(Tools.OwnComputations.Keys).Where(tool => !ToolNames.Contains(tool)));
        Assert.All(Tools.OfferedNames, tool => Assert.NotEmpty(tool.Value));
    }

    [Fact]
    public void EveryEntryIsOfferedOrSaysWhyNot()
    {
        var offered = Tools.OfferedNames.Values.SelectMany(names => names).ToHashSet();
        var catalogue = Tools.CatalogueNames.ToHashSet();
        Assert.Empty(catalogue.Where(entry => !offered.Contains(entry) && !Tools.NotOffered.ContainsKey(entry)));
        Assert.Empty(offered.Intersect(Tools.NotOffered.Keys));
        Assert.Empty(Tools.NotOffered.Keys.Where(entry => !catalogue.Contains(entry)));
    }
}
