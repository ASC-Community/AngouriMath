//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

module AngouriMath.Interactive.Tests.HostCompatibility

open System.Reflection
open Xunit

let private fsharpCoreReferencedBy (assembly : Assembly) =
    assembly.GetReferencedAssemblies()
    |> Array.find (fun name -> name.Name = "FSharp.Core")
    |> fun name -> name.Version

// A notebook host runs its own FSharp.Core and cannot give a package a newer one. Built against
// FSharp.Core 10.1 under a host carrying 10.0, the kernel extension hung a C# kernel as it loaded,
// and an F# kernel failed its first call into the wrappers unless the extension had loaded first:
// https://github.com/asc-community/AngouriMath/issues/2108
[<Fact>]
let ``The wrappers ask for no newer FSharp.Core than the F# kernel is built against`` () =
    let kernel = fsharpCoreReferencedBy typeof<Microsoft.DotNet.Interactive.FSharp.FSharpKernel>.Assembly
    for wrapper in [ typeof<AngouriMath.InteractiveExtension.KernelExtension>.Assembly
                     Assembly.Load "AngouriMath.FSharp" ] do
        let asked = fsharpCoreReferencedBy wrapper
        Assert.True(asked <= kernel, $"{wrapper.GetName().Name} asks for FSharp.Core {asked}, the F# kernel is built against {kernel}")
