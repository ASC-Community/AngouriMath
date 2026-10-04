//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Transformations.Matching;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    [Trait("Area", "Core")]
    public sealed class EBindingsTest
    {
        [Fact]
        public void EmptyHasNothingBound()
        {
            Assert.False(EBindings.Empty.TryGet("x", out _));
        }

        [Fact]
        public void WithBindsAName()
        {
            var bindings = EBindings.Empty.With("x", 7);
            Assert.True(bindings.TryGet("x", out var value));
            Assert.Equal(7, value);
        }

        [Fact]
        public void ANameBoundTwiceReadsAsTheNewest()
        {
            var bindings = EBindings.Empty.With("x", 1).With("x", 2);
            Assert.True(bindings.TryGet("x", out var value));
            Assert.Equal(2, value);
        }

        [Fact]
        public void WithDoesNotMutateTheOriginal()
        {
            var original = EBindings.Empty.With("x", 1);
            _ = original.With("x", 2);
            Assert.True(original.TryGet("x", out var value));
            Assert.Equal(1, value);
        }
    }
}
