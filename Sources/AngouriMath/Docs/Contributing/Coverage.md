# Coverage

Four legs of CI run tests, and they do not all measure the same thing. This says what each one
measures, so that a missing number is read as the boundary it is rather than as a leg nobody got
round to instrumenting.

| leg | workflow | what it runs | how coverage is collected | codecov flag |
|---|---|---|---|---|
| C# | `CSharpTest.yml` | `dotnet test Sources/Tests/UnitTests` | coverlet, both as `/p:CollectCoverage=true` and as the VSTest `XPlat Code Coverage` collector | `csharp` |
| F# | `FSharpTest.yml` | `dotnet test Sources/Tests/FSharpWrapperUnitTests` | VSTest `XPlat Code Coverage` collector | `fsharp` |
| Interactive | `InteractiveTest.yml` | `dotnet test` over the Interactive wrapper and the terminal | VSTest `XPlat Code Coverage` collector | `interactive` |
| C++ | `CPPTest.yml` | `ctest` over a native executable | `gcov`, reported by `gcovr` as Cobertura | `cpp` |

The first three all take the same route: `dotnet test` hosts managed assemblies, and a data
collector instruments them in that host. The fourth cannot, and the reason is worth writing down
because it decides what the `cpp` number means.

## The C++ leg does not carry managed code into CI

`AngouriMath.CPP.Exporting` sets `PublishAot`, and `CPPTest.yml` publishes it with
`-p:NativeLib=Shared -p:SelfContained=true`. What arrives in
`AngouriMath.CPP.Importing/out-x64` is one shared object and a symbol file; there is no
`AngouriMath.dll` and no `AngouriMath.CPP.Exporting.dll` in it. The tests are then a C++ binary
that ctest runs, not a test host.

So neither half of the mechanism the other three legs use is present. There is no `dotnet test` for
a collector to attach to, and there is no managed assembly in the deployed artefact for a collector
to instrument. Published Native AOT output is a native binary, and .NET documents the diagnostic
tooling that follows managed code into it as
[partial](https://learn.microsoft.com/dotnet/core/deploying/native-aot/diagnostics) — the managed
debugger does not attach, and managed heap analysis is unsupported.

That is a statement about this route, not a proof that no route exists. What it settles is that
copying a step from `CSharpTest.yml` into `CPPTest.yml` would upload an empty report under a flag
claiming the C++ leg was covered, which is worse than the flag being absent.

## What the `cpp` flag does cover

The C++ leg has C++ of its own, and until now nothing measured it:
`Sources/Wrappers/AngouriMath.CPP.Importing` is the handle cache, the error-code translation and
the exception boundary that every C++ caller passes through, and a defect there is a defect no
managed test can see. It is ordinary C++, so `gcov` covers it.

The Linux job in the matrix configures cmake with `--coverage -O0 -g`, runs the same ctest suite as
the other two, and reports with

```
gcovr --root <repo root> --filter '.*AngouriMath\.CPP\.Importing.*' --cobertura coverage-cpp.xml
```

The filter is the honest part. Without it the report is mostly vendored googletest, and it includes
`RunTests.cpp`, which is fully covered by construction because it is the thing being run.

**So `cpp` is coverage of the C++ wrapper, not of AngouriMath through the C++ wrapper.** A line of
`Simplify` exercised only from a C++ test appears in no report at all. Read the flag as answering
"how much of the wrapper do these tests reach", and read `csharp` for the kernel.

To reproduce it locally, publish the exporting project as `CPPTest.yml` does, then configure the
tests with the two cmake flags above and run `gcovr` from the build directory.

## How a report reaches Codecov

Codecov takes an upload without a token only from a fork's branch. From a branch of this
repository, `master` included, it answers `Token required because branch is protected`, and
`codecov-action` logs that as an error and finishes green unless `fail_ci_if_error` is set. No
leg had a token or set it, so the four legs collected coverage and Codecov kept none of it: the
newest `master` commit it holds is from 2022-11-28, and the newest pull request upload came from
a fork on 2026-08-04.

Each upload step now sets `use_oidc: true`, in a job granted `id-token: write`. The job asks
GitHub for a token that lasts for the run, so no secret is stored anywhere. On a fork's pull
request the action skips it, since GitHub gives a fork no id-token, and Codecov takes that upload
without one. `fail_ci_if_error` is set on pushes to `master` only, so a rejected upload turns
`master` red, and an outage at Codecov does not fail a pull request.

To check a run, read the upload step's log. `Upload queued for processing complete` is followed
by `Upload queued for processing failed: …` when Codecov refused it. To see what Codecov holds:

```
curl -s 'https://api.codecov.io/api/v2/github/asc-community/repos/AngouriMath/commits/?branch=master'
```
