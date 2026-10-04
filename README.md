<p align="center">
  <a href="https://github.com/asc-community/AngouriMath">
    <img src="./.github/additional/readme/icon_white.png" alt="AngouriMath logo" width="200" height="200">
  </a>
</p>

<h2 align="center">AngouriMath</h2>

<p align="center">
  <i>Open-source cross-platform symbolic algebra library for </i><b>C# · F# · Jupyter · C++ (WIP)</b>
  <br>
  <a href="https://am.angouri.org/quickstart/"><b>Get started</b></a>
  <b>·</b>
  <a href="#exam"><b>Examples</b></a>
  <b>·</b>
  <a href="#contrib"><b>Contributions</b></a>
  <b>·</b>
  <a href="https://am.angouri.org/whatsnew/"><b>What's new</b></a>
  <b>·</b>
  <a href="https://am.angouri.org/"><b>Website</b></a>
  <br>
  <br>
  <a href="https://dotnetfiddle.net/u901sI"><img src="https://img.shields.io/static/v1?label=Fiddle&message=Try%21&color=purple&style=flat&logo=.NET&labelColor=646"></a>
  <a href="https://mybinder.org/v2/gh/asc-community/AngouriMathLab/try"><img src="https://img.shields.io/static/v1?label=Jupyter&message=Try%21&color=purple&style=flat&logo=Jupyter&labelColor=646"></a>
  <a href="https://discord.gg/YWJEX7a"><img alt="Join our Discord" src="https://img.shields.io/discord/642350046213439489?label=chat%20with%20us&logo=discord&style=flat&labelColor=474&logoColor=white&color=252"></a> 
  <a href="https://github.com/quozd/awesome-dotnet"><img src="https://awesome.re/mentioned-badge.svg"></a>
  
</p>

<details><summary><strong>Status board</strong></summary>


![Solution Build](https://github.com/asc-community/AngouriMath/actions/workflows/EverythingBuild.yml/badge.svg)

#### Builds and tests
|       | Kernel/C# | F# | Interactive | C++ |
|-------|-----------|----|-------------|-----|
| Build | ![C#/Kernel Build](https://github.com/asc-community/AngouriMath/actions/workflows/CSharpBuild.yml/badge.svg) | ![F# Build](https://github.com/asc-community/AngouriMath/actions/workflows/FSharpBuild.yml/badge.svg) | ![Interactive Build](https://github.com/asc-community/AngouriMath/actions/workflows/InteractiveBuild.yml/badge.svg) | ![C++ Build](https://github.com/asc-community/AngouriMath/actions/workflows/CPPBuild.yml/badge.svg) | 
| Test  | ![C# Test](https://github.com/asc-community/AngouriMath/actions/workflows/CSharpTest.yml/badge.svg) | ![F# Test](https://github.com/asc-community/AngouriMath/actions/workflows/FSharpTest.yml/badge.svg) | ![Interactive Test](https://github.com/asc-community/AngouriMath/actions/workflows/InteractiveTest.yml/badge.svg) | ![C++ Test](https://github.com/asc-community/AngouriMath/actions/workflows/CPPTest.yml/badge.svg) |

Note, that all tests and builds are tested for the following three operating systems: Windows, Ubuntu, Mac OS.

#### Coverage
| Kernel/C# | F# | C++ |
|-----------|----|-----|
| <a href="https://codecov.io/gh/asc-community/AngouriMath"><img src="https://codecov.io/gh/asc-community/AngouriMath/branch/master/graph/badge.svg?token=XaA0JGyNrS"/></a> | ??? | ??? |

#### Versions
|    | Prerelease | Stable | Downloads |
|:--:|:----------:|:------:|:---------:|
| Kernel/C# | <a href="https://www.nuget.org/packages/AngouriMath"><img alt="Nuget (with prereleases)" src="https://img.shields.io/nuget/vpre/AngouriMath?color=blue&label=NuGet&logo=nuget&style=flat-square"></a> | <a href="https://www.nuget.org/packages/AngouriMath"><img alt="Nuget" src="https://img.shields.io/nuget/v/AngouriMath?color=blue&label=NuGet&logo=nuget&style=flat-square"></a> | <a href="https://www.nuget.org/packages/AngouriMath"><img alt="Nuget" src="https://img.shields.io/nuget/dt/AngouriMath?color=darkblue&label=Downloads&style=flat-square"></a> |
| F# | <a href="https://www.nuget.org/packages/AngouriMath.FSharp"><img alt="Nuget (with prereleases)" src="https://img.shields.io/nuget/vpre/AngouriMath.FSharp?color=blue&label=NuGet&logo=nuget&style=flat-square"></a> | <a href="https://www.nuget.org/packages/AngouriMath.FSharp"><img alt="Nuget" src="https://img.shields.io/nuget/v/AngouriMath.FSharp?color=blue&label=NuGet&logo=nuget&style=flat-square"></a> | <a href="https://www.nuget.org/packages/AngouriMath.FSharp"><img alt="Nuget" src="https://img.shields.io/nuget/dt/AngouriMath.FSharp?color=darkblue&label=Downloads&style=flat-square"></a> |
| Interactive | <a href="https://www.nuget.org/packages/AngouriMath.Interactive"><img alt="Nuget (with prereleases)" src="https://img.shields.io/nuget/vpre/AngouriMath.Interactive?color=blue&label=NuGet&logo=nuget&style=flat-square"></a> | <a href="https://www.nuget.org/packages/AngouriMath.Interactive"><img alt="Nuget" src="https://img.shields.io/nuget/v/AngouriMath.Interactive?color=blue&label=NuGet&logo=nuget&style=flat-square"></a> | <a href="https://www.nuget.org/packages/AngouriMath.Interactive"><img alt="Nuget" src="https://img.shields.io/nuget/dt/AngouriMath.Interactive?color=darkblue&label=Downloads&style=flat-square"></a> |
| Experimental | <a href="https://www.nuget.org/packages/AngouriMath.Experimental"><img alt="Nuget (with prereleases)" src="https://img.shields.io/nuget/vpre/AngouriMath.Experimental?color=blue&label=NuGet&logo=nuget&style=flat-square"></a> | <a href="https://www.nuget.org/packages/AngouriMath.Experimental"><img alt="Nuget" src="https://img.shields.io/nuget/v/AngouriMath.Experimental?color=blue&label=NuGet&logo=nuget&style=flat-square"></a> | <a href="https://www.nuget.org/packages/AngouriMath.Experimental"><img alt="Nuget" src="https://img.shields.io/nuget/dt/AngouriMath.Experimental?color=darkblue&label=Downloads&style=flat-square"></a> |
| Terminal | <a href="https://www.nuget.org/packages/AngouriMath.Terminal"><img alt="Nuget (with prereleases)" src="https://img.shields.io/nuget/vpre/AngouriMath.Terminal?color=blue&label=NuGet&logo=nuget&style=flat-square"></a> | <a href="https://www.nuget.org/packages/AngouriMath.Terminal"><img alt="Nuget" src="https://img.shields.io/nuget/v/AngouriMath.Terminal?color=blue&label=NuGet&logo=nuget&style=flat-square"></a> | <a href="https://www.nuget.org/packages/AngouriMath.Terminal"><img alt="Nuget" src="https://img.shields.io/nuget/dt/AngouriMath.Terminal?color=darkblue&label=Downloads&style=flat-square"></a> |
| C++ | <img alt="GitHub release (latest SemVer including pre-releases)" src="https://img.shields.io/github/v/release/asc-community/AngouriMathLab?include_prereleases&label=GH%20Releases"> | WIP | WIP |

  
#### Other info
| Website | Stars | License |
|---------|-------|---------|
| <a href="https://am.angouri.org"><img alt="Website" src="https://img.shields.io/website?down_message=Down&label=Website&up_message=Up&url=https%3A%2F%2Fam.angouri.org&style=flat-square"></a> | <img alt="GitHub Repo stars" src="https://img.shields.io/github/stars/asc-community/AngouriMath?label=Stars&style=flat-square"> | <img alt="GitHub" src="https://img.shields.io/github/license/asc-community/AngouriMath?color=dark-green&label=License&style=flat-square"> |

<a href="CONTRIBUTING.md"><img alt="GitHub contributors" src="https://img.shields.io/github/contributors/asc-community/AngouriMath"></a>

If you want, you can add a badge to your repo:
```
[![Powered by AngouriMath](https://img.shields.io/badge/Powered%20by-AngouriMath-purple?style=flat-square&labelColor=646)](https://am.angouri.org)
```
[![Powered by AngouriMath](https://img.shields.io/badge/Powered%20by-AngouriMath-purple?style=flat-square&labelColor=646)](https://am.angouri.org)

</details>


## What is it about?

<a href="https://am.angouri.org">AngouriMath</a> is an open source symbolic algebra library.
That is, via AngouriMath, you can
automatically <a href="https://am.angouri.org/wiki/07.-Solvers.html">solve</a> 
equations, systems of equations,
<a href="https://am.angouri.org/wiki/05.-Differentiation.html">differentiate</a>,
<a href="https://am.angouri.org/wiki/01.-Expressions.html">parse</a> from string,
<a href="https://am.angouri.org/wiki/09.-Compilation.html">compile expressions</a>, work
with <a href="https://am.angouri.org/wiki/10.-Matrices.html">matrices</a>, find limits,
convert an expression to LaTeX, and <a href="https://am.angouri.org/wiki/">many other things</a>.

## How does it compare?

Measured rather than asserted:
<a href="./Sources/AngouriMath/Docs/Usage/Comparison.md">Docs/Usage/Comparison.md</a> puts the
library beside Math.NET Symbolics, Symbolism, SymPy 1.14.0 and Rubi's integration test-suite, and
reproduces the rows it loses as well as the ones it wins &mdash; 1,706 of 1,774 Rubi integrals
answered with no wrong answer, and Math.NET faster on all four operations both libraries can do.

## Where can be used?

<a href="#jupyter"><img src="./.github/additional/readme/side.PNG" align="right" width="25%" alt="AngouriMath now supports Jupyter integration"/></a>


The two areas of use:

<hr>

<details><summary>🧪<b> Research / Data Science</b> <sub>[click 🖱️]</sub></summary>
  
## <a name="research"></a>AngouriMath for research

As F#, great first-functional language, skyrocketing in the area of data analysis and interactive research, AngouriMath
offers a few ways to conveniently work with symbolic expressions.

### Notebooks

![gif](./.github/additional/readme/vscnotebook.gif)

Notebooks provide amazing experience working with function visualization, for functions
over one and two variables. With [dotnet/interactive](https://github.com/dotnet/interactive),
it can be used in Visual Studio Code notebooks as well as Jupyter notebooks. To install
the package, simply run this in the notebook:

```
#r "nuget:AngouriMath.Interactive,*-*"
```

### Terminal

[![gif](./Sources/Terminal/terminal.gif)](./Sources/Terminal)

As both a demonstration sample and a convenient tool, this repository includes
tool called AngouriMath.Terminal. It is a CLI-based program to interact with
AngouriMath (as opposed to API-based interaction, that is, consuming it as a lib).

[**[ Download ]**](https://github.com/asc-community/AngouriMathLab/releases)

Or build from sources:
```
git clone https://github.com/asc-community/AngouriMath
cd AngouriMath/Sources/Terminal/AngouriMath.Terminal
dotnet run -c release
```

See the online [Jupyter notebook](https://mybinder.org/v2/gh/asc-community/AngouriMathLab/try?filepath=HelloBook.AngouriMath.Interactive.ipynb) on
how to use the F# API of AngouriMath. Note, that the C# API is still available
via `open AngouriMath` command, then you can call the main library's methods.

See its [source folder](./Sources/Terminal).

### More

Read more about using AngouriMath for research on [the website](https://am.angouri.org/research).


  
</details>

<hr>

<details><summary>💻<b> Software Development</b> <sub>[click 🖱️]</sub></summary>
<br>
  
It is installed from [nuget](https://am.angouri.org/quickstart/#dotnet) for both C# and F# and can be used by Web/Desktop/Mobile development.

## Installing the library
1. Install AngouriMath from [NuGet](https://www.nuget.org/packages/AngouriMath).
2. Write the following code:
```cs
using AngouriMath; using System;
Entity expr = "x + sin(y)";
Console.WriteLine(expr);
```
3. Run.

<a href="https://am.angouri.org/quickstart/"><strong>More detailed Quick Start</strong></a>.

If you are new to AM, we suggest you checking out some samples instead of reading boring 
documentation. If you want to contribute, we would be happy to welcome you in our
community.

For any questions, feel free to contact us via <a href="https://discord.gg/YWJEX7a">Discord</a>.

Official website: [am.angouri.org](https://am.angouri.org/).

<a id="exam"></a>

## Examples

Expand any section to see. Examples with live shell are on the [website](https://am.angouri.org/).

<details><summary><strong>Computations</strong></summary>

Use as a simple calculator:
```cs
Entity expr = "1 + 2 * log(3, 9)";
Console.WriteLine(expr.EvalNumerical());
```
```math
5
```

```cs
Console.WriteLine("2 / 3 + sqrt(-16)".EvalNumerical());
>>> 2/3 + 4i
```
```math
\frac{2}{3} + 4\mathrm{i}
```

```cs
Console.WriteLine("(-2) ^ 3".EvalNumerical());
```
```math
-8
```

Build expressions with variables and substitute them:
```cs
Entity expr = "2x + sin(x) / sin(2 ^ x)";
var subs = expr.Substitute("x", 0.3m);
Console.WriteLine(subs);
```
```math
2 \cdot \frac{3}{10}+\frac{\sin\left(\frac{3}{10}\right)}{\sin\left(\sqrt[10]{2}^{3}\right)}
```

Simplify complicated expressions:
```cs
Console.WriteLine("2x + x + 3 + (4 a * a^6) / a^3 / 5".Simplify());
```
```math
3+\frac{4}{5} \cdot {a}^{4}+3 x \quad \text{for} \quad a \neq 0
```

```cs
var expr = "1/2 + sin(pi / 4) + (sin(3x)2 + cos(3x)2)";
Console.WriteLine(expr.Simplify());
```
```math
\frac{3}{2}+\frac{\sqrt{2}}{2}
```

Compiled functions work 15x+ faster
```cs
var x = MathS.Var("x");
var expr = MathS.Sin(x) + MathS.Sqrt(x) / (MathS.Sqrt(x) + MathS.Cos(x)) + MathS.Pow(x, 3);
var func = expr.Compile(x);
Console.WriteLine(func.Substitute(3));
```

```cs
var expr = "sin(x) + sqrt(x) / (sqrt(x) + cos(x)) + x3";
var compiled = expr.Compile("x");
Console.WriteLine(compiled.Substitute(4));
```

</details>

<details><summary><strong>Algebra</strong></summary>

Start with boolean algebra:
```cs
Entity expr1 = "a and b or c";

// Those are the same
Entity expr3 = "a -> b";
Entity expr3 = "a implies b";
```

```cs
Entity expr = "a -> true";
Console.WriteLine(MathS.SolveBooleanTable(expr, "a"));
```

```
>>> Matrix[2 x 1]
>>> False
>>> True
```

Next, solve some equations:
```cs
Console.WriteLine("x^2 + x + a".SolveEquation("x"));
```
```math
\left\{ \frac{-1-\sqrt{1-4 a}}{2}, \frac{-1+\sqrt{1-4 a}}{2} \right\}
```

Under developing now and forever (always available)
```cs
Entity expr = "(sin(x)^2 - sin(x) + a)(b - x)((-3) * x + 2 + 3 * x ^ 2 + (x + (-3)) * x ^ 3)";
Console.WriteLine(expr.SolveEquation("x").Latexize());
```
```math
\left\{ \arcsin\left(\frac{1-\sqrt{1-4 a}}{2}\right)+2 \mathrm{\pi} n_{1}, \mathrm{\pi}-\arcsin\left(\frac{1-\sqrt{1-4 a}}{2}\right)+2 \mathrm{\pi} n_{1}, \arcsin\left(\frac{1+\sqrt{1-4 a}}{2}\right)+2 \mathrm{\pi} n_{1}, \mathrm{\pi}-\arcsin\left(\frac{1+\sqrt{1-4 a}}{2}\right)+2 \mathrm{\pi} n_{1}, b, 1, 2, \mathrm{i}, -\mathrm{i} \right\}
```

Try some inequalities:
```cs
Console.WriteLine("(x - 6)(x + 9) >= 0".Solve("x"));
```
```math
\left\{ 6, -9 \right\} \cup \left(-\infty , -9\right) \cup \left(6, \infty \right)
```

Systems of equations:
```cs
var system = MathS.Equations(
    "x^2 + y + a",
    "y - 0.1x + b"
);
Console.WriteLine(system);
var solutions = system.Solve("x", "y");
Console.WriteLine(solutions);
```
System:

```math
\begin{cases}{x}^{2}+y+a = 0\\y-\frac{1}{10} \cdot x+b = 0\end{cases}
```

Result:

```math
\begin{bmatrix}\frac{\frac{1}{10}-\sqrt{\frac{1}{100}+4 \left(-a+b\right)}}{-2} & -\left({\left(\frac{\frac{1}{10}-\sqrt{\frac{1}{100}+4 \left(-a+b\right)}}{-2}\right)}^{2}+a\right) \\ \frac{\frac{1}{10}+\sqrt{\frac{1}{100}+4 \left(-a+b\right)}}{-2} & -\left({\left(\frac{\frac{1}{10}+\sqrt{\frac{1}{100}+4 \left(-a+b\right)}}{-2}\right)}^{2}+a\right)\end{bmatrix}
```

```cs
var system = MathS.Equations(
    "cos(x2 + 1)^2 + 3y",
    "y * (-1) + 4cos(x2 + 1)"
);
Console.WriteLine(system.Latexize());
var solutions = system.Solve("x", "y");
Console.WriteLine(solutions);
```
```math
\begin{cases}{\cos\left({x}^{2}+1\right)}^{2}+3 y = 0\\y \left(-1\right)+4 \cos\left({x}^{2}+1\right) = 0\end{cases}
```
(solution matrix is too complicated to show)

</details>

<details><summary><strong>Calculus</strong></summary>

Find derivatives:
```cs
Entity func = "x^2 + ln(cos(x) + 3) + 4x";
Entity derivative = func.Differentiate("x");
Console.WriteLine(derivative.Simplify());
```
```math
2 x-\frac{\sin\left(x\right)}{\cos\left(x\right)+3}+4
```

Find limits:
```cs
WriteLine("(a x^2 + b x) / (e x - h x^2 - 3)".Limit("x", "+oo").InnerSimplified);
```
```math
\frac{a}{-h}
```

Find integrals:
```cs
WriteLine("x^2 + a x".Integrate("x").InnerSimplified);
```
```math
\frac{{x}^{3}}{3}+a \frac{{x}^{2}}{2}+C
```

</details>

<details><summary><strong>Sets</strong></summary>

There are four types of sets:
```cs
WriteLine("{ 1, 2 }".Latexize());
WriteLine("[3; +oo)".Latexize());
WriteLine("RR".Latexize());
WriteLine("{ x : x^8 + a x < 0 }".Latexize());
```

```
>>> \left\{ 1, 2 \right\}
>>> \left[3, \infty \right)
>>> \mathbb{R}
>>> \left\{ x : {x}^{8}+a x < 0 \right\}
```
```math
\left\{ 1, 2 \right\} \qquad \left[3, \infty \right) \qquad \mathbb{R} \qquad \left\{ x : {x}^{8}+a x < 0 \right\}
```

And there operators:
```cs
WriteLine(@"A \/ B".Latexize());
WriteLine(@"A /\ B".Latexize());
WriteLine(@"A \ B".Latexize());
```

```
>>> A \cup B
>>> A \cap B
>>> A \setminus B
```
```math
A \cup B \qquad A \cap B \qquad A \setminus B
```

</details>

<details><summary><strong>Syntax</strong></summary>

You can build LaTeX with AngouriMath:
```cs
var expr = "x ^ y + sqrt(x) + integral(sqrt(x) / a, x) + derivative(sqrt(x) / a, x) + limit(sqrt(x) / a, x, +oo)";
Console.WriteLine(expr.Latexize());
>>> {x}^{y}+\sqrt{x}+\int \frac{\sqrt{x}}{a}\,\mathrm{d}x+\frac{\mathrm{d}}{\mathrm{d}x}\frac{\sqrt{x}}{a}+\lim_{x\to \infty } \frac{\sqrt{x}}{a}
```
```math
{x}^{y}+\sqrt{x}+\int \frac{\sqrt{x}}{a}\,\mathrm{d}x+\frac{\mathrm{d}}{\mathrm{d}x}\frac{\sqrt{x}}{a}+\lim_{x\to \infty } \frac{\sqrt{x}}{a}
```

You can parse `Entity` from string with
```cs
var expr = MathS.FromString("x + 2 + sqrt(x)");
Entity expr = "x + 2 + sqrt(x)";
```

A few convenient features: `x2` => `x^2`, `a x` => `a * x`, `(...)2` => `(...)^2`, `2(...)` => `2 * (...)`

</details>

<details><summary><strong>Compilation</strong></summary>

Now you can compile expressions with pritimives into native lambdas. They will be
at least as fast as if you wrote them in line in code, or faster if you have
same subexpressions in your expression.

```cs
Entity expr = "a and x > 3";
var func = expr.Compile<bool, double, bool>("a", "x");
WriteLine(func(true, 6));
WriteLine(func(false, 6));
WriteLine(func(true, 2));
WriteLine(func(false, 2));
```

Output:

```
True
False
False
False
```

</details>
  
<details><summary><strong>Multithreading</strong></summary>

You are guaranteed that all functions in AM run in one thread. It is also guaranteed that you can safely run multiple 
functions from AM in different threads, that is, all static variables and lazy properties are thread-safe.

There is also support of cancellation a task. However, to avoid injecting the cancellation token argument into all methods,
we use `AsyncLocal<T>` instead. That is why instead of passing your token to all methods what you need is to pass it once
to the `MathS.Multithreading.SetLocalCancellationToken(CancellationToken)` method.

There is a sample code demonstrating cancellation:

```cs
var cancellationTokenSource = new CancellationTokenSource();

// That goes instead of passing your token to methods
MathS.Multithreading.SetLocalCancellationToken(cancellationTokenSource.Token);

// Then you normally run your task
var currTask = Task.Run(() => InputText.Text.Solve("x"), cancellationTokenSource.Token);

try
{
    await currTask;
    LabelState.Text = currTask.Result.ToString();
}
catch (OperationCanceledException)
{
    LabelState.Text = "Operation canceled";
}
```

</details>

<details><summary><strong>F#</strong></summary>

<a href="https://www.nuget.org/packages/AngouriMath.FSharp">Download</a>

Not everything is supported directly from F#, so if something missing, you will need
to call the necessary methods from AngouriMath.

```fs
open Functions
open Operators
open Shortcuts

printfn "%O" (solutions "x" "x + 2 = 0")

printfn "%O" (simplified (solutions "x" "x^2 + 2 a x + a^2 = 0"))

printfn "%O" (``dy/dx`` "x^2 + a x")

printfn "%O" (integral "x" "x2 + e")

printfn "%O" (``lim x->0`` "sin(a x) / x")

printfn "%O" (latex "x / e + alpha + sqrt(x) + integral(y + 3, y, 1)")

```

</details>

<details><summary><strong>C++ (Experimental)</strong></summary>

At the moment, AngouriMath.CPP is in the experimental phase. See <a href="https://am.angouri.org/quickstart/#cpp">how to get AngouriMath for C++</a>.
```cpp
#include <AngouriMath.h>

int main()
{
    AngouriMath::Entity expr = "x y + 2";
    std::cout << expr.Differentiate("x");
}
```

</details>

  
</details>

<hr>

## <a name="contrib"></a>Contribution

AngouriMath is a free open-source project, there is no big company backing us. That is why we warmly welcome any contributors
to the project. Aside from volunteer donations, you can help developing the project: check the [guide for developers](./CONTRIBUTING.md).

## <a name="license"></a>License & citation

<a href="./LICENSE.md"><img alt="GitHub" src="https://img.shields.io/github/license/asc-community/AngouriMath?color=purple&label=License&style=flat-square"></a> [![DOI](https://zenodo.org/badge/224485143.svg)](https://zenodo.org/badge/latestdoi/224485143)

The project is open source, but can be used in closed commercial projects. There is no restriction on it
with the only requirement to keep the MIT license with all distributives of AngouriMath.

