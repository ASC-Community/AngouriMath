## AngouriMath Terminal

![](https://img.shields.io/nuget/vpre/AngouriMath?color=blue&label=NuGet&logo=nuget&style=flat-square)

This is desktop-targeted convenient CLI for AngouriMath. The idea is to provide convenient 
easy-to-use and lightweight terminal to run some basic calculations in it.

The interface language is F#. If you're not familiar with it, check out a [Jupyter notebook](https://mybinder.org/v2/gh/asc-community/AngouriMathLab/try?filepath=HelloBook.AngouriMath.Interactive.ipynb),
an interactive graphical notebook to working with AngouriMath.

![gif](https://raw.githubusercontent.com/asc-community/AngouriMath/terminal-as-global-tool/Sources/Terminal/terminal.gif)

## Installation

### For your desktop

Refer for instructions [**here**](https://am.angouri.org/quickstart/#terminal)

### As a .NET tool

It is available on [NuGet](https://www.nuget.org/packages/AngouriMath.Terminal), install it via
```
dotnet tool install --global AngouriMath.Terminal --version versionyoulike
```
To uninstall it, run
```
dotnet tool uninstall --global AngouriMath.Terminal
```

## One command, from a script

With a command after it, `amcli` answers once and exits, without opening the terminal or starting
F#, so it can sit in a pipe or a script:

```
$ amcli simp "sin(x)^2 + cos(x)^2"
1
$ amcli solve x "x^2 - 1 = 0"
1
-1
$ echo "1 + x^2" | amcli diff x
2 * x
```

The commands are `eval`, `simp`, `fsimp`, `diff`, `solve`, `sub`, `latex` and `info`, and
`amcli help` says what each takes. `info` describes an expression: its variables, the derivative
and the roots for each, and its stationary points, each classified by the second derivative
test. An argument written as `_`, or left out, is read from stdin. The exit code
is 0 for an answer, 1 when the input does not parse or cannot be answered, and 2 for a command
that does not exist or is missing an argument. These are the commands of the earlier standalone
command-line tool, with the same names and argument order.

## For an agent, over MCP

`amcli mcp` serves the library to an LLM agent over the Model Context Protocol, as JSON-RPC on
stdin and stdout. Register it with a client by naming `amcli` and the argument `mcp`:

```
claude mcp add angourimath -- amcli mcp
```

`amcli mcp --selftest` checks the install. [`Sources/MCP`](../MCP/README.md) says what the server
offers and how it is built.


## Earlier results

Every value a cell produces is kept: `it` is the last one, as in F# Interactive, `run` is all of
them oldest first, and `back n` counts from the end.

```
[...] 1 + 10
11

[...] 2 * 10
20

[...] back 1
20

[...] run.[0]
11
```

A cell with no value — a `let` — adds nothing, and a question about the history is itself a cell
with a value, so it is remembered too.

## LaTeX

`latex` gives an expression's LaTeX, and `render` shows it typeset in the browser, the way a plot
is shown: a page in the temporary directory, rendered by MathJax, with the LaTeX beneath it.

```
[...] latex (x / 2)
\frac{x}{2}

[...] render (x / 2)
Showing /tmp/angourimath-….html in the browser
```
