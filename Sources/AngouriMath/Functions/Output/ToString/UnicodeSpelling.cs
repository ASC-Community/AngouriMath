//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using Antlr4.Runtime;
using System.IO;
using System.Text;

namespace AngouriMath.Core
{
    using Antlr;

    /// <summary>
    /// What <see cref="MathS.Settings.UnicodeOutput"/> prints: the ASCII form, lexed, with each
    /// operator token replaced by the glyph the lexer reads as that same token, and an integer
    /// exponent by superscript digits.
    /// </summary>
    /// <remarks>
    /// Respelling tokens rather than the nodes is what keeps the round trip a property of the
    /// grammar rather than of sixty printers: a name, a number and a function call are tokens
    /// that are never touched, so <c>sin</c> or a variable called <c>notable</c> cannot be
    /// rewritten, and every glyph lexes as the token it replaced, so the parser sees the token
    /// stream it would have seen from the ASCII form.
    /// https://github.com/asc-community/AngouriMath/issues/1242
    /// </remarks>
    internal static class UnicodeSpelling
    {
        [ConstantField]
        private static readonly Dictionary<string, string> Glyphs = new()
        {
            ["'-'"] = "−",
            ["'+oo'"] = "+∞",
            ["'-oo'"] = "−∞",
            ["'<='"] = "≤",
            ["'>='"] = "≥",
            ["'<>'"] = "≠",
            ["'in'"] = "∈",
            ["'subset'"] = "⊆",
            ["'superset'"] = "⊇",
            [@"'\/'"] = "∪",
            ["'unite'"] = "∪",
            [@"'/\'"] = "∩",
            ["'intersect'"] = "∩",
            ["'not'"] = "¬",
            ["'and'"] = "∧",
            ["'&'"] = "∧",
            ["'or'"] = "∨",
            ["'forall'"] = "∀",
            ["'exists'"] = "∃",
            ["'exists!'"] = "∃!",
        };

        // Indexed by the digit: superscript 1, 2 and 3 are in Latin-1, the rest in Superscripts
        // and Subscripts.
        private const string SuperscriptDigits = "⁰¹²³⁴⁵⁶⁷⁸⁹";
        private const string SuperscriptMinus = "⁻";

        private sealed class Listener : IAntlrErrorListener<int>
        {
            internal bool Failed;
            public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
                => Failed = true;
        }

        internal static string Respell(string ascii)
        {
            var listener = new Listener();
            var lexer = new AngouriMathLexer(new AntlrInputStream(ascii), TextWriter.Null, TextWriter.Null);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(listener);
            var tokens = lexer.GetAllTokens();
            // Something the lexer cannot read is printed as it is: the ASCII form round-trips
            // as well as it does, and a guess at respelling around it might not.
            if (listener.Failed)
                return ascii;

            var text = new StringBuilder(ascii.Length);
            var position = 0;
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (Superscript(tokens, i) is var (superscript, last))
                {
                    // The space before the ^ goes with it: x², not x ².
                    text.Append(superscript);
                    position = tokens[last].StopIndex + 1;
                    i = last;
                    continue;
                }
                text.Append(ascii, position, token.StartIndex - position);
                position = token.StopIndex + 1;
                var name = Name(token);
                if (Glyphs.TryGetValue(name, out var glyph))
                {
                    text.Append(glyph);
                    // ¬x rather than ¬ x, as a minus sign is written against what it negates.
                    if (name == "'not'")
                        while (position < ascii.Length && ascii[position] == ' ')
                            position++;
                }
                else if (token.Type == AngouriMathLexer.VARIABLE && token.Text is "pi" or "e")
                    // Both read back as the constants they print: π and ℯ are constants in the
                    // lexer, as pi and e are. https://github.com/asc-community/AngouriMath/issues/1608
                    text.Append(token.Text == "pi" ? "π" : "ℯ");
                else
                    text.Append(token.Text);
            }
            text.Append(ascii, position, ascii.Length - position);
            return text.ToString();
        }

        private static string Name(IToken token)
            => AngouriMathLexer.DefaultVocabulary.GetDisplayName(token.Type);

        /// <summary>
        /// An integer exponent at <paramref name="at"/> -- <c>^ 2</c> or <c>^ (-2)</c> -- as
        /// superscript digits, and the index of its last token; or null where the exponent is
        /// anything else, or where a superscript would misplace it to the eye. Those are a
        /// further <c>^</c>, since <c>x² ^ 3</c> reads as <c>(x²)³</c> where the expression is
        /// <c>x ^ (2 ^ 3)</c>, and a factorial or an entry taken of the exponent, since
        /// <c>2³!</c> reads as <c>(2³)!</c>.
        /// </summary>
        private static (string superscript, int last)? Superscript(IList<IToken> tokens, int at)
        {
            if (Name(tokens[at]) != "'^'")
                return null;
            var negative = at + 4 < tokens.Count
                && Name(tokens[at + 1]) == "'('" && Name(tokens[at + 2]) == "'-'"
                && Name(tokens[at + 4]) == "')'";
            var number = negative ? at + 3 : at + 1;
            if (number >= tokens.Count || tokens[number].Type != AngouriMathLexer.NUMBER)
                return null;
            var digits = tokens[number].Text;
            foreach (var c in digits)
                if (c is < '0' or > '9')
                    return null;
            var last = negative ? at + 4 : at + 1;
            if (last + 1 < tokens.Count && Name(tokens[last + 1]) is "'^'" or "'!'" or "'!!'" or "'!!!'" or "'['")
                return null;
            var superscript = new StringBuilder(negative ? SuperscriptMinus : "", digits.Length + 1);
            foreach (var c in digits)
                superscript.Append(SuperscriptDigits[c - '0']);
            return (superscript.ToString(), last);
        }
    }
}
