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
        partial record Number
        {
#pragma warning disable SealedOrAbstract // The only exception: those three records are neither abstract nor sealed
            partial record Complex
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Complex;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Complex;
            }

            partial record Rational
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Rational;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Rational;
            }

            partial record Real
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Real;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Real;
            }
#pragma warning restore SealedOrAbstract

            partial record Integer
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Integer;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Integer;
            }
        }

        partial record Variable
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Any;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Any;
        }

        partial record Matrix
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Any;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Any;
        }

        partial record Sumf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Minusf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Mulf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Divf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Modf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Real;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Real;
        }

        partial record Sinf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Cosf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Tanf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Cotanf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Logf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Powf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Secantf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Cosecantf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Arcsecantf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Arccosecantf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Arcsinf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Arccosf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Arctanf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Arccotanf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Factorialf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Binomialf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Iversonf
        {
            // 0 or 1, whatever the statement.
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Real;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Real;
        }

        partial record Alephf
        {
            // A size of an infinite set, which is no number.
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Any;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Any;
        }

        partial record Derivativef
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Integralf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Summationf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Productf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record SumOverSetf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Maximumf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Real;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Real;
        }

        partial record Minimumf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Real;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Real;
        }

        partial record Argmaxf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Any;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Any;
        }

        partial record Argminf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Any;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Any;
        }

        partial record Limitf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Signumf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Erff
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Erfcf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Erfif
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Eif
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Lif
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Sif
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Cif
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Shif
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Chif
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Absf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Real;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Real;
        }

        partial record Floorf
        {
            // Complex rather than Integer: taken componentwise, floor of a complex number
            // is a Gaussian integer, and there is no domain for those. The codomain of the
            // real case is stated by the value, not by this.
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Ceilf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Roundf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Minf
        {
            // Only ordered arguments compare, so the value is real wherever there is one.
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Real;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Real;
        }

        partial record Maxf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Real;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Real;
        }

        partial record Gcdf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Lcmf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Complex;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Complex;
        }

        partial record Boolean
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Boolean;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Boolean;
        }

        partial record Notf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Boolean;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Boolean;
        }

        partial record Andf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Boolean;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Boolean;
        }

        partial record Orf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Boolean;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Boolean;
        }

        partial record Xorf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Boolean;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Boolean;
        }

        partial record Impliesf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Boolean;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Boolean;
        }

        partial record Equalsf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Boolean;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Boolean;
        }

        partial record Greaterf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Boolean;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Boolean;
        }

        partial record GreaterOrEqualf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Boolean;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Boolean;
        }

        partial record Lessf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Boolean;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Boolean;
        }

        partial record LessOrEqualf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Boolean;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Boolean;
        }

        partial record Set
        {
            partial record FiniteSet
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Any;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Any;
            }

            partial record Interval
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Real;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Real;
            }

            partial record ConditionalSet
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Any;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Any;
            }

            partial record SpecialSet
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Any;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Any;
            }

            partial record Unionf
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Any;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Any;
            }

            partial record Intersectionf
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Any;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Any;
            }

            partial record SetMinusf
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Any;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Any;
            }

            partial record Inf
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Boolean;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Boolean;
            }

            partial record Subsetf
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Boolean;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Boolean;
            }

            partial record Powersetf
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Any;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Any;
            }

            partial record IndexedSetOperation
            {
                /// <inheritdoc/>
                public override Domain Codomain { get; protected init; } = Domain.Any;
                /// <inheritdoc/>
                internal override Domain DefaultCodomain => Domain.Any;
            }
        }

        partial record Dividesf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Boolean;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Boolean;
        }

        partial record Congruentf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Boolean;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Boolean;
        }

        partial record Quantifier
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Boolean;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Boolean;
        }

        partial record Cardf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Integer;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Integer;
        }

        partial record Valuationf
        {
            // A whole number wherever the argument is not zero, and +oo there, which no
            // domain of whole numbers admits: the reals, so that the convention is kept.
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Real;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Real;
        }

        partial record Primef
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Prime;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Prime;
        }

        partial record Phif
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Integer;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Integer;
        }

        partial record Providedf
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Any;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Any;
        }

        partial record Piecewise
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Any;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Any;
        }

        partial record Application
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Any;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Any;
        }

        partial record Lambda
        {
            /// <inheritdoc/>
            public override Domain Codomain { get; protected init; } = Domain.Any;
            /// <inheritdoc/>
            internal override Domain DefaultCodomain => Domain.Any;
        }
    }
}
