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
        /// <summary>
        /// Converts an expression into a string. Works synonymically into <see cref="Entity.ToString"/>.
        /// </summary>
        /// <remarks>
        /// A node whose <see cref="Codomain"/> is not the one its type carries by default is
        /// printed inside <c>domain(inner, SET)</c>, which is the syntax the parser already has
        /// for it. Without that the annotation is dropped and the printed form reads back as a
        /// different expression — <c>domain(sqrt(-1), RR)</c> is <see cref="MathS.NaN"/> and
        /// <c>sqrt(-1)</c> is <c>i</c>, and the two used to print the same string.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1022">#1022</a>
        /// </remarks>
        public string Stringize()
        {
            if (spelling != Spelling.Unchosen || !MathS.Settings.UnicodeOutput)
                return StringizeAsWritten();
            spelling = Spelling.ForUnicode;
            string ascii;
            try { ascii = StringizeAsWritten(); }
            finally { spelling = Spelling.Unchosen; }
            return UnicodeSpelling.Respell(ascii);
        }

        /// <summary>
        /// The ASCII form whatever <see cref="MathS.Settings.UnicodeOutput"/> says: what the
        /// library reads for itself -- an ordering by printed form, a key, the SymPy code, the
        /// JSON form -- so that no answer depends on how the caller wants output shown.
        /// https://github.com/asc-community/AngouriMath/issues/1242
        /// </summary>
        internal string StringizeAscii()
        {
            if (spelling == Spelling.Unchosen && !MathS.Settings.UnicodeOutput)
                return StringizeAsWritten();
            var was = spelling;
            spelling = Spelling.Ascii;
            try { return StringizeAsWritten(); }
            finally { spelling = was; }
        }

        private enum Spelling { Unchosen, ForUnicode, Ascii }

        /// <summary>
        /// Chosen by the outermost <see cref="Stringize()"/> of a print, and read by the nodes
        /// inside it, so that a child printed on its way to a parent is not respelled on its own.
        /// </summary>
        [System.ThreadStatic] private static Spelling spelling;

        /// <summary>
        /// Whether this print is the ASCII text a Unicode one is respelled from, in which case
        /// the two nodes the respelling cannot see the extent of print the ASCII token standing
        /// for their glyph: <c>a &lt;&gt; b</c> for <c>not a = b</c>, and <c>≡</c> for a
        /// congruence's <c>=</c>.
        /// </summary>
        private protected static bool SpelledForUnicode => spelling == Spelling.ForUnicode;

        private string StringizeAsWritten()
            => PrintsItsCodomain
                // `Any` is a keyword in that position rather than a set, since there is no node
                // for "no restriction" to ask for a name. #1048
                ? Codomain is Domain.Any
                    ? $"domain({StringizeNode()}, Any)"
                    : $"domain({StringizeNode()}, {Set.SpecialSet.Create(Codomain).Stringize()})"
                : StringizeNode();

        /// <summary>
        /// Converts this node, and this node only, into a string: the codomain wrapper is
        /// <see cref="Stringize()"/>'s to add, so that it is decided in one place rather than in
        /// each of the sixty-odd nodes.
        /// </summary>
        private protected abstract string StringizeNode();

        /// <summary>
        /// Converts an expression into a string
        /// </summary>
        /// <param name="parenthesesRequired">Whether to wrap with '(' and ')'</param>
        /// <remarks>
        /// A node that prints its codomain needs no brackets whatever its priority, because
        /// <c>domain(...)</c> is a function call and so already binds tighter than any operator
        /// around it.
        /// </remarks>
        protected internal string Stringize(bool parenthesesRequired) =>
            (parenthesesRequired && !PrintsItsCodomain) || MathS.Diagnostic.OutputExplicit
                ? $"({Stringize()})" : Stringize();
    }
}
