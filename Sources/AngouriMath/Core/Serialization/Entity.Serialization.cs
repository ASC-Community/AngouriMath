//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

#if NET8_0_OR_GREATER

using AngouriMath.Core.Serialization;

namespace AngouriMath
{
    /*
     * Which node types System.Text.Json is told about, and nothing else. It is here rather than
     * beside the node definitions because none of them changes to become serializable -- the
     * whole of it is one attribute per type and the converter next to this file.
     *
     * Every type is named, and not only Entity, because System.Text.Json looks the attribute up
     * with inherit: false. A member declared as Entity.Variable or Entity.Matrix would otherwise
     * fall through to the reflecting object converter, which walks Nodes -- a node's own
     * enumeration of itself -- and reports an object cycle. EntityCarriesItsConverterTest
     * enumerates the public node types and fails on any that is missing from this list, so a new
     * node cannot join it silently.
     */
    [EntityJsonConverter] partial record Entity
    {
        [EntityJsonConverter] partial record Absf;
        [EntityJsonConverter] partial record Andf;
        [EntityJsonConverter] partial record Application;
        [EntityJsonConverter] partial record Arccosecantf;
        [EntityJsonConverter] partial record Arccosf;
        [EntityJsonConverter] partial record Arccotanf;
        [EntityJsonConverter] partial record Arcsecantf;
        [EntityJsonConverter] partial record Arcsinf;
        [EntityJsonConverter] partial record Arctanf;
        [EntityJsonConverter] partial record Boolean;
        [EntityJsonConverter] partial record CalculusOperator;
        [EntityJsonConverter] partial record Ceilf;
        [EntityJsonConverter] partial record ComparisonSign;
        [EntityJsonConverter] partial record Constant;
        [EntityJsonConverter] partial record ContinuousNode;
        [EntityJsonConverter] partial record Cosecantf;
        [EntityJsonConverter] partial record Cosf;
        [EntityJsonConverter] partial record Cotanf;
        [EntityJsonConverter] partial record Derivativef;
        [EntityJsonConverter] partial record Divf;
        [EntityJsonConverter] partial record Equalsf;
        [EntityJsonConverter] partial record Factorialf;
        [EntityJsonConverter] partial record Binomialf;
        [EntityJsonConverter] partial record Floorf;
        [EntityJsonConverter] partial record Function;
        [EntityJsonConverter] partial record Gcdf;
        [EntityJsonConverter] partial record Lcmf;
        [EntityJsonConverter] partial record GreaterOrEqualf;
        [EntityJsonConverter] partial record Greaterf;
        [EntityJsonConverter] partial record Impliesf;
        [EntityJsonConverter] partial record Integralf;
        [EntityJsonConverter] partial record Lambda;
        [EntityJsonConverter] partial record LessOrEqualf;
        [EntityJsonConverter] partial record Lessf;
        [EntityJsonConverter] partial record Limitf;
        [EntityJsonConverter] partial record Logf;
        [EntityJsonConverter] partial record Matrix;
        [EntityJsonConverter] partial record Maxf;
        [EntityJsonConverter] partial record Minf;
        [EntityJsonConverter] partial record Minusf;
        [EntityJsonConverter] partial record Modf;
        [EntityJsonConverter] partial record Mulf;
        [EntityJsonConverter] partial record Notf;
        [EntityJsonConverter] partial record Number
        {
            [EntityJsonConverter] partial record Complex;
            [EntityJsonConverter] partial record Integer;
            [EntityJsonConverter] partial record Rational;
            [EntityJsonConverter] partial record Real;
        }
        [EntityJsonConverter] partial record Orf;
        [EntityJsonConverter] partial record Phif;
        [EntityJsonConverter] partial record Primef;
        [EntityJsonConverter] partial record Valuationf;
        [EntityJsonConverter] partial record Dividesf;
        [EntityJsonConverter] partial record Congruentf;
        [EntityJsonConverter] partial record Quantifier;
        [EntityJsonConverter] partial record Forallf;
        [EntityJsonConverter] partial record Existsf;
        [EntityJsonConverter] partial record ExistsUniquef;
        [EntityJsonConverter] partial record Cardf;
        [EntityJsonConverter] partial record Piecewise;
        [EntityJsonConverter] partial record Powf;
        [EntityJsonConverter] partial record Productf;
        [EntityJsonConverter] partial record Providedf;
        [EntityJsonConverter] partial record Roundf;
        [EntityJsonConverter] partial record Secantf;
        [EntityJsonConverter] partial record Set
        {
            [EntityJsonConverter] partial record ConditionalSet;
            [EntityJsonConverter] partial record FiniteSet;
            [EntityJsonConverter] partial record Inf;
            [EntityJsonConverter] partial record Subsetf;
            [EntityJsonConverter] partial record Powersetf;
            [EntityJsonConverter] partial record IndexedSetOperation;
            [EntityJsonConverter] partial record IndexedUnionf;
            [EntityJsonConverter] partial record IndexedIntersectionf;
            [EntityJsonConverter] partial record Intersectionf;
            [EntityJsonConverter] partial record Interval;
            [EntityJsonConverter] partial record SetMinusf;
            [EntityJsonConverter] partial record SpecialSet
            {
                [EntityJsonConverter] partial record Booleans;
                [EntityJsonConverter] partial record Complexes;
                [EntityJsonConverter] partial record Integers;
                [EntityJsonConverter] partial record NonNegativeIntegers;
                [EntityJsonConverter] partial record PositiveIntegers;
                [EntityJsonConverter] partial record Primes;
                [EntityJsonConverter] partial record Rationals;
                [EntityJsonConverter] partial record Reals;
            }
            [EntityJsonConverter] partial record Unionf;
        }
        [EntityJsonConverter] partial record Signumf;
        [EntityJsonConverter] partial record Erff;
        [EntityJsonConverter] partial record Iversonf;
        [EntityJsonConverter] partial record Alephf;
        [EntityJsonConverter] partial record Erfcf;
        [EntityJsonConverter] partial record Erfif;
        [EntityJsonConverter] partial record Eif;
        [EntityJsonConverter] partial record Lif;
        [EntityJsonConverter] partial record Sif;
        [EntityJsonConverter] partial record Cif;
        [EntityJsonConverter] partial record Shif;
        [EntityJsonConverter] partial record Chif;
        [EntityJsonConverter] partial record Sinf;
        [EntityJsonConverter] partial record Statement;
        [EntityJsonConverter] partial record Sumf;
        [EntityJsonConverter] partial record Summationf;
        [EntityJsonConverter] partial record Maximumf;
        [EntityJsonConverter] partial record SumOverSetf;
        [EntityJsonConverter] partial record Minimumf;
        [EntityJsonConverter] partial record Argmaxf;
        [EntityJsonConverter] partial record Argminf;
        [EntityJsonConverter] partial record Tanf;
        [EntityJsonConverter] partial record TrigonometricFunction;
        [EntityJsonConverter] partial record Variable;
        [EntityJsonConverter] partial record Xorf;
    }
}

#endif
