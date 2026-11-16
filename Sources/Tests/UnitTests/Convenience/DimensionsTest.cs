//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
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

        // Drag on a sphere: force, density, speed, diameter, viscosity. Five variables over the
        // three dimensions M, L, T, all reached, so two groups: the drag coefficient's and the
        // Reynolds number's.
        [Fact]
        public void DragHasTwoGroups()
        {
            var dims = new Dictionary<Variable, Dimension>
            {
                ["F"] = M * L / T.Pow(2), ["rho"] = M / L.Pow(3), ["v"] = L / T, ["d"] = L, ["mu"] = M / (L * T),
            };
            var groups = MathS.Dimensions.DimensionlessGroups(new Variable[] { "rho", "v", "d", "F", "mu" }, dims);
            Assert.Equal(new[] { "F / (rho * v ^ 2 * d ^ 2)", "mu / (rho * v * d)" }, groups.Select(g => g.ToString()));
            foreach (var group in groups)
                Assert.Equal("Consistent: 1", MathS.Dimensions.Of(group, dims).ToString());
        }

        // Water waves, from omega^2 = g k tanh(k h): the relation is consistent, the phase of
        // the shallow-water slip omega = sqrt(g k h) is not, and wave number, gravity, amplitude,
        // depth and time give the groups k A, k h and g k t^2.
        [Fact]
        public void WaterWaves()
        {
            var dims = new Dictionary<Variable, Dimension>
            {
                ["omega"] = Dimension.Dimensionless / T, ["g"] = L / T.Pow(2), ["k"] = Dimension.Dimensionless / L,
                ["h"] = L, ["A"] = L, ["x"] = L, ["t"] = T,
            };
            Assert.Equal("Consistent: 1", MathS.Dimensions.Of("omega^2 = g k tanh(k h)", dims).ToString());
            Assert.Equal("Consistent: L", MathS.Dimensions.Of("A sin(k x - omega t)", dims).ToString());
            Assert.Equal(DimensionStatus.Inconsistent, MathS.Dimensions.Of("A sin(k x - sqrt(g k h) t)", dims).Status);

            var groups = MathS.Dimensions.DimensionlessGroups(new Variable[] { "k", "g", "A", "h", "t" }, dims);
            Assert.Equal(new[] { "k * A", "k * h", "k * g * t ^ 2" }, groups.Select(g => g.ToString()));
            foreach (var group in groups)
                Assert.Equal("Consistent: 1", MathS.Dimensions.Of(group, dims).ToString());
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

        [Theory]
        [InlineData("iverson(h > d)", "1")]
        [InlineData("dot([h, d], [v, v])", "L^2 T^-1")]
        [InlineData("cross([h, d, l], [v, v, v])", "L^2 T^-1")]
        [InlineData("abs([v, v])", "L T^-1")]
        [InlineData("norm([h, d], 1)", "L")]
        [InlineData("det([[h, d], [d, h]])", "L^2")]
        [InlineData("trace([[h, d], [d, h]])", "L")]
        [InlineData("zeta(h / d)", "1")]
        [InlineData("erf(t / tau)", "1")]
        [InlineData("tanh(h / d)", "1")]
        [InlineData("conj(v)", "L T^-1")]
        [InlineData("integral(v, t, 0, tau)", "L")]
        [InlineData("product(h, n, 1, 3)", "L^3")]
        [InlineData("sum(h / n, n, 1, +oo)", "L")]
        public void TheRulesOfLaterNodes(string expr, string dimension)
        {
            var check = Check(expr);
            Assert.True(check.IsConsistent, check.ToString());
            Assert.Equal(dimension, check.Dimension!.ToString());
        }

        [Theory]
        [InlineData("zeta(h)")]
        [InlineData("erf(t)")]
        [InlineData("tanh(h)")]
        [InlineData("iverson(h > t)")]
        [InlineData("dot([h, d], [v, v]) = h")]
        [InlineData("integral(v, t, 0, h)")]
        public void LaterNodesRejectAQuantityWhereItCannotBe(string expr)
            => Assert.Equal(DimensionStatus.Inconsistent, Check(expr).Status);

        // A product of a symbolic number of dimensional terms, the determinant of a matrix of
        // unknown order, a vector of mixed dimensions and a variable ranging over a set that
        // fixes no dimension each leave the answer open.
        [Fact]
        public void WhatTheLaterNodesCannotSettleIsUndetermined()
        {
            Variable k = "k", w = "w";
            Assert.Equal(DimensionStatus.Undetermined, MathS.Dimensions.Of(new Productf("h", k, 1, "n"), Mechanics).Status);
            Assert.Equal(DimensionStatus.Undetermined, MathS.Dimensions.Of(new Determinantf("h"), Mechanics).Status);
            Assert.Equal(DimensionStatus.Undetermined, MathS.Dimensions.Of(MathS.Vector("h", "v"), Mechanics).Status);
            Assert.Equal(DimensionStatus.Undetermined, MathS.Dimensions.Of(new Maximumf(k, k, MathS.Interval(0, Number.Real.PositiveInfinity)), Mechanics).Status);
            Assert.Equal(DimensionStatus.Undetermined, MathS.Dimensions.Of(new ProductOverSetf("h", k, MathS.Sets.Z), Mechanics).Status);
            Assert.Equal(DimensionStatus.Undetermined, MathS.Dimensions.Of(new Set.Inf(w, MathS.Interval(0, "h")), Mechanics).Status);
        }

        // l in RR says a length is real, and is not a claim that it is a pure number; a sum
        // over the integers makes its undeclared index one.
        [Fact]
        public void ANumberSetFitsAQuantityAndMakesAnIndexAPureNumber()
        {
            Variable k = "k";
            Assert.True(MathS.Dimensions.Of(new Set.Inf("h", MathS.Sets.R), Mechanics).IsConsistent);
            Assert.Equal("L", MathS.Dimensions.Of(new SumOverSetf(new Divf("h", new Powf(k, 2)), k, MathS.Sets.PositiveIntegers), Mechanics).Dimension!.ToString());
            // A declared variable ranging over a set whose members measure something else.
            Assert.Equal(DimensionStatus.Inconsistent, MathS.Dimensions.Of(new Maximumf("h", "h", MathS.Interval(0, "t")), Mechanics).Status);
        }

        // ---------------------------------------------------------------- every node type

        private static readonly Variable X = "x", Y = "y", Tt = "t", V = "v", N = "n", K = "k";

        private static readonly Dictionary<Variable, Dimension> Nodes = new()
        {
            [X] = L, [Y] = L, [Tt] = T, [V] = L / T, [N] = Dimension.Dimensionless,
        };

        private const string NotRead = "not read";

        /// <summary>
        /// One sample of every concrete node type, with the verdict it gets with x and y lengths,
        /// t a time, v a speed and n a pure number. <see cref="NotRead"/> marks a node the check
        /// reports undetermined because it does not read it.
        /// </summary>
        private static readonly (Entity Sample, string Verdict)[] EveryNode =
        {
            (X, "Consistent: L"),
            (MathS.pi, "Consistent: 1"),
            (3, "Consistent: 1"),
            (Number.Rational.Create(1, 2), "Consistent: 1"),
            (MathS.pi.Evaled, "Consistent: 1"),
            (Number.Complex.Create(1, 2), "Consistent: 1"),
            (Entity.Boolean.True, "Consistent: 1"),

            (new Sumf(X, Y), "Consistent: L"),
            (new Minusf(X, Tt), "Inconsistent"),
            (new Mulf(X, Tt), "Consistent: L T"),
            (new Divf(X, Tt), "Consistent: L T^-1"),
            (new Powf(X, 2), "Consistent: L^2"),
            (new Modf(X, Y), "Consistent: L"),
            (new Maxf(X, Tt), "Inconsistent"),
            (new Minf(X, Y), "Consistent: L"),

            (new Absf(V), "Consistent: L T^-1"),
            (new Conjugatef(X), "Consistent: L"),
            (new Ref(X), "Consistent: L"),
            (new Imf(X), "Consistent: L"),
            (new Signumf(X), "Consistent: 1"),
            (new Argf(X), "Consistent: 1"),

            // A length where a pure number is needed.
            (new Sinf(X), "Inconsistent"),
            (new Cosf(X), "Inconsistent"),
            (new Tanf(X), "Inconsistent"),
            (new Cotanf(X), "Inconsistent"),
            (new Secantf(X), "Inconsistent"),
            (new Cosecantf(X), "Inconsistent"),
            (new Arcsinf(X), "Inconsistent"),
            (new Arccosf(X), "Inconsistent"),
            (new Arctanf(X), "Inconsistent"),
            (new Arccotanf(X), "Inconsistent"),
            (new Arcsecantf(X), "Inconsistent"),
            (new Arccosecantf(X), "Inconsistent"),
            (new Logf(N, X), "Inconsistent"),
            (new Eif(X), "Inconsistent"),
            (new Sif(X), "Inconsistent"),
            (new Cif(X), "Inconsistent"),
            (new Shif(X), "Inconsistent"),
            (new Chif(X), "Inconsistent"),
            (new Lif(X), "Inconsistent"),
            (new Erff(X), "Inconsistent"),
            (new Erfcf(X), "Inconsistent"),
            (new Erfif(X), "Inconsistent"),
            (new LambertWf(N, X), "Inconsistent"),
            (new Polygammaf(N, X), "Inconsistent"),
            (new Zetaf(X, N), "Inconsistent"),
            (new Betaf(X, N), "Inconsistent"),
            (new IncompleteGammaf(N, X), "Inconsistent"),
            (new Factorialf(X), "Inconsistent"),
            (new DoubleFactorialf(X), "Inconsistent"),
            (new Binomialf(X, N), "Inconsistent"),
            (new FallingFactorialf(X, N), "Inconsistent"),
            (new RisingFactorialf(X, N), "Inconsistent"),
            (new CatalanNumberf(X), "Inconsistent"),
            (new HarmonicNumberf(X, N), "Inconsistent"),
            (new Floorf(X), "Inconsistent"),
            (new Ceilf(X), "Inconsistent"),
            (new Roundf(X), "Inconsistent"),
            (new Gcdf(X, N), "Inconsistent"),
            (new Lcmf(X, N), "Inconsistent"),
            (new Phif(X), "Inconsistent"),
            (new Mobiusf(X), "Inconsistent"),
            (new DivisorSigmaf(N, X), "Inconsistent"),
            (new PrimeNuf(X), "Inconsistent"),
            (new PrimeOmegaf(X), "Inconsistent"),
            (new PrimePif(X), "Inconsistent"),
            (new Primef(X), "Inconsistent"),
            (new Valuationf(X, N), "Inconsistent"),
            (new PrimeFactorizationf(X), "Inconsistent"),
            (new Alephf(X), "Inconsistent"),
            (new Iversonf(X), "Inconsistent"),
            (new Andf(X, Entity.Boolean.True), "Inconsistent"),
            (new Orf(new Greaterf(X, Y), new Lessf(Tt, 0)), "Consistent: 1"),
            (new Notf(X), "Inconsistent"),
            (new Xorf(new Greaterf(X, Y), Entity.Boolean.False), "Consistent: 1"),
            (new Impliesf(new Greaterf(X, Y), new Greaterf(X, Tt)), "Inconsistent"),
            (new Dividesf(X, N), "Inconsistent"),
            (new Congruentf(X, N, 3), "Inconsistent"),

            (new Cardf(MathS.Sets.Finite(X, Y)), "Consistent: 1"),
            (MathS.Vector(X, Y), "Consistent: L"),
            (new DotProductf(MathS.Vector(V, V), MathS.Vector(Tt, Tt)), "Consistent: L"),
            (new CrossProductf(MathS.Vector(X, X, X), MathS.Vector(V, V, V)), "Consistent: L^2 T^-1"),
            (new Normf(MathS.Vector(X, Y), 2), "Consistent: L"),
            (new Tracef(MathS.Matrix(new Entity[,] { { X, Y }, { Y, X } })), "Consistent: L"),
            (new Transposef(MathS.Vector(X, Y)), "Consistent: L"),
            (new MatrixEntryf(MathS.Vector(X, Y), 1, 1), "Consistent: L"),
            (new Determinantf(MathS.Matrix(new Entity[,] { { X, Y }, { Y, X } })), "Consistent: L^2"),

            (MathS.Derivative(X, Tt, 2), "Consistent: L T^-2"),
            (MathS.PartialDerivative(X, Tt, 1), "Consistent: L T^-1"),
            (MathS.Integral(V, Tt, 0, Tt), "Consistent: L"),
            (MathS.Limit(X / Tt, Tt, 0), "Consistent: L T^-1"),
            (new Summationf(X, K, 1, N), "Consistent: L"),
            (new Productf(X, K, 1, 3), "Consistent: L^3"),
            (new SumOverSetf(K * X, K, MathS.Sets.Finite(1, 2)), "Consistent: L"),
            (new ProductOverSetf(X, K, MathS.Sets.Finite(1, 2, 3)), "Consistent: L^3"),
            (new Maximumf(K, K, MathS.Interval(0, Y)), "Consistent: L"),
            (new Minimumf(K * V, K, MathS.Interval(0, Tt)), "Consistent: L"),

            (new Providedf(X, new Greaterf(Tt, 0)), "Consistent: L"),
            (MathS.Piecewise((X, new Greaterf(Tt, 0)), (Y, new LessOrEqualf(Tt, 0))), "Consistent: L"),
            (new Equalsf(X, Y), "Consistent: 1"),
            (new Lessf(X, Tt), "Inconsistent"),
            (new Greaterf(V, 0), "Consistent: 1"),
            (new LessOrEqualf(X, Y), "Consistent: 1"),
            (new GreaterOrEqualf(Tt, V), "Inconsistent"),
            (new Set.Inf(X, MathS.Interval(0, Y)), "Consistent: 1"),
            (new Set.Subsetf(MathS.Interval(0, X), MathS.Interval(0, Tt)), "Inconsistent"),
            (new Forallf(K, MathS.Sets.R, new Greaterf(K * K, 0)), "Consistent: 1"),
            (new Existsf(K, MathS.Interval(0, Y), new Greaterf(K, X)), "Consistent: 1"),
            (new ExistsUniquef(K, MathS.Interval(0, Y), new Greaterf(K, Tt)), "Inconsistent"),

            // A set is not a quantity; it is read only where a variable ranges over it.
            (MathS.Sets.Finite(X, Y), NotRead),
            (MathS.Interval(0, X), NotRead),
            (new Set.ConditionalSet(X, new Greaterf(X, 0)), NotRead),
            (MathS.Sets.R, NotRead),
            (MathS.Sets.C, NotRead),
            (MathS.Sets.Q, NotRead),
            (MathS.Sets.Z, NotRead),
            (MathS.Sets.NonNegativeIntegers, NotRead),
            (MathS.Sets.PositiveIntegers, NotRead),
            (MathS.Sets.Primes, NotRead),
            (Set.SpecialSet.Create(Domain.Boolean), NotRead),
            (new Set.Unionf(MathS.Interval(0, X), MathS.Interval(0, Y)), NotRead),
            (new Set.Intersectionf(MathS.Interval(0, X), MathS.Interval(0, Y)), NotRead),
            (new Set.SetMinusf(MathS.Interval(0, X), MathS.Interval(0, Y)), NotRead),
            (new Set.Powersetf(MathS.Sets.Finite(X)), NotRead),
            (new Set.PrimeFactorsf(N), NotRead),
            (new Set.IndexedUnionf(K, MathS.Sets.Finite(1, 2), MathS.Interval(0, K * X)), NotRead),
            (new Set.IndexedIntersectionf(K, MathS.Sets.Finite(1, 2), MathS.Interval(0, K * X)), NotRead),
            (new Argmaxf(K, K, MathS.Interval(0, Y)), NotRead),
            (new Argminf(K, K, MathS.Interval(0, Y)), NotRead),
            // A function, and a function applied, whose dimensions nothing declares.
            (MathS.Lambda(X, X), NotRead),
            (MathS.Apply(MathS.Var("f"), X), NotRead),
        };

        private static string Verdict(DimensionCheck check)
            => check.Status switch
            {
                DimensionStatus.Consistent => check.ToString(),
                DimensionStatus.Undetermined when check.Reason.EndsWith("is not read by the dimension check", StringComparison.Ordinal) => NotRead,
                var status => status.ToString(),
            };

        /// <summary>
        /// Every node type has a sample above and the check gives it the verdict listed.
        /// </summary>
        /// <remarks>
        /// The walk reads a node only where an arm names it, so a node type added to the
        /// library is reported undetermined rather than read as a pure number by default; and
        /// this fails on it, by name, until a sample and a verdict are written for it here,
        /// which is where someone decides what its dimension is.
        /// </remarks>
        [Fact]
        public void EveryNodeTypeHasADecidedReading()
        {
            var types = typeof(Entity).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(Entity).IsAssignableFrom(t))
                .Select(t => t.Name).ToHashSet();
            var sampled = EveryNode.Select(e => e.Sample.GetType().Name).ToHashSet();
            Assert.True(types.SetEquals(sampled),
                "node types with no sample: " + string.Join(", ", types.Except(sampled).OrderBy(n => n, StringComparer.Ordinal))
                + "; samples of no node type: " + string.Join(", ", sampled.Except(types)));

            var wrong = EveryNode
                .Select(e => (e.Sample, e.Verdict, Actual: MathS.Dimensions.Of(e.Sample, Nodes)))
                .Where(e => Verdict(e.Actual) != e.Verdict)
                .Select(e => $"{e.Sample.GetType().Name} {e.Sample}: expected {e.Verdict}, got {e.Actual}")
                .ToList();
            Assert.True(wrong.Count is 0, string.Join("\n", wrong));
        }

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
