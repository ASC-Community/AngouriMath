//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

namespace AngouriMath
{
    partial record Entity
    {
        public partial record Variable
        {
            /// <summary>
            /// List of letters LaTeX will correctly display
            /// Yet to be extended
            /// Case does matter, for example \Alpha does not exist because the latin A is used instead
            /// </summary>
            [ConstantField] private static readonly HashSet<string> LatexisableConstants =
            [
                "alpha", "beta", "gamma", "delta", "epsilon", "varepsilon", "zeta", "eta", "theta", "vartheta",
                "iota", "kappa", "varkappa", "lambda", "mu", "nu", "xi", "omicron", "pi", "varpi", "rho",
                "varrho", "sigma", "varsigma", "tau", "upsilon", "phi", "varphi", "chi", "psi", "omega",

                "Gamma", "Delta", "Theta", "Lambda", "Xi", "Pi", "Sigma", "Upsilon", "Phi", "Psi", "Omega",
            ];
            /// <summary>An upright (multi-character or constant) variable needs separation from other upright variables for clarity.
            /// Non-upright (only includes italic for now) variables can be written together with all other variables.</summary>
            internal bool IsLatexUprightFormatted => SplitIndex() is var (prefix, _) ? IsNameLatexUprightFormatted(prefix) : IsNameLatexUprightFormatted(Name);
            internal static bool IsNameLatexUprightFormatted(string varName) =>
                // NOTE: Mathematical constants like "pi" and "e" are rendered upright following ISO 80000-2.
                // This applies everywhere: as main variables or as subscripts.
                ConstantList.ContainsKey(varName) ||
                // NOTE: Multi-character identifiers are rendered upright.
                // This distinguishes multi-character variable names (e.g., "velocity", "temp", "mass")
                // from products of single-letter variables (e.g., v·e·l·o·c·i·t·y).
                // Single-letter variables remain italic as per standard mathematical typography.
                varName.Length > 1 && !LatexisableConstants.Contains(varName);

            /// <summary>
            /// Whether this node's own name is set upright. ISO 80000-2 sets constants upright and
            /// variables italic, and which of the two a name is depends on the node and not on the
            /// spelling — a <c>pi</c> a binder declares is a variable and is set as one. A
            /// subscript is part of a name rather than a node of its own, so it keeps being read
            /// by name above. <a href="https://github.com/asc-community/AngouriMath/issues/984">#984</a>
            /// </summary>
            private protected virtual bool IsOwnNameUpright =>
                Name.Length > 1 && !LatexisableConstants.Contains(Name);
            /// <summary>
            /// Returns latexized const if it is possible to latexize it,
            /// or its original name otherwise
            /// </summary>
            private protected override string LatexizeNode()
            {
                static string LatexizePart(string symbol, bool upright)
                {
                    var inner = LatexisableConstants.Contains(symbol) ? $@"\{symbol}" : symbol;
                    return upright ? $@"\mathrm{{{inner}}}" : inner;
                }
                // For variables with subscripts (e.g., "pi_2", "x_e", "e_pi")
                // Both the main part and subscript are processed through LatexizePart,
                // which handles upright formatting for "pi" and "e" consistently.
                // A subscript may have one of its own, so X_a_beta is X_{a_{\beta}}.
                // https://github.com/asc-community/AngouriMath/issues/524
                static string LatexizeIndex(string index)
                    => index.IndexOf('_') is var underscore && underscore == -1
                    ? LatexizePart(index, IsNameLatexUprightFormatted(index))
                    : $"{LatexizePart(index.Substring(0, underscore), IsNameLatexUprightFormatted(index.Substring(0, underscore)))}_{{{LatexizeIndex(index.Substring(underscore + 1))}}}";
                return SplitIndex() is var (prefix, index)
                    ? $"{LatexizePart(prefix, IsNameLatexUprightFormatted(prefix))}_{{{LatexizeIndex(index)}}}"
                    : LatexizePart(Name, IsOwnNameUpright);
            }
        }

        public partial record Constant
        {
            /// <inheritdoc/>
            private protected override bool IsOwnNameUpright => true;

            /// <inheritdoc/>
            /// <remarks>Euler's constant is drawn as its symbol, upright as every constant is, so that
            /// it reads back as the constant and not as a variable called <c>gamma</c>.
            /// <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a></remarks>
            private protected override string LatexizeNode()
                => Name == nameof(eulergamma) ? @"\mathrm{\gamma}" : base.LatexizeNode();
        }
    }
}
