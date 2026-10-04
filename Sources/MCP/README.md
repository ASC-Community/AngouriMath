# AngouriMath over MCP

`amcli mcp` serves AngouriMath to an LLM agent over the [Model Context Protocol](https://modelcontextprotocol.io):
newline-delimited JSON-RPC 2.0 on stdin and stdout, protocol revision `2024-11-05`.

The point is verification rather than calculation. A model is confident it can do algebra, so a
tool that only offers to do it goes unused; a tool that checks the model's work gets called. So
every integral is checked by differentiating it back, a decline is reported as a decline rather
than dressed up as an answer, and every response echoes what was parsed.

## Install

`amcli` is the [terminal's](../Terminal/readme.md) dotnet tool:

```sh
dotnet tool install --global AngouriMath.Terminal
amcli mcp --selftest
```

The selftest checks a dozen identities, Euler's and Machin's among them, and that each library
defect the server's documents describe still reproduces. A defect that no longer reproduces is
reported as drift, since the documents then need editing.

Register it with a client by naming `amcli` and the argument `mcp`. For Claude Code:

```sh
claude mcp add angourimath --scope user -- amcli mcp
```

For Claude Desktop, in `claude_desktop_config.json`, and the same shape for any other client:

```json
{
  "mcpServers": {
    "angourimath": { "command": "amcli", "args": ["mcp"] }
  }
}
```

There is no network access, no file access, no configuration and no secret. Every tool is
annotated `readOnlyHint` and `openWorldHint: false`, so a client can approve calls without asking,
which matters: a maths tool that costs a click per call does not get used.

To check the protocol without a client:

```sh
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | amcli mcp
```

## Tools

| Tool | |
|---|---|
| `am_parse` | the parse as understood, its LaTeX, its free variables, and warnings |
| `am_simplify` | `alternatives: true` returns several candidate forms |
| `am_solve` | a list of constraints, combined with `and`, so `['x^2 = 4', 'x > 0']` narrows to `2` |
| `am_differentiate` | any order |
| `am_integrate` | always verified by differentiating back. With `from` and `to`, a declined integral still gets a numeric value for that interval, to the digits two step counts agree on |
| `am_limit` | one-sided with `side`; tells "no limit" from failure |
| `am_evaluate` | the exact form and a decimal, after optional substitutions |
| `am_verify_equal` | did your own algebra change the meaning? Names the difference |
| `am_check_steps` | checks a chain of working and names the step that broke |
| `am_truth_table` | the table and the satisfying assignments |
| `am_solve_system` | takes `x + y = 3` as well as `x + y - 3` |
| `am_domain_check` | domain conditions, structural hazards, and where it stops being real |
| `am_represent` | encodings: bases 2–36, Q-format fixed point, IEEE 754 bits, polar form |
| `am_matrix` | determinant, inverse, transpose, rank, RREF, trace, multiply, tensor product, power |
| `am_eigenvalues` | exact, via the characteristic polynomial; symbolic entries allowed |
| `am_compare_numeric` | worst and RMS error of an approximation over an interval, and where |
| `am_substitute` | plug in without evaluating: the shape, not a number |
| `am_expand`, `am_factor` | brackets out, or back into a product |
| `am_series` | Taylor or Maclaurin, to a given degree |
| `am_number_theory` | factorisation, totient, gcd, divisor count, primality |
| `am_classify` | which field a formula comes from, read off its symbols |
| `am_to_sympy` | a runnable SymPy program, for cross-checking |

Five **prompts** surface as slash commands in a client: `verify-derivation`, `check-formula`,
`derive-jacobian`, `analyse-approximation` and `solve-with-constraints`. A model rarely reaches for
a maths tool on its own; a prompt is the user reaching for it instead, and it costs nothing in
tool-list context.

Three **resources**: `angourimath://syntax`, the grammar and its two silent traps;
`angourimath://reliability`, when to trust a result and when to expect a decline; and
`angourimath://curiosities`, famous results with the call that reproduces each. Point a model at
the first two at the start of a session.

## What the design rests on

**Every response echoes the parse.** The parser is permissive in two silent ways, and silence is
the dangerous part: a valid parse of a different expression, with a plausible answer.

- A number after a name is an **exponent**: `x2` is x², and `2(g+e)3` is 2(g+e)³. A variable
  named `x2` or `t0` is silently raised to a power.
- An unknown name before a bracket is a **product**: `im(z)` is `im * z`.

Both raise a warning, and the `parsed` field always shows what was understood. Whether a name is a
function is asked of the parser itself, by parsing `name(x)`, so the warning cannot fall behind the
grammar.

**The status is explicit**: `solved`, `unchanged`, `declined`, `suspect`, `timeout`, `failed` or
`conflict`. `declined` means the library has no rule, whether it left the expression unevaluated or
raised `NotSufficientlySupportedException`. `unchanged` means no progress, not "already simplest".

**A printed `NaN` is screened.** An answer that exists has none in it.

**An approximate answer says how approximate it is.** The numeric fallback for a declined definite
integral, `Entity.DefiniteIntegral`, is a first-order rule that returns a hundred digits whatever
its accuracy. It is run at two step counts, and only the digits they agree on are reported.

**Every call has a budget and its own stack.** Each runs under the library's cancellation token on a
thread with a 64 MB stack, abandoned rather than killed on timeout, so neither a runaway search nor
a deep recursion takes the server down.

## Known limits

- **LaTeX is output only.** Convert `\frac{a}{b}` to `a/b` before calling.
- `am_verify_equal` decides on positive real points, then checks the negatives separately: for
  `sqrt(x^2)` against `x` it answers `equal: true` **with a note** that the two agree only on the
  positives. The note is part of the answer.
- `am_solve`'s `solutions[]` is tidied root by root and its raw `result` is not, so the two can
  read differently. Prefer `solutions[]`.
- A nonlinear system can come back with no solution when one exists.
- Requests are served one at a time. Settings scopes follow the call, so this is not needed for
  correctness; a stdio client sends one request at a time anyway.

## Changing it

```sh
dotnet test Sources/Tests/McpUnitTests
```

The tests drive the server as a client would. They include the selftest, which fails here on a
library change that fixes a documented defect, and a check that `angourimath://syntax` names every
function the grammar has. `.github/workflows/InteractiveTest.yml` runs them.

Each of these looks arbitrary and is not:

- **Nothing but JSON-RPC goes to the output.** Diagnostics go to the error stream; one stray line
  corrupts the stream, and the client reports the server as failed.
- **Every library call goes through `Guard.Run`.** It is what keeps a stack overflow or a hang
  inside one request.
- **Decline detection reads the raw result, before any `Simplify`.** An unevaluated `limit(...)`
  simplifies to `NaN`, which would turn an honest decline into what looks like a wrong answer.
- **Simplify before stripping a `provided` guard, never after.** The guard is what licenses the
  cancellation: strip `provided not a = 0` first and `a*(d*a - c*b)/a` no longer reduces.
- **A warning that fires on correct input is worse than none**, because it teaches the caller to
  ignore the channel. Check a new warning against the correct spelling as well as the broken one.

Each tool costs context and dilutes the descriptions a model routes on, so prefer a parameter on an
existing tool, then a prompt, and add a tool last. A tool's description is a routing prompt, not
documentation: say when to call it, and where a model would wrongly trust itself, say so.

This is an adapter, not a second computer algebra system. Mathematics belongs in the library, where
the terminal and the notebooks get it too, and four workarounds here wait for it:
eigenvalues by the characteristic polynomial ([#1676](https://github.com/asc-community/AngouriMath/issues/1676)),
a numeric comparison with a tolerance ([#1674](https://github.com/asc-community/AngouriMath/issues/1674)),
`a = b` rewritten to `a - b` for a system ([#1673](https://github.com/asc-community/AngouriMath/issues/1673)),
and the numeric definite integral run twice to find its good digits
([#1675](https://github.com/asc-community/AngouriMath/issues/1675)).
