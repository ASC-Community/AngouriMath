# Dimensional analysis

`MathS.Dimensions.Of` reads a formula against the physical dimension of each of its variables and
says whether it means anything: the dimension of the result, or the subexpression where two
dimensions are added, compared or passed to a function that only takes a pure number.
`MathS.Dimensions.DimensionlessGroups` gives the Buckingham π groups of a set of variables.

A dimension is a product of powers of the seven SI base dimensions, `L`, `M`, `T`, `I`, `Θ`, `N`
and `J`, with rational exponents, and is written as `Dimension.Length / Dimension.Time.Pow(2)` or
`Dimension.Create(1, 0, -2)`. It says what kind of quantity something is and nothing about its
unit: checking values in metres and feet is what a units library is for. What a units library
cannot do is look at `sqrt(2 g h)` and say whether it is a speed.

## Checking a formula

The dimensions are passed in a map from each variable to its dimension, not attached to the
variables, so the same expression can be read under different assignments and nothing about how
it prints, parses or simplifies changes. A body falling from a height `h`:

```cs
using System.Collections.Generic;
using AngouriMath.Core;

var dims = new Dictionary<Variable, Dimension>
{
    ["g"] = Dimension.Length / Dimension.Time.Pow(2),
    ["h"] = Dimension.Length,
    ["t"] = Dimension.Time,
};
Console.WriteLine(MathS.Dimensions.Of("sqrt(2 g h)", dims));
Console.WriteLine(MathS.Dimensions.Of("h - g t^2 / 2 = 0", dims));
Console.WriteLine(MathS.Dimensions.Of("h + g t", dims));
```

Output:

```
Consistent: L T^-1
Consistent: 1
Inconsistent at h + g * t: h has dimension L and g * t has dimension L T^-1, so they cannot be added
```

The answer is a `DimensionCheck`: its `Status` is one of three, its `Dimension` is the dimension of
the result where it is `Consistent`, and `At` and `Reason` say where and why it is not. A statement
such as an equation is a truth value, a pure number written `1`, once the sides of every
comparison in it agree.

**`Undetermined` is not a verdict.** A variable with no declared dimension, a dimensional quantity
raised to a symbolic power, and a node the check does not read are each reported as undetermined,
with the subexpression, rather than guessed at:

```cs
using System.Collections.Generic;
using AngouriMath.Core;

var dims = new Dictionary<Variable, Dimension>
{
    ["h"] = Dimension.Length,
    ["n"] = Dimension.Dimensionless,
};
Console.WriteLine(MathS.Dimensions.Of("h + w", dims));
Console.WriteLine(MathS.Dimensions.Of("h^n", dims));
```

Output:

```
Undetermined at w: no dimension is declared for w
Undetermined at h ^ n: h has dimension L and is raised to n, whose value is not known
```

## The rules

**Nothing is simplified first.** The check is about the formula as written: `sin(x) - sin(x)` with
`x` a length is inconsistent, although it simplifies to `0`.

| | |
|---|---|
| `a + b`, `a - b`, `a mod b`, `max(a, b)`, `min(a, b)`, `a = b`, `a < b` | `a` and `b` have one dimension. A literal `0` or `oo` fits any, so `v = 0` is meaningful |
| `a * b`, `a / b`, `dot(a, b)`, `cross(a, b)` | the dimensions multiply or divide |
| `a ^ p` | `p` is a pure number; where `a` has a dimension, `p` is rational: `L^pi` is no dimension |
| `abs`, `conj`, `re`, `im`, `norm(v, p)`, `trace`, `transpose`, an entry of a matrix | the dimension of the argument |
| `sign`, `arg` | a pure number, whatever the argument measures |
| `sin` and the other trigonometric functions, `exp` and `ln`, the hyperbolic functions, `erf`, `Ei`, `Si`, `Ci`, `li`, `gamma` and `n!`, `beta`, `zeta`, `polygamma`, `lambertw`, `floor`, `ceil`, `round`, the functions of number theory | each argument is a pure number, and so is the value |
| `and`, `or`, `not`, `iverson(P)`, divisibility and congruence | each operand is a truth value or a pure number, and so is the result |
| a matrix or vector | its entries share a dimension, which is the matrix's; a state vector `[x, v]` of mixed dimensions is undetermined |
| `det(A)` of an `n` by `n` matrix | the entries' dimension to the `n`th power, undetermined where `n` is not written |
| `derivative(f, x, n)`, `partialderivative(f, x, n)` | `f`'s dimension over `x`'s to the `n`th power |
| `integral(f, x, a, b)` | `f`'s dimension times `x`'s, with `a` and `b` of `x`'s dimension |
| `limit(f, x, a)` | `f`'s dimension, with `a` of `x`'s |
| `sum(f, k, a, b)`, `product(f, k, a, b)` | the index counts terms and is a pure number unless declared otherwise; a product of `b - a + 1` terms of dimension `D` is `D^(b - a + 1)`, undetermined where that count is not a number |
| `sum(f, x in S)`, `product(f, x in S)`, `max(f, x in S)`, `forall`, `exists`, `x in S` | a variable ranging over `S` takes the dimension `S`'s members share: `x in [0; l]` makes `x` a length. A number set, `RR` or `ZZ`, fits a declared variable of any dimension, since `l in RR` says a length is real, and makes an undeclared one a pure number |
| `f provided P`, a piecewise | `P` is a consistent statement, and the cases share a dimension |
| a set on its own, `lambda`, a function applied | undetermined: none of these is a quantity |

A node type the check has not been taught is undetermined too, never read as a pure number by
default; a test in the suite lists the verdict on every node type there is and fails on a new one
until someone decides what its dimension is.

## Buckingham π groups

A relation between `n` quantities whose dimension matrix has rank `r` can be written as a relation
between `n - r` dimensionless products of them. `DimensionlessGroups` gives a basis of those
products, each with the smallest whole powers, and checks each is a pure number before returning
it. Flow in a pipe, with density, speed, diameter and viscosity, has one, the reciprocal of the
Reynolds number:

```cs
using System.Collections.Generic;
using AngouriMath.Core;

var (L, M, T) = (Dimension.Length, Dimension.Mass, Dimension.Time);
var pipe = new Dictionary<Variable, Dimension>
{
    ["rho"] = M / L.Pow(3), ["v"] = L / T, ["d"] = L, ["mu"] = M / (L * T),
};
foreach (var group in MathS.Dimensions.DimensionlessGroups(new Variable[] { "rho", "v", "d", "mu" }, pipe))
    Console.WriteLine(group);
```

Output:

```
mu / (rho * v * d)
```

The order of the variables chooses the basis. The ones listed first recur across the groups, as a
physicist's "repeating variables" do, and each later one appears to the first power in exactly
one group.

## A worked example: water waves

The dispersion relation of waves on water of depth `h` is `ω² = g k tanh(k h)`, with `ω` the
angular frequency and `k` the wave number. In deep water `tanh(k h)` is close to `1`, and in
shallow water close to `k h`, which gives the shallow-water speed `ω / k = sqrt(g h)`. Writing
`ω = sqrt(g k h)` for that, a factor of `k` short, is a typical slip, and the check finds it inside
the phase of the wave it is used in:

```cs
using System.Collections.Generic;
using AngouriMath.Core;

var (L, T) = (Dimension.Length, Dimension.Time);
var dims = new Dictionary<Variable, Dimension>
{
    ["omega"] = Dimension.Dimensionless / T, ["g"] = L / T.Pow(2), ["k"] = Dimension.Dimensionless / L,
    ["h"] = L, ["A"] = L, ["x"] = L, ["t"] = T,
};
Console.WriteLine(MathS.Dimensions.Of("omega^2 = g k tanh(k h)", dims));
Console.WriteLine(MathS.Dimensions.Of("A sin(k x - omega t)", dims));
Console.WriteLine(MathS.Dimensions.Of("A sin(k x - sqrt(g k h) t)", dims));
foreach (var group in MathS.Dimensions.DimensionlessGroups(new Variable[] { "k", "g", "A", "h", "t" }, dims))
    Console.WriteLine(group);
```

Output:

```
Consistent: 1
Consistent: L
Inconsistent at k * x - sqrt(g * k * h) * t: k * x has dimension 1 and sqrt(g * k * h) * t has dimension L^(1/2), so they cannot be subtracted
k * A
k * h
k * g * t ^ 2
```

The three groups say that the surface of a wave started from rest depends on the amplitude, the
depth and the time only through `k A`, the steepness, `k h`, the depth in wavelengths, and
`g k t²`, the time in units of the deep-water period. A simulation tuned over those three is tuned
over every wave with the same three values.
