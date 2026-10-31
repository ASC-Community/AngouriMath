//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;

namespace AngouriMath.Functions
{
    using static Entity;
    using static Entity.Number;
    using TrigTable = List<(EDecimal arg, Entity res)>;
    internal static class TrigonometryTableValues
    {
        private static bool TryPulling(TrigTable table, Complex arg,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Entity? res)
        {
            if (!(arg is Real { EDecimal: var dArg }))
            {
                res = null;
                return false;
            }
            // arg in [0; 2pi]
            var twoPi = Number.CtxMultiply(2, MathS.DecimalConst.pi);
            dArg = Number.CtxMod(
                Number.CtxAdd(           // (
                    Number.CtxMod(dArg, twoPi)         //     dArg % 2pi
                    ,                          //   +
                    twoPi                    //   2pi
                    )                          // )
                ,                              // %
                twoPi                    // 2pi
                );

            int begin = 0;
            int end = table.Count - 1;
            while (end - begin > 1)
            {
                var mid = (end + begin) / 2;
                if (table[mid].arg.GreaterThan(dArg))
                    begin = mid;
                else
                    end = mid;
                if (end >= table.Count)
                {
                    res = null;
                    return false;
                }
            }

            for (var j = begin; j <= end; j++)
            {
                if (Number.IsZero(table[j].arg - dArg) && !IsWrittenThroughTheComplexNumbers(table[j].res))
                {
                    res = table[j].res;
                    return true;
                }
            }
            res = null;
            return false;
        }

        /// <summary>
        /// Whether a value is written through numbers that are not real: <c>i</c>, or a square root
        /// of a negative number. Every angle here is real, so such a value is a real number whose
        /// radical form needs the complex numbers, casus irreducibilis, and the function of its angle
        /// is the shorter answer: <c>cos(2pi/7)</c> is
        /// <c>(-1 + cbrt((7 + 21 sqrt(-3))/2) + cbrt((7 - 21 sqrt(-3))/2))/6</c>, and no real
        /// radical form exists. <c>sin(pi/7)</c> simplified to the half-angle of it, many times
        /// longer than the input. Such an entry is passed over rather than removed, so the closed form
        /// stays on record. https://github.com/asc-community/AngouriMath/issues/2143
        /// </summary>
        private static bool IsWrittenThroughTheComplexNumbers(Entity value)
            => value.Nodes.Any(node => node is Complex and not Real
                || node is Powf(Real { IsNegative: true }, Rational { ERational.Denominator.IsEven: true }));

        /// <summary>
        /// Every angle in the tables below is written <c>2pi/n</c>, so they name nothing at
        /// all between <c>2pi/5</c> and <c>2pi/3</c> -- the whole lower half of the circle
        /// is missing, and most of the second quadrant with it. The identities each Pull
        /// tries reach some of what is missing and not the rest, which is why
        /// <c>sin(2pi/3)</c> was answered exactly while its neighbour <c>sin(5pi/3)</c>, the
        /// same value negated, was left as written. Turning the argument by half a circle
        /// and taking the sign back is what the tables are short of. It is applied once,
        /// around the lookups rather than inside them, so it cannot turn back and loop.
        /// https://github.com/asc-community/AngouriMath/issues/743
        /// </summary>
        private static Complex HalfTurn(Complex arg) => arg - (Real)MathS.DecimalConst.pi;

        /// <summary>
        /// Tries a lookup on the argument and then on the argument turned by half a circle,
        /// taking the sign back over the turn if <paramref name="oddOverHalfTurn"/>. Sine
        /// and cosine both change sign; the tangent's period *is* half a circle, so it does
        /// not.
        /// </summary>
        private static bool OrHalfTurn(System.Func<Complex, (bool Found, Entity? Value)> lookup,
            Complex arg, bool oddOverHalfTurn,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Entity? res)
        {
            if (lookup(arg) is (true, { } here))
            {
                res = here;
                return true;
            }
            if (lookup(HalfTurn(arg)) is (true, { } turned))
            {
                // Folded here rather than left to whoever receives it. The negation is built
                // above a value that is already in its final form, so nothing downstream is
                // obliged to look at it again -- and where the answer is wrapped rather than
                // rebuilt, nothing does: cos(0 ^ y) came back as `-(-1) provided ...` and
                // needed a second InnerSimplified to become `1 provided ...`, which is an
                // idempotence failure rather than a cosmetic one.
                // https://github.com/asc-community/AngouriMath/issues/930
                res = oddOverHalfTurn ? (-turned).InnerSimplified : turned;
                return true;
            }
            res = null;
            return false;
        }

        /// <summary>Sine read off the table itself, which is where the exact values are.</summary>
        private static (bool, Entity?) SinFromTable(Complex arg)
        {
            if (TryPulling(TableSin, arg, out var res))
                return (true, res);
            // sin(x) = sin(pi - x)
            if (TryPulling(TableSin, (Real)MathS.DecimalConst.pi - arg, out res))
                return (true, res);
            return (false, null);
        }

        /// <summary>
        /// Sine built out of the cosine of the doubled angle. Tried only after every table
        /// reading has been, because what it builds is a nested radical where the table has
        /// a flat one: at 6pi/5 it gives sqrt((1 + (sqrt(5) - 1)/4) / 2) for a value the
        /// table names as (sqrt(5) + 1)/4, and the roots of x^5 = 1 then come back written
        /// two different ways.
        /// </summary>
        private static (bool, Entity?) SinFromDoubledAngle(Complex arg)
        {
            // sin(x) = +-sqrt((1 - cos(2x)) / 2)
            if (!TryPulling(TableCos, arg * 2, out var res))
                return (false, null);
            res = MathS.Sqrt((1 - res) / 2);
            if (Number.Sin(arg) is Real real && real < 0)
                res *= -1;
            return (true, res);
        }

        internal static bool PullSin(Complex arg,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Entity? res)
            => OrHalfTurn(SinFromTable, arg, oddOverHalfTurn: true, out res)
            || OrHalfTurn(SinFromDoubledAngle, arg, oddOverHalfTurn: true, out res);

        private static (bool, Entity?) CosFromTable(Complex arg)
        {
            if (TryPulling(TableCos, arg, out var res))
                return (true, res);
            // cos(x) = cos(-x)
            if (TryPulling(TableCos, -1 * arg, out res))
                return (true, res);
            return (false, null);
        }

        private static (bool, Entity?) CosFromDoubledAngle(Complex arg)
        {
            // cos(x) = +-sqrt((1 + cos(2x)) / 2)
            if (!TryPulling(TableCos, arg * 2, out var res))
                return (false, null);
            res = MathS.Sqrt((1 + res) / 2);
            if (Number.Cos(arg) is Real real && real < 0)
                res *= -1;
            return (true, res);
        }

        internal static bool PullCos(Complex arg,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Entity? res)
            => OrHalfTurn(CosFromTable, arg, oddOverHalfTurn: true, out res)
            || OrHalfTurn(CosFromDoubledAngle, arg, oddOverHalfTurn: true, out res);

        private static (bool, Entity?) TanFromTable(Complex arg)
        {
            if (TryPulling(TableTan, arg, out var res))
                return (true, res);
            // tan(x) = -tan(pi - x)
            if (TryPulling(TableTan, (Real)MathS.DecimalConst.pi - arg, out res))
                return (true, res * -1);
            return (false, null);
        }

        private static (bool, Entity?) TanFromDoubledAngle(Complex arg)
        {
            // tan(x) = sqrt((1 - cos(2x)) / (1 + cos(2x)))
            if (!TryPulling(TableCos, arg * 2, out var res))
                return (false, null);
            return (true, MathS.Sqrt((1 - res) / (1 + res)));
        }

        internal static bool PullTan(Complex arg,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Entity? res)
            => OrHalfTurn(TanFromTable, arg, oddOverHalfTurn: false, out res)
            || OrHalfTurn(TanFromDoubledAngle, arg, oddOverHalfTurn: false, out res);

        private static Entity Sqrt(Entity a) => MathS.Pow(a, f1_2);
        private static Entity Cbrt(Entity a) => MathS.Pow(a, f1_3);
        [ConstantField] private static readonly Entity f1_2 = Rational.Create(1, 2);
        [ConstantField] private static readonly Entity f1_3 = Rational.Create(1, 3);
        [ConstantField] private static readonly Entity f1_4 = Rational.Create(1, 4);
        [ConstantField] private static readonly Entity f1_5 = Rational.Create(1, 5);
        [ConstantField] private static readonly Entity f1_6 = Rational.Create(1, 6);
        [ConstantField] private static readonly Entity f1_8 = Rational.Create(1, 8);
        [ConstantField] private static readonly Entity f1_16 = Rational.Create(1, 16);
        [ConstantField] private static readonly Entity i = MathS.i;
        private static EDecimal TwoPiOver(int a)
            => Number.CtxDivide(Number.CtxMultiply(2, MathS.DecimalConst.pi), a);

        /// <summary>
        /// Credit: https://en.wikipedia.org/wiki/Trigonometric_constants_expressed_in_real_radicals#List_of_trigonometric_constants_of_2%CF%80/n
        /// Although some formulas have been changed because they are wrong on Wikipedia
        /// </summary>
        // TODO: Some values here (e.g. sin(2pi/7)) are not present on Wikipedia. Needs additional reference sources.
        [ConstantField] private static readonly TrigTable TableSin = new()
        {
            (TwoPiOver(1), 0),
            (TwoPiOver(2), 0),
            (TwoPiOver(3), f1_2 * Sqrt(3)),
            (TwoPiOver(4), 1),
            (TwoPiOver(5), f1_4 * Sqrt(10 + 2 * Sqrt(5))),
            (TwoPiOver(6), f1_2 * Sqrt(3)),
            (TwoPiOver(7), Sqrt(1 - MathS.Pow(f1_6 * (-1 + Cbrt((7 + 21 * Sqrt(-3)) / 2) + Cbrt((7 - 21 * Sqrt(-3)) / 2)), 2))),
            (TwoPiOver(8), f1_2 * Sqrt(2)),
            // TODO: Why was this removed but not other TwoPiOver(9) values?
            // (TwoPiOver(9), i / 2 * (Cbrt((-1 - Sqrt(-3)) / 2) - Cbrt((-1 + Sqrt(-3)) / 2))),
            (TwoPiOver(10), f1_4 * Sqrt(10 - 2 * Sqrt(5))),
            (TwoPiOver(12), f1_2),
            // (TwoPiOver(14), f1_24 * Sqrt(3 * (112 - Cbrt(14336 + Sqrt(-5549064193)) - Cbrt(14336 - Sqrt(-5549064193))))),
            // Incorrect simplification! sin(2pi/14) = 0.433883739117558120475768332848358754609990727787459876444...
            //                  Above simplification = 0.433883739105630062845060102366441172904921259133054243740...
            (TwoPiOver(15), f1_8 * (Sqrt(15) + Sqrt(3) - Sqrt(10 - 2 * Sqrt(5)))),
            (TwoPiOver(16), f1_2 * Sqrt(2 - Sqrt(2))),
            (TwoPiOver(17), f1_4 * Sqrt(8 - Sqrt(2 * (
                                                15 + Sqrt(17) + Sqrt(34 - 2 * Sqrt(17)) - 2 * Sqrt(
                                                    17 + 3 * Sqrt(17) - Sqrt(170 + 38 * Sqrt(17))
                                                    )
                                                )))),
            (TwoPiOver(18), i * f1_4 * (Cbrt(4 - 4 * Sqrt(-3)) - Cbrt(4 + 4 * Sqrt(-3)))),
            (TwoPiOver(20), f1_4 * (Sqrt(5) - 1)),
            (TwoPiOver(24), f1_4 * (Sqrt(6) - Sqrt(2)))
        };

        [ConstantField] private static readonly TrigTable TableCos = new()
        {
            (TwoPiOver(1), 1),
            (TwoPiOver(2), -1),
            (TwoPiOver(3), -f1_2),
            (TwoPiOver(4), 0),
            (TwoPiOver(5), f1_4 * (Sqrt(5) - 1)),
            (TwoPiOver(6), f1_2),
            (TwoPiOver(7), f1_6 * (-1 + Cbrt((7 + 21 * Sqrt(-3)) / 2) + Cbrt((7 - 21 * Sqrt(-3)) / 2))),
            (TwoPiOver(8), f1_2 * Sqrt(2)),
            (TwoPiOver(9), f1_2 * (Cbrt((-1 + Sqrt(-3)) / 2) + Cbrt((-1 - Sqrt(-3)) / 2))),
            (TwoPiOver(10), f1_4 * (Sqrt(5) + 1)),
            (TwoPiOver(12), f1_2 * Sqrt(3)),
            // (TwoPiOver(14), f1_24 * Sqrt(3 * (80 + Cbrt(14336 + Sqrt(-5549064193)) + Cbrt(14336 - Sqrt(-5549064193))))),
            // Incorrect simplification! cos(2pi/14) = 0.900968867902419126236102319507445051165919162131857150053...
            //                  Above simplification = 0.900968867908163376042627598612270994870357666484835165523...
            (TwoPiOver(15), f1_8 * (1 + Sqrt(5) + Sqrt(30 - 6 * Sqrt(5)))),
            (TwoPiOver(16), f1_2 * Sqrt(2 + Sqrt(2))),
            (TwoPiOver(17), f1_16 * (-1 + Sqrt(17) + Sqrt(34 - 2 * Sqrt(17)) + 2 * Sqrt(
                17 + 3 * Sqrt(17) - Sqrt(34 - 2 * Sqrt(17)) - 2 * Sqrt(34 + 2 * Sqrt(17))
                ))),
            (TwoPiOver(18), f1_4 * (Cbrt(4 + 4 * Sqrt(-3)) + Cbrt(4 - 4 * Sqrt(-3)))),
            (TwoPiOver(20), f1_4 * Sqrt(10 + 2 * Sqrt(5))),
            (TwoPiOver(24), f1_4 * (Sqrt(6) + Sqrt(2)))
        };

        [ConstantField] private static readonly TrigTable TableTan = new()
        {
            (TwoPiOver(1), 0),
            (TwoPiOver(2), 0),
            (TwoPiOver(3), -Sqrt(3)),
            (TwoPiOver(4), Real.NaN), // tan (pi / 2) is undefined
            (TwoPiOver(5), Sqrt(5 + 2 * Sqrt(5))),
            (TwoPiOver(6), Sqrt(3)),
            (TwoPiOver(8), 1),
            (TwoPiOver(10), Sqrt(5 - 2 * Sqrt(5))),
            (TwoPiOver(12), f1_3 * Sqrt(3)),
            // (TwoPiOver(14), Sqrt(
            //     (112 - Cbrt(14336 + Sqrt(-5549064193)) - Cbrt(14336 - Sqrt(-5549064193)))
            //     /
            //     (80 + Cbrt(14336 + Sqrt(-5549064193)) + Cbrt(14336 - Sqrt(-5549064193)))
            //     )),
            // Incorrect simplification! tan(2pi/14) = 0.481574618807528644332162353056970575219078891752299935554...
            //                  Above simplification = 0.231914113463908048843246525445639553891057785614708159332...
            (TwoPiOver(15), f1_2 * (-3 * Sqrt(3) - Sqrt(15) + Sqrt(50 + 22 * Sqrt(5)))),
            (TwoPiOver(16), Sqrt(2) - 1),
            (TwoPiOver(20), f1_5 * Sqrt(25 - 10 * Sqrt(5))),
            (TwoPiOver(24), 2 - Sqrt(3))
        };
    }
}