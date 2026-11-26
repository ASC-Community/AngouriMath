# Shader source

`MathS.Shaders.Hlsl` and `MathS.Shaders.Glsl` print expressions as straight-line HLSL or GLSL:
one assignment to each output you name, with every subexpression that occurs more than once
computed once into a local. The use is to derive on the CPU what is tedious or error-prone to
derive by hand — an exact gradient, a Hessian, the closed form of a physical law — and to run
the result on the GPU.

Both take the outputs as a list of pairs, the target of each assignment and the expression it
gets, and return a `GeneratedSource`: its `Code` is the statements, or `null` where printing was
declined, in which case `At` is the subexpression that could not be printed and `Reason` says why.

```cs
using System.Collections.Generic;

Entity sphere = "sqrt(x^2 + y^2 + z^2) - 1";
var names = new Dictionary<Variable, string> { ["x"] = "p.x", ["y"] = "p.y", ["z"] = "p.z" };
var normal = new List<KeyValuePair<string, Entity>>
{
    new("n.x", sphere.Differentiate("x")),
    new("n.y", sphere.Differentiate("y")),
    new("n.z", sphere.Differentiate("z")),
};
Console.Write(MathS.Shaders.Hlsl(normal, names).Code);
```

Output:

```
float am0 = (0.5 * rsqrt((((p.x * p.x) + (p.y * p.y)) + (p.z * p.z))));
n.x = (am0 * (2.0 * p.x));
n.y = (am0 * (2.0 * p.y));
n.z = (am0 * (2.0 * p.z));
```

## A worked example: the normal of a smooth union

A signed distance field gives, at each point, the distance to a surface. Its gradient is the
surface normal, and a ray marcher usually estimates it by finite differences, four or six extra
evaluations of the field per pixel, with an error that is worst exactly at the creases a smooth
union produces. The exact gradient costs one evaluation's worth of shared subexpressions.

Two unit spheres, one at the origin and one at `x = 3/2`, joined by the exponential smooth
minimum with radius `k = 1/4`:

```
d(p) = -k ln(e^(-a/k) + e^(-b/k)),   a = |p| - 1,   b = |p - (3/2, 0, 0)| - 1
```

`Differentiate` gives the gradient, and `Simplify` makes it smaller before it is printed —
nothing is simplified by the printer itself:

```cs
using System.Collections.Generic;

Entity a = "sqrt(x^2 + y^2 + z^2) - 1";
Entity b = "sqrt((x - 3/2)^2 + y^2 + z^2) - 1";
Entity k = "1/4";
var d = -k * MathS.Ln(MathS.Pow(MathS.e, -a / k) + MathS.Pow(MathS.e, -b / k));

var names = new Dictionary<Variable, string> { ["x"] = "p.x", ["y"] = "p.y", ["z"] = "p.z" };
var gradient = new List<KeyValuePair<string, Entity>>
{
    new("n.x", d.Differentiate("x").Simplify()),
    new("n.y", d.Differentiate("y").Simplify()),
    new("n.z", d.Differentiate("z").Simplify()),
};
Console.Write(MathS.Shaders.Hlsl(gradient, names).Code);
```

Output:

```
float am0 = (p.y * p.y);
float am1 = (p.z * p.z);
float am2 = (((p.x * p.x) + am0) + am1);
float am3 = exp((-4.0 * (sqrt(am2) - 1.0)));
float am4 = rsqrt(am2);
float am5 = (p.x - 1.5);
float am6 = (((am5 * am5) + am0) + am1);
float am7 = exp((-4.0 * (sqrt(am6) - 1.0)));
float am8 = rsqrt(am6);
float am9 = ((am7 * am8) + (am3 * am4));
float am10 = (am7 + am3);
n.x = (-(0.25 * ((((-(4.0 * (p.x * am3))) * am4) + ((-(4.0 * (am7 * am5))) * am8)) / (am3 + am7))));
n.y = ((p.y * am9) / am10);
n.z = ((p.z * am9) / am10);
```

GLSL is the same code with GLSL's names for the intrinsics that differ — `inversesqrt` for
`rsqrt`, and no `sincos`, which HLSL uses for a sine and a cosine of one argument:

<!-- amcheck:continues -->
```cs
Console.Write(MathS.Shaders.Glsl(gradient, names).Code);
```

Output:

```
float am0 = (p.y * p.y);
float am1 = (p.z * p.z);
float am2 = (((p.x * p.x) + am0) + am1);
float am3 = exp((-4.0 * (sqrt(am2) - 1.0)));
float am4 = inversesqrt(am2);
float am5 = (p.x - 1.5);
float am6 = (((am5 * am5) + am0) + am1);
float am7 = exp((-4.0 * (sqrt(am6) - 1.0)));
float am8 = inversesqrt(am6);
float am9 = ((am7 * am8) + (am3 * am4));
float am10 = (am7 + am3);
n.x = (-(0.25 * ((((-(4.0 * (p.x * am3))) * am4) + ((-(4.0 * (am7 * am5))) * am8)) / (am3 + am7))));
n.y = ((p.y * am9) / am10);
n.z = ((p.z * am9) / am10);
```

Pasted into a shader, with `p` the point and `n` a `float3` (or `vec3`):

```hlsl
float3 SmoothUnionNormal(float3 p)
{
    float3 n;
    // ... the statements printed above ...
    return normalize(n);
}
```

The gradient of an exact distance field already has length 1; the smooth union is not exact,
so its gradient is normalised.

### Why `Simplify` here, and a condition `Differentiate` attaches

`Differentiate` keeps the condition under which its answer holds. The derivative of a logarithm
divides by its argument, so the unsimplified gradient comes back as
`... provided not e^(...) + e^(...) = 0`, and a shader has no way to say "no value here":

```cs
Entity d = "-1/4 ln(e^(-4 x) + e^(-4 y))";
var source = MathS.Shaders.Hlsl([new("g", d.Differentiate("x"))]);
Console.WriteLine(source.Succeeded);
Console.WriteLine(source.At is Providedf);
```

Output:

```
False
True
```

A sum of two exponentials is never zero, so here the condition cannot fail, and `Simplify`
drops it. Where it does not, and the condition is one you know holds, give the printer the
expression without it — `((Providedf)expr).Expression` — and only then.

## Naming the variables

A variable keeps its own name unless `names` maps it to another, so `x` can be read from
`p.x`, a swizzle, a member or a uniform. Every name has to be an identifier, or identifiers
joined by `.`; a name that is not one, such as `θ` for `theta`, is declined. The locals the printer declares are `am0`, `am1`, ..., and a variable mapped
to one of those names is declined as well. The output names are written as given.

```cs
using System.Collections.Generic;

var source = MathS.Shaders.Glsl(
    [new("fragColor.r", "sin(t) / 2 + 1/2")],
    new Dictionary<Variable, string> { ["t"] = "u_time" });
Console.Write(source.Code);
```

Output:

```
fragColor.r = ((sin(u_time) / 2.0) + 0.5);
```

## The contract

**It computes what `Compile<double, …>` computes**, the real-valued reading of the expression.
The tests hold the printer to that: the same printer writes C# against helpers named as HLSL's
intrinsics, the text is compiled, and its value is compared with `Compile<double, double, double>`
at 200 points per case.

**Where `Compile` has no value, the shader's is `NaN`, an infinity, or unspecified.** Three of
those cases are worth knowing, because a GPU does give them a value:

| at | `Compile<double>` | the shader |
|---|---|---|
| `a / b` with `b = 0` | `NaN` | `a / b`: ±inf under IEEE 754 |
| `x^(-2)`, `x^(-1/2)` at `x = 0` | `NaN` | `1.0 / (x * x)`, `rsqrt(x)`: +inf |
| an unsimplified `x^0` at `x = 0` | `NaN` | `1.0` |

Guarding every quotient would cost a branch per division, so the difference is documented here
rather than paid for in every shader:

```cs
using System.Collections.Generic;

Entity f = "1 / x";
Console.WriteLine(f.Compile<double, double>("x")(0));
Console.Write(MathS.Shaders.Hlsl([new("r", f)]).Code);
```

Output:

```
NaN
r = (1.0 / x);
```

**Powers.** A whole power is multiplied out up to the fourth, `x^3` as `(x * x * x)`; above that
it is `pow(abs(x), n)`, with the sign put back for an odd `n`, because `pow` of a negative base
is undefined in HLSL and GLSL while `x^5` is defined for every real `x`. A rational power with an
odd denominator below ten is the real root of a negative base, as `Compile` takes it: `x^(1/3)` is
printed `(float(sign(x)) * pow(abs(x), 0.3333333333333333))`, which is `-2` at `x = -8`. `x^(1/2)`
and `x^(-1/2)` are `sqrt` and `rsqrt` (`inversesqrt` in GLSL), and `e^x` is `exp(x)`. A power to
a variable exponent is `pow`, and so has no value at a negative base in a shader even where the
exponent happens to be whole.

**`mod` is floored**, as the library's `mod` is: the remainder takes the sign of the divisor,
`x - y * floor(x / y)`. HLSL's `fmod` and C's `%` truncate.

**Nothing is simplified.** What is printed is the expression you give, so `Simplify` it first if
you want a smaller one. The only rewrites are the ones `Compile` makes as well, and each keeps the
value: a scalar product, a determinant, a transpose, a trace or an entry is carried out with every
variable a number, so `dot(x * [1, 2], [3, 4])` is `11 x`, and a product of matrices written out
whose value is a number is that number; a membership in an interval
or a finite set is the comparisons it stands for; and `(b^v + c1) / (b^v + c2)`, which is how
`tanh` is spelled, is taken as `1 + (c1 - c2) / (b^v + c2)`, so that `tanh(x)` is 1 for a large
`x` rather than infinity over infinity — in single precision `e^(2x)` overflows from `x = 45` on.

**What is declined**, with the node named, rather than printed approximately:

- a number that is not real, such as `i`, and a constant other than `pi` and `e`;
- `provided`, since a shader cannot say there is no value, and a piecewise whose last case is
  not `true`, for the same reason;
- `round`, which takes a tie to the even neighbour where HLSL's and GLSL's `round` leave ties to
  the implementation;
- the functions with no intrinsic and no straight-line form: the factorial and the special
  functions, and `gcd` and `lcm`, which need a loop;
- sets and memberships in a set of numbers, and matrices: each output is one number, so assign a
  vector's entries one by one;
- a matrix operation with no value where every variable is a number, such as `dot` of two vectors
  of different lengths or the transpose of a variable;
- whatever `Compile` refuses — a derivative, an integral, a limit, a sum or a product left
  unevaluated, or a statement where a number is expected — with the message `Compile` gives.

```cs
Console.WriteLine(MathS.Shaders.Hlsl([new("r", "x! + 1")]));
```

Output:

```
Declined at x!: `x!` has no intrinsic in HLSL or GLSL and no straight-line form
```
