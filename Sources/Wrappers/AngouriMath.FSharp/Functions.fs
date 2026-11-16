module AngouriMath.FSharp.Functions

open Core
open AngouriMath

/// Creates a node of power and inner simplifies it
let ( ** ) a b =
    MathS.Pow(a, b).InnerSimplified

/// simplifies the given expression
let simplified x =
    (parsed x).Simplify()

/// Takes the evaluated form of an expression (preserving the type)
let evaled expr =
    (parsed expr).Evaled

/// Takes the evaluated form of an expression (as a number; if it cannot, an exception is thrown)
let asNumber expr =
    (parsed expr).EvalNumerical()

/// Takes the evaluated form of an expression (as a boolean; if it cannot, an exception is thrown)
let asBool expr =
    (parsed expr).EvalBoolean()

/// Gets the LaTeX form of an expression
let latex x =
    (parsed x).Latexize()

/// Substitutes the given variable with the given value in the given expression
let substituted x value expr = (parsed expr).Substitute(parsed x, parsed value).InnerSimplified

/// Returns a conjunction node of two nodes
let conjunction a b = MathS.Conjunction(parsed a, parsed b).InnerSimplified

/// Returns a disjunction node of two nodes
let disjunction a b = MathS.Disjunction(parsed a, parsed b).InnerSimplified

/// Returns a implication node of two nodes
/// Where the first one is the assumption,
/// and the second one is the conclusion
let implication assum concl = MathS.Implication(parsed assum, parsed concl).InnerSimplified

/// Returns a negation node of a node
let negation a = MathS.Negation(parsed a).InnerSimplified

/// Returns an exclusive disjunction node of two nodes
let exDisjunction a b = MathS.ExclusiveDisjunction(parsed a, parsed b).InnerSimplified

/// Returns a union node of two nodes
let union a b = MathS.Union(parsed a, parsed b).InnerSimplified

/// Returns a intersection node of two nodes
let intersect a b = MathS.Intersection(parsed a, parsed b).InnerSimplified

/// Returns a set subtraction node of two nodes
let setSubtraction a b = MathS.SetSubtraction(parsed a, parsed b).InnerSimplified

/// Returns an equality node of two nodes (not an equation!)
let equality a b = MathS.Equality(parsed a, parsed b).InnerSimplified

/// Returns a greater than node of two nodes (a > b)
let greater a b = MathS.GreaterThan(parsed a, parsed b).InnerSimplified

/// Returns a less than node of two nodes (a < b)
let less a b = MathS.LessThan(parsed a, parsed b).InnerSimplified

/// Returns a greater or equal than node of two nodes (a >= b)
let greaterOrEqual a b = MathS.GreaterOrEqualThan(parsed a, parsed b).InnerSimplified

/// Returns a less or equal than node of two nodes (a >= b)
let lessOrEqual a b = MathS.LessOrEqualThan(parsed a, parsed b).InnerSimplified

/// Returns a logarithm node with the given base (as the first argument)
let log logBase x = MathS.Log(parsed logBase, parsed x).InnerSimplified

/// Returns a logarithm node with e as the base
let ln x = MathS.Ln(parsed x).InnerSimplified

/// Returns a power of the expression with exponent equals 2
let sqr x = MathS.Sqr(parsed x).InnerSimplified

/// Returns a power of the expression with exponent equals 1 / 2
let sqrt x = MathS.Sqrt(parsed x).InnerSimplified

/// Returns a power of the expression with exponent equals 1 / 3
let cbrt x = MathS.Cbrt(parsed x).InnerSimplified

/// Returns a power of the expression with exponent as the second argument
let pow x power = MathS.Pow(parsed x, parsed power).InnerSimplified

/// Returns a factorial node
let factorial x = MathS.Factorial(parsed x).InnerSimplified

/// Returns a gamma node
let gamma x = MathS.Gamma(parsed x).InnerSimplified

/// Returns a sign node
let sgn x = MathS.Signum(parsed x).InnerSimplified

/// Returns a sine node
let sin x = (parsed x).Sin().InnerSimplified

/// Returns a cosine node
let cos x = (parsed x).Cos().InnerSimplified

/// Returns a tangent node
let tan x = (parsed x).Tan().InnerSimplified

/// Returns a cotangent node
let cot x = (parsed x).Cotan().InnerSimplified

/// Returns a secant node
let sec x = (parsed x).Sec().InnerSimplified

/// Returns a cosecant node
let csc x = (parsed x).Cosec().InnerSimplified

/// Returns a arcsine node
let asin x = (parsed x).Arcsin().InnerSimplified

/// Returns a arccosine node
let acos x = (parsed x).Arccos().InnerSimplified

/// Returns a arctangent node
let atan x = (parsed x).Arctan().InnerSimplified

/// Returns a arccotangent node
let acot x = (parsed x).Arccotan().InnerSimplified

/// Returns a arcsecant node
let asec x = (parsed x).Arcsec().InnerSimplified

/// Returns a arccosecant node
let acsc x = (parsed x).Arccosec().InnerSimplified

/// Returns a hyperbolic sine node
let sinh x = MathS.Hyperbolic.Sinh(parsed x).InnerSimplified

/// Returns a hyperbolic cosine node
let cosh x = MathS.Hyperbolic.Cosh(parsed x).InnerSimplified

/// Returns a hyperbolic tangent node
let tanh x = MathS.Hyperbolic.Tanh(parsed x).InnerSimplified

/// Returns a hyperbolic cotangent node
let coth x = MathS.Hyperbolic.Cotanh(parsed x).InnerSimplified

/// Returns a hyperbolic secant node
let sech x = MathS.Hyperbolic.Sech(parsed x).InnerSimplified

/// Returns a hyperbolic cosecant node
let csch x = MathS.Hyperbolic.Cosech(parsed x).InnerSimplified

/// Returns a hyperbolic arsine node
let asinh x = MathS.Hyperbolic.Arsinh(parsed x).InnerSimplified

/// Returns a hyperbolic arcosine node
let acosh x = MathS.Hyperbolic.Arcosh(parsed x).InnerSimplified

/// Returns a hyperbolic artangent node
let atanh x = MathS.Hyperbolic.Artanh(parsed x).InnerSimplified

/// Returns a hyperbolic arcotangent node
let acoth x = MathS.Hyperbolic.Arcotanh(parsed x).InnerSimplified

/// Returns a hyperbolic arsecant node
let asech x = MathS.Hyperbolic.Arsech(parsed x).InnerSimplified

/// Returns a hyperbolic arcosecant node
let acsch x = MathS.Hyperbolic.Arcosech(parsed x).InnerSimplified


/// Returns a bounded closed interval
let closedInterval from ``to`` =
    MathS.Interval(parsed from, parsed ``to``)

/// Returns a bounded open interval
let openInterval from ``to`` =
    MathS.Interval(parsed from, false, parsed ``to``, false)

/// Returns a bounded interval with left point included and right point excluded
let leftInclusiveRightExclusive from ``to`` =
    MathS.Interval(parsed from, true, parsed ``to``, false)

/// Returns a bounded interval with right point included and left point excluded
let leftExclusiveRightInclusive from ``to`` =
    MathS.Interval(parsed from, false, parsed ``to``, true)

/// Returns a half-bounded with left point included
let leftInclusive from =
    MathS.Interval(parsed from, true, parsed "+oo", false)

/// Returns a half-bounded with left point excluded
let leftExclusive from =
    MathS.Interval(parsed from, false, parsed "+oo", false)

/// Returns a half-bounded with right point included
let rightInclusive ``to`` =
    MathS.Interval(parsed "-oo", false, parsed ``to``, true)

/// Returns a half-bounded with right point excluded
let rightExclusive ``to`` =
    MathS.Interval(parsed "-oo", false, parsed ``to``, false)


/// Computes the derivative of the given variable and expression
let derivative x expr =
    (parsed expr).Differentiate(symbol x).InnerSimplified

/// Returns a derivative node
let derivativeNode x expr =
    MathS.Derivative(parsed expr, parsed x)

/// Returns a partial derivative node, the derivative in the slot q with every other slot held fixed
let partialDerivativeNode q expr =
    MathS.PartialDerivative(parsed expr, parsed q)

/// Computes the integral of the given variable and expression
let integral x expr = 
    (parsed expr).Integrate(symbol x).InnerSimplified

/// Returns an integral node
let integralNode x expr =
    MathS.Integral(parsed expr, parsed x)

/// Computes the limit of the given variable, destination to where it approaches, and expression
let limit x destination expr =
    (parsed expr).Limit(symbol x, parsed destination).InnerSimplified

/// Returns a limit node
let limitNode x destination expr =
    MathS.Limit(parsed expr, parsed x, parsed destination)

type LimSide =
    | Left
    | Right

/// Computes the limit of the given variable, destination to where it approaches, expression itself, and the origin side
let limitSided x destination side expr =
    match side with
    | Left -> (parsed expr).Limit(symbol x, parsed destination, AngouriMath.Core.ApproachFrom.Left).InnerSimplified
    | Right -> (parsed expr).Limit(symbol x, parsed destination, AngouriMath.Core.ApproachFrom.Right).InnerSimplified

/// Returns a limit node with a side from which to approach specified
let limitSidedNode x destination side expr =
    match side with
    | Left -> MathS.Limit(parsed expr, parsed x, parsed destination, AngouriMath.Core.ApproachFrom.Left)
    | Right -> MathS.Limit(parsed expr, parsed x, parsed destination, AngouriMath.Core.ApproachFrom.Right)

/// Returns the set of possible values for x
/// provided the statement or equality
let solutions x expr =
    match parsed expr with
    | :? Entity.Statement as statement -> statement.Solve(symbol x).InnerSimplified :?> Entity.Set
    | func -> (equality func 0).Solve(symbol x).InnerSimplified :?> Entity.Set

/// Returns a system of equations, built from a list of equalities or expressions.
/// An expression that is not an equality is read as being equal to zero, exactly as
/// `solutions` reads a single one.
let equationSystem (equations : 'T list) =
    MathS.Equations(equations |> List.map (fun eq ->
        match parsed eq with
        // EquationSystem reads each entry as being equal to zero, so an equality written out
        // is moved to one side rather than handed over as a node -- passing the node makes the
        // solver try to invert an equality, which it cannot do.
        | :? Entity.Equalsf as equation -> equation.Left - equation.Right
        | func -> func))

/// Solves a system of equations over the given variables.
///
/// Each row of the resulting matrix is one solution and its columns follow the order of
/// `vars`, so `solveSystem ["x"; "y"] ["x + y - 3"; "x - y - 1"]` answers `[[2, 1]]`:
/// one solution, with x = 2 and y = 1. Returns None where none was found, rather than an
/// empty matrix, since "no solution" and "the solver gave up" are the same answer here and
/// neither is a matrix.
let solveSystem (vars : 'T list) (equations : 'U list) =
    (equationSystem equations).Solve(vars |> List.map symbol |> Array.ofList)
    |> Option.ofObj
