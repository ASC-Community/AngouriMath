//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Text;

namespace AngouriMath
{
    partial record Entity
    {
        partial record CalculusOperator
        {
            /// <inheritdoc/>
            internal override Priority LatexPriority => Priority.LatexCalculusOperation;
        }
        public partial record Derivativef
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
            {
                if (Iterations < 0)
                {
                    var sb = new StringBuilder();
                    for (int i = 0; i < -Iterations; i++)
                        sb.Append(@"\int");
                    sb.Append(' ').Append(Expression.Latexize(Expression.LatexPriority < Priority.LatexCalculusOperation));

                    for (int i = 0; i < -Iterations; i++)
                    {
                        sb.Append(@"\,");
                        sb.Append(@"\mathrm{d}");
                        sb.Append(Var.Latexize(Var is not Variable { IsLatexUprightFormatted: false }));
                    }
                    return sb.ToString();
                }
                var powerIfNeeded = Iterations == 1 ? "" : "^{" + Iterations + "}";
                // NOTE: \mathrm{d} is used for upright 'd' following ISO 80000-2 standard.
                // The differential operator should be upright (roman) to distinguish it from variables, similar to sin, cos, log, etc.
                return $$"""\frac{\mathrm{d}{{powerIfNeeded}}}{\mathrm{d}{{Var.Latexize(Var is not Variable { IsLatexUprightFormatted: false })
                    }}{{powerIfNeeded}}}{{Expression.Latexize(Expression.LatexPriority < Priority.LatexCalculusOperation)}}""";
            }
        }

        public partial record Integralf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
            {
                var sb = new StringBuilder(@"\int");
                if (Range is var (from, to)) sb.Append('_').Append('{').Append(from.Latexize()).Append('}').Append('^').Append('{').Append(to.Latexize()).Append('}');
                sb.Append(' ').Append(Expression.Latexize(Expression.LatexPriority < Priority.LatexCalculusOperation));

                // NOTE: \mathrm{d} is used for upright 'd' following ISO 80000-2 standard.
                // The differential operator should be upright (roman) to distinguish it from variables.
                // Multiple integrals use repeated differentials (\mathrm{d}x \mathrm{d}x) rather than power notation (\mathrm{d}^2 x).
                // While derivatives use \mathrm{d}^n / \mathrm{d}x^n, power notation for integrals (\mathrm{d}^2 x) would be confusing
                // as the number of \mathrm{d} is usually expected to match the number of \int.
                // Thin spaces (\,) are added between differentials following standard practice.
                sb.Append(@"\,"); // Leading space before first differential and between differentials
                sb.Append(@"\mathrm{d}");
                sb.Append(Var.Latexize(Var is not Variable { IsLatexUprightFormatted: false }));
                return sb.ToString();
            }
        }

        public partial record SumOverSetf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() =>
                @"\sum_{" + Var.Latexize() + @" \in " + Over.Latexize() + "} "
                + Expression.Latexize(Expression.Priority < Priority.Sum);
        }

        public partial record Maximumf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() =>
                Expression == Var && Over is Set ? @"\max " + Over.Latexize()
                : @"\max_{" + Var.Latexize() + @" \in " + Over.Latexize() + "} "
                + Expression.Latexize(Expression.Priority < Priority.Sum);
        }

        public partial record Minimumf
        {
            /// <inheritdoc/>
            // The least member of a set is \min S, with no name under it.
            private protected override string LatexizeNode() =>
                Expression == Var && Over is Set ? @"\min " + Over.Latexize()
                : @"\min_{" + Var.Latexize() + @" \in " + Over.Latexize() + "} "
                + Expression.Latexize(Expression.Priority < Priority.Sum);
        }

        public partial record Argmaxf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() =>
                @"\operatorname{argmax}_{" + Var.Latexize() + @" \in " + Over.Latexize() + "} "
                + Expression.Latexize(Expression.Priority < Priority.Sum);
        }

        public partial record Argminf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() =>
                @"\operatorname{argmin}_{" + Var.Latexize() + @" \in " + Over.Latexize() + "} "
                + Expression.Latexize(Expression.Priority < Priority.Sum);
        }

        public partial record Summationf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() =>
                @"\sum_{" + Var.Latexize() + "=" + From.Latexize() + "}^{" + To.Latexize() + "}"
                + Expression.Latexize(Expression.Priority < Priority.Sum);
        }

        public partial record Productf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() =>
                @"\prod_{" + Var.Latexize() + "=" + From.Latexize() + "}^{" + To.Latexize() + "}"
                + Expression.Latexize(Expression.Priority < Priority.Mul);
        }

        public partial record Limitf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
            {
                var sb = new StringBuilder();
                sb.Append(@"\lim_{").Append(Var.Latexize())
                    .Append(@"\to ");

                switch (ApproachFrom)
                {
                    case ApproachFrom.Left:
                        sb.Append(Destination.Latexize(Destination.LatexPriority <= Priority.Pow)).Append("^-");
                        break;
                    case ApproachFrom.Right:
                        sb.Append(Destination.Latexize(Destination.LatexPriority <= Priority.Pow)).Append("^+");
                        break;
                    case ApproachFrom.BothSides:
                        sb.Append(Destination.Latexize());
                        break;
                }

                sb.Append("} ");
                sb.Append(Expression.Latexize(Expression.LatexPriority < Priority.LatexCalculusOperation));
                return sb.ToString();
            }
        }
    }
}
