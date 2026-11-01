# What ships in which package, and how to decide where the next thing goes

[#746](https://github.com/asc-community/AngouriMath/issues/746) item 78 asks for this file: which
capabilities belong in the kernel package, which ship separately, and what the dependency rules
between them are. It is asked *before* anything large lands, because a published package boundary is
close to immovable — moving a type out of `AngouriMath` is a breaking change for everyone who
referenced it.

Every number below was measured on this checkout, and the command that produced it is next to it.
Where something was not measured it says so rather than estimating. Run the commands again before
quoting a figure at a later commit: a report records a build, not a repository.

---

## 1. The five packages that ship

`.github/workflows/Nuget.yml` fires on `release: published`, takes the version from the tag, and has
exactly five pack-and-push steps. That workflow is the definition of "published"; a project's
settings only decide whether the step would succeed.

| package | project | TFMs | nupkg | payload |
|---|---|---|---|---|
| `AngouriMath` | `Sources/AngouriMath` | `netstandard2.0;net8.0;net10.0` | 1,726,251 B | `AngouriMath.dll` — 1,184,256 B (net8.0, net10.0), 1,175,552 B (netstandard2.0) — plus a ~1.26 MB XML doc per TFM |
| `AngouriMath.FSharp` | `Sources/Wrappers/AngouriMath.FSharp` | `netstandard2.0` | 75,600 B | `AngouriMath.FSharp.dll`, 52,736 B |
| `AngouriMath.UnitsNet` | `Sources/Wrappers/AngouriMath.UnitsNet` | `netstandard2.0` | 45,679 B | `AngouriMath.UnitsNet.dll`, 10,752 B; most of the nupkg is the icon |
| `AngouriMath.Interactive` | `Sources/Wrappers/AngouriMath.Interactive` | `netstandard2.1` | 94,674 B | the Jupyter kernel extension |
| `AngouriMath.Terminal` | `Sources/Terminal/AngouriMath.Terminal` | `net10.0`, `PackAsTool`, command `amcli` | **28,750,908 B** (79,915,066 B uncompressed, 192 entries, 186 of them under `tools/`) | a self-contained dotnet tool |

```
dotnet build Sources/AngouriMath/AngouriMath.csproj -c Release
unzip -l Sources/AngouriMath/bin/Release/AngouriMath.2.3.0.nupkg
unzip -p Sources/AngouriMath/bin/Release/AngouriMath.2.3.0.nupkg AngouriMath.nuspec
```

Declared dependencies, read out of each generated `.nuspec`:

| package | depends on |
|---|---|
| `AngouriMath` | `Antlr4.Runtime.Standard` 4.13.1, `GenericTensor` 1.0.4, `HonkSharp` 1.0.3, `PeterO.Numbers` 1.8.0 — and `System.Memory` 4.5.4 on the `netstandard2.0` leg only |
| `AngouriMath.FSharp` | `AngouriMath`, `FSharp.Core` |
| `AngouriMath.UnitsNet` | `AngouriMath`, `UnitsNet` 5.75.1 |
| `AngouriMath.Interactive` | `AngouriMath.FSharp`, `FSharp.Core`, `Microsoft.DotNet.Interactive`, `Microsoft.DotNet.Interactive.Formatting`, `Plotly.NET` |
| `AngouriMath.Terminal` | none declared; a tool carries its closure in `tools/`, which is why it is 16× the kernel |

So the dependency shape is a **chain, not a graph**: kernel ← FSharp ← Interactive ← Terminal, with
UnitsNet attached to the kernel at one point, beside FSharp.

### What is in the tree and does not ship

- **`Sources/Terminal/AngouriMath.Terminal.Lib`** — `IsPackable=false`, with the reason in the project file.
- **`Sources/Wrappers/AngouriMath.CPP.Exporting`** — **not published.** It has no `PackageId`, does not
  set `GeneratePackageOnBuild`, does not import `Package.Build.props`, and has no step in `Nuget.yml`.
  It is a NativeAOT export surface consumed by `AngouriMath.CPP.Importing`, which is C++ headers and
  a `CMakeLists.txt` rather than a .NET project at all.
- **`Sources/Analyzers/*`** — referenced by the kernel as
  `PrivateAssets="all" ReferenceOutputAssembly="false" OutputItemType="Analyzer"`, so they run at
  build time and are absent from the package: the kernel nupkg has 13 entries and no `analyzers/`
  folder. `Sources/Utils`, `Sources/Samples` and `Sources/Tests` are likewise build-only.
- **`Experimental`** is **not a package.** It is `MathS.ExperimentalFeatures`, a `public static class`
  in `Convenience/Experimental/MathS.Experimental.cs` — 191 lines, 12 public members in
  `Tests/UnitTests/Common/PublicApi.txt` — inside the kernel assembly, under the same pinned
  `AssemblyVersion`. §6 says what that costs.

---

## 2. What the kernel contains

```
git ls-files 'Sources/AngouriMath/*.cs' | wc -l          # 211
git ls-files 'Sources/AngouriMath/*.cs' | xargs cat | wc -l   # 53665
```

**211 files, 53,665 lines** — of which one file, `Docs/Contributing/AddingNode.cs` (56 lines), is
documentation that the SDK's default glob happens to compile. The engine is 210 files, 53,609 lines.

By subsystem, so that a caller who wants only `Simplify` can see what the assembly is made of:

| subsystem | files | lines |
|---|--:|--:|
| `Entity` model, domains, exceptions | 54 | 8,508 |
| `MathS` facade and extension methods | 9 | 9,032 |
| Parser (ANTLR-generated + driver) | 6 | 5,159 |
| Evaluation, substitution, tree analysis | 20 | 5,161 |
| Polynomial layer | 12 | 4,184 |
| Limits (Gruntz and the rule table) | 7 | 3,792 |
| Simplification rules and search | 15 | 3,291 |
| Transformation layer | 15 | 2,899 |
| — of which the pattern matcher | 3 | 986 |
| Solvers (equations, inequalities, sets, numeric) | 15 | 2,846 |
| Compilation into LINQ / FastExpression | 9 | 1,797 |
| Integration | 3 | 978 |
| Gröbner bases | 4 | 929 |
| LaTeX printer | 8 | 923 |
| `ToString` printer | 8 | 792 |
| SymPy export | 8 | 640 |
| Number theory, base conversion | 5 | 634 |
| Boolean minimisation and table solving | 3 | 589 |
| Differentiation | 1 | 488 |
| Quantum | 3 | 414 |
| Monoid algebra / semiring | 3 | 288 |
| Series, matrix operations | 2 | 265 |
| documentation compiled by accident | 1 | 56 |

Growth, measured at each release tag with the same counting rule:

| tag | files | lines |
|---|--:|--:|
| `v1.4.0` | 164 | 34,652 |
| `v2.0.0` | 196 | 46,445 |
| `v2.1.0` | 196 | 46,769 |
| `v2.2.0` | 208 | 50,925 |
| `v2.3.0` | 211 | 53,665 |

```
git ls-tree -r --name-only <tag> -- Sources/AngouriMath | grep '\.cs$'
```

**+47 files and +19,013 lines, or 55%, since `v1.4.0`.** Per subsystem, the arrivals are:

| subsystem | `v1.4.0` | `v2.0.0` | `v2.2.0` | `v2.3.0` |
|---|--:|--:|--:|--:|
| Gröbner | — | 929 | 929 | 929 |
| monoid algebra | — | 288 | 288 | 288 |
| quantum | — | 414 | 414 | 414 |
| transformation layer | — | 1,458 | 2,118 | 2,899 |
| polynomial layer | — | 1,188 | 4,184 | 4,184 |
| SymPy export (directory only) | 454 | 515 | 559 | 591 |

**SymPy export is not an arrival.** It has been in the kernel since before `v1.4.0` and grew by 30%;
what 2.3.0 changed is that the code it emits runs
([#985](https://github.com/asc-community/AngouriMath/issues/985)). §4 explains why it could not have
been anywhere else.

### The assembly, from the outside

```
typeof(AngouriMath.Entity).Assembly     # measured by reflection on the built net10.0 assembly
```

- 634 types, of which **158 are public**;
- **68 concrete `Entity` subclasses** and 10 abstract ones;
- **14 abstract members declared on `Entity`** — `<Clone>$` is the compiler's, and `Codomain`'s getter
  and setter are one property, so **12 distinct capabilities**: `Codomain`, `DefaultCodomain`,
  `InitDirectChildren`, `InnerSimplify`, `IntrinsicCondition`, `InvertNode`, `LatexizeNode`,
  `Priority`, `Replace`, `SortHashName`, `StringizeNode`, `ToSymPy`;
- 14 referenced assemblies, 4 of them third-party. (`System.Console` is on the list because the
  ANTLR-generated lexer and parser default their error streams to it.) This said 13 until
  `System.Text.Json` arrived with `Core/Serialization` — a framework assembly, so no consumer's
  restore changed, and nothing noticed for the same reason. `KernelDependenciesTest` is the gate §7
  asked for. It asserts the **third-party** set exactly, in both directions, and the framework ones
  only as a set nothing may exceed — which framework assemblies a build resolves depends on the
  target framework leg, and `netstandard` appears on the netstandard2.0 one and not on `net10.0`.
  A leg resolving fewer of them is not a packaging event; one pulling in something new is.

**Twelve capabilities × 68 node types is the reason the kernel is one assembly.** A capability written
as an abstract member of `Entity` cannot be in a different package than `Entity`, whatever anyone
decides. Three of the twelve are output formats and one is a solver detail.

---

## 3. What "the common case pays for nothing it does not use" actually costs

The standing condition is usually read as a size rule. Measured, it is not one.

**A publish that trims cannot separate `Simplify` from parsing at all.** Two self-contained
`linux-x64` apps were published with `PublishTrimmed=true, TrimMode=full` against the local
2.3.0 package: one that parses a string and prints it, one that parses and calls `Simplify`. The
trimmed `AngouriMath.dll` is **byte-identical** between them — same md5 — at **852,480 B**. A third
app exercising thirteen entry points (`Simplify`, `Solve`, `Integrate`, `Limit`, `Differentiate`,
`Latex`, `ToSympyCode`, `Compile(…).Call`, a system solve, `Determinant`, `Expand`,
`SolveBooleanTable`, `Factorize`) trims to **951,808 B**.

| | trimmed `AngouriMath.dll` | of untrimmed |
|---|--:|--:|
| untrimmed | 1,184,256 B | 100% |
| parse only | 852,480 B | 72.0% |
| parse + `Simplify` | 852,480 B — identical | 72.0% |
| thirteen entry points | 951,808 B | 80.4% |

**The whole spread between the narrowest and the broadest use of the library is 99,328 bytes — 8.4%
of the assembly.** Everything else is one connected component: parsing reaches evaluation, evaluation
reaches the rule sets, and the rule sets reach nearly all of it.

Whole publish directories, self-contained `linux-x64`: 84,410,202 B untrimmed against 25,883,816 B
trimmed — the runtime dominates either way.

**Native AOT works, and cleanly.** The thirteen-entry-point app publishes with `PublishAot=true` to a
single 4,787,704 B binary, prints output byte-identical to the untrimmed run, and emits **no `IL2xxx`
or `IL3xxx` warning at all**. That the analyser was running is not assumed: a control app calling
`MakeGenericType` in the same configuration emits `IL3050`.

**Startup does not scale with the assembly.** Minimum of ten runs, self-contained:

| app | wall clock |
|---|--:|
| hello world | 20 ms |
| parse + one `Simplify` | 164 ms |
| thirteen entry points | 234 ms |

In-process, the cost is first-call and not load:

| | first | second |
|---|--:|--:|
| parse `(x + 1)^2 - x^2` | 88.7–97.2 ms | 0.8–1.4 ms |
| `Simplify` it | 51.1–54.8 ms | 8.0–8.4 ms |

Peak working set: 25.3 MB for hello world, 56.5 MB for parse + `Simplify`. The kernel has no
`[ModuleInitializer]`, so nothing runs at assembly load; 166 of its 634 types carry a static
constructor, and each runs on first touch of that type rather than on load. The ~88 ms is therefore
JIT plus the tables on the path actually taken. **Adding an unreached subsystem to the kernel costs
bytes on disk and nothing at startup.**

### Not measured, and therefore not claimed

Restore and download time from nuget.org; the payload under Blazor WebAssembly; ReadyToRun; the
`netstandard2.0` leg under trimming — `PublishTrimmed` on a *project* reference to the kernel fails
with `NETSDK1124`, because the property flows into the `netstandard2.0` target. Item 79's smoke test
therefore has to consume the kernel as a **package**, or pin the target framework of the reference.

---

## 4. The rule

**A capability ships in the kernel unless one of four things is true.** Each is a property of the
code, so a pull request can be checked against it.

It ships **separately** if:

- **(D) it adds a dependency the kernel does not have** — a third-party package, a native asset, or a
  per-RID payload. Checkable: `Assembly.GetReferencedAssemblies()` against the recorded 13, or the
  `<dependencies>` group of the generated nuspec against the recorded 4.
- **(F) it raises the kernel's floor** — its minimum target framework. Checkable: `TargetFrameworks`
  in `AngouriMath.csproj`. A capability that needs a newer framework and is otherwise kernel work is
  excluded per-framework instead, the way `Core/Entity/GenericMath/**` already is.
- **(P) its payload is data rather than code** — a corpus, a table, a trained model — so package size
  stops tracking source size. Checkable: non-`lib/` content in the nupkg.
- **(C) it consumes the kernel rather than composing it** — a language wrapper, a notebook host, an
  executable. Checkable: it references `AngouriMath` and nothing in `AngouriMath` references it.

Otherwise it ships **in the kernel**, and that is a conclusion rather than a default:

1. **A capability written as a member of the `Entity` hierarchy cannot be anywhere else.** Eleven are,
   and 68 node types implement each.
2. **Managed IL in the kernel is measurably cheap** — §3. A split that moves only managed code buys
   at most a fraction of 8.4% of one assembly, and costs a build workflow, a test workflow, a test
   project, a `Nuget.yml` step and a version-matrix entry, permanently. §7 lists what each existing
   package actually carries, so the cost is known rather than guessed.

**So the standing condition is not a size rule, and reading it as one is what lets it drift.** What it
forbids, and what this file enforces, is paying in **dependencies, in startup, or in a slower default
path**. An addition that is large and inert is fine. An addition that is small and sits on
`Simplify`'s default path is not, and that is the fifth clause:

- **(B) Anything that changes what a default entry point does is measured on the popular use cases
  before it merges, and is reachable only by explicit request if it costs more than the threshold.**
  Checkable: [`WhatsNew/version_performance_control.md`](../WhatsNew/version_performance_control.md).

(B) is where the real risk lives. (D)–(P) decide packages; (B) decides whether the kernel stays fast.

---

## 5. The rule applied

### To what is in the kernel today

Coupling measured by grepping for each subsystem's type names across the kernel, excluding the
subsystem's own directory.

| capability | why it is where it is |
|---|---|
| `Entity` model, parser, evaluation, `InnerSimplify`, `ToString` | no clause fires; several are abstract members of `Entity` — **kernel by construction** |
| LaTeX printer | `private protected abstract string LatexizeNode()` on `Entity` — **kernel by construction** |
| SymPy export | `internal abstract string ToSymPy()` on `Entity`, overridden by every node — **kernel by construction**, and has been since before `v1.4.0` |
| simplification rules and the transformation layer | on the default path; 111 public members in the `AngouriMath.Core.Transformations` namespace — **kernel** |
| polynomial layer | six inbound edges from the engine, including `Simplificator`, `RewriteRules`, `Patterns` and `Evaluation.Continuous.Arithmetics` — **kernel** |
| Gröbner bases | one inbound edge, `Functions/Continuous/Solvers/EquationSolver.cs`, and **0 public members**. Reachable from `Solve` on a system, which is a measured popular use case — **kernel** |
| Boolean minimisation | reached from `Simplificator` — **kernel** |
| compilation into LINQ | `System.Linq.Expressions` is in the framework, so (D) does not fire; measured AOT-clean — **kernel** |
| quantum, and the monoid algebra it is the only consumer of | 702 lines, 0.5% of the assembly. Reached **only from `Convenience/MathS.cs`** — the facade — and by nothing in the engine. Its whole public surface is `MathS.Quantum` and its five methods. No clause fires, so the rule leaves it in the kernel; §6 says why that is the right answer despite the boundary being arguably wrong |
| `MathS.ExperimentalFeatures` | 191 lines, 12 public members, kernel. The one case where a boundary would buy something real — §6 |

### To what #746 says is coming

| capability | clause | side |
|---|---|---|
| **e-graph / equality saturation** | none — pure managed, no new dependency | **kernel**, and gated by **(B)**. Its cost is runtime memory, not bytes: the honest evaluation #746 item 51 asked for measured a graph reaching the 100,000-e-node ceiling and gigabytes on `(a + b) / (a * b)` once rules fire at every member of a class. A package boundary does not fix that. An explicit entry point that `Simplify` never enters by default does. (Those figures are from the item 51 evaluation, not re-measured here.) |
| **SMT-backed `Provided` discharge** | **(D)** — an SMT solver brings native, per-RID binaries | **separate package** |
| **planner / tactic layer** | none for the engine; **(P)** for its tactic corpus | **kernel** for the mechanism, under (B); **separate package** for any corpus or table it is trained or seeded from |
| **domain packages** (geometry, statistics, …) | — | **blocked, not decided.** A domain pack means new node types, and an external assembly cannot define one: `Entity`'s constructor is `protected`, but five of its eleven abstract members — `IntrinsicCondition`, `InvertNode`, `Priority`, `SortHashName`, `ToSymPy` — are `internal` or `private protected`. A subclass in another assembly fails to compile, with a `CS0534` naming each of those five among the members it cannot satisfy. Domain packages are behind making the node contract extensible ([#363](https://github.com/asc-community/AngouriMath/issues/363), [#552](https://github.com/asc-community/AngouriMath/issues/552)), not behind a packaging decision |
| **learned rewrite guidance** | **(D)** and **(P)** — an inference runtime plus weights | **separate package** |

---

## 6. What moving something would cost

Moving a type out of a published package is a breaking change for everyone who named it. That is a
cost to users, and it is a legitimate argument. It is not the same as the work being hard, which is
never an argument for the easier of two right answers.

| move | cost | verdict |
|---|---|---|
| **Gröbner out of the kernel** | none to users — 0 public members — but `Solve` calls it, so the kernel would depend on the new package and the split would not be one | **not a move.** It stays because it is reachable from a popular use case, not because moving is hard |
| **quantum + monoid algebra out** | `MathS.Quantum` and its five public methods move, so every caller of them has to change. Buys 702 lines, 0.5% of the assembly, and by §3 approximately nothing at run time | **leave it.** The boundary is arguably wrong — the engine does not use it, only the facade does — and moving it costs users a rename for a saving that was measured and is negligible. If quantum ever needs a dependency of its own, (D) fires and it moves regardless |
| **SymPy export or the LaTeX printer out** | they are abstract members of `Entity`. Moving means either a public, extensible node contract or a visitor over 68 node types | **not on size grounds.** Worth doing if and when domain packages are built, because the same change unblocks those; the payoff is extensibility, and the size saving (640 and 923 lines) is not the reason |
| **`MathS.ExperimentalFeatures` out** | 12 public members, one build workflow, one test workflow, one test project, one `Nuget.yml` step | **the one move that pays, and it is a maintainer's decision rather than an implementation.** Today "experimental" is a promise the packaging cannot keep: it sits in the same assembly under an `AssemblyVersion` pinned at `2.0.0.0` for all of 2.x, so it can neither break nor version independently. A separate package makes the word mean something |
| **`AngouriMath.Terminal`** | already separate, and at 28.7 MB against the kernel's 1.7 MB it is what the rule exists for | the rule retrodicts the split that exists, which is the test of a rule |

---

## 7. Dependency rules between packages

1. **The kernel depends on no project in this repository.** `AngouriMath.csproj` has exactly two
   `ProjectReference`s, both `PrivateAssets="all" ReferenceOutputAssembly="false"
   OutputItemType="Analyzer"`, so they are build-time only and the package carries no `analyzers/`
   folder.
2. **Dependencies form a chain, never a cycle and never a sideways edge:**
   `AngouriMath ← AngouriMath.FSharp ← AngouriMath.Interactive ← AngouriMath.Terminal.Lib ←
   AngouriMath.Terminal`. A new package attaches to this chain at one point, as
   `AngouriMath.UnitsNet` does at `AngouriMath`.
3. **A package's public surface may name types from packages below it and its own, never from above.**
4. **The kernel's third-party dependency set is a list of four**, and adding to it is a packaging
   decision taken deliberately, because it changes what every consumer restores.
5. **Every published package is built by CI, tested by CI against its own test project, and pushed by
   `Nuget.yml`.** That is the cost of a boundary, and it is worth knowing before adding one:

   | package | built by | tested by | test project |
   |---|---|---|---|
   | `AngouriMath` | `CSharpBuild`, `EverythingBuild` | `CSharpTest` | `UnitTests` — 188 files, 29,703 lines |
   | `AngouriMath.FSharp` | `FSharpBuild`, `EverythingBuild` | `FSharpTest` | `FSharpWrapperUnitTests` — 13, 549 |
   | `AngouriMath.Interactive` | `InteractiveBuild`, `EverythingBuild` | `InteractiveTest` | `InteractiveWrapperUnitTests` — 3, 168 |
   | `AngouriMath.Terminal` | `TerminalNightly` only | inside `InteractiveTest` | `TerminalUnitTests` — 2, 75 |

   The Terminal row is the one that does not fit: `EverythingBuild.yml` builds the kernel, the F#
   wrapper, the Interactive wrapper, the analyzers, the utils and seven samples, and does not build
   the Terminal. A published package that no pre-merge workflow builds is a gap, not a convention.
6. **A project that does not ship says so at the project**: `IsPackable=false`, or no `PackageId`, no
   `GeneratePackageOnBuild` and no import of `Package.Build.props`.

### What could enforce this today

**Nothing does.** There is no architecture test, no `NetArchTest` or equivalent, and no
`Directory.Packages.props`, so a pull request adding a `PackageReference` to the kernel passes every
gate. What the tree does have is the mechanism: `Sources/Analyzers/` already carries two custom
analyzers, so a structural rule can be made a build error here without new infrastructure.

Two gates, cheapest first:

- **Rule 4, as a unit test — done, as `KernelDependenciesTest`.** It asserts that
  `typeof(MathS).Assembly.GetReferencedAssemblies()` equals the recorded list —
  `Antlr4.Runtime.Standard`, `GenericTensor`, `HonkSharp`, `Numbers`, `System.Collections`,
  `System.Collections.Concurrent`, `System.Console`, `System.Linq`, `System.Linq.Expressions`,
  `System.Memory`, `System.Runtime`, `System.Runtime.Numerics`, `System.Text.Json`,
  `System.Threading` — `Numbers` being the assembly `PeterO.Numbers` ships, and the four
  non-`System` ones asserted separately because they are what a restore fetches. It fails on the
  commit that adds a dependency rather than on the release that ships it, and asserts in the other
  direction too, so a dependency that goes away is deleted from the list rather than left there
  asserting nothing.

  Writing it found that the list above was already a version behind — `System.Text.Json` was
  missing — and that the document elsewhere said 13 where the count was 14. That is the whole
  argument for the gate: the drift had already happened, harmlessly, and unremarked.

  **It sees one target framework**, the one `UnitTests` builds. The `netstandard2.0` leg carries
  `System.Memory` as a package rather than a framework reference and is checked by nothing; that is
  a real gap and its own piece of work, not a reason to loosen this one.
- **Rules 2 and 3, from the packed nuspecs.** Assert each published package's `<dependencies>` group
  against the chain. This needs `dotnet pack` in CI, which only `Nuget.yml` does today.

Item 79's trimming and NativeAOT smoke test is the gate for §3, and §3 records the one thing that
would otherwise be found the hard way: it must reference the kernel as a package, or `NETSDK1124`
fires on the `netstandard2.0` leg.
