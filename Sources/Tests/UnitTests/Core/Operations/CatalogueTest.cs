//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Core.Operations;
using AngouriMath.Core.Transformations;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Core.Operations
{
    /// <summary>
    /// The catalogue of operations: that it lists every entry it declares, that each entry
    /// describes the delegate it holds, and that every factory of <see cref="Transformation"/>
    /// is either an entry or a building block that says why it is not.
    /// https://github.com/asc-community/AngouriMath/issues/1684
    /// </summary>
    [Trait("Area", "Transformations")]
    public sealed class CatalogueTest
    {
        /// <summary>
        /// Each factory of <see cref="Transformation"/> that an entry applies, by the entry's name.
        /// </summary>
        private static readonly Dictionary<string, string> EntryOfFactory = new()
        {
            [nameof(Transformation.Simplification)] = "simplify",
            [nameof(Transformation.SimplificationAtLevel)] = "simplify",
            [nameof(Transformation.InnerSimplification)] = "normalize",
            [nameof(Transformation.Expansion)] = "expand",
            [nameof(Transformation.ExpansionAtLevel)] = "expand",
            [nameof(Transformation.Factorization)] = "factorize",
            [nameof(Transformation.FactorizationAtLevel)] = "factorize",
            [nameof(Transformation.Differentiation)] = "differentiate",
            [nameof(Transformation.Integration)] = "integrate",
            [nameof(Transformation.LimitAt)] = "limit",
            [nameof(Transformation.Substitution)] = "substitute",
        };

        /// <summary>
        /// The factories that are parts for a caller to compose rather than operations to ask
        /// for by name, each with the reason.
        /// </summary>
        private static readonly Dictionary<string, string> BuildingBlocks = new()
        {
            [nameof(Transformation.Rewriting)] = "applies a rule set the caller passes, which no text names",
            [nameof(Transformation.NumericContentExtraction)] = "the last step of factorize",
            [nameof(Transformation.AsSingleFraction)] = "a step of the rational canonical form",
            [nameof(Transformation.RuleBasedFactorizationAtLevel)] = "the rule-based half of factorize",
            [nameof(Transformation.PolynomialFactorization)] = "the polynomial half of factorize",
            [nameof(Transformation.Normalization)] = "orders commutative operands, so that expressions can be compared rather than simplified",
            [nameof(Transformation.RationalCanonicalization)] = "a canonical form for comparing rational functions, which nothing runs by default",
            [nameof(Transformation.Canonicalization)] = "a canonical form for comparing expressions, which nothing runs by default",
            [nameof(Transformation.Rationalization)] = "a step simplify takes",
            [nameof(Transformation.EqualitySaturation)] = "an experimental search, which takes a budget and a cost model",
            [nameof(Transformation.CanonicalizationOverGraph)] = "an experimental search, which takes a budget",
        };

        private static readonly Variable x = MathS.Var("x");

        private static string DelegateName(Operation operation) => operation.GetType().GetGenericArguments().Single().Name;

        [Fact]
        public void TheListHoldsEveryEntryOnce()
        {
            var declared = typeof(Catalogue)
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(property => typeof(Operation).IsAssignableFrom(property.PropertyType))
                .Select(property => (Operation)property.GetValue(null)!)
                .ToList();
            Assert.Equal(declared.Count, Catalogue.All.Count);
            Assert.Equal(declared.OrderBy(o => o.Name), Catalogue.All.OrderBy(o => o.Name));
            Assert.Equal(Catalogue.All.Count, Catalogue.All.Select(o => o.Name).Distinct().Count());
        }

        [Fact]
        public void NamesAreLowercaseWordsJoinedByUnderscores()
        {
            foreach (var operation in Catalogue.All)
                Assert.Matches("^[a-z]+(_[a-z]+)*$", operation.Name);
        }

        [Fact]
        public void FindAnswersByName()
        {
            foreach (var operation in Catalogue.All)
                Assert.Same(operation, Catalogue.Find(operation.Name));
            Assert.Null(Catalogue.Find("simp"));
        }

        [Fact]
        public void EachDescriptionIsOneSentence()
        {
            foreach (var operation in Catalogue.All)
                foreach (var description in operation.Parameters.Select(p => p.Description).Prepend(operation.Description))
                {
                    Assert.False(string.IsNullOrWhiteSpace(description), operation.Name);
                    Assert.DoesNotContain('\n', description);
                    Assert.EndsWith(".", description);
                }
        }

        /// <summary>
        /// The parameters are the delegate's arguments after its input, so a parameter added to
        /// one and not the other is caught here.
        /// </summary>
        [Fact]
        public void TheParametersAreTheDelegatesArgumentsAfterTheInput()
        {
            foreach (var operation in Catalogue.All)
            {
                var invoke = operation.GetType().GetGenericArguments().Single().GetMethod("Invoke")!;
                Assert.True(invoke.GetParameters().Length == operation.Parameters.Count + 1,
                    $"{operation.Name}: {DelegateName(operation)} takes {invoke.GetParameters().Length} arguments, and it names {operation.Parameters.Count} parameters");
                Assert.Equal(operation.Parameters.Count, operation.Parameters.Select(p => p.Name).Distinct().Count());
            }
        }

        /// <summary>
        /// A factory added to <see cref="Transformation"/> fails here until it is either an
        /// entry or a building block, and a name that leaves it fails here too.
        /// </summary>
        [Fact]
        public void EveryFactoryOfTransformationIsPlaced()
        {
            var factories = typeof(Transformation)
                .GetMembers(BindingFlags.Public | BindingFlags.Static)
                .Where(member => member switch
                {
                    PropertyInfo property => property.PropertyType == typeof(Transformation),
                    MethodInfo method => method.ReturnType == typeof(Transformation) && !method.IsSpecialName,
                    _ => false,
                })
                .Select(member => member.Name)
                .ToHashSet();

            var unplaced = factories.Where(name => !EntryOfFactory.ContainsKey(name) && !BuildingBlocks.ContainsKey(name)).ToList();
            Assert.True(unplaced.Count == 0, $"Not an entry and not a building block: {string.Join(", ", unplaced)}");
            var stale = EntryOfFactory.Keys.Concat(BuildingBlocks.Keys).Where(name => !factories.Contains(name)).ToList();
            Assert.True(stale.Count == 0, $"Listed here and no longer a factory: {string.Join(", ", stale)}");
            Assert.Empty(EntryOfFactory.Keys.Intersect(BuildingBlocks.Keys));
            foreach (var entry in EntryOfFactory.Values)
                Assert.NotNull(Catalogue.Find(entry));
        }

        public static readonly IEnumerable<object[]> TransformationsAndTheirEntries = new (Transformation, string)[]
        {
            (Transformation.Simplification, "simplify"),
            (Transformation.InnerSimplification, "normalize"),
            (Transformation.Expansion, "expand"),
            (Transformation.Factorization, "factorize"),
            (Transformation.Differentiation(x), "differentiate"),
            (Transformation.Integration(x), "integrate"),
            (Transformation.LimitAt(x, 0, ApproachFrom.BothSides), "limit"),
            (Transformation.Substitution(x, 1), "substitute"),
        }.Select(pair => new object[] { pair.Item1, pair.Item2 });

        [Theory]
        [MemberData(nameof(TransformationsAndTheirEntries))]
        public void AnEntryClaimsWhatItsTransformationClaims(Transformation transformation, string entry)
        {
            var relation = Catalogue.Find(entry)!.GetType().GetProperty("Relation")!.GetValue(Catalogue.Find(entry));
            Assert.Equal(transformation.Relation, relation);
        }

        /// <summary>
        /// The series is taken to the degree asked for, which is the highest power it keeps. It
        /// was passed on as the number of terms, one fewer: <c>sin(x)</c> to degree 1 was <c>0</c>.
        /// https://github.com/asc-community/AngouriMath/issues/2299
        /// </summary>
        [Theory]
        [InlineData("sin(x)", 1, null, "x")]
        [InlineData("e^x", 1, null, "1 + x")]
        [InlineData("sin(x)", 7, null, "x - x^3/6 + x^5/120 - x^7/5040")]
        [InlineData("e^x", 2, "1", "e + e (x - 1) + e (x - 1)^2 / 2")]
        [InlineData("cos(x)", 0, null, "1")]
        public void ASeriesKeepsTheHighestPowerItIsAskedFor(string expression, int degree, string? around, string expected)
        {
            var series = Catalogue.Series.Ask(expression, MathS.Var("x"), degree, around is null ? null : MathS.FromString(around));
            Assert.Equal(Entity.Number.Integer.Zero, (series - MathS.FromString(expected)).Simplify());
        }

        /// <summary>
        /// One answer from each entry, against the method the entry names, on an input where
        /// that method and its neighbours answer differently -- so that an entry holding the
        /// wrong method is caught.
        /// </summary>
        [Fact]
        public void EachEntryAnswersAsTheMethodItNames()
        {
            Entity trigonometric = "sin(x)^2 + cos(x)^2", square = "(x + 1)^2";
            var y = MathS.Var("y");
            Assert.Equal(trigonometric.Simplify(), Catalogue.Simplify.Apply(trigonometric));
            Assert.Equal(trigonometric.InnerSimplified, Catalogue.Normalize.Apply(trigonometric));
            Assert.NotEqual(Catalogue.Simplify.Apply(trigonometric), Catalogue.Normalize.Apply(trigonometric));
            Assert.Equal(square.Expand(), Catalogue.Expand.Apply(square));
            Assert.Equal(square.Expand().Factorize(), Catalogue.Factorize.Apply(square.Expand()));
            Assert.Equal(square.Differentiate(x, 2), Catalogue.Differentiate.Apply(square, x, 2));
            Assert.Equal(square.Integrate(x), Catalogue.Integrate.Apply(square, x));
            Assert.Equal(MathS.FromString("sin(x) / x").Limit(x, 0, ApproachFrom.Left), Catalogue.Limit.Apply("sin(x) / x", x, 0, ApproachFrom.Left));
            Assert.Equal(square.Substitute(x, y), Catalogue.Substitute.Apply(square, x, y));

            Assert.Equal(MathS.FromString("x^2 = 4").Solve(x), Catalogue.Solve.Ask("x^2 = 4", x));
            Entity[] system = ["x + y - 3", "x - y - 1"];
            Assert.Equal(MathS.Equations(system).Solve(x, y), Catalogue.SolveSystem.Ask(system, [x, y]));
            Assert.Equal(MathS.FromString("sqrt(2) + 1").Evaled, Catalogue.Evaluate.Ask("sqrt(2) + 1"));
            Assert.Equal(MathS.FromString("sqrt(2) + 1").EvalNumerical(), Catalogue.EvaluateNumerically.Ask("sqrt(2) + 1"));
            Assert.Equal(MathS.FromString("x + y").Vars, Catalogue.Variables.Ask("x + y"));
            Assert.Equal(trigonometric.Alternate(2), Catalogue.Alternatives.Ask(trigonometric, 2));
            Assert.Equal(square.Latexize(), Catalogue.Latex.Ask(square));
            Assert.Equal(MathS.ToSympyCode(square), Catalogue.Sympy.Ask(square));
            // HLSL has sincos and GLSL does not, so the two print a sine beside its cosine differently.
            KeyValuePair<string, Entity>[] outputs = [new("r", "sin(x) * cos(x)")];
            var names = new Dictionary<Variable, string> { [x] = "p.x" };
            Assert.Equal(MathS.Shaders.Hlsl(outputs, names), Catalogue.Hlsl.Ask(outputs, names));
            Assert.Equal(MathS.Shaders.Glsl(outputs, names), Catalogue.Glsl.Ask(outputs, names));
            Assert.NotEqual(Catalogue.Hlsl.Ask(outputs, names), Catalogue.Glsl.Ask(outputs, names));
            Assert.Equal(MathS.Boolean.BuildTruthTable("a and not b", "a", "b"), Catalogue.TruthTable.Ask("a and not b", [MathS.Var("a"), MathS.Var("b")]));
            Assert.Equal(MathS.SolveBooleanTable("a and not b", "a", "b"), Catalogue.SatisfyingAssignments.Ask("a and not b", [MathS.Var("a"), MathS.Var("b")]));
            Assert.NotEqual(Catalogue.TruthTable.Ask("a and not b", [MathS.Var("a"), MathS.Var("b")]), Catalogue.SatisfyingAssignments.Ask("a and not b", [MathS.Var("a"), MathS.Var("b")]));
            Assert.Equal(MathS.Series.Maclaurin("e^x", 4, x), Catalogue.Series.Ask("e^x", x, 3, null));
            Assert.Equal(MathS.Series.Taylor("e^x", 4, (x, 1)), Catalogue.Series.Ask("e^x", x, 3, 1));
            Assert.NotEqual(Catalogue.Series.Ask("e^x", x, 3, null), Catalogue.Series.Ask("e^x", x, 3, 1));

            Assert.Equal(MathS.ToBaseN(10.5m, 2), Catalogue.ToBase.Ask(10.5m, 2));
            Assert.Equal(MathS.FromBaseN("1010.1", 2), Catalogue.FromBase.Ask("1010.1", 2));

            var matrix = MathS.Matrix(new Entity[,] { { 1, 2 }, { 3, 4 } });
            Assert.Equal(matrix.Determinant, Catalogue.Determinant.Ask(matrix));
            Assert.Equal(matrix.Inverse, Catalogue.Inverse.Ask(matrix));
            Assert.Equal(matrix.T, Catalogue.Transpose.Ask(matrix));
            Assert.Equal(matrix.Rank, Catalogue.Rank.Ask(matrix));
            Assert.Equal(matrix.ReducedRowEchelonForm, Catalogue.ReducedRowEchelonForm.Ask(matrix));
            Assert.Equal(matrix.Trace, Catalogue.Trace.Ask(matrix));
            Assert.Equal(Matrix.TensorProduct(matrix, matrix.T), Catalogue.TensorProduct.Ask(matrix, matrix.T));

            Number.Integer twelve = 12, eighteen = 18;
            Assert.Equal(MathS.NumberTheory.Factorize(twelve), Catalogue.PrimeFactorization.Ask(twelve));
            Assert.Equal(MathS.NumberTheory.Phi(twelve).Evaled, Catalogue.Totient.Ask(twelve));
            Assert.Equal(MathS.NumberTheory.GreatestCommonDivisor(twelve, eighteen), Catalogue.GreatestCommonDivisor.Ask(twelve, eighteen));
            Assert.Equal(MathS.NumberTheory.CountDivisors(twelve), Catalogue.DivisorCount.Ask(twelve));
            Assert.Equal(twelve.IsPrime, Catalogue.IsPrime.Ask(twelve));
            Assert.True(Catalogue.IsPrime.Ask(7));
        }
    }
}
