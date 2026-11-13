//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Core.Exceptions;

namespace AngouriMath
{
    partial record Entity
    {
        partial record Set
        {
            partial record FiniteSet
            {
                internal override string ToSymPy()
                    => $"sympy.FiniteSet({string.Join(", ", Elements.Select(c => c.ToSymPy()))})";
            }

            partial record Interval
            {
                internal override string ToSymPy()
                    => $"sympy.Interval({Left.ToSymPy()}, {Right.ToSymPy()}, left_open={((Boolean)!LeftClosed).ToSymPy()}, right_open={((Boolean)!RightClosed).ToSymPy()})";
            }

            partial record ConditionalSet
            {
                // A set builder's Codomain is Domain.Any, which SpecialSet.Create has no
                // member for and throws on -- so every set builder threw an AngouriBugException
                // out of the exporter.
                // https://github.com/asc-community/AngouriMath/issues/985
                //
                // S.UniversalSet is what SymPy prints a ConditionSet without a third argument
                // over, so it is the right thing to emit for a predicate under no further
                // restriction. It is a choice about the target language and not a claim about
                // this one: SymPy has a universal set and AngouriMath does not, and an exporter
                // to a system without one (or with a typed one, as Lean and SMT have) picks
                // differently. Nothing here may be read back as "Domain.Any is a set".
                // https://github.com/asc-community/AngouriMath/issues/996
                internal override string ToSymPy()
                    => $"sympy.ConditionSet({Var.ToSymPy()}, {Predicate.ToSymPy()}, {CodomainToSymPy()})";

                private string CodomainToSymPy()
                    => Codomain is Domain.Any ? "sympy.S.UniversalSet" : SpecialSet.Create(Codomain).ToSymPy();
            }

            partial record SpecialSet
            {
                partial record Integers
                {
                    internal override string ToSymPy()
                        => "sympy.S.Integers";
                }

                partial record NonNegativeIntegers
                {
                    internal override string ToSymPy()
                        => "sympy.S.Naturals0";
                }

                partial record Primes
                {
                    internal override string ToSymPy()
                        => "sympy.S.Primes";
                }

                partial record PositiveIntegers
                {
                    internal override string ToSymPy()
                        => "sympy.S.Naturals";
                }

                partial record Rationals
                {
                    internal override string ToSymPy()
                        => "sympy.S.Rationals";
                }

                partial record Reals
                {
                    internal override string ToSymPy()
                        => "sympy.S.Reals";
                }

                partial record Complexes
                {
                    internal override string ToSymPy()
                        => "sympy.S.Complexes";
                }

                internal override string ToSymPy()
                        => throw new NotSufficientlySupportedException($"There is no {this} in either SymPy or AM's {nameof(ToSymPy)}");
            }

            partial record Unionf
            {
                internal override string ToSymPy()
                    => $"sympy.Union({Left.ToSymPy()}, {Right.ToSymPy()})";
            }

            partial record Intersectionf
            {
                internal override string ToSymPy()
                    => $"sympy.Intersection({Left.ToSymPy()}, {Right.ToSymPy()})";
            }

            partial record SetMinusf
            {
                internal override string ToSymPy()
                    => $"sympy.Complement({Left.ToSymPy()}, {Right.ToSymPy()})";
            }

            partial record Inf
            {
                // Python's `in` forces its result to a bool, and a membership that is not
                // decided is not one: `x in sympy.S.Reals` raises "did not evaluate to a
                // bool: (-oo < x) & (x < oo)". `.contains` is the form that answers with the
                // condition instead, and it still answers True or False when it can.
                // https://github.com/asc-community/AngouriMath/issues/985
                internal override string ToSymPy()
                    => $"({SupSet.ToSymPy()}).contains({Element.ToSymPy()})";
            }

            partial record Subsetf
            {
                internal override string ToSymPy()
                    => $"({Sub.ToSymPy()}).is_subset({Super.ToSymPy()})";
            }

            partial record IndexedSetOperation
            {
                // SymPy's Union and Intersection take the sets listed; a family over a set
                // it cannot list has no translation.
                internal override string ToSymPy()
                    => Over is FiniteSet listed
                        ? $"sympy.{(this is IndexedUnionf ? "Union" : "Intersection")}({string.Join(", ", listed.Select(index => Body.Substitute(Var, index).ToSymPy()))})"
                        : throw new NotSufficientlySupportedException("SymPy lists the sets a union or an intersection is over, and this family is over a set that is not listed");
            }

            partial record Powersetf
            {
                internal override string ToSymPy()
                    => $"({Argument.ToSymPy()}).powerset()";
            }
        }



        partial record Providedf
        {
            // TODO: is it the right way of using refine?
            internal override string ToSymPy() => $"sympy.refine({Expression.ToSymPy()}, sympy.Q.is_true({Predicate.ToSymPy()}))";
        }

        partial record Piecewise
        {
            internal override string ToSymPy() => $"sympy.Piecewise({string.Join(", ", Cases.Select(c => $"({c.Expression.ToSymPy()}, {c.Predicate.ToSymPy()})"))})";
        }
        
        // https://github.com/asc-community/AngouriMath/issues/2147
        partial record Determinantf
        {
            internal override string ToSymPy() => $"sympy.det({Argument.ToSymPy()})";
        }

        partial record Transposef
        {
            internal override string ToSymPy() => $"sympy.transpose({Argument.ToSymPy()})";
        }

        partial record Tracef
        {
            internal override string ToSymPy() => $"sympy.trace({Argument.ToSymPy()})";
        }

        // SymPy counts a matrix's rows and columns from 0, and this node from 1.
        partial record MatrixEntryf
        {
            internal override string ToSymPy() => $"({Argument.ToSymPy()})[({Row.ToSymPy()}) - 1, ({Column.ToSymPy()}) - 1]";
        }

        partial record Matrix
        {
            internal override string ToSymPy()
                => "sympy.ImmutableMatrix([" +
                    string.Join(", ",
                        IsVector switch
                        {
                            true => this.Select(c => c.ToSymPy()),
                            false => this.Select(c => $"[{string.Join(", ", ((Matrix)c).T.Select(e => e.ToSymPy()))}]"),
                        }) +
                   "])";
        }

        partial record Application
        {
            internal override string ToSymPy()
                => Expression is Lambda
                    ? $"({Expression.ToSymPy()})({", ".Join(Arguments.Select(arg => arg.ToSymPy()))})"
                    : throw new NotSufficientlySupportedException("Sympy might not have application of undeclared lambda");
        }

        partial record Lambda
        {
            internal override string ToSymPy()
                => $"sympy.Lambda({Parameter.ToSymPy()}, {Body.ToSymPy()})";
        }
    }
}
