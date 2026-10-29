//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;

namespace AngouriMath
{
    partial record Entity
    {
        // Variable, along with Set and Tensor is a unique element, that might be either Continuous or Discrete
        // and/or contain either Continuous or Discrete elements

        #region Variable
        /// <summary>
        /// Variable node. It only has a name.
        /// Construct a <see cref="Variable"/> with an implicit conversion from <see cref="string"/>.
        /// It has no type, so you can substitute any value under a given variable.
        /// </summary>
#pragma warning disable SealedOrAbstract // Constant derives from it, as Real does from Complex
        public partial record Variable : Entity
#pragma warning restore SealedOrAbstract
        {
            /// <summary>
            /// Deconstructs Variable as follows
            /// </summary>
            /// <param name="name">To where the result is put</param>
            public void Deconstruct(out string name)
                => name = Name;
            internal static Variable CreateVariableUnchecked(string name) => new(name);
            private protected Variable(string name) => Name = name;

            /// <summary>
            /// The name of the variable as a string
            /// </summary>
            public string Name { get; }
            internal override Priority Priority => Priority.Leaf;

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(this);
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => Array.Empty<Entity>();

            /// <summary>
            /// Which names the language reads as mathematical constants, and what each is worth
            /// -- asked at the moment of evaluation, so that the digits are the current
            /// precision's and not those of whichever context was set when this class was
            /// initialised (https://github.com/asc-community/AngouriMath/issues/1367), and a
            /// <see cref="Real"/> whatever the downcasting says: through the conversion from a
            /// decimal, with the downcasting off, <c>e</c> was a <see cref="Complex"/> with a
            /// zero imaginary part, and cached on the constant that made <c>sgn(e^x - 1)</c>
            /// not real-valued and <c>e^x</c> not the exponential of <c>x</c> for the rest of
            /// the process. This is the whole registry: a name is a constant exactly when it
            /// is a key here, and nothing below asks about <c>pi</c> or <c>e</c> by name.
            /// </summary>
            [ConstantField] internal static readonly IReadOnlyDictionary<string, Func<Complex>> ConstantList =
                new Dictionary<string, Func<Complex>>
                {
                    { nameof(pi), static () => Real.Create(MathS.DecimalConst.pi) },
                    { nameof(e), static () => Real.Create(MathS.DecimalConst.e) },
                    { nameof(eulergamma), static () => Real.Create(MathS.DecimalConst.eulergamma) }
                };

            /// <summary>Each constant as the name a writer types, which is the form a binder can take.</summary>
            [ConstantField] internal static readonly IReadOnlyDictionary<string, Constant> NamedConstants =
                ConstantList.Keys.ToDictionary(name => name, Constant.Named);

            [ConstantField] internal static readonly Constant pi = NamedConstants[nameof(pi)];
            [ConstantField] internal static readonly Constant e = NamedConstants[nameof(e)];
            [ConstantField] internal static readonly Constant eulergamma = NamedConstants[nameof(eulergamma)];

            /// <summary>
            /// The other spellings of a constant's name, each read as the constant itself: <c>π</c>
            /// (U+03C0) is <c>pi</c> and the script <c>ℯ</c> (U+212F) is <c>e</c>, as mathematics
            /// writes them. The Cyrillic <c>е</c> is not among them: it is a letter of an alphabet the
            /// grammar admits on purpose, and reading it as a constant would be the same surprise the
            /// other way round. https://github.com/asc-community/AngouriMath/issues/1260
            /// Euler's constant is also read as <c>EulerGamma</c>, Mathematica's and SymPy's name for it,
            /// and not as <c>γ</c>, which is as often a variable.
            /// https://github.com/asc-community/AngouriMath/issues/2182
            /// </summary>
            [ConstantField] private static readonly IReadOnlyDictionary<string, string> ConstantSpellings =
                new Dictionary<string, string> { { "\u03C0", nameof(pi) }, { "\u212F", nameof(e) }, { "EulerGamma", nameof(eulergamma) } };

            /// <summary>The name a spelling stands for: <c>π</c> is <c>pi</c>, and any other name is itself.</summary>
            private static string Spelled(string name) => ConstantSpellings.TryGetValue(name, out var constant) ? constant : name;

            /// <summary>Is this name one the language reads as a mathematical constant?</summary>
            internal static bool IsConstantName(string name) => ConstantList.ContainsKey(Spelled(name));

            /// <summary>
            /// A name as the language reads it: a constant where the name is one, a variable
            /// otherwise. This is what the parser calls, so a written <c>pi</c> is the constant
            /// and a <c>pi</c> that a binder declares is not — they stop being one object.
            /// <a href="https://github.com/asc-community/AngouriMath/issues/984">#984</a>
            /// </summary>
            internal static Variable CreateVariableOrConstant(string name)
                => NamedConstants.TryGetValue(Spelled(name), out var constant) ? constant : new Variable(name);

            /// <summary>
            /// Extracts this <see cref="Variable"/>'s name and index
            /// from its <see cref="Name"/> (e. g. "qua" or "phi_3" or "qu_q")
            /// </summary>
            /// <returns>
            /// If this contains _ and valid name and index, returns a pair of
            /// (<see cref="string"/> Prefix, <see cref="string"/> Index),
            /// <see langword="null"/> otherwise
            /// </returns>
            internal (string Prefix, string Index)? SplitIndex() =>
                Name.IndexOf('_') is var pos_ && pos_ == -1
                ? null
                : ((string Prefix, string Index)?)(Name.Substring(0, pos_), Name.Substring(pos_ + 1));
            /// <summary>
            /// Finds next var index name that is unused in <paramref name="expr"/> starting with 1, e. g.
            /// x + n_0 + n_a + n_3 + n_1
            /// will find n_2
            /// </summary>
            /// <remarks>
            /// This is intended for variables visible to the user.
            /// For non-visible variables, use <see cref="CreateTemp"/> instead.
            /// </remarks>
            internal static Variable CreateUnique(Entity expr, string prefix)
            {
                // Compared against the names in use rather than against indices picked out
                // of them. SplitIndex cuts at the *first* underscore, so for a prefix that
                // contains one -- "u_sub", which is the one integration substitutes with --
                // it read u_sub_1 as the prefix "u" and the index "sub_1", parsed nothing,
                // and handed back u_sub_1 as though it were free. Substituting with a
                // variable already in the expression is silent and produces a wrong answer:
                // it turned the integral of x * (x^2 + 1)^2 into something whose derivative
                // is not the integrand.
                var taken = new HashSet<string>();
                foreach (var variable in expr.Vars)
                    taken.Add(variable.Name);
                var i = 1;
                while (taken.Contains(prefix + "_" + i))
                    i++;
                return new Variable(prefix + "_" + i);
            }

            [ConstantField]
            private static readonly Variable[] letterVars = 
                "xyzabcdefghijklmnopqrstuvw"
                .Select(c => new Variable(c.ToString()))
                // A fresh name that prints as `e` would parse back as the constant, so the
                // constant names stay out of here even though they are ordinary variables now.
                .Where(c => !IsConstantName(c.Name))
                .ToArray();

            /// <summary>
            /// First, tries to find a good single-character variable
            /// in the alphabet list. Then, if all used, returns
            /// a unique with incrementable prefix
            /// </summary>
            internal static Variable CreateUniqueAlphabetFirst(Entity expr, string prefix = "a")
            {
                // TODO: Vars to be a set
                foreach (var v in letterVars)
                    if (expr.Vars.Contains(v) is false)
                        return v;
                return CreateUnique(expr, prefix);
            }

            /// <summary>Creates a temporary variable like %1, %2 and %3 that is not in <paramref name="existingVars"/></summary>
            /// <remarks>
            /// This is intended for variables not visible to the user.
            /// For visible variables, use <see cref="CreateUnique"/> instead.
            /// </remarks>
            internal static Variable CreateTemp(IEnumerable<Variable> existingVars)
            {
                var indices = new HashSet<int>();
                foreach (var var in existingVars)
                    if (var.Name.StartsWith("%") && int.TryParse(var.Name.Substring(1), out var num))
                        indices.Add(num);
                var i = 1;
                while (indices.Contains(i))
                    i++;
                return new Variable("%" + i);
            }

        }

        /// <summary>
        /// A mathematical constant: a number whose spelling happens to be a legal identifier.
        /// </summary>
        /// <remarks>
        /// <para>
        /// It is a <see cref="Variable"/> by inheritance, so everything that reads a leaf by name
        /// keeps working, and a distinct type by record identity, which is the whole point: a
        /// binder that declares <c>pi</c> and the constant <c>pi</c> are no longer one object, so
        /// evaluation can tell them apart without being told where it is.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/984">#984</a>
        /// </para>
        /// <para>
        /// A binder binds <b>occurrences</b>, not values, and that is the whole of the second
        /// distinction here. Every constant a writer types is the one object in
        /// <c>NamedConstants</c>, because the parser and <see cref="MathS.pi"/> hand that
        /// object back; the base of <c>ln</c> and of <c>exp</c> is
        /// <see cref="EulerIntrinsic"/>, a separate object, because it is Euler's number standing
        /// in an operator's own definition and not a reference to the name <c>e</c>. A binder
        /// replaces the occurrences it was handed — compared by reference — so
        /// <c>sum(ln(x), e, 1, 2)</c> is <c>2 * ln(x)</c> while <c>sum(log(e, x), e, 1, 2)</c>,
        /// where the writer did name <c>e</c>, is <c>log(1, x) + log(2, x)</c>.
        /// </para>
        /// <para>
        /// The two are <b>equal</b>, and deliberately so: they are the same number, they print
        /// alike, and any rule that matches one matches the other, so nothing about canonical form,
        /// equality or substitution changes. Only the identity of the occurrence differs, and only
        /// while a binder is deciding what it binds — which is at construction, before anything
        /// evaluates.
        /// </para>
        /// </remarks>
        public sealed partial record Constant : Variable
        {
            private Constant(string name) : base(name) { }

            /// <summary>A constant as the name a writer types. One object per name, kept in
            /// <c>Variable.NamedConstants</c> — a binder recognises it by reference.</summary>
            internal static Constant Named(string name) => new(name);

            /// <summary>What this constant is worth.</summary>
            /// <remarks>
            /// A computed property on purpose: a record compares its instance fields, and the
            /// identity of a constant is its name and its role, not a hundred digits of it.
            /// </remarks>
            internal Number.Complex Value => ConstantList[Name]();

            /// <summary>
            /// Euler's number as the base of <c>ln</c> and of <c>exp</c>. Equal to the written
            /// <c>e</c> and a different object from it, which is what keeps a binder over the
            /// name <c>e</c> out of a logarithm.
            /// </summary>
            [ConstantField] internal static readonly Constant EulerIntrinsic = new(nameof(e));
        }
        #endregion
    }
}
