open System
open AngouriMath.Terminal.Lib.FSharpInteractive
open AngouriMath.Terminal.Lib.PreRunCode
open AngouriMath.Terminal.Lib.Consts
open UserInterface
open Spectre.Console
open AngouriMath.Terminal.Lib.AssemblyLoadBuilder
open AngouriMath.Terminal.Lib


// A command on the command line answers once and exits. This comes before anything below touches
// the console, which a pipe does not have.
match Environment.GetCommandLineArgs() |> List.ofArray |> List.tail with
| [] -> ()
// The protocol is UTF-8 on the pipe whatever code page the console has, so the server is given
// the standard streams as bytes rather than Console.In and Console.Out, which follow the code page.
| "mcp" :: _ as args ->
    let input = new IO.StreamReader(Console.OpenStandardInput(), Text.UTF8Encoding(false))
    let output = new IO.StreamWriter(Console.OpenStandardOutput(), Text.UTF8Encoding(false), AutoFlush = true)
    exit (OneShot.run args input output Console.Error)
| args -> exit (OneShot.run args Console.In Console.Out Console.Error)

// All other platforms do not support setting custom window width
if System.OperatingSystem.IsWindows() then
    Console.WindowHeight <- Math.Min(50, Console.LargestWindowHeight)
    Console.WindowWidth <- Math.Min(150, Console.LargestWindowWidth)


let lineEditor = getLineEditor AnsiConsole.Console

let rec readAndRespond kernel =
    printf "\n"
    match readLine lineEditor |> nonNull |> execute kernel with
    | PlainTextSuccess text ->
        writeLine AnsiConsole.Console text
    | LatexSuccess (_, text) ->
        writeLine AnsiConsole.Console text
    | Error message ->
        writeLineError AnsiConsole.Console message
    | _ -> ()

    readAndRespond kernel

let handleErrors errors =
    let concat = String.concat "\n"
    printfn $"Errors: {concat errors}"
    printfn $"Report about it to the official repo. The terminal will be closed."
    Console.ReadLine() |> ignore

"\n\n" |> Console.Write

FigletText("AngouriMath", Justification = Justify.Center, Color = Color.Pink1)
|> AnsiConsole.Console.Write

Markup($@"
Hi! Type `help ()` to get more info.
", Justification = Justify.Center)
|> AnsiConsole.Console.Write

printf "Starting the kernel..."

match createKernel () with
| Result.Error reasons -> handleErrors reasons
| Result.Ok kernel ->
    execute kernel "1 + 1" |> ignore  // warm up
    match enableAngouriMath kernel with
    | Error msg -> handleErrors [ msg ]
    | _ -> 
        printfn " loaded."
        readAndRespond kernel