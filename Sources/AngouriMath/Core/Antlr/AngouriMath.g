/*

Remember to run the "antlr_rerun.bat" file located at "Sources/Utils/antlr_rerun.bat" (relative to the repository root)
every time you modify this file so that the generated source files under the Antlr folder are updated and changes are
reflected in other parts of AngouriMath. The script only consists of commands that are consistent across CMD and Bash,
so you should be able to run it on Windows, Linux, or Mac. You need to have an installed Java Runtime, however.

*/

grammar AngouriMath; // Should be identical to the file name or ANTLR will complain

options
{
    // Equivalent to passing "-Dlanguage=CSharp" into "antlr-4.8-complete.jar" in "antlr_rerun.bat"
    language = CSharp;
}

@modifier{internal}
@ctorModifier{internal}

@parser::header
{
    using System.Linq;
    using AngouriMath;
    using static AngouriMath.Core.Exceptions.FunctionArgumentCountException;
    using static AngouriMath.Entity.Number;
    using AngouriMath.Core.Exceptions;
    using static AngouriMath.Entity.Set;
    using static AngouriMath.Entity;
}

@lexer::members
{
    // As the declaration order of static fields is the initialization order
    // We will get null if we access the private static field _LiteralNames from static fields defined here
    // So these are instance fields
    public readonly CommonToken Multiply = new(Array.IndexOf(_LiteralNames, "'*'"), "*");
    public readonly CommonToken Power = new(Array.IndexOf(_LiteralNames, "'^'"), "^");
}
@parser::members
{
    // Nullable reference type analysis is disabled by default for generated code without '#nullable enable'
    public Entity Result = null;

    public void Parse() { this.statement(); }

    // A name the grammar does not know, followed by a bracket, falls through to the implicit
    // multiplication that lets a(b + c) mean a * (b + c). For a one-argument call that never
    // fails: floor(x) came back as the product of an undeclared variable named floor with x,
    // and (floor(x) - 3 = 0).Solve("x") answered { 3 / floor }, which is a root of nothing.
    // Refusing every unknown name is not an option -- it is what makes a(b + c) work -- so the
    // names below are refused one at a time, exactly as arcsinh is above.
    // https://github.com/asc-community/AngouriMath/issues/733
    static Entity NotImplementedFunction(string name, string what)
        => throw new UnrecognizedFunctionParseException(
            $"there is no function {name}: AngouriMath has no {what}. It is refused by name " +
            $"rather than read as the product of a variable named {name} with its argument, " +
            $"which is what an unknown name followed by a bracket would otherwise mean");
}

@namespace { Antlr }

// Order of expressions do not affect parsing, but we put dependees first and dependents after to clarify logic 

// But according to https://stackoverflow.com/a/16490720/5429648, 
// You need to place parser rules (which start with a lowercase letter)
// before lexer rules (which start with an uppercase letter) in your grammar
// After encountering a lexer rule, the [ triggers a LEXER_CHAR_SET instead of ARG_ACTION,
// so the token stream seen by the compiler looks like you're passing a set of characters where the
// return value should be.

// So we put UPPER_CASE lexer rules after lower_case parser rules.

// atom is defined later
factorial_expression returns[Entity value]
    : p = atom '!' { $value = MathS.Factorial($p.value); }
    | p = atom { $value = $p.value; }
    ;

power_list returns[List<Entity> value]
    @init { $value = new(); }
    : ('^' factorial_expression { $value.Add($factorial_expression.value); })+
    | ('^' unary_expression { $value.Add($unary_expression.value); })+
    ;
    
power_expression returns[Entity value]
    : factorial_expression { $value = $factorial_expression.value; }
        (power_list {
            $value = $power_list.value
                        .Prepend($factorial_expression.value)
                        .Reverse()
                        .Aggregate((exp, @base) => @base.Pow(exp));
        })?
    ;
    
/*

Numerical nodes

*/

unary_expression returns[Entity value]
    : ('-' p = power_expression { $value = $p.value is Number num ? -num : -$p.value; } | 
       '+' p = power_expression { $value = $p.value; })
    | ('-' u = unary_expression { $value = -$u.value; } | 
       '+' u = unary_expression { $value = $u.value; })
    | p = power_expression { $value = $p.value; }
    ;
    
/* `1 * 2 * ... * n` is the product of the progression the shown factors determine: the
   factors are collected, and where dots stood among them the pattern operator builds the
   product. Without dots the fold is the same as it was. https://github.com/asc-community/AngouriMath/issues/1437 */
mult_expression returns[Entity value]
   @init { Entity beforeDots = null; Entity afterDots = null; bool mixed = false; }
   : u1 = unary_expression { $value = $u1.value; }
   ('*' ( ('...' | '…') { if (beforeDots is not null) throw new InvalidArgumentParseException("A product written with dots has one ... in it"); beforeDots = $value; }
        | u2 = unary_expression { if (beforeDots is null) $value = $value * $u2.value; else if (afterDots is null) afterDots = $u2.value; else throw new InvalidArgumentParseException("A product written with dots ends with the one factor after them, as 1 * 2 * ... * n"); } ) |
    '/' u2 = unary_expression { $value = $value / $u2.value; mixed = true; } |
    'mod' u2 = unary_expression { $value = $value % $u2.value; mixed = true; })*
   { if (beforeDots is not null) $value = AngouriMath.Core.PatternOperator.SumOrProduct(mixed ? null : Mulf.LinearChildren(beforeDots), afterDots, product: true); }
   ;

/* `1 + 2 + ... + n` is the sum of the progression the shown terms determine, the same way. The
   terms before the dots are read off the folded sum, so that a sum without dots allocates
   nothing it did not. */
sum_expression returns[Entity value]
   @init { Entity beforeDots = null; Entity afterDots = null; }
   : m1 = mult_expression { $value = $m1.value; }
   ('+' ( ('...' | '…') { if (beforeDots is not null) throw new InvalidArgumentParseException("A sum written with dots has one ... in it"); beforeDots = $value; }
        | m2 = mult_expression { if (beforeDots is null) $value = $value + $m2.value; else if (afterDots is null) afterDots = $m2.value; else throw new InvalidArgumentParseException("A sum written with dots ends with the one term after them, as 1 + 2 + ... + n"); } ) |
    '-' m2 = mult_expression { if (beforeDots is null) $value = $value - $m2.value; else throw new InvalidArgumentParseException("A sum written with dots ends with the one term after them, as 1 + 2 + ... + n"); })*
   { if (beforeDots is not null) $value = AngouriMath.Core.PatternOperator.SumOrProduct(Sumf.LinearChildren(beforeDots), afterDots, product: false); }
   ;

/*

Sets

*/

set_operator_intersection returns[Entity value]
    : left = sum_expression { $value = $left.value; }
    ( 'intersect' right = sum_expression { $value = $value.Intersect($right.value); }
    | '/\\' right = sum_expression { $value = $value.Intersect($right.value); }
    )*
    ;

set_operator_union_setsubtraction returns[Entity value]
    : left = set_operator_intersection { $value = $left.value; }
    ( 'unite' right = set_operator_intersection { $value = $value.Unite($right.value); }
    | '\\/' right = set_operator_intersection { $value = $value.Unite($right.value); }
    | 'setsubtract' right = set_operator_intersection { $value = $value.SetSubtract($right.value); }
    | '\\' right = set_operator_intersection { $value = $value.SetSubtract($right.value); }
    )*
    ;

in_operator returns[Entity value]
    : m1 = set_operator_union_setsubtraction { $value = $m1.value; }
    ( 'in' m2 = set_operator_union_setsubtraction { $value = $value.In($m2.value); }
    | 'subset' m2 = set_operator_union_setsubtraction { $value = $value.SubsetOf($m2.value); }
    | '⊆' m2 = set_operator_union_setsubtraction { $value = $value.SubsetOf($m2.value); }
    | 'superset' m2 = set_operator_union_setsubtraction { $value = $m2.value.SubsetOf($value); }
    | '⊇' m2 = set_operator_union_setsubtraction { $value = $m2.value.SubsetOf($value); }
    | 'divides' m2 = set_operator_union_setsubtraction { $value = $value.Divides($m2.value); }
    | '|' m2 = set_operator_union_setsubtraction { $value = $value.Divides($m2.value); })*
    ;


/*

Equality/inequality nodes

*/

comparison_expression returns[Entity value]
   @init { List<Entity> terms = []; List<string> operators = []; Entity modulus = null; }
   : m1 = in_operator { terms.Add($m1.value); }
   (
    ('>=' { operators.Add(">="); } | 
     '<=' { operators.Add("<="); } |
     '>'  { operators.Add(">");  } |
     '<'  { operators.Add("<");  } |
     '='  { operators.Add("=");  } |
     '<>' { operators.Add("<>"); } |
     '≡'  { operators.Add("≡");  })
    m2 = in_operator { terms.Add($m2.value); }
   )*
   // A congruence: `a = b (mod n)` or `a ≡ b (mod n)`, the modulus written once at the end of
   // the line and applying to every link of the chain, as mathematics writes it. `mod` here is
   // not the remainder operator of mult_expression -- that one is a number, this one makes the
   // comparison a relation between residue classes. Read here and nowhere else, so that `(mod n)`
   // after an order comparison is an error rather than a product with nothing.
   // https://github.com/asc-community/AngouriMath/issues/1409
   ('(' 'mod' mod = in_operator ')' { modulus = $mod.value; })?
   {
       if (modulus is not null)
       {
           if (terms.Count == 1)
               throw new InvalidArgumentParseException("A modulus follows a congruence: write a = b (mod n)");
           foreach (var op in operators)
               if (op != "=" && op != "≡")
                   throw new InvalidArgumentParseException($"(mod n) applies to a congruence a = b (mod n), not to {op}");
           for (int i = 0; i < operators.Count; i++)
           {
               var link = new Congruentf(terms[i], terms[i + 1], modulus);
               if (i == 0) $value = link; else $value &= link;
           }
       }
       else if (operators.Contains("≡"))
           throw new InvalidArgumentParseException("A congruence needs its modulus: write a ≡ b (mod n)");
       else if (terms.Count == 1)
           $value = terms[0];
       else
           // Create chain: a < b = c < d becomes (a < b) and (b = c) and (c < d)
           for (int i = 0; i < operators.Count; i++)
           {
               var connective = operators[i] switch
               {   // Directly construct the nodes instead of using convenience methods to avoid built-in chained comparisons
                   ">=" => new GreaterOrEqualf(terms[i], terms[i + 1]),
                   "<=" => new LessOrEqualf(terms[i], terms[i + 1]),
                   ">"  => new Greaterf(terms[i], terms[i + 1]),
                   "<"  => new Lessf(terms[i], terms[i + 1]),
                   "="  => new Equalsf(terms[i], terms[i + 1]),
                   "<>" => !new Equalsf(terms[i], terms[i + 1]),
                   _ => throw new AngouriBugException($"Unknown operator in chained comparison: {operators[i]}")
               };
               if (i == 0) $value = connective; else $value &= connective;
           }
   }
   ;

/*

Boolean nodes

*/

negate_expression returns[Entity value]
    : 'not' op = comparison_expression { $value = !$op.value; }
    | 'not' opn = negate_expression { $value = !$opn.value; }
    | 'not' q = quantified_expression { $value = !$q.value; }
    | op = comparison_expression { $value = $op.value; }
    ;

/* A quantifier may follow a connective, and its body runs to the end of the line there as it
   does at the start: `n >= 2 implies exists a in ZZ* : n = 2 a + 1` quantifies `n = 2 a + 1`,
   as Sullivan and Mackey write it. Being last, it ends the chain it closes.
   https://github.com/asc-community/AngouriMath/issues/1409 */
and_expression returns[Entity value]
    : m1 = negate_expression { $value = $m1.value; }
    ('and' m2 = negate_expression { $value = $value & $m2.value; } |
     '&' m2 = negate_expression { $value = $value & $m2.value; } |
     ('and' | '&') q = quantified_expression { $value = $value & $q.value; }
    )*
    ;

xor_expression returns[Entity value]
    : m1 = and_expression { $value = $m1.value; }
    ('xor' m2 = and_expression { $value = $value ^ $m2.value; } |
     'xor' q = quantified_expression { $value = $value ^ $q.value; })*
    ;

or_expression returns[Entity value]
    : m1 = xor_expression { $value = $m1.value; }
    ( 'or' m2 = xor_expression { $value = $value | $m2.value; } |
      'or' q = quantified_expression { $value = $value | $q.value; })*
    ;

implies_expression returns[Entity value]
    : m1 = or_expression { $value = $m1.value; }
    ('implies' m2 = or_expression { $value = $value.Implies($m2.value); } |
     '->' m2 = or_expression { $value = $value.Implies($m2.value); } |
     ('implies' | '->') q = quantified_expression { $value = $value.Implies($q.value); })*
    ;

/*

Keyword nodes

*/

provided_expression returns[Entity value]
    : expr = implies_expression { $value = $expr.value; }
    ('provided' pred = provided_expression { $value = $value.Provided($pred.value); })?
    // note: even though Provided is associative, we parse it right-to-left matching natural language
    // "I'll go, provided you go, provided it's sunny" - Natural reading: I'll go ← (you go ← sunny)
    ;

/*

Nodes end

*/

/* `a => a + 3`, and `a b => a + b` for `a => b => a + b`. Lowest priority of anything, and
   right-associative through the recursion on `expression`, so the body runs to the end of what
   is being parsed: `a => a + 3` is `a => (a + 3)` and never `(a => a) + 3`.

   The parameters are VARIABLE tokens rather than expressions, which is what makes `a 3 => 3`
   invalid as the plan says it should be: `3` cannot match, the alternative fails, and what is
   left is not a parse.

   The body is built exactly as `lambda(...)` builds it, through Binding, so that the two spell
   the same thing -- including the case a lambda's Variable-typed parameter cannot state
   directly, an index called `i`. https://github.com/asc-community/AngouriMath/issues/976 */
expression returns[Entity value]
    : s = provided_expression { $value = $s.value; }
      ('=>' b = expression
        {
            /* The parameters are read back out of an ordinary expression rather than matched as
               a list of names, and that is about what the parser can predict rather than about
               taste. Written as `names+ '=>' body | expression`, both alternatives begin with a
               name and stay viable through a second one -- juxtaposition being multiplication --
               so `a b => a + b` was decided as a product before the arrow was ever reached, and
               came back "mismatched input '=>'". Sharing the left side leaves one decision, taken
               on the token after it.

               So `a b c` arrives here as the product it parsed as, and its factors in order are
               the parameters. Anything that is not a name fails here instead of failing to
               parse: `a 3 => 3`, which the plan calls invalid, raises rather than being read as
               a lambda over `a` and `3`.

               Through Binding, exactly as `lambda(...)` builds it, so the two spell the same
               thing -- including an index called `i`, which lexes as the imaginary unit and can
               therefore never arrive as a Variable token at all.
               https://github.com/asc-community/AngouriMath/issues/976 */
            Entity lambdaBody = $b.value;
            var parameters = ($value is Mulf ? Mulf.LinearChildren($value) : new[] { $value }).ToList();
            foreach (var x in ((IEnumerable<Entity>)parameters).Reverse())
            {
                var bound = AngouriMath.Core.Binding.Of(x);
                if (bound.Name is not Variable v) throw new InvalidArgumentParseException($"Lambda is expected to have valid parameters, {x} encountered instead");
                lambdaBody = bound.In(lambdaBody).LambdaOver(v);
            }
            $value = lambdaBody;
        }
      )?
    | q = quantified_expression { $value = $q.value; }
    ;

/* A quantified statement: `forall x in S : P`, `exists x in S : P`, `exists! x in S : P`,
   and `∀`, `∃`, `∃!` for the same three. The body runs to the end of the line, as a lambda's
   does, so `forall x in S : P and Q` quantifies `P and Q`; after a connective it runs to the
   end of the line too, so `P implies forall x in S : Q` is `P implies (forall x in S : Q)`, and
   `not forall x in S : P` is the negation of the whole statement, as it is written. Several names share a set --
   `forall x, y in S : P` -- and several sets are listed in order -- `forall x in S, y in T : P`
   -- either way nesting from the left, and a name without a set is an error rather than a
   quantification over everything, since a statement is quantified over something.
   https://github.com/asc-community/AngouriMath/issues/1409 */
quantified_expression returns[Entity value]
    : q = quantifier_keyword names = quantified_names ':' b = expression
        {
            Entity quantified = $b.value;
            var groups = $names.list;
            for (var g = groups.Count - 1; g >= 0; g--)
                quantified = $q.kind switch
                {
                    "forall" => new Forallf(groups[g].name, groups[g].over, quantified),
                    "exists" => new Existsf(groups[g].name, groups[g].over, quantified),
                    _ => new ExistsUniquef(groups[g].name, groups[g].over, quantified)
                };
            $value = quantified;
        }
    ;

quantifier_keyword returns[string kind]
    : ('forall' | '∀') { $kind = "forall"; }
    | ('exists!' | '∃!') { $kind = "exists!"; }
    | ('exists' | '∃') { $kind = "exists"; }
    ;

/* `x in S`, `x, y in S`, `x in S, y in T`: each item is read as an expression so that `x in S`
   arrives as the membership it is, and a bare name takes the set of the next item that has
   one. Resolved from the right, so that the set is known when the name is reached. */
quantified_names returns[List<(Entity name, Entity over)> list]
    @init { var items = new List<Entity>(); }
    : e = in_operator { items.Add($e.value); } (',' e = in_operator { items.Add($e.value); })*
    {
        $list = new List<(Entity name, Entity over)>();
        Entity over = null;
        for (var k = items.Count - 1; k >= 0; k--)
        {
            Entity name;
            if (items[k] is Entity.Set.Inf(var element, var set))
                (name, over) = (element, set);
            else
                name = items[k];
            if (over is null)
                throw new InvalidArgumentParseException($"A quantified name needs a set to range over: write forall {name} in S : ...");
            if (AngouriMath.Core.Binding.Of(name).Name is not Variable)
                throw new InvalidArgumentParseException($"A quantifier binds a name, and {name} is not one");
            $list.Insert(0, (name, over));
        }
    }
    ;


function_arguments returns[List<Entity> list]
    @init { $list = new List<Entity>(); }
    : (e = expression { $list.Add($e.value); } (',' e = expression { $list.Add($e.value); })*)?
    ;

/* The items of a set literal, with `...` (or `…`) allowed among them: a listed set as it
   was, or a pattern -- {1, 2, ..., n}, {2, 4, 6, ...}, {..., -1, 0} -- which the pattern
   operator reads as the progression the shown terms determine. A null stands for the dots.
   https://github.com/asc-community/AngouriMath/issues/1437 */
set_items returns[List<Entity> list]
    @init { $list = new List<Entity>(); }
    : ( ( e = expression { $list.Add($e.value); } | ('...' | '…') { $list.Add(null); } )
        (',' ( e = expression { $list.Add($e.value); } | ('...' | '…') { $list.Add(null); } ))* )?
    ;

interval_arguments returns[(Entity from, Entity to) couple]
    : from = expression { $couple.from = $from.value; } ';' to = expression { $couple.to = $to.value; }
    ;
   
cset_arguments returns[(Entity variable, Entity predicate) couple]
    : variable = expression { $couple.variable = $variable.value; } ':' predicate = expression { $couple.predicate = $predicate.value; }
    ;

atom returns[Entity value]
    : '+oo' { $value = Entity.Number.Real.PositiveInfinity; }
    | '-oo' { $value = Entity.Number.Real.NegativeInfinity; }
    | NAN { $value = Entity.Number.Real.NaN; }
    | NUMBER { $value = Entity.Number.Complex.Parse($NUMBER.text); }
    | BOOLEAN { $value = Entity.Boolean.Parse($BOOLEAN.text); }
    | SPECIALSET { $value = Entity.Set.SpecialSet.Create($SPECIALSET.text); }
    | VARIABLE
        {
            // There is no set of natural numbers here, because the name means {0, 1, 2, ...} to
            // some authors and {1, 2, 3, ...} to others; the two are spelled apart as ZZ* and
            // ZZ+. Read as a variable, NN would go on silently standing for nothing, so the
            // name is refused with the two spellings.
            // https://github.com/asc-community/AngouriMath/issues/1409
            if ($VARIABLE.text == "NN")
                throw new InvalidArgumentParseException("There is no set NN: write ZZ* for the non-negative integers {0, 1, 2, ...} or ZZ+ for the positive integers {1, 2, 3, ...}");
            $value = Entity.Variable.CreateVariableOrConstant($VARIABLE.text);
        }
    | '(|' expression '|)' { $value = $expression.value.Abs(); }
    | '#' p = atom { $value = MathS.Sets.Card($p.value); }

    | '[' function_arguments ']T' { $value = ParsingHelpers.TryBuildingMatrix($function_arguments.list).T; }
    | '[' function_arguments ']' { $value = ParsingHelpers.TryBuildingMatrix($function_arguments.list); }

    | '(' interval_arguments ')' { $value = new Entity.Set.Interval($interval_arguments.couple.from, false, $interval_arguments.couple.to, false); }
    | '[' interval_arguments ')' { $value = new Entity.Set.Interval($interval_arguments.couple.from, true, $interval_arguments.couple.to, false); }
    | '[' interval_arguments ']' { $value = new Entity.Set.Interval($interval_arguments.couple.from, true, $interval_arguments.couple.to, true); }
    | '(' interval_arguments ']' { $value = new Entity.Set.Interval($interval_arguments.couple.from, false, $interval_arguments.couple.to, true); }
    | '(' expression ')' { $value = $expression.value; }
    | '{' cset_args = cset_arguments '}' { $value = new ConditionalSet($cset_args.couple.variable, $cset_args.couple.predicate); }
    | '{' items = set_items '}' { $value = $items.list.Contains(null) ? AngouriMath.Core.PatternOperator.Set($items.list) : new FiniteSet($items.list.Cast<Entity>()); }
    | 'log(' args = function_arguments ')' { $value = Assert("log", (1, 2), $args.list.Count) ? MathS.Log(10, $args.list[0]) : MathS.Log($args.list[0], $args.list[1]); }
    | 'log10(' args = function_arguments ')' { Assert("log10", 1, $args.list.Count); $value = MathS.Log(10, $args.list[0]); }
    | 'log2(' args = function_arguments ')' { Assert("log2", 1, $args.list.Count); $value = MathS.Log(2, $args.list[0]); }
    | 'pow(' args = function_arguments ')' { Assert("pow", 2, $args.list.Count); $value = MathS.Pow($args.list[0], $args.list[1]); }
    | 'sqrt(' args = function_arguments ')' { Assert("sqrt", 1, $args.list.Count); $value = MathS.Sqrt($args.list[0]); }
    | 'cbrt(' args = function_arguments ')' { Assert("cbrt", 1, $args.list.Count); $value = MathS.Cbrt($args.list[0]); }
    | 'sqr(' args = function_arguments ')' { Assert("sqr", 1, $args.list.Count); $value = MathS.Sqr($args.list[0]); }
    | 'ln(' args = function_arguments ')' { Assert("ln", 1, $args.list.Count); $value = MathS.Ln($args.list[0]); }
    | 'exp(' args = function_arguments ')' { Assert("exp", 1, $args.list.Count); $value = MathS.Pow(Entity.Constant.EulerIntrinsic, $args.list[0]); }

    /* Trigonometric functions */
    | 'sin(' args = function_arguments ')' { Assert("sin", 1, $args.list.Count); $value = MathS.Sin($args.list[0]); }
    | 'cos(' args = function_arguments ')' { Assert("cos", 1, $args.list.Count); $value = MathS.Cos($args.list[0]); }
    | 'tan(' args = function_arguments ')' { Assert("tan", 1, $args.list.Count); $value = MathS.Tan($args.list[0]); }
    | 'cotan(' args = function_arguments ')' { Assert("cotan", 1, $args.list.Count); $value = MathS.Cotan($args.list[0]); }
    | 'cot(' args = function_arguments ')' { Assert("cotan", 1, $args.list.Count); $value = MathS.Cotan($args.list[0]); }
    | 'sec(' args = function_arguments ')' { Assert("sec", 1, $args.list.Count); $value = MathS.Sec($args.list[0]); }
    | 'cosec(' args = function_arguments ')' { Assert("cosec", 1, $args.list.Count); $value = MathS.Cosec($args.list[0]); }
    | 'csc(' args = function_arguments ')' { Assert("cosec", 1, $args.list.Count); $value = MathS.Cosec($args.list[0]); }
    | 'arcsin(' args = function_arguments ')' { Assert("arcsin", 1, $args.list.Count); $value = MathS.Arcsin($args.list[0]); }
    | 'arccos(' args = function_arguments ')' { Assert("arccos", 1, $args.list.Count); $value = MathS.Arccos($args.list[0]); }
    | 'arctan(' args = function_arguments ')' { Assert("arctan", 1, $args.list.Count); $value = MathS.Arctan($args.list[0]); }
    | 'arccotan(' args = function_arguments ')' { Assert("arccotan", 1, $args.list.Count); $value = MathS.Arccotan($args.list[0]); }
    | 'arcsec(' args = function_arguments ')' { Assert("arcsec", 1, $args.list.Count); $value = MathS.Arcsec($args.list[0]); }
    | 'arccosec(' args = function_arguments ')' { Assert("arccosec", 1, $args.list.Count); $value = MathS.Arccosec($args.list[0]); }
    | 'arccsc(' args = function_arguments ')' { Assert("arccosec", 1, $args.list.Count); $value = MathS.Arccosec($args.list[0]); }
    | 'acsc(' args = function_arguments ')' { Assert("arccosec", 1, $args.list.Count); $value = MathS.Arccosec($args.list[0]); }
    | 'asin(' args = function_arguments ')' { Assert("arcsin", 1, $args.list.Count); $value = MathS.Arcsin($args.list[0]); }
    | 'acos(' args = function_arguments ')' { Assert("arccos", 1, $args.list.Count); $value = MathS.Arccos($args.list[0]); }
    | 'atan(' args = function_arguments ')' { Assert("arctan", 1, $args.list.Count); $value = MathS.Arctan($args.list[0]); }
    | 'acotan(' args = function_arguments ')' { Assert("arccotan", 1, $args.list.Count); $value = MathS.Arccotan($args.list[0]); }
    | 'asec(' args = function_arguments ')' { Assert("arcsec", 1, $args.list.Count); $value = MathS.Arcsec($args.list[0]); }
    | 'acosec(' args = function_arguments ')' { Assert("arccosec", 1, $args.list.Count); $value = MathS.Arccosec($args.list[0]); }
    | 'acot(' args = function_arguments ')' { Assert("arccotan", 1, $args.list.Count); $value = MathS.Arccotan($args.list[0]); }
    | 'arccot(' args = function_arguments ')' { Assert("arccotan", 1, $args.list.Count); $value = MathS.Arccotan($args.list[0]); }
    /* End */


    /* Hyperbolic functions */
    | 'sinh(' args = function_arguments ')' { Assert("sin", 1, $args.list.Count); $value = MathS.Hyperbolic.Sinh($args.list[0]); }
    | 'sh(' args = function_arguments ')' { Assert("sin", 1, $args.list.Count); $value = MathS.Hyperbolic.Sinh($args.list[0]); }

    | 'cosh(' args = function_arguments ')' { Assert("cos", 1, $args.list.Count); $value = MathS.Hyperbolic.Cosh($args.list[0]); }
    | 'ch(' args = function_arguments ')' { Assert("cos", 1, $args.list.Count); $value = MathS.Hyperbolic.Cosh($args.list[0]); }

    | 'tanh(' args = function_arguments ')' { Assert("tan", 1, $args.list.Count); $value = MathS.Hyperbolic.Tanh($args.list[0]); }
    | 'th(' args = function_arguments ')' { Assert("tan", 1, $args.list.Count); $value = MathS.Hyperbolic.Tanh($args.list[0]); }

    | 'cotanh(' args = function_arguments ')' { Assert("cotan", 1, $args.list.Count); $value = MathS.Hyperbolic.Cotanh($args.list[0]); }
    | 'coth(' args = function_arguments ')' { Assert("cotan", 1, $args.list.Count); $value = MathS.Hyperbolic.Cotanh($args.list[0]); }
    | 'cth(' args = function_arguments ')' { Assert("cotan", 1, $args.list.Count); $value = MathS.Hyperbolic.Cotanh($args.list[0]); }

    | 'sech(' args = function_arguments ')' { Assert("sec", 1, $args.list.Count); $value = MathS.Hyperbolic.Sech($args.list[0]); }
    | 'sch(' args = function_arguments ')' { Assert("sec", 1, $args.list.Count); $value = MathS.Hyperbolic.Sech($args.list[0]); }

    | 'cosech(' args = function_arguments ')' { Assert("cosec", 1, $args.list.Count); $value = MathS.Hyperbolic.Cosech($args.list[0]); }
    | 'csch(' args = function_arguments ')' { Assert("cosec", 1, $args.list.Count); $value = MathS.Hyperbolic.Cosech($args.list[0]); }

    | 'asinh(' args = function_arguments ')' { Assert("arcsin", 1, $args.list.Count); $value = MathS.Hyperbolic.Arsinh($args.list[0]); }
    | 'arsinh(' args = function_arguments ')' { Assert("arcsin", 1, $args.list.Count); $value = MathS.Hyperbolic.Arsinh($args.list[0]); }
    | 'arsh(' args = function_arguments ')' { Assert("arcsin", 1, $args.list.Count); $value = MathS.Hyperbolic.Arsinh($args.list[0]); }
    | 'arcsinh(' args = function_arguments ')' { throw new UnrecognizedFunctionParseException("there is no function arcsinh: the inverse hyperbolic functions are area functions, not arc functions, so the inverse hyperbolic sine is arsinh, asinh or arsh"); }

    | 'acosh(' args = function_arguments ')' { Assert("arccos", 1, $args.list.Count); $value = MathS.Hyperbolic.Arcosh($args.list[0]); }
    | 'arcosh(' args = function_arguments ')' { Assert("arccos", 1, $args.list.Count); $value = MathS.Hyperbolic.Arcosh($args.list[0]); }
    | 'arch(' args = function_arguments ')' { Assert("arccos", 1, $args.list.Count); $value = MathS.Hyperbolic.Arcosh($args.list[0]); }
    | 'arccosh(' args = function_arguments ')' { throw new UnrecognizedFunctionParseException("there is no function arccosh: the inverse hyperbolic functions are area functions, not arc functions, so the inverse hyperbolic cosine is arcosh, acosh or arch"); }

    | 'atanh(' args = function_arguments ')' { Assert("arctan", 1, $args.list.Count); $value = MathS.Hyperbolic.Artanh($args.list[0]); }
    | 'artanh(' args = function_arguments ')' { Assert("arctan", 1, $args.list.Count); $value = MathS.Hyperbolic.Artanh($args.list[0]); }
    | 'arth(' args = function_arguments ')' { Assert("arctan", 1, $args.list.Count); $value = MathS.Hyperbolic.Artanh($args.list[0]); }
    | 'arctanh(' args = function_arguments ')' { throw new UnrecognizedFunctionParseException("there is no function arctanh: the inverse hyperbolic functions are area functions, not arc functions, so the inverse hyperbolic tangent is artanh, atanh or arth"); }

    | 'acoth(' args = function_arguments ')' { Assert("arccotan", 1, $args.list.Count); $value = MathS.Hyperbolic.Arcotanh($args.list[0]); }
    | 'arcoth(' args = function_arguments ')' { Assert("arccotan", 1, $args.list.Count); $value = MathS.Hyperbolic.Arcotanh($args.list[0]); }
    | 'acotanh(' args = function_arguments ')' { Assert("arccotan", 1, $args.list.Count); $value = MathS.Hyperbolic.Arcotanh($args.list[0]); }
    | 'arcotanh(' args = function_arguments ')' { Assert("arccotan", 1, $args.list.Count); $value = MathS.Hyperbolic.Arcotanh($args.list[0]); }
    | 'arcth(' args = function_arguments ')' { Assert("arccotan", 1, $args.list.Count); $value = MathS.Hyperbolic.Arcotanh($args.list[0]); }
    | 'arccotanh(' args = function_arguments ')' { throw new UnrecognizedFunctionParseException("there is no function arccotanh: the inverse hyperbolic functions are area functions, not arc functions, so the inverse hyperbolic cotangent is arcotanh, acotanh, arcoth, acoth or arcth"); }

    | 'asech(' args = function_arguments ')' { Assert("arcsec", 1, $args.list.Count); $value = MathS.Hyperbolic.Arsech($args.list[0]); }
    | 'arsech(' args = function_arguments ')' { Assert("arcsec", 1, $args.list.Count); $value = MathS.Hyperbolic.Arsech($args.list[0]); }
    | 'arsch(' args = function_arguments ')' { Assert("arcsec", 1, $args.list.Count); $value = MathS.Hyperbolic.Arsech($args.list[0]); }
    | 'arcsech(' args = function_arguments ')' { throw new UnrecognizedFunctionParseException("there is no function arcsech: the inverse hyperbolic functions are area functions, not arc functions, so the inverse hyperbolic secant is arsech, asech or arsch"); }

    | 'acosech(' args = function_arguments ')' { Assert("arccosec", 1, $args.list.Count); $value = MathS.Hyperbolic.Arcosech($args.list[0]); }
    | 'arcosech(' args = function_arguments ')' { Assert("arccosec", 1, $args.list.Count); $value = MathS.Hyperbolic.Arcosech($args.list[0]); }
    | 'arcsch(' args = function_arguments ')' { Assert("arccosec", 1, $args.list.Count); $value = MathS.Hyperbolic.Arcosech($args.list[0]); }
    | 'arccosech(' args = function_arguments ')' { throw new UnrecognizedFunctionParseException("there is no function arccosech: the inverse hyperbolic functions are area functions, not arc functions, so the inverse hyperbolic cosecant is arcosech, acosech, arcsch or acsch"); }
    | 'acsch(' args = function_arguments ')' { Assert("arccosec", 1, $args.list.Count); $value = MathS.Hyperbolic.Arcosech($args.list[0]); }
    /* End */


    | 'factorial(' args = function_arguments ')' { Assert("factorial", 1, $args.list.Count); $value = MathS.Factorial($args.list[0]); }
    | 'gamma(' args = function_arguments ')' { Assert("gamma", 1, $args.list.Count); $value = MathS.Gamma($args.list[0]); }
    | 'derivative(' args = function_arguments ')' 
        {
            if (Assert("derivative", (3, 2), $args.list.Count))
            {
                if ($args.list[2] is Integer { EInteger: var asEInt })
                    $value = MathS.Derivative($args.list[0], $args.list[1], asEInt.ToInt32Checked());
                else
                    throw new InvalidArgumentParseException("Expected integer number for the third argument of derivative");
            } 
            else
                $value = MathS.Derivative($args.list[0], $args.list[1]);
        }
    | 'integral(' args = function_arguments ')' 
        { 
            if (Assert("integral", (4, 2), $args.list.Count))
            {
                if ($args.list.Count == 4)
                    $value = MathS.Integral($args.list[0], $args.list[1], $args.list[2], $args.list[3]);
                else
                    $value = MathS.Integral($args.list[0], $args.list[1]);
            }
            else
                $value = MathS.Integral($args.list[0], $args.list[1]);
        }
    | 'limit(' args = function_arguments ')' { Assert("limit", 3, $args.list.Count); $value = MathS.Limit($args.list[0], $args.list[1], $args.list[2]); }
    | 'limitleft(' args = function_arguments ')' { Assert("limitleft", 3, $args.list.Count); $value = MathS.Limit($args.list[0], $args.list[1], $args.list[2], AngouriMath.Core.ApproachFrom.Left); }
    | 'limitright(' args = function_arguments ')' { Assert("limitright", 3, $args.list.Count); $value = MathS.Limit($args.list[0], $args.list[1], $args.list[2], AngouriMath.Core.ApproachFrom.Right); }
    | 'sum(' args = function_arguments ')' { if ($args.list.Count == 2 && AngouriMath.Functions.ExtremumOverSet.AsRange($args.list[1]) is var (sumVar, sumOver)) $value = MathS.Sum($args.list[0], sumVar, sumOver); else { Assert("sum", 4, $args.list.Count); $value = MathS.Sum($args.list[0], $args.list[1], $args.list[2], $args.list[3]); } }
    | 'product(' args = function_arguments ')' { Assert("product", 4, $args.list.Count); $value = MathS.Product($args.list[0], $args.list[1], $args.list[2], $args.list[3]); }
    | 'signum(' args = function_arguments ')' { Assert("signum", 1, $args.list.Count); $value = MathS.Signum($args.list[0]); }
    | 'sgn(' args = function_arguments ')' { Assert("sgn", 1, $args.list.Count); $value = MathS.Signum($args.list[0]); }
    | 'sign(' args = function_arguments ')' { Assert("sign", 1, $args.list.Count); $value = MathS.Signum($args.list[0]); }
    | 'abs(' args = function_arguments ')' { Assert("abs", 1, $args.list.Count); $value = MathS.Abs($args.list[0]); }
    | 'phi(' args = function_arguments ')' { Assert("phi", 1, $args.list.Count); $value = MathS.NumberTheory.Phi($args.list[0]); }
    | 'prime(' args = function_arguments ')' { Assert("prime", 1, $args.list.Count); $value = MathS.NumberTheory.Prime($args.list[0]); }
    | 'valuation(' args = function_arguments ')' { Assert("valuation", 2, $args.list.Count); $value = MathS.NumberTheory.Valuation($args.list[0], $args.list[1]); }
    | 'card(' args = function_arguments ')' { Assert("card", 1, $args.list.Count); $value = MathS.Sets.Card($args.list[0]); }
    | 'powerset(' args = function_arguments ')' { Assert("powerset", 1, $args.list.Count); $value = MathS.Sets.PowerSet($args.list[0]); }
    | 'union(' args = function_arguments ')' { Assert("union", 2, $args.list.Count); $value = $args.list[1] is Entity.Set.Inf { Element: Variable } unionRange ? MathS.Sets.IndexedUnion($args.list[0], unionRange.Element, unionRange.SupSet) : throw new InvalidArgumentParseException("union expects its second argument to say which name ranges over which set, as in union(A_i, i in I)"); }
    | 'intersection(' args = function_arguments ')' { Assert("intersection", 2, $args.list.Count); $value = $args.list[1] is Entity.Set.Inf { Element: Variable } intersectionRange ? MathS.Sets.IndexedIntersection($args.list[0], intersectionRange.Element, intersectionRange.SupSet) : throw new InvalidArgumentParseException("intersection expects its second argument to say which name ranges over which set, as in intersection(A_i, i in I)"); }
    | 'complement(' args = function_arguments ')' { Assert("complement", 2, $args.list.Count); $value = MathS.Sets.Complement($args.list[0], $args.list[1]); }
    | 'image(' args = function_arguments ')' { Assert("image", 2, $args.list.Count); $value = $args.list[1] is Entity.Set.Inf { Element: Variable } imageRange ? MathS.Sets.Image($args.list[0], imageRange.Element, imageRange.SupSet) : throw new InvalidArgumentParseException("image expects its second argument to say which name ranges over which set, as in image(f(x), x in A)"); }
    | 'preimage(' args = function_arguments ')' { Assert("preimage", 3, $args.list.Count); $value = $args.list[1] is Entity.Set.Inf { Element: Variable } preimageRange ? MathS.Sets.PreImage($args.list[0], preimageRange.Element, preimageRange.SupSet, $args.list[2]) : throw new InvalidArgumentParseException("preimage expects its second argument to say which name ranges over which set, as in preimage(f(x), x in A, Y)"); }
    | 'floor(' args = function_arguments ')' { Assert("floor", 1, $args.list.Count); $value = MathS.Floor($args.list[0]); }
    | 'ceil(' args = function_arguments ')' { Assert("ceil", 1, $args.list.Count); $value = MathS.Ceil($args.list[0]); }
    /* SymPy's spelling, accepted so that an expression copied from there parses. Stringize
       prints the short form, which is what the round-trip test pins. */
    | 'ceiling(' args = function_arguments ')' { Assert("ceiling", 1, $args.list.Count); $value = MathS.Ceil($args.list[0]); }
    | 'round(' args = function_arguments ')' { Assert("round", 1, $args.list.Count); $value = MathS.Round($args.list[0]); }
    /* min and max take any number of arguments, as they do everywhere else, and fold left
       into the binary node. One argument is that argument. */
    /* min(S) is the least member of a set; min(x, x in S and P) the least of { x in S : P }, the
       conjunction read as the set builder reads it. https://github.com/asc-community/AngouriMath/issues/1450 */
    | 'min(' args = function_arguments ')' { AssertAtLeast("min", 1, $args.list.Count); $value = $args.list.Count == 1 && $args.list[0] is Entity.Set minSet ? MathS.Minimum(Variable.CreateUnique(minSet, "x"), Variable.CreateUnique(minSet, "x"), minSet) : $args.list.Count == 2 && AngouriMath.Functions.ExtremumOverSet.AsRange($args.list[1]) is var (minVar, minOver) ? MathS.Minimum($args.list[0], minVar, minOver) : $args.list.Aggregate((a, b) => MathS.Min(a, b)); }
    | 'max(' args = function_arguments ')' { AssertAtLeast("max", 1, $args.list.Count); $value = $args.list.Count == 1 && $args.list[0] is Entity.Set maxSet ? MathS.Maximum(Variable.CreateUnique(maxSet, "x"), Variable.CreateUnique(maxSet, "x"), maxSet) : $args.list.Count == 2 && AngouriMath.Functions.ExtremumOverSet.AsRange($args.list[1]) is var (maxVar, maxOver) ? MathS.Maximum($args.list[0], maxVar, maxOver) : $args.list.Aggregate((a, b) => MathS.Max(a, b)); }
    | 'argmax(' args = function_arguments ')' { Assert("argmax", 2, $args.list.Count); $value = $args.list[1] is Entity.Set.Inf { Element: Variable } argmaxRange ? MathS.Argmax($args.list[0], argmaxRange.Element, argmaxRange.SupSet) : throw new InvalidArgumentParseException("argmax expects its second argument to say which variable ranges over which set, as in argmax(f(t), t in S)"); }
    | 'argmin(' args = function_arguments ')' { Assert("argmin", 2, $args.list.Count); $value = $args.list[1] is Entity.Set.Inf { Element: Variable } argminRange ? MathS.Argmin($args.list[0], argminRange.Element, argminRange.SupSet) : throw new InvalidArgumentParseException("argmin expects its second argument to say which variable ranges over which set, as in argmin(f(t), t in S)"); }
    | 'gcd(' args = function_arguments ')' { AssertAtLeast("gcd", 1, $args.list.Count); $value = $args.list.Aggregate((a, b) => MathS.Gcd(a, b)); }
    | 'lcm(' args = function_arguments ')' { AssertAtLeast("lcm", 1, $args.list.Count); $value = $args.list.Aggregate((a, b) => MathS.Lcm(a, b)); }
    | 'binomial(' args = function_arguments ')' { Assert("binomial", 2, $args.list.Count); $value = MathS.Binomial($args.list[0], $args.list[1]); }
    /* The Iverson bracket, 1 where a statement holds and 0 where it does not: a count written as
       a sum. https://github.com/asc-community/AngouriMath/issues/1478 */
    | 'iverson(' args = function_arguments ')' { Assert("iverson", 1, $args.list.Count); $value = MathS.Iverson($args.list[0]); }
    /* The aleph numbers, the sizes of infinite sets. https://github.com/asc-community/AngouriMath/issues/1409 */
    | 'aleph(' args = function_arguments ')' { Assert("aleph", 1, $args.list.Count); $value = MathS.Aleph($args.list[0]); }
    /* The error functions. https://github.com/asc-community/AngouriMath/issues/1501 */
    | 'erf(' args = function_arguments ')' { Assert("erf", 1, $args.list.Count); $value = MathS.Erf($args.list[0]); }
    | 'erfc(' args = function_arguments ')' { Assert("erfc", 1, $args.list.Count); $value = MathS.Erfc($args.list[0]); }
    | 'erfi(' args = function_arguments ')' { Assert("erfi", 1, $args.list.Count); $value = MathS.Erfi($args.list[0]); }
    /* The exponential and logarithmic integrals. https://github.com/asc-community/AngouriMath/issues/1501 */
    | 'Ei(' args = function_arguments ')' { Assert("Ei", 1, $args.list.Count); $value = MathS.Ei($args.list[0]); }
    | 'li(' args = function_arguments ')' { Assert("li", 1, $args.list.Count); $value = MathS.Li($args.list[0]); }
    /* The trigonometric and hyperbolic integrals. https://github.com/asc-community/AngouriMath/issues/1501 */
    | 'Si(' args = function_arguments ')' { Assert("Si", 1, $args.list.Count); $value = MathS.Si($args.list[0]); }
    | 'Ci(' args = function_arguments ')' { Assert("Ci", 1, $args.list.Count); $value = MathS.Ci($args.list[0]); }
    | 'Shi(' args = function_arguments ')' { Assert("Shi", 1, $args.list.Count); $value = MathS.Shi($args.list[0]); }
    | 'Chi(' args = function_arguments ')' { Assert("Chi", 1, $args.list.Count); $value = MathS.Chi($args.list[0]); }

    /* Names the library does not have. Each is a function every other CAS spells this way, so
       a caller reaches for it, and without these rules each is silently read as a product --
       see NotImplementedFunction above. Refusing is not the feature; it is the difference
       between a missing function and a wrong answer. */

    | 'trunc(' args = function_arguments ')' { $value = NotImplementedFunction("trunc", "rounding functions"); }
    | 'conjugate(' args = function_arguments ')' { $value = NotImplementedFunction("conjugate", "complex conjugate as a symbolic function"); }
    | 'domain(' args = function_arguments ')' 
        { 
            Assert("domain", 2, $args.list.Count); 
            // `Any` is the unrestricted codomain. It is read here rather than lexed as a
            // keyword, because a literal in a parser rule becomes a global lexer token and
            // would reserve the name everywhere -- `Any + 1` stopped parsing when that was
            // tried. It is not a SpecialSet either: there is no node for "no restriction", see
            // SpecialSet.Create(Domain). Reading it in this one position commits to a spelling
            // without deciding whether there is a universal *set*, which is #996.
            // https://github.com/asc-community/AngouriMath/issues/1048
            // The argument is folded to a rational literal first. A codomain is a property of a
            // node rather than a node of its own, so `1/2` annotated here and `1/2` written bare
            // have to become the same shape before the annotation lands -- otherwise the sweep
            // that folds the rest of the tree meets an annotated quotient it cannot tell from an
            // unannotated one, and `domain(1/2, CC)` loses what it was asked for.
            // https://github.com/asc-community/AngouriMath/issues/1048
            var annotated = ParsingHelpers.RationalLiteral($args.list[0]);
            if ($args.list[1] is Variable { Name: "Any" })
                $value = annotated.WithCodomain(AngouriMath.Core.Domain.Any);
            else if ($args.list[1] is not SpecialSet ss)
                throw new InvalidArgumentParseException($"Unrecognized special set {$args.list[1].Stringize()}");
            else
                $value = annotated.WithCodomain(ss.ToDomain());
        }
    | 'piecewise(' args = function_arguments ')'
        {
            var cases = new List<Providedf>();
            foreach (var arg in $args.list)
                if (arg is Providedf provided)
                    cases.Add(provided);
                else
                    cases.Add(new Providedf(arg, true));
            $value = new Piecewise(cases);
        }
    | 'apply(' args = function_arguments ')'
        {
            if ($args.list.Count < 2)
                throw new FunctionArgumentCountException("Should be at least one argument in apply function");
            $value = $args.list[0].Apply($args.list.Skip(1).ToLList());
        }
    | 'lambda(' args = function_arguments ')'
        {
            if ($args.list.Count < 2)
                throw new FunctionArgumentCountException("Should be at least two arguments in lambda function");
            var body = $args.list.Last();
            foreach (var x in ((IEnumerable<Entity>)$args.list).Reverse().Skip(1))
            {
                /* Lambda's parameter is typed Variable, so unlike every other binder it cannot
                   be handed the imaginary unit and read it there. i is what a lambda over an
                   index is called, so it is read here instead.
                   https://github.com/asc-community/AngouriMath/issues/976 */
                var bound = AngouriMath.Core.Binding.Of(x);
                if (bound.Name is not Variable v) throw new InvalidArgumentParseException($"Lambda is expected to have valid parameters, {x} encountered instead");
                body = bound.In(body).LambdaOver(v);
            }
            $value = body;
        }
    ;

statement: expression EOF { Result = $expression.value; } ;

NEWLINE: ('\r'?'\n')+ -> skip ;

// A fragment will never be counted as a token, it only serves to simplify a grammar.
fragment EXPONENT: ('e'|'E') ('+'|'-')? ('0'..'9')+ ;

NUMBER: ('0'..'9')+ '.' ('0'..'9')* EXPONENT? 'i'? | '.'? ('0'..'9')+ EXPONENT? 'i'? | 'i' ;

SPECIALSET: ('CC' | 'RR' | 'QQ' | 'ZZ' | 'BB' | 'ZZ*' | 'ZZ+' | 'PP') ;

BOOLEAN: ('true' | 'True' | 'false' | 'False') ;

// Only the one spelling, which is what Stringize prints. BOOLEAN carries two capitalisations of
// each word because it has to read back its own output -- Entity.Boolean prints True and a caller
// types true -- and NaN prints and reads the same way, so there is nothing to reconcile.
// Declared above VARIABLE, since equal-length matches go to the earlier rule and this word would
// otherwise be an identifier. https://github.com/asc-community/AngouriMath/issues/906
NAN: 'NaN' ;

VARIABLE: ('a'..'z'|'A'..'Z'|'\u0370'..'\u03FF'|'\u1F00'..'\u1FFF'|'\u0400'..'\u04FF'|'\u212F')+ ('_' ('a'..'z'|'A'..'Z'|'0'..'9'|'\u0370'..'\u03FF'|'\u1F00'..'\u1FFF'|'\u0400'..'\u04FF')+)? ;
  
COMMENT: ('//' ~[\r\n]* ('\r'? '\n')? | '/*' .*? '*/') -> skip ;
    
WS : (' ' | '\t')+ -> skip ;