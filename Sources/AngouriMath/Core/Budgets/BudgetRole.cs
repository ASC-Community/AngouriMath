//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

namespace AngouriMath.Core.Budgets
{
    /// <summary>
    /// What a computation's own budget is for, which decides what a set
    /// <see cref="MathS.Settings.Budget"/> does to it. Declared where the ledger is made,
    /// <see cref="BudgetLedger.For"/>, so that each site says which it is.
    /// https://github.com/asc-community/AngouriMath/issues/2454
    /// </summary>
    internal enum BudgetRole
    {
        /// <summary>
        /// A guard against a stall inside an operation that is not about this computation
        /// at all -- the perfect-square proof <c>Factorize</c> runs. A set budget can only
        /// tighten it, so setting one never makes <c>Simplify</c> or <c>Factorize</c> run
        /// longer than they would unset.
        /// </summary>
        Lid,

        /// <summary>
        /// The working allowance of an algorithm a caller asks for by name -- the system
        /// solver, the Gr&#246;bner path. A set budget replaces it, which is what lets a caller
        /// raise it; capping it instead would make raising it do nothing.
        /// </summary>
        Allowance,

        /// <summary>
        /// A budget the caller passed as an argument. It is the allowance, and a set budget,
        /// being the outer scope, caps it: the smaller limit on each axis.
        /// </summary>
        Argument,
    }
}
