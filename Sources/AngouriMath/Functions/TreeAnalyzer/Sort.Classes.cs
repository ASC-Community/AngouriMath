//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;

namespace AngouriMath
{
    using SortLevel = Functions.TreeAnalyzer.SortLevel;
    partial record Entity
    {
        /// <summary>
        /// Returns the correct way for sort string
        /// </summary>
        /// <param name="level">The level at which we are sorting</param>
        /// <param name="highLevel">The most general way (that is, the least uniqueness of the string)</param>
        /// <param name="middleLevel">The one in between (for example, for grouping some functions)</param>
        /// <param name="lowLevel">The most unique string id</param>
        private static string Choice(SortLevel level, string highLevel, string middleLevel, string lowLevel)
            => level switch
            {
                SortLevel.HIGH_LEVEL => highLevel,
                SortLevel.MIDDLE_LEVEL => middleLevel,
                SortLevel.LOW_LEVEL => lowLevel,
                _ => throw new AngouriBugException($"Unrecognized SortLevel: {level}")
            };

        /// <summary>
        /// Returns the correct way for sort string
        /// </summary>
        /// <param name="level">The level at which we are sorting</param>
        /// <param name="highLevel">The most general way (that is, the least uniqueness of the string)</param>
        /// <param name="lowLevel">The most unique string id</param>
        private static string Choice(SortLevel level, string highLevel, string lowLevel)
            => Choice(level, highLevel, lowLevel, lowLevel);

        public partial record Number
        {
            private protected override string SortHashName(SortLevel level) 
                => Choice(level, "", Stringize(), Stringize() + " ");
        }

        // TODO: reorder all those names to make similar functions appear closer

        public partial record Variable
        {
            private protected override string SortHashName(SortLevel level) => "v_" + Name;
        }

        partial record Matrix
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "", "tensort_");
        }

        // Each function and operator processing
        public partial record Sumf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "", "summinus_", "sum_");
        }

        public partial record Minusf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "", "summinus_", "min_");
        }

        public partial record Mulf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "", "divmul_", "mul_");
        }

        public partial record Divf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "", "divmul_", "div_");
        }

        public partial record Modf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "", "divmul_", "mod_");
        }

        public partial record Powf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "", "logpow_", "pow_");
        }

        public partial record Logf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "", "logpow_", "log_");
        }

        public partial record Sinf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "sincoss_", "sin_", "sin_");
        }

        public partial record Cosf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "sincosc_", "cos_", "cos_");
        }

        public partial record Secantf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "seccsc_", "sec_", "sec_");
        }

        public partial record Cosecantf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "seccsc_", "csc_", "csc_");
        }

        public partial record Tanf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "tancot_", "tan_", "tan_");
        }

        public partial record Cotanf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "tancot_", "cot_", "cot_");
        }

        public partial record Arcsecantf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "arcseccsc_", "arcseccsc_", "arcsec_");
        }

        public partial record Arccosecantf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "arcseccsc_", "arcseccsc_", "arccsc_");
        }

        public partial record Arcsinf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "arcsincos_", "arcsincos_", "arcsin_");
        }

        public partial record Arccosf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "arcsincos_", "arcsincos_", "arccos_");
        }

        public partial record Arctanf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "arctancot_", "arctancot_", "arctan_");
        }

        public partial record Arccotanf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "arctancot_", "arctancot_", "arccot_");
        }

        public partial record Factorialf
        {
            private protected override string SortHashName(SortLevel level) => "factorialf_";
        }

        public partial record Binomialf
        {
            private protected override string SortHashName(SortLevel level) => "binomialf_";
        }

        public partial record Derivativef
        {
            private protected override string SortHashName(SortLevel level) => "derivativef_";
        }

        public partial record Integralf
        {
            private protected override string SortHashName(SortLevel level) => "integralf_";
        }

        public partial record Summationf
        {
            private protected override string SortHashName(SortLevel level) => "summationf_";
        }

        public partial record Productf
        {
            private protected override string SortHashName(SortLevel level) => "productf_";
        }

        public partial record SumOverSetf
        {
            private protected override string SortHashName(SortLevel level) => "sumoversetf_";
        }

        public partial record Maximumf
        {
            private protected override string SortHashName(SortLevel level) => "maximumf_";
        }

        public partial record Minimumf
        {
            private protected override string SortHashName(SortLevel level) => "minimumf_";
        }

        public partial record Argmaxf
        {
            private protected override string SortHashName(SortLevel level) => "argmaxf_";
        }

        public partial record Argminf
        {
            private protected override string SortHashName(SortLevel level) => "argminf_";
        }

        public partial record Limitf
        {
            private protected override string SortHashName(SortLevel level) => "limitf_";
        }

        public partial record Signumf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "sgnabs_", "sgn_", "sgn_");
        }

        public partial record Erff
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "erf_", "erf_", "erf_");
        }

        public partial record Iversonf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "iverson_", "iverson_", "iverson_");
        }

        public partial record Alephf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "aleph_", "aleph_", "aleph_");
        }

        public partial record Erfcf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "erf_", "erfc_", "erfc_");
        }

        public partial record Erfif
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "erf_", "erfi_", "erfi_");
        }

        public partial record Eif
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "ei_", "ei_", "ei_");
        }

        public partial record Lif
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "ei_", "li_", "li_");
        }

        public partial record Sif
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "si_", "si_", "si_");
        }

        public partial record Cif
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "si_", "ci_", "ci_");
        }

        public partial record Shif
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "shi_", "shi_", "shi_");
        }

        public partial record Chif
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "shi_", "chi_", "chi_");
        }

        public partial record Absf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "sgnabs_", "abs_", "abs_");
        }

        public partial record Floorf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "floorceil_", "floor_", "floor_");
        }

        public partial record Ceilf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "floorceil_", "ceil_", "ceil_");
        }

        public partial record Roundf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "floorceil_", "round_", "round_");
        }

        public partial record Minf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "minmax_", "min_", "min_");
        }

        public partial record Maxf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "minmax_", "max_", "max_");
        }

        public partial record Gcdf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "gcd_", "gcd_", "gcd_");
        }

        public partial record Lcmf
        {
            private protected override string SortHashName(SortLevel level)
                => Choice(level, "lcm_", "lcm_", "lcm_");
        }

        partial record Boolean
        {
            private protected override string SortHashName(SortLevel level)
                => level switch
                {
                    SortLevel.HIGH_LEVEL => "",
                    SortLevel.MIDDLE_LEVEL => Stringize(),
                    _ => Stringize() + " "
                };
        }

        partial record Notf
        {
            private protected override string SortHashName(SortLevel level)
                => level == SortLevel.LOW_LEVEL ? "not_" : "";
        }

        partial record Andf
        {
            private protected override string SortHashName(SortLevel level)
                => level == SortLevel.LOW_LEVEL ? "and_" : "";
        }

        partial record Orf
        {
            private protected override string SortHashName(SortLevel level)
                => level == SortLevel.LOW_LEVEL ? "or_" : "";
        }

        partial record Xorf
        {
            private protected override string SortHashName(SortLevel level)
                => level == SortLevel.LOW_LEVEL ? "xor_" : "";
        }

        partial record Impliesf
        {
            private protected override string SortHashName(SortLevel level)
                => level == SortLevel.LOW_LEVEL ? "impl_" : "";
        }

        partial record Equalsf
        {
            private protected override string SortHashName(SortLevel level)
               => level == SortLevel.LOW_LEVEL ? "equals_" : "";
        }

        partial record Greaterf
        {
            private protected override string SortHashName(SortLevel level)
               => level == SortLevel.LOW_LEVEL ? "greater_" : "";
        }

        partial record GreaterOrEqualf
        {
            private protected override string SortHashName(SortLevel level)
               => level == SortLevel.LOW_LEVEL ? "greaterequal_" : "";
        }

        partial record Lessf
        {
            private protected override string SortHashName(SortLevel level)
               => level == SortLevel.LOW_LEVEL ? "less_" : "";
        }

        partial record LessOrEqualf
        {
            private protected override string SortHashName(SortLevel level)
               => level == SortLevel.LOW_LEVEL ? "lessequal_" : "";
        }

        partial record Set
        {
            partial record FiniteSet
            {
                private protected override string SortHashName(SortLevel level)
                    => level switch
                    {
                        SortLevel.HIGH_LEVEL => "",
                        SortLevel.MIDDLE_LEVEL => Stringize(),
                        _ => Stringize() + " "
                    };
            }

            partial record Interval
            {
                private protected override string SortHashName(SortLevel level)
                    => level switch
                    {
                        SortLevel.HIGH_LEVEL => "",
                        SortLevel.MIDDLE_LEVEL => Stringize(),
                        _ => Stringize() + " "
                    };
            }

            partial record ConditionalSet
            {
                private protected override string SortHashName(SortLevel level)
                    => level switch
                    {
                        SortLevel.HIGH_LEVEL => "",
                        SortLevel.MIDDLE_LEVEL => Stringize(),
                        _ => Stringize() + " "
                    };
            }

            partial record SpecialSet
            {
                private protected override string SortHashName(SortLevel level)
                    => level switch
                    {
                        SortLevel.HIGH_LEVEL => "",
                        SortLevel.MIDDLE_LEVEL => Stringize(),
                        _ => Stringize() + " "
                    };
            }

            partial record Unionf
            {
                private protected override string SortHashName(SortLevel level)
                    => level == SortLevel.LOW_LEVEL ? "union_" : "";
            }

            partial record Intersectionf
            {
                private protected override string SortHashName(SortLevel level)
                    => level == SortLevel.LOW_LEVEL ? "intersection_" : "";
            }

            partial record SetMinusf
            {
                private protected override string SortHashName(SortLevel level)
                    => level == SortLevel.LOW_LEVEL ? "setminus_" : "";
            }

            partial record Inf
            {
                private protected override string SortHashName(SortLevel level)
                    => level == SortLevel.LOW_LEVEL ? "in_" : "";
            }

            partial record Subsetf
            {
                private protected override string SortHashName(SortLevel level)
                    => level == SortLevel.LOW_LEVEL ? "subset_" : "";
            }

            partial record Powersetf
            {
                private protected override string SortHashName(SortLevel level)
                    => level == SortLevel.LOW_LEVEL ? "powerset_" : "";
            }

            partial record IndexedSetOperation
            {
                private protected override string SortHashName(SortLevel level)
                    => level == SortLevel.LOW_LEVEL ? "indexed" + Keyword + "_" : "";
            }
        }

        partial record Phif
        {
            private protected override string SortHashName(SortLevel level) => "phi_";
        }

        partial record Primef
        {
            private protected override string SortHashName(SortLevel level) => "prime_";
        }

        partial record Valuationf
        {
            private protected override string SortHashName(SortLevel level) => "valuation_";
        }

        partial record Dividesf
        {
            private protected override string SortHashName(SortLevel level)
                => level == SortLevel.LOW_LEVEL ? "divides_" : "";
        }

        partial record Congruentf
        {
            private protected override string SortHashName(SortLevel level)
                => level == SortLevel.LOW_LEVEL ? "congruent_" : "";
        }

        partial record Quantifier
        {
            private protected override string SortHashName(SortLevel level)
                => level == SortLevel.LOW_LEVEL ? Keyword + "_" : "";
        }

        partial record Cardf
        {
            private protected override string SortHashName(SortLevel level) => "card_";
        }

        partial record Providedf
        {
            private protected override string SortHashName(SortLevel level) => "provided_";
        }

        partial record Piecewise
        {
            private protected override string SortHashName(SortLevel level) => "__";
        }

        partial record Application
        {
            private protected override string SortHashName(SortLevel level) => "___";
        }

        partial record Lambda
        {
            private protected override string SortHashName(SortLevel level) => "___";
        }
    }
}
