# Exceptions

Every exception this library raises deliberately derives from `AngouriMathBaseException`, in
`AngouriMath.Core.Exceptions`. One `catch` on that type is enough to separate "AngouriMath
refused" from anything else going wrong in a program.

```csharp
try { var roots = "x2 + 1 = 0".ToEntity().Solve("x"); }
catch (AngouriMathBaseException e) { Console.WriteLine(e.Message); }
```

There is exactly one deliberate exception to that rule, and it is a debugging aid rather than a
failure: [`MathS.Diagnostic.DiagnosticCatchException`](#mathsdiagnosticdiagnosticcatchexception).

A message should name what it choked on — the sub-expression, the shape, the value — because
*"I could not settle this"* is a far weaker statement than *"I could not invert `sin(x) + x`"*
([#746](https://github.com/asc-community/AngouriMath/issues/746)). Where a message does not yet do
that, it is a defect worth a pull request, not a documented feature.

## The three questions the hierarchy answers

The tree under the base splits by *whose problem it is*, and that is the distinction a caller
actually acts on:

```
System.Exception
└── AngouriMathBaseException                 abstract; catch this to catch everything
    ├── MathSException                       abstract; the input was wrong — catch and report
    │   ├── ParseException                   abstract; the input string was wrong
    │   │   ├── UnhandledParseException
    │   │   ├── MissingOperatorParseException
    │   │   ├── UnrecognizedFunctionParseException
    │   │   ├── InvalidArgumentParseException
    │   │   ├── FunctionArgumentCountException
    │   │   ├── CannotParseInstanceException
    │   │   └── UnrecognizedDomainException  never thrown — see below
    │   ├── TreeException                    abstract; the expression tree was wrong
    │   │   └── UncompilableNodeException
    │   ├── CannotEvalException
    │   ├── ElementInSetAmbiguousException
    │   ├── SolveRequiresStatementException
    │   ├── LimitOperationNotSupportedException
    │   ├── InvalidNumberException
    │   ├── NumberCastException
    │   ├── InvalidNumericSystemException
    │   ├── WrongNumberOfArgumentsException
    │   ├── InvalidMatrixOperationException
    │   ├── BadMatrixShapeException
    │   └── InvalidProtocolProvided
    ├── NotSufficientlySupportedException    the library cannot do this — a feature request
    └── AngouriBugException                  a defect in this library — report it
```

| you are asking | catch |
|---|---|
| did the user's input get refused? | `MathSException` |
| specifically, was the *string* bad? | `ParseException` |
| is this library missing a capability I need? | `NotSufficientlySupportedException` |
| is this library broken? | `AngouriBugException` — do not catch it, let it reach a bug report |

**`AngouriBugException` is not a catch target.** It is thrown from an invariant that the library
believes cannot be violated, its message ends by asking you to report it, and catching it turns a
reproducible defect into a silent wrong answer. If you see one, open an issue with the expression
that produced it. It is by a wide margin the most-thrown type in the library, which says more
about how many internal invariants are asserted than about how often they fail.

**`NotSufficientlySupportedException` is a catch target, and a different conversation.** Nothing is
broken; the library is telling you it has no procedure for what you asked. Falling back to a
numerical method, or to another system, is a reasonable response — so is opening a feature request,
since the message names what was missing.

## The types

### `AngouriMathBaseException`

Abstract, the root. Catch it to catch everything this library raises on purpose. It carries no
information of its own.

### `MathSException`

Abstract, and the honest name for it is *the caller handed us something we cannot work with*. Every
type below it means the input was at fault; none of them means the library is broken. Catching this
and showing `e.Message` to the person who typed the expression is the intended use.

### `ParseException`

Abstract, under `MathSException`: the *string* could not be read. Thrown out of
`MathS.FromString`, the `(Entity)"…"` cast, `"…".ToEntity()` and every typed parse
(`Entity.Variable`, `Number.Complex`, `Entity.Boolean`, …).

`MathS.Parse` is the non-throwing form of the same thing: it hands back a `ParsingResult` carrying
a `ReasonOfFailureWhileParsing` instead of throwing, and it is what to use when an invalid input is
expected rather than exceptional.

#### `UnhandledParseException`

The parser generator reported a syntax error and the library has no more specific reading of it.
The message begins with ANTLR's own `line L:C …` location and ends with the input it was measured
against. This is the ordinary "that is not an expression" answer — `"()"`, `"+!"`, `"a*a_"`.

#### `MissingOperatorParseException`

Only reachable with `MathS.Settings.ExplicitParsingOnly` on. With it off, `2x` has a `*` inserted
for you; with it on, the insertion becomes this refusal, and the message names the two tokens the
operator was wanted between.

#### `UnrecognizedFunctionParseException`

A name that is not a function was applied to arguments, in one of the cases where reading it as a
product would be a silent surprise. `arcsinh(x)` is the founding example — the inverse hyperbolic
functions are *area* functions, so the spelling is `arsinh` — and the same refusal covers names
this library simply does not have yet, such as `trunc` or `lcm`
([#733](https://github.com/asc-community/AngouriMath/issues/733)). The message names the function
and, where there is one, the spelling that works.

It is deliberately *not* thrown for every unknown name: `a(b + c)` has to keep meaning
`a * (b + c)`, so names are refused one at a time.

#### `InvalidArgumentParseException`

A known construct was given an argument of the wrong shape — `derivative(x, x, y)` where the order
has to be an integer, a lambda whose parameter is not a variable, an unrecognised special set.

#### `FunctionArgumentCountException`

A known function was called with the wrong number of arguments: `sin(3, 5)`, `log()`,
`derivative(x)`. The message names the function, the arity it wanted and the arity it got. The
`Assert` helpers on this type are what the grammar's actions call, so it is raised uniformly for
every function rather than per call site.

Note the split with `WrongNumberOfArgumentsException` below: this one is about a function call
*written in a string*, that one about a call *made through the API*.

#### `CannotParseInstanceException`

The string parsed, but not into the type asked for. `(Entity.Variable)"x + 1"` and
`Number.Complex.Parse("quack")` raise it; the message names the target type and the input. Every
type that raises it also has a `TryParse` that returns `false` instead.

#### `UnrecognizedDomainException`

**Nothing throws this.** It exists for an unknown domain name reaching the domain function, and the
place that would raise it — `Entity.Set.SpecialSet.ToDomain(string)`, behind the public
`SpecialSet.Create(string)` — throws `AngouriBugException` instead, which tells the caller to
report their own typo as a library defect. That is the wrong answer to a wrong string, and
correcting it changes which exception a caller sees, so it wants a pull request of its own.

### `TreeException`

Abstract: the expression tree, rather than the string it came from, is not one this operation can
take.

#### `UncompilableNodeException`

`Compile` was asked for a function it cannot build. Two distinct causes, and the message says which:

- a node with no compiled form — a derivative, an integral, a limit, a function of a matrix;
- a variable the expression mentions that the compilation was not given. The message names the
  variable and lists the ones it *was* given, since `Compile("x")` on `a * x` failing used to
  surface as a bare `KeyNotFoundException` about some dictionary.

Both compilers raise it: the stack-machine one (`Compile`) and the LINQ one
(`Compile<TIn, TOut>`).

A matrix is not on the first list any more: under the LINQ compiler it compiles to a
`GenTensor<T, TWrapper>` over its elements' type — `GenTensor<double, DoubleOperations>`,
`GenTensor<Complex, ComplexOperations>`, and the same for `float`, `int`, `long`, `BigInteger`,
the wrappers living in `AngouriMath.Core.Compilation.IntoLinq` — and that is the type to compile
to and to pass a matrix in as: `"A * B".ToEntity().Compile<GenTensor<double, DoubleOperations>,
GenTensor<double, DoubleOperations>, GenTensor<double, DoubleOperations>>("A", "B")` is the matrix
product, `2 A + B - 1` scales, adds and shifts element by element, `A ^ 3` is the matrix power
([#526](https://github.com/asc-community/AngouriMath/issues/526)). Compiling a matrix as a
number, or a number as a matrix, or `sin(A)`, is still this exception.

### `CannotEvalException`

Something did not collapse to a single number or boolean. Overwhelmingly this is
`Entity.EvalNumerical()` or `Entity.EvalBoolean()` on an expression with a free variable in it, and
the message says what the expression *did* evaluate to. `EvaluableNumerical` and `EvaluableBoolean`
are the checks that avoid it; `Evaled` is the answer that does not throw.

### `ElementInSetAmbiguousException`

`Set.Contains` could not decide. Membership of a symbolic element in a symbolic set is not always
decidable, so `Contains` is the convenience over `TryContains`, which returns whether it could
decide instead of raising. Use `TryContains` unless you are confident about the set.

### `SolveRequiresStatementException`

`Solve` was handed an expression rather than a statement. `(x + 1).Solve("x")` is not a question —
`(x + 1 = 0).Solve("x")` is. It is the one exception type in the library with no message parameter,
since there is nothing to say beyond the rule.

### `LimitOperationNotSupportedException`

A limit that the limit machinery declines to take, currently only one: a complex infinity as the
destination. The message writes out the whole limit that was asked for.

### `InvalidNumberException`

A number, or a numeric argument, that this operation cannot use: a non-finite value handed to
`Rational.Create`, a negative term count for a Taylor expansion, a factorial argument too wide for
an `int32`, a decimal precision setting that does not fit in one.

### `NumberCastException`

An explicit cast between number types could not be made — `(int)someComplexNumber`. The message
names both types. It is the numeric-tower counterpart of `InvalidCastException`, and it is a
`MathSException` because the cast is something the caller asked for.

### `InvalidNumericSystemException`

`MathS.ToBaseN` was given a base it cannot work in. The message names the
largest base there are digits for, and the base that was asked for.

### `WrongNumberOfArgumentsException`

An API call whose arity does not match: a compiled function called with the wrong number of values,
or a truth table asked for over a different number of variables than the expression has. Where the
expression is in scope the message names it and its variables. A system of equations with more or
fewer equations than unknowns is not one: it is solved, or declined as `null`.

Compare `FunctionArgumentCountException`, which is the same complaint about a function call written
inside a string. **Use `FunctionArgumentCountException` from the parser and this one from
everywhere else**; they are not interchangeable, since only the first is a `ParseException` and a
caller wrapping `FromString` in `catch (ParseException)` will miss this one.

### `InvalidMatrixOperationException`

The general matrix complaint, and the one to reach for: the shapes do not agree for the operation,
the matrix is not square, `AsScalar` on something that is not 1x1, a tensor of the wrong number of
dimensions, a row of the wrong width added to a `MatrixBuilder`. The messages name the shapes
involved.

### `BadMatrixShapeException`

The same subject, narrower: matrices being concatenated whose extents disagree along the joining
axis — `MathS.Matrices.Concat`. **It exists only for concatenation.** For any new shape check,
`InvalidMatrixOperationException` is the right type; adding to this one would make two types mean
the same thing.

### `InvalidProtocolProvided`

The built-in `CompilationProtocol` has no rule for the operator and type combination being
compiled, and no custom protocol was supplied. The fix is to provide a
`CompilationProtocol` overriding the conversion the message names. (The type name is missing the
`Exception` suffix that every other type here carries; renaming it is a breaking change and has not
been made.)

### `NotSufficientlySupportedException`

Not a failure of the input and not a defect: the library, or something it is talking to, has no
procedure for what was asked. Currently raised by inversion that would need a set-valued or
piecewise answer, inequalities above the second degree, solving a piecewise statement, serializing
a matrix, and `ToSymPy` on nodes SymPy has no spelling for.

Distinct from `CannotEvalException`, which says *this expression* does not collapse to a value, and
from an unevaluated answer, which is how most missing machinery is reported: an integral this
library cannot take comes back as an `Integralf`, not as an exception. This type is for the paths
where handing back the input is not available.

### `AngouriBugException`

An internal invariant did not hold. Its constructor appends a request to report it, so the message
always ends in the repository and website addresses. **A caller should not catch this** — see the
note above. If one reaches you, the expression that produced it is a bug report.

### `MathS.Diagnostic.DiagnosticCatchException`

The one type here that is *not* under `AngouriMathBaseException`, deliberately: it derives straight
from `System.Exception`, because it is not a failure. Set
`MathS.Diagnostic.CatchOnSimplify` to a predicate and `Simplify` raises this the moment an
intermediate expression satisfies it — a breakpoint for a rewrite you cannot otherwise locate.
Being outside the hierarchy is what keeps a caller's `catch (AngouriMathBaseException)` from
swallowing their own debugging aid.

## Two things a caller can rely on

**Invalid input arrives as this library's own exception.** Anything reachable by handing a string
to the parser comes back under `AngouriMathBaseException` and never as a raw framework exception —
a `NullReferenceException` out of a recovering parse was
[#813](https://github.com/asc-community/AngouriMath/issues/813), and there is a test holding that
line.

**`ArgumentNullException` and `ArgumentOutOfRangeException` are framework contracts, not
mathematics.** The transformation layer and a few numeric helpers raise them for a null or
out-of-range argument, as any .NET API does. They are outside this hierarchy on purpose: they mean
the calling *code* is wrong, not the mathematics it asked about.
