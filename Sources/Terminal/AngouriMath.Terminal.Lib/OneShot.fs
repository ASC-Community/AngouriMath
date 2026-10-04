/// <summary>
/// The commands that answer once and exit, so that <c>amcli</c> can sit in a pipe or a script:
/// <c>amcli simp "sin(x)^2 + cos(x)^2"</c> prints <c>1</c>. With no arguments, <c>amcli</c> opens
/// the terminal instead.
/// </summary>
/// <remarks>
/// These are the commands of the command-line tool that lived in asc-community/AngouriMathCLI,
/// with its names, its argument order and its reading of stdin, so that a script written for it
/// keeps working. Each is one call to the library, and none of them starts the F# kernel, which
/// is what makes the terminal take seconds to open.
/// https://github.com/asc-community/AngouriMath/issues/1649
/// </remarks>
module AngouriMath.Terminal.Lib.OneShot

open System
open System.IO
open AngouriMath

/// The usage text, printed by <c>amcli help</c>.
let usage =
    """amcli                        opens the terminal
amcli eval EXPR              evaluates to a number, a boolean, or a + bi
amcli simp EXPR              simplifies
amcli fsimp EXPR             simplifies quickly: the normalisation, without the search
amcli diff VAR EXPR          differentiates with respect to VAR
amcli solve VAR STATEMENT    solves for VAR, one root per line when there are finitely many
amcli sub VAR VALUE EXPR     substitutes VALUE for VAR
amcli latex EXPR             writes EXPR as LaTeX
amcli info EXPR              its variables, derivatives, roots and stationary points
amcli mcp                    serves the library to an agent over MCP, on stdin and stdout
amcli help                   prints this

An argument written as _ is read from stdin, and so is one that is left out:
    echo "1 + x^2" | amcli diff x
"""

/// The names <c>run</c> answers. Anything else as a first argument is a usage error.
let commands = set [ "eval"; "simp"; "fsimp"; "diff"; "solve"; "sub"; "latex"; "info"; "mcp"; "help"; "-h"; "--help" ]

/// The arguments still to read, falling back to a line of stdin where one is missing or is <c>_</c>.
type private Arguments(given: string list, input: TextReader) =
    let mutable rest = given

    /// The next argument, or None where it is missing and stdin has ended.
    member _.Next() : string option =
        match rest with
        | head :: tail ->
            rest <- tail
            if head = "_" then Option.ofObj (input.ReadLine()) else Some head
        | [] -> Option.ofObj (input.ReadLine())

/// <summary>
/// A real number's sign, where <paramref name="value"/> evaluates to one; otherwise None.
/// </summary>
let private signOf (value: Entity) =
    match value.Evaled with
    | :? Entity.Number.Real as real -> Some (if real.IsZero then 0 elif real.IsNegative then -1 else 1)
    | _ -> None

/// <summary>Whether <paramref name="value"/> is a number off the real line.</summary>
let private isNotReal (value: Entity) =
    match value.Evaled with
    | :? Entity.Number.Real -> false
    | :? Entity.Number.Complex -> true
    | _ -> false

/// <summary>
/// What a stationary point is, by the second derivative test: with the Hessian's leading principal
/// minors <c>D1 ... Dn</c>, all positive is a minimum, <c>(-1)^k Dk</c> all positive a maximum, and
/// any other nonzero determinant a saddle point (Sylvester's criterion). A zero determinant decides
/// nothing, and a minor that is not a real number leaves the point unclassified.
/// </summary>
let private kindOfStationaryPoint (hessian: int -> int -> Entity) (size: int) =
    let minors =
        [ for k in 1 .. size ->
            if k = 1 then hessian 0 0
            else (MathS.Matrix(k, k, fun row column -> hessian row column)).Determinant |> Option.ofObj |> Option.defaultValue MathS.NaN ]
        |> List.map signOf
    if minors |> List.exists Option.isNone then "not classified: its second derivatives there are not real numbers"
    else
        let signs = minors |> List.map Option.get
        if List.last signs = 0 then "a degenerate point, which the second derivative test does not decide"
        elif signs |> List.forall (fun sign -> sign > 0) then "a minimum"
        elif signs |> List.mapi (fun k sign -> if k % 2 = 0 then -sign else sign) |> List.forall (fun sign -> sign > 0) then "a maximum"
        else "a saddle point"

/// <summary>
/// The lines <c>amcli info</c> prints: the variables, the derivative and the roots for each, and
/// the stationary points, each classified by the second derivative test.
/// </summary>
let private describe (expression: Entity) =
    let variables = expression.Vars |> Seq.toList
    let names = String.Join(", ", variables)
    [ yield $"variables: {names}"
      for v in variables do
          yield $"derivative with respect to {v}: {expression.Differentiate(v).Simplify().Stringize()}"
      for v in variables do
          yield $"roots for {v}: {expression.Equalizes(Entity.Number.Integer.Zero).Solve(v).Simplify().Stringize()}"
      match variables with
      | [] -> ()
      | [ v ] ->
          let first = expression.Differentiate(v).Simplify()
          let second = first.Differentiate(v).Simplify()
          match first.Equalizes(Entity.Number.Integer.Zero).Solve(v) with
          | :? Entity.Set.FiniteSet as points ->
              if points.Count = 0 then yield "stationary points: none"
              for point in points do
                  if isNotReal point then
                      yield $"stationary point {v} = {point.Stringize()}: not real"
                  else
                      let at (e: Entity) = e.Substitute(v, point)
                      let kind = kindOfStationaryPoint (fun _ _ -> at second) 1
                      yield $"stationary point {v} = {point.Stringize()}: {kind}, value {(at expression).Simplify().Stringize()}"
          | points -> yield $"stationary points: {points.Stringize()}"
      | _ ->
          let gradient = [ for v in variables -> expression.Differentiate(v).Simplify() ]
          match MathS.Equations(gradient).Solve(variables |> List.toArray) with
          | Null -> yield "stationary points: not found"
          | NonNull solutions ->
              for row in 0 .. solutions.RowCount - 1 do
                  let at (e: Entity) =
                      variables |> List.indexed |> List.fold (fun (acc: Entity) (i, v) -> acc.Substitute(v, solutions.[row, i])) e
                  let hessian i j = at (expression.Differentiate(variables.[i]).Differentiate(variables.[j]))
                  let point = String.Join(", ", [ for i in 0 .. variables.Length - 1 -> solutions.[row, i].Stringize() ])
                  if [ for i in 0 .. variables.Length - 1 -> solutions.[row, i] ] |> List.exists isNotReal then
                      yield $"stationary point ({names}) = ({point}): not real"
                  else
                      let kind = kindOfStationaryPoint hessian variables.Length
                      yield $"stationary point ({names}) = ({point}): {kind}, value {(at expression).Simplify().Stringize()}" ]

/// What a command printed, one line each.
let private answer (command: string) (next: unit -> string option) : Result<string list, string> =
    let expression () = next () |> Option.map MathS.FromString
    let variable () = next () |> Option.map MathS.Var
    let missing = Error $"amcli {command} needs more arguments. Run amcli help for what each command takes."
    match command with
    | "eval" ->
        match expression () with
        | Some e ->
            match e.Evaled with
            | :? Entity.Number.Rational as r -> Ok [ r.EDecimal.ToString() ]
            | value -> Ok [ value.Stringize() ]
        | None -> missing
    | "simp" ->
        match expression () with
        | Some e -> Ok [ e.Simplify().Stringize() ]
        | None -> missing
    | "fsimp" ->
        match expression () with
        | Some e -> Ok [ e.InnerSimplified.Stringize() ]
        | None -> missing
    | "diff" ->
        match variable () with
        | Some v ->
            match expression () with
            | Some e -> Ok [ e.Differentiate(v).Stringize() ]
            | None -> missing
        | None -> missing
    | "solve" ->
        match variable () with
        | Some v ->
            match expression () with
            | Some e ->
                match e.Solve(v) with
                | :? Entity.Set.FiniteSet as roots -> Ok [ for root in roots -> root.Stringize() ]
                | roots -> Ok [ roots.Stringize() ]
            | None -> missing
        | None -> missing
    | "sub" ->
        match variable () with
        | Some v ->
            match expression () with
            | Some value ->
                match expression () with
                | Some e -> Ok [ e.Substitute(v, value).Stringize() ]
                | None -> missing
            | None -> missing
        | None -> missing
    | "latex" ->
        match expression () with
        | Some e -> Ok [ e.Latexize() ]
        | None -> missing
    | "info" ->
        match expression () with
        | Some e -> Ok (describe e)
        | None -> missing
    | _ -> Ok (usage.TrimEnd().Split('\n') |> List.ofArray)

/// <summary>
/// Runs one command and returns the process's exit code: 0 for an answer, 1 when the input did
/// not parse or the library could not answer it, 2 for a command that does not exist or is
/// missing an argument.
/// </summary>
/// <param name="args">The command line after the program's own name, which must not be empty.</param>
let run (args: string list) (input: TextReader) (output: TextWriter) (error: TextWriter) : int =
    match args with
    // Not one answer but a session: the server reads requests until stdin ends.
    | "mcp" :: rest -> AngouriMath.Mcp.Server.Run(List.toArray rest, input, output, error)
    | command :: rest when commands.Contains command ->
        let arguments = Arguments(rest, input)
        try
            match answer command arguments.Next with
            | Ok lines ->
                for line in lines do output.WriteLine line
                0
            | Error message ->
                error.WriteLine message
                2
        with ex ->
            error.WriteLine ex.Message
            1
    | command :: _ ->
        error.WriteLine $"amcli has no command {command}. Run amcli help for the list, or amcli with no arguments for the terminal."
        2
    | [] ->
        error.WriteLine usage
        2
