//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using Xunit;

namespace AngouriMath.Mcp.Tests;

/// <summary>
/// <c>amcli mcp --selftest</c>, held to more than its exit code: a documented defect that no
/// longer reproduces fails here too, since the server's documents then describe a library this
/// repository no longer builds.
/// </summary>
public sealed class SelfTestTest
{
    [Fact]
    public void EveryIdentityHoldsAndEveryDocumentedDefectStillReproduces()
    {
        using var output = new StringWriter();
        var (failed, drifted) = SelfTest.Check(output);
        Assert.True(failed == 0 && drifted == 0, output.ToString());
    }
}
