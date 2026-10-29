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
            // A name, whatever it is spelled. A bound `e` is a variable and exports as one; only
            // the constant node below exports as SymPy's constant.
            internal override string ToSymPy() => Name;
        }

        public partial record Constant
        {
            internal override string ToSymPy()
                => Name switch
                {
                    "e" => "sympy.E",
                    nameof(eulergamma) => "sympy.EulerGamma",
                    _ => "sympy." + Name
                };
        }
    }
}