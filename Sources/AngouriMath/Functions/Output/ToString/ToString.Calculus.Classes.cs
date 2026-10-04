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
            /// <inheritdoc/>
            private protected override string StringizeNode()
            {
                if (Iterations == 1)
                    return $"derivative({Expression.Stringize()}, {Var.Stringize()})";
                else
                    return $"derivative({Expression.Stringize()}, {Var.Stringize()}, {Iterations})";
            }
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Integralf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() =>
                Range is var (from, to)
                ? $"integral({Expression.Stringize()}, {Var.Stringize()}, {from.Stringize()}, {to.Stringize()})"
                : $"integral({Expression.Stringize()}, {Var.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record SumOverSetf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() =>
                $"sum({Expression.Stringize()}, {Var.Stringize()} in {Over.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Maximumf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() =>
                Expression == Var && Over is Set ? $"max({Over.Stringize()})"
                : $"max({Expression.Stringize()}, {Var.Stringize()} in {Over.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Minimumf
        {
            /// <inheritdoc/>
            // The least member of a set, min(S), is the extremum of the name over it and prints
            // as it was written. https://github.com/asc-community/AngouriMath/issues/1450
            private protected override string StringizeNode() =>
                Expression == Var && Over is Set ? $"min({Over.Stringize()})"
                : $"min({Expression.Stringize()}, {Var.Stringize()} in {Over.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Argmaxf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() =>
                $"argmax({Expression.Stringize()}, {Var.Stringize()} in {Over.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Argminf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() =>
                $"argmin({Expression.Stringize()}, {Var.Stringize()} in {Over.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Summationf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() =>
                $"sum({Expression.Stringize()}, {Var.Stringize()}, {From.Stringize()}, {To.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Productf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() =>
                $"product({Expression.Stringize()}, {Var.Stringize()}, {From.Stringize()}, {To.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Limitf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() =>

                ApproachFrom switch
                {
                    ApproachFrom.Left => "limitleft",
                    ApproachFrom.BothSides => "limit",
                    ApproachFrom.Right => "limitright",
                    _ => throw new AngouriBugException
                        ($"Unresolved enum {ApproachFrom}")
                } + $"({Expression.Stringize()}, {Var.Stringize()}, {Destination.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }
    }
}