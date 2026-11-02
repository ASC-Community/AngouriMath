//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// An equation in a radical is raised to a power, which adds roots, and each root is put back into the equation to be
    /// dropped where it fails. With a parameter left in that cannot be decided, and every root of the raised equation was
    /// answered: <c>sqrt(x + 1) + a + x = 0</c> had both roots of its square, and for each <c>a</c> one of them is not a root.
    /// Each is now provided it is one. No issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ARaisedRootWithAParameterTest
    {
        // Each root left once a is given is a root; a = 2 has none, both roots of the square being complex and not roots.
        [Theory]
        [InlineData("sqrt(x + 1) + a + x = 0", -5, new[] { "3" })]
        [InlineData("sqrt(x + 1) + a + x = 0", 0, new[] { "(1 - sqrt(5)) / 2" })]
        [InlineData("sqrt(x + 1) + a + x = 0", 2, new string[0])]
        [InlineData("x + sqrt(x - a) = 3", 2, new[] { "(7 - sqrt(5)) / 2" })]
        public void AreTheRootsOnceTheParameterIsGiven(string equation, int a, string[] expected)
        {
            var roots = Assert.IsType<Entity.Set.FiniteSet>(equation.ToEntity().Solve("x").Substitute("a", a).Simplify());
            Assert.Equal(expected.Select(root => root.ToEntity().Simplify()).ToHashSet(), roots.ToHashSet());
        }
    }
}
