//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// Each member's documentation describes that member.
    /// </summary>
    /// <remarks>
    /// A member added between a doc comment and the declaration it was written for takes that
    /// comment on top of its own, and the compiler says nothing: the member it described is left
    /// undocumented and the new one shows two summaries. Twenty-three comments had drifted that
    /// way, one since the commit that removed the method it was for, and seven members had their
    /// remarks written as two. A second <c>&lt;summary&gt;</c>, <c>&lt;remarks&gt;</c>,
    /// <c>&lt;returns&gt;</c> or <c>&lt;value&gt;</c> is the trace either leaves. A member may
    /// have several examples, which <see cref="DocumentationExamplesRunTest"/> runs one by one.
    /// </remarks>
    [Trait("Area", "Convenience")]
    public sealed class DocCommentPlacementTest
    {
        [Fact]
        public void NoMemberCarriesASecondSummaryOrRemarks()
        {
            var path = Path.Combine(Path.GetDirectoryName(typeof(MathS).Assembly.Location)!, "AngouriMath.xml");
            var repeated = XDocument.Load(path).Descendants("member")
                .Where(member => member.Elements()
                    .GroupBy(element => element.Name.LocalName)
                    .Any(group => group.Key is "summary" or "remarks" or "returns" or "value" && group.Count() > 1))
                .Select(member => member.Attribute("name")?.Value)
                .ToList();
            Assert.True(repeated.Count == 0,
                "A doc comment that belongs to another member, or one element written twice:\n"
                + string.Join("\n", repeated));
        }
    }
}
