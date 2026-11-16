//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;

namespace AngouriMath.Functions
{
    internal static class TrigonometricAngleExpansion
    {
        // Exact, not 0.5 rounded to a rational: that rounding reads MathS.Settings.FloatToRationalIterCount,
        // which a solve sets to 0, and a first use inside one left 0.5 a decimal and failed the cast, so that
        // this type could not be initialized for the rest of the process.
        [ConstantField] internal static readonly Rational OneHalf = Rational.Create(1, 2);
        
        /// <summary>
        /// We are given angle thetaRat and sin(theta)
        /// where thetaRat = theta / pi.
        /// This function returns sin(theta / 2).
        /// </summary>
        /// <param name="thetaRat">
        /// An angle already divided by pi
        /// </param>
        /// <param name="sin2x">
        /// The value of the sine
        /// of the doubled angle
        /// </param>
        /// <returns>
        /// The value of sine of half of the
        /// given angle if can (null otherwise)
        /// </returns>
        internal static Entity? GetSineOfHalvedAngle(Entity thetaRat, Entity sin2x)
        {
            // sin(2x) = 2sin(x)cos(x)
            // 
            // c = sin(2x)
            //

            if ((thetaRat / 2).Evaled is not Rational x)
                return null;
           
            var mod = x % 2;
            if (mod.IsZero)
                return 0;
            if (mod == OneHalf)
                return 1;
            if (mod == 1)
                return 0;
            if (mod == 1.5)
                return -1;

            // cos(x) = cosSign * sqrt(1 - sin(x)2)
            //
            // c = 2 sin(x) sqrt(1 - sin(x)2) * cosSign
            // c2 = 4 sin(x)2 (1 - sin(x)2)
            // sin(x)4 - sin(x)2 + c2/4 = 0
            //
            // sin(x) = +- sqrt( 1/2 * ( 1 -+ sqrt( 1 - c2 ) ) )


            var sinSign = 1;
            
            
            if (mod > 1)
                sinSign = -1;
             
//------I.
//                 
//      sin(2x) = 2 sin(x) cos(x)
//      
//      For simplicity, let c = sin(2x).
//      
        
        
//------II.
//      
//      sin(x)2 + cos(x)2 = 1 =>
//      
//                    //================|
//      cos(x) = \\  //  1 - sin(x)^2       * cosSign
//                \\//
//      
//      For some cosSign in { -1, 1 }
//       

//------III.
//                        //================|            
//      c = 2 sin(x) \\  //  1 - sin(x)^2       * cosSign
//                    \\//                       
//

//------IV.
//
//      For now we will ignore cosSign
//
//      c^2 = 4 sin(x)^2 (1 - sin(x)^2)
//
//      c^2
//     -----  = sin(x)^2 (1 - sin(x)^2)
//       4
//
//                             c^2 
//      sin(x)^4 - sin(x)^2 + ----- = 0
//                              4  
//

//------V.
//                                   //========================================||
//                                  //   1        1     /------------------|
//      sin(x) = sinSign     \\    //   ---  +-  ---   / 1 -   sin(2x) ^ 2
//                            \\  //     2        2  \/
//                             \\//
//

            var sin1 = (sinSign * MathS.Sqrt(OneHalf - OneHalf * MathS.Sqrt(1 - sin2x.Pow(2)))).InnerSimplified;
            var sin2 = (sinSign * MathS.Sqrt(OneHalf + OneHalf * MathS.Sqrt(1 - sin2x.Pow(2)))).InnerSimplified;
//                                                  ^ that's where we change the sign

            // there must be a better way to determine whether we need sin1 or sin2
            if ((MathS.Sin(x * MathS.pi) - sin1).Abs().EvalNumerical().Downcast<Real>() < 0.01)
                return sin1;
            if ((MathS.Sin(x * MathS.pi) - sin2).Abs().EvalNumerical().Downcast<Real>() < 0.01)
                return sin2;
            
            throw new AngouriBugException("It should be either of the two sines");
        }
        
        internal static Entity? GetCosineOfHalvedAngle(Entity thetaRat, Entity cos2x)
        {
            // TODO:
            // it's not the best way as it will generate unnecessary roots,
            // so theoretically it should be done similary to sines, but for
            // now it will remain simple and irrational.
            var sin1 = GetSineOfHalvedAngle(thetaRat + 1 / 2, -MathS.Sqrt(1 - cos2x.Pow(2)));
            var sin2 = GetSineOfHalvedAngle(thetaRat + 1 / 2, +MathS.Sqrt(1 - cos2x.Pow(2)));
            if (sin1 is null || sin2 is null)
                return null;
            var cos1 = -MathS.Sqrt(1 - sin1.Pow(2)); 
            var cos2 = +MathS.Sqrt(1 - sin2.Pow(2));
            var cos3 = -MathS.Sqrt(1 + sin1.Pow(2)); 
            var cos4 = +MathS.Sqrt(1 + sin2.Pow(2));
            
            if (IsTheRightOne(cos1, thetaRat))
                return cos1;
            if (IsTheRightOne(cos2, thetaRat))
                return cos2;
            if (IsTheRightOne(cos3, thetaRat))
                return cos3;
            if (IsTheRightOne(cos4, thetaRat))
                return cos4;
            
            throw new AngouriBugException("It should be either of the two cosines");
            
            static bool IsTheRightOne(Entity cos, Entity thetaRat)
                => (MathS.Cos(thetaRat / 2 * MathS.pi) - cos).Abs().EvalNumerical().Downcast<Real>() < 0.01;
        }

        /// <summary>
        /// Assume you have sin(n x), where
        /// n is an integer number. Then
        /// sin(n x) can be easily represented
        /// as a combination of arithmetic operations
        /// of sin(x) and cos(x), which is exactly what
        /// this function does.
        /// </summary>
        /// <param name="sinx">
        /// The value of sin(x)
        /// </param>
        /// <param name="cosx">
        /// The value of cos(x)
        /// </param>
        /// <param name="n">
        /// The integer multiplier of the
        /// angle in the original sin(n x)
        /// </param>
        /// <returns>
        /// Expanded sine.
        /// </returns>
        internal static Entity ExpandSineArgumentMultiplied(Entity sinx, Entity cosx, int n)
            => n switch
                {
                    // sin(-x) = -sin(x) 
                    < 0 => -ExpandSineArgumentMultiplied(sinx, cosx, -n),
                    
                    // sin(0) = 0
                    0 => 0,
                    
                    // sin(x) = sin(x)
                    1 => sinx,
                    
                    // sin(2x) = 2 sin(x) cos(x)
                    2 => 2 * sinx * cosx,
                    
                    // sin(n x) = 2 sin(n/2 x) cos(n/2 x)
                    var nEven when nEven % 2 is 0 =>
                        2 * ExpandSineArgumentMultiplied(sinx, cosx, nEven / 2)
                          * ExpandCosineArgumentMultiplied(sinx, cosx, nEven / 2),
                    
                    // sin(n x) = sin((n - 1) x) cos(x) + sin(x) cos((n - 1) x)  
                    var nOdd => 
                        ExpandSineArgumentMultiplied(sinx, cosx, nOdd - 1) * cosx
                        +  ExpandCosineArgumentMultiplied(sinx, cosx, nOdd - 1) * sinx
                };
        
        /// <summary>
        /// Assume you have cos(n x), where
        /// n is an integer number. Then
        /// cos(n x) can be easily represented
        /// as a combination of arithmetic operations
        /// of sin(x) and cos(x), which is exactly what
        /// this function does.
        /// </summary>
        /// <param name="sinx">
        /// The value of sin(x)
        /// </param>
        /// <param name="cosx">
        /// The value of cos(x)
        /// </param>
        /// <param name="n">
        /// The integer multiplier of the
        /// angle in the original cos(n x)
        /// </param>
        /// <returns>
        /// Expanded cosine.
        /// </returns>
        internal static Entity ExpandCosineArgumentMultiplied(Entity sinx, Entity cosx, int n)
            => n switch
            {
                // cos(-n) = cos(n)
                < 0 => ExpandCosineArgumentMultiplied(sinx, cosx, -n),
                
                // cos(0) = 1
                0 => 1,
                
                // cos(x) = cos(x)
                1 => cosx,
                
                // cos(2x) = cos(x)^2 - sin(x)^2
                2 => cosx.Pow(2) - sinx.Pow(2),
                
                // cos(n x) = cos(n/2 x)^2 - sin(n/2 x)^2 
                _ when n % 2 is 0
                    => ExpandCosineArgumentMultiplied(sinx, cosx, n / 2).Pow(2)
                    - ExpandSineArgumentMultiplied(sinx, cosx, n / 2).Pow(2),
                
                // cos(n x) = cos((n - 1) x) * cos(x) - sin((n - 1) x) * sin(x)
                _ => ExpandCosineArgumentMultiplied(sinx, cosx, n - 1) * cosx
                - ExpandSineArgumentMultiplied(sinx, cosx, n - 1) * sinx
            };

        internal static Entity ExpandSineOfSum(IReadOnlyList<(Entity SinX, Entity CosX)> terms, int from, int to)
            => (to - from)
            .Let(out var halfDiff, (to - from) / 2 + 1)
            .Let(out var mod, 1 - (to - from) % 2) switch
            {
                // sine of a single term
                0 => terms[from].SinX,
                
                // sin(a + b) = sin(a)cos(b) + sin(b)cos(a)
                1 => terms[from].SinX * terms[to].CosX + terms[to].SinX * terms[from].CosX,
                
                // Consider an example.
                //
                // Let
                // from = 3
                // to = 8
                // diff = 5
                // halfDiff = 3
                // 
                // Then we will compute
                // sin(a) = sine expansion of   from: 3,         to: 8 - 3 = 5     (3 to 5)
                // cos(b) = cosine expansion of from: 3 + 3 = 6, to: 8             (6 to 8)
                // sin(b) = sine expansion of   from: 3 + 3 = 6, to: 8             (6 to 8)
                // cos(a) = cosine expansion of from: 3,         to: 8 - 3 = 5     (3 to 5)
                //
                // Now let's consider an example with diff being odd
                //
                // Let
                // from = 4
                // to = 8
                // diff = 4
                // halfDiff = 3
                // mod = 1
                //
                // sin(a) = sine expansion of   from: 4,             to: 8 - 3 = 5     (4 to 5)
                // cos(b) = cosine expansion of from: 4 + 3 - 1 = 6, to: 8             (6 to 8)
                // sin(b) = sine expansion of   from: 4 + 3 - 1 = 6, to: 8             (6 to 8)
                // cos(a) = cosine expansion of from: 4,             to: 8 - 3 = 5     (4 to 5)
                //                                            ^
                //                                       this is mod
                var diff => 
                    ExpandSineOfSum(terms, from, to - halfDiff) * ExpandCosineOfSum(terms, from + halfDiff - mod, to)
                    + ExpandSineOfSum(terms, from + halfDiff - mod, to) * ExpandCosineOfSum(terms, from, to - halfDiff)
            };
        
        internal static Entity ExpandCosineOfSum(IReadOnlyList<(Entity SinX, Entity CosX)> terms, int from, int to)
            // see the explanation for halfDiff and mod magic above (for sine)
            => (to - from)
            .Let(out var halfDiff, (to - from) / 2 + 1)
            .Let(out var mod, 1 - (to - from) % 2)
            switch
            {
                0 => terms[from].CosX,
                1 => terms[from].CosX * terms[to].CosX - terms[to].SinX * terms[from].SinX,
                var diff => 
                    ExpandCosineOfSum(terms, from, to - halfDiff) * ExpandCosineOfSum(terms, from + halfDiff - mod, to)
                    - ExpandSineOfSum(terms, from + halfDiff - mod, to) * ExpandSineOfSum(terms, from, to - halfDiff)
            };
            
        
        [ConstantField] private static readonly Dictionary<Integer, Entity> anglesSin = new() 
        {
            { 1, "0" },
            { 2, "1" },
            { 3, "sqrt(3) / 2" },
            { 4, "sqrt(2) / 2" },
            { 5, "sqrt(5/8 - sqrt(5)/8)" },
            
            // Reproduction code:
            // Console.WriteLine(MathS.ExperimentalFeatures.GetSineOfHalvedAngle("2pi / 7", MathS.Sqrt(1 - MathS.Pow("1/6" * (-1 + MathS.Cbrt((7 + 21 * Sqrt(-3)) / 2) + MathS.Cbrt((7 - 21 * Sqrt(-3)) / 2)), 2))));
            { 7, "sqrt(1/2 - 1/2 * sqrt(1 - sqrt(1 - (1/6 * (-1 + ((7 + 21 * sqrt(-3)) / 2) ^ (1/3) + ((7 - 21 * sqrt(-3)) / 2) ^ (1/3))) ^ 2) ^ 2))" }
        };
            
        [ConstantField] private static readonly Dictionary<Integer, Entity> anglesCos = new()
        {
            { 1, "-1" },
            { 2, "0" },
            { 3, "1 / 2" },
            { 4, "sqrt(2) / 2" },
            { 5, "1/4 * (sqrt(5) - 1)" },
            
            // Reproduction code:
            // Console.WriteLine(MathS.ExperimentalFeatures.GetCosineOfHalvedAngle("2pi / 7", "1/6" * (-1 + Cbrt((7 + 21 * Sqrt(-3)) / 2) + Cbrt((7 - 21 * Sqrt(-3)) / 2))));
            { 7, "sqrt(1 - sqrt(1/2 - 1/2 * sqrt(1 - sqrt(1 - (1/6 * (-1 + ((7 + 21 * sqrt(-3)) / 2) ^ (1/3) + ((7 - 21 * sqrt(-3)) / 2) ^ (1/3))) ^ 2) ^ 2)) ^ 2)" }
        };
        
        private static IReadOnlyList<(Entity SinX, Entity CosX)>? PrepareTerms(Entity angle)
        {
            var piCoef = MathS.UnsafeAndInternal.DivideByEntityStrict(angle, MathS.pi);
            if (piCoef is not { Evaled: Rational rationalCoef })
                return null;
            
            rationalCoef %= 2; // 2pi doesn't ever change anything
            
            var terms = new List<(Entity SinX, Entity CosX)>();
            
            foreach (var (num, prime, power) in Fraction.Decompose(rationalCoef.Numerator, rationalCoef.Denominator))
            {
                var elem = (num / prime.Pow(power)).Evaled.Downcast<Rational>();
                if (!anglesSin.TryGetValue(elem.Denominator, out var sin1x))
                    return null;
                if (!anglesCos.TryGetValue(elem.Denominator, out var cos1x))
                    return null;
                var sinnx = ExpandSineArgumentMultiplied(sin1x, cos1x, (int)elem.Numerator);
                var cosnx = ExpandCosineArgumentMultiplied(sin1x, cos1x, (int)elem.Numerator);
                terms.Add((SinX: sinnx, CosX: cosnx));
            }
            
            return terms;
        }
        
        internal static Entity? SymbolicFormOfSine(Entity angle)
            => PrepareTerms(angle)?.Pipe(terms => ExpandSineOfSum(terms, 0, terms.Count - 1));
        
        internal static Entity? SymbolicFormOfCosine(Entity angle)
            => PrepareTerms(angle)?.Pipe(terms => ExpandCosineOfSum(terms, 0, terms.Count - 1));
    }
}