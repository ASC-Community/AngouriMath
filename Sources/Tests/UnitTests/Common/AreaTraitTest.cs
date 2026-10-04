//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// Every test class says which area of the library it tests, with
    /// <c>[Trait("Area", …)]</c>, so that a run can be filtered to an area and its time broken
    /// down by area. A taxonomy is worth what its completeness is worth, and nothing else
    /// notices a class without one: there were 22 such files when this was asked for and 37 a
    /// fortnight later.
    /// https://github.com/asc-community/AngouriMath/issues/1183
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class AreaTraitTest
    {
        [Fact]
        public void EveryTestClassHasAnArea()
        {
            var missing = typeof(AreaTraitTest).Assembly.GetTypes()
                .Where(static type => !type.IsAbstract && TestMethods(type).Any())
                .Where(static type => !HasArea(type) && !TestMethods(type).All(HasArea))
                .Select(static type => type.FullName)
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToList();
            Assert.True(missing.Count == 0,
                "These test classes say nothing about which area they test; give each a [Trait(\"Area\", …)]:\n  "
                + string.Join("\n  ", missing));
        }

        /// <summary>The methods xUnit runs as tests: a <see cref="TheoryAttribute"/> is a <see cref="FactAttribute"/>.</summary>
        private static System.Collections.Generic.IEnumerable<MethodInfo> TestMethods(Type type)
            => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(static method => method.IsDefined(typeof(FactAttribute), inherit: true));

        /// <summary>Declared on the member itself or, for a class, on a class it derives from.</summary>
        private static bool HasArea(MemberInfo member)
            => member.GetCustomAttributesData().Any(static attribute =>
                    attribute.AttributeType == typeof(TraitAttribute)
                    && attribute.ConstructorArguments is [{ Value: "Area" }, _])
                || member is Type { BaseType: { } baseType } && baseType != typeof(object) && HasArea(baseType);
    }
}
