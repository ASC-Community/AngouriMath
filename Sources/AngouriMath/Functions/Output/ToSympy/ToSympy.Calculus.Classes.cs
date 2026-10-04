//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;

namespace AngouriMath
{
    partial record Entity
    {
        public partial record Derivativef
        {
            internal override string ToSymPy() => $"sympy.diff({Expression.ToSymPy()}, {Var.ToSymPy()}, {Iterations})";
        }

        public partial record Integralf
        {
            internal override string ToSymPy() => $"sympy.integrate({Expression.ToSymPy()}, {(Range is var (from, to) ? $"({Var.ToSymPy()}, {from.ToSymPy()}, {to.ToSymPy()})" : Var.ToSymPy())})";
        }

        public partial record SumOverSetf
        {
            // SymPy's RootSum counts a repeated root as often as it is repeated, and a set counts
            // it once, so the polynomial goes over as its square-free part.
            internal override string ToSymPy() => Over switch
            {
                Set.FiniteSet finite =>
                    $"sympy.Add(*[({Expression.ToSymPy()}).subs({Var.ToSymPy()}, member) for member in [{string.Join(", ", finite.Elements.Select(static e => e.ToSymPy()))}]])",
                Set.ConditionalSet { Var: Variable w, Predicate: Equalsf(var left, var right) } when w == Var =>
                    $"sympy.RootSum(sympy.sqf_part(sympy.Poly({(left - right).ToSymPy()}, {Var.ToSymPy()})), sympy.Lambda({Var.ToSymPy()}, {Expression.ToSymPy()}))",
                _ => throw new NotSufficientlySupportedException("SymPy has no sum over a set; a listed set or the roots of a polynomial can be exported"),
            };
        }

        public partial record Maximumf
        {
            internal override string ToSymPy() =>
                $"sympy.maximum({Expression.ToSymPy()}, {Var.ToSymPy()}, {Over.ToSymPy()})";
        }

        public partial record Minimumf
        {
            internal override string ToSymPy() =>
                $"sympy.minimum({Expression.ToSymPy()}, {Var.ToSymPy()}, {Over.ToSymPy()})";
        }

        public partial record Argmaxf
        {
            // SymPy has maximum and minimum but no argmax; the points are not one call.
            internal override string ToSymPy() =>
                throw new NotSufficientlySupportedException("SymPy has no argmax; ask for the maximum and solve for where it is taken");
        }

        public partial record Argminf
        {
            internal override string ToSymPy() =>
                throw new NotSufficientlySupportedException("SymPy has no argmin; ask for the minimum and solve for where it is taken");
        }

        public partial record Summationf
        {
            internal override string ToSymPy() =>
                $"sympy.Sum({Expression.ToSymPy()}, ({Var.ToSymPy()}, {From.ToSymPy()}, {To.ToSymPy()}))";
        }

        public partial record Productf
        {
            internal override string ToSymPy() =>
                $"sympy.Product({Expression.ToSymPy()}, ({Var.ToSymPy()}, {From.ToSymPy()}, {To.ToSymPy()}))";
        }

        public partial record Limitf
        {
            internal override string ToSymPy() =>
                @$"sympy.limit({Expression.ToSymPy()}, {Var.ToSymPy()}, {Destination.ToSymPy()}{ApproachFrom switch
                {
                    ApproachFrom.Left => ", '-'",
                    ApproachFrom.BothSides => "",
                    ApproachFrom.Right => ", '+'",
                    _ => throw new AngouriBugException
                        ($"Unresolved enum {ApproachFrom}")
                }})";
        }
    }
}
