//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// <see cref="MathS.Dimensions"/>: the rules are the textbook ones, so each case is a
    /// formula whose verdict a physicist can read off by eye.
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class DimensionsTest
    {
        private static readonly Dimension L = Dimension.Length, M = Dimension.Mass, T = Dimension.Time;

        private static readonly Dictionary<Variable, Dimension> Mechanics = new()
        {
            ["h"] = L, ["d"] = L, ["l"] = L,
            ["t"] = T, ["tau"] = T,
            ["m"] = M,
            ["v"] = L / T,
            ["g"] = L / T.Pow(2),
            ["n"] = Dimension.Dimensionless,
        };

        private static DimensionCheck Check(string expr) => MathS.Dimensions.Of(expr.ToEntity(), Mechanics);

        [Theory]
        [InlineData("sqrt(2 g h)", "L T^-1")]
        [InlineData("2 pi sqrt(l / g)", "T")]
        [InlineData("m g h", "L^2 M T^-2")]
        [InlineData("m v^2 / 2", "L^2 M T^-2")]
        [InlineData("h / d", "1")]
        [InlineData("sin(h / d)", "1")]
        [InlineData("e^(-t / tau)", "1")]
        [InlineData("ln(h / d)", "1")]
        [InlineData("abs(h - d)", "L")]
        [InlineData("sign(h)", "1")]
        [InlineData("h^(3/2)", "L^(3/2)")]
        [InlineData("max(h, d)", "L")]
        [InlineData("h + 0", "L")]
        [InlineData("derivative(h, t)", "L T^-1")]
        [InlineData("integral(v, t)", "L")]
        [InlineData("limit(h / t, t, +oo)", "L T^-1")]
        [InlineData("h provided t > 0", "L")]
        public void AConsistentFormulaHasItsDimension(string expr, string dimension)
        {
            var check = Check(expr);
            Assert.True(check.IsConsistent, check.ToString());
            Assert.Equal(dimension, check.Dimension!.ToString());
        }

        [Theory]
        [InlineData("h - g t^2 / 2 = 0")]
        [InlineData("v = 0")]
        [InlineData("h > d and t < tau")]
        [InlineData("v^2 = 2 g h")]
        public void AConsistentStatementIsATruthValue(string expr)
        {
            var check = Check(expr);
            Assert.True(check.IsConsistent, check.ToString());
            Assert.True(check.Dimension!.IsDimensionless);
        }

        [Theory]
        [InlineData("h + g t", "h + g * t")]
        [InlineData("v = g h", "v = g * h")]
        [InlineData("sin(h)", "sin(h)")]
        [InlineData("e^t", "t")]
        [InlineData("ln(h)", "ln(h)")]
        [InlineData("h^pi", "h ^ pi")]
        [InlineData("h provided t > h", "t > h")]
        [InlineData("v = g t^2", "v = g * t ^ 2")]
        public void AnInconsistentFormulaNamesWhereItBreaks(string expr, string at)
        {
            var check = Check(expr);
            Assert.Equal(DimensionStatus.Inconsistent, check.Status);
            Assert.Equal(at, check.At!.ToString());
        }

        // Written, not simplified: the formula is meaningless before it vanishes.
        [Fact]
        public void NothingIsSimplifiedAway()
            => Assert.Equal(DimensionStatus.Inconsistent, Check("sin(h) - sin(h)").Status);

        [Theory]
        [InlineData("h + w", "w")]
        [InlineData("h^n", "h ^ n")]
        public void WhatCannotBeSettledIsNotGuessed(string expr, string at)
        {
            var check = Check(expr);
            Assert.Equal(DimensionStatus.Undetermined, check.Status);
            Assert.Equal(at, check.At!.ToString());
        }

        [Fact]
        public void APendulumHasOneGroup()
        {
            var groups = MathS.Dimensions.DimensionlessGroups(new Variable[] { "l", "g", "m", "t" }, Mechanics);
            var group = Assert.Single(groups);
            Assert.Equal("g * t ^ 2 / l", group.ToString());
        }

        [Fact]
        public void PipeFlowHasTheReynoldsNumber()
        {
            var dims = new Dictionary<Variable, Dimension>
            {
                ["rho"] = M / L.Pow(3), ["v"] = L / T, ["d"] = L, ["mu"] = M / (L * T),
            };
            var group = Assert.Single(MathS.Dimensions.DimensionlessGroups(new Variable[] { "rho", "v", "d", "mu" }, dims));
            Assert.Equal("mu / (rho * v * d)", group.ToString());
        }

        [Fact]
        public void EveryGroupIsAPureNumber()
        {
            var vars = new Variable[] { "h", "t", "v", "g", "m", "d", "n" };
            var groups = MathS.Dimensions.DimensionlessGroups(vars, Mechanics);
            // Seven variables over a dimension matrix of rank three.
            Assert.Equal(4, groups.Count);
            foreach (var group in groups)
                Assert.True(MathS.Dimensions.Of(group, Mechanics).Dimension!.IsDimensionless, group.ToString());
        }

        [Fact]
        public void IndependentDimensionsHaveNoGroup()
            => Assert.Empty(MathS.Dimensions.DimensionlessGroups(new Variable[] { "h", "m" }, Mechanics));

        [Fact]
        public void AnUndeclaredVariableIsRefused()
            => Assert.Throws<ArgumentException>(() => MathS.Dimensions.DimensionlessGroups(new Variable[] { "h", "w" }, Mechanics));

        [Fact]
        public void DimensionArithmetic()
        {
            Assert.Equal("L M T^-2", Dimension.Create(1, 1, -2).ToString());
            Assert.Equal(M * L / T.Pow(2), Dimension.Create(1, 1, -2));
            Assert.Equal("L^(1/2)", L.Pow(Number.Rational.Create(1, 2)).ToString());
            Assert.Equal("1", (L / L).ToString());
            Assert.True((L / L).IsDimensionless);
            Assert.False(L.Pow(Number.Rational.Create(1, 2)).IsIntegral);
        }
    }
}
