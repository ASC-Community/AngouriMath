//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core;
using static AngouriMath.Entity.Number;
using Xunit;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Convenience
{
    [Trait("Area", "Convenience")]
    public sealed class LatexTests
    {
        private static readonly Entity x = MathS.Var(nameof(x));
        private static readonly Entity frac34 = Rational.Create(3, 4);
        private static readonly Entity m1 = -1;
        private static readonly Entity im1 = MathS.i - 1;
        private static readonly Entity m2i = -2 * MathS.i;
        private static readonly Entity numPi = MathS.DecimalConst.pi;
        private static readonly Entity mNumPi = -MathS.DecimalConst.pi;
        void Test(string expected, ILatexizeable actual) =>
            Assert.Equal(expected, actual.Latexize());
        void TestSimplify(string expected, Entity actual) =>
            Test(expected, actual.Simplify());
        [Fact] public void Num0() => TestSimplify("0", 0);
        [Fact] public void Num123() => TestSimplify("123", 123);
        [Fact] public void Float() => Test(@"\frac{15432}{125}", (Entity)123.456m);
        [Fact] public void FloatSimplify() => TestSimplify(@"\frac{15432}{125}", 123.456m);
        [Fact] public void FloatParse() => Test(@"3", Complex.Parse("3.000"));
        [Fact] public void FloatZero() => Test(@"123.4561234567890", Complex.Parse("123.4561234567890"));
        [Fact] public void Pi() => Test(@"\mathrm{\pi}", MathS.pi);
        [Fact] public void E() => Test(@"\mathrm{e}", MathS.e);
        [Fact] public void I() => TestSimplify(@"\mathrm{i}", MathS.Sqrt(-1));
        [Fact] public void Variable() => Test("x", x);
        [Fact] public void Add() => Test("x+x", x + x);
        [Fact] public void AddAdd() => Test("x+x+x", x + x + x);
        [Fact] public void AddSimplify() => TestSimplify(@"2 x", x + x);
        [Fact] public void Subtract() => Test("x-x", x - x);
        [Fact] public void SubtractSubtract() => Test("x-x-x", x - x - x);
        [Fact] public void SubtractSimplify() => TestSimplify("0", x - x);
        // Subtracting something written with a leading minus is adding the rest of it -- the
        // system solver's `1/100 - (-4)(-a + b)` printed as `\frac{1}{100}--4 \left(-a+b\right)`.
        [Fact] public void SubtractANegativeMultiple() => Test(@"x+4 \left(-a+b\right)", x - (-4) * (Entity)"-a + b");
        [Fact] public void SubtractANegation() => Test("x+y", x - (-(Entity)"y"));
        [Fact] public void SubtractASumWithALeadingMinusKeepsItsParentheses() => Test(@"x-\left(-a+b\right)", x - (Entity)"-a + b");
        [Fact] public void Multiply() => Test(@"x x", x * x);
        [Fact] public void MultiplyNum() => Test(@"2 \cdot 2", (Entity)2 * 2);
        [Fact] public void MultiplyMultiply() => Test(@"x x x", x * x * x);
        [Fact] public void MultiplyMultiplyNum() => Test(@"2 \cdot 2 \cdot 2", (Entity)2 * 2 * 2);
        [Fact] public void MultiplySimplify() => TestSimplify("{x}^{2}", x * x);
        [Fact] public void Divide() => Test(@"\frac{x}{x}", x / x);
        [Fact] public void DivideDivide() => Test(@"\frac{\frac{x}{x}}{x}", x / x / x);
        [Fact] public void DivideSimplify() => TestSimplify(@"1 \quad \text{for} \quad x \neq 0", x / x);
        [Fact] public void Greek1() => Test(@"\alpha", MathS.Var("alpha"));
        [Fact] public void Greek2() => Test(@"\beta", MathS.Var("beta"));
        [Fact] public void Greek3() => Test(@"a_{\beta}", MathS.Var("a_beta"));
        [Fact] public void Greek4() => Test(@"\alpha_{3}", MathS.Var("alpha_3"));
        [Fact] public void Greek5() => Test(@"\alpha_{\beta}", MathS.Var("alpha_beta"));
        [Fact] public void Greek6() => Test(@"\beta_{\alpha}", MathS.Var("beta_alpha"));
        [Fact] public void SubscriptSingle() => Test(@"c_{\mathrm{e}}", MathS.Var("c_e"));
        [Fact] public void SubscriptDuo() => Test(@"\Delta_{\mathrm{a2}}", MathS.Var("Delta_a2"));
        [Fact] public void SubscriptDuoPlus() => TestSimplify(@"6+\Delta_{\mathrm{a2}}", "Delta_a2+6");
        // A subscript of a subscript. https://github.com/asc-community/AngouriMath/issues/524
        [Fact] public void SubscriptNested() => Test(@"X_{a_{\beta}}", MathS.Var("X_a_beta"));
        [Fact] public void SubscriptNestedThrice() => Test(@"x_{1_{2_{\mathrm{e}}}}", MathS.Var("x_1_2_e"));
        [Fact] public void SubscriptNestedParsed() => Test(@"X_{a_{\beta}}+10", MathS.FromString("X_a_beta + 10"));
        [Fact] public void Square() => Test(@"{x}^{2}", MathS.Sqr(x));
        [Fact] public void TwoXSquare() => Test(@"2 {x}^{2}", 2 * MathS.Sqr(x));
        [Fact] public void MTwoXSquare() => Test(@"-2 \cdot {x}^{2}", -2 * MathS.Sqr(x));
        [Fact] public void MTwoPTwoIXSquare() => Test(@"\left(-2 - \mathrm{i}\right) \cdot {x}^{2}", Complex.Create(-2, -1) * MathS.Sqr(x));
        [Fact] public void XTwo() => Test(@"x \cdot 2", x * 2);
        [Fact] public void XMTwo() => Test(@"x \left(-2\right)", x * -2);
        [Fact] public void XSquareTwo() => Test(@"{x}^{2} \cdot 2", MathS.Sqr(x) * 2);
        [Fact] public void XSquareMTwo() => Test(@"{x}^{2} \left(-2\right)", MathS.Sqr(x) * -2);
        [Fact] public void TwoAXSquare() => Test(@"2 a {x}^{2}", 2 * (Entity)"a" * MathS.Sqr(x));
        [Fact] public void TwoXSquareA() => Test(@"2 {x}^{2} \cdot a", 2 * MathS.Sqr(x) * "a");
        [Fact] public void TwoSinXXSquare() => Test(@"2 \sin\left(x\right) {x}^{2}", 2 * MathS.Sin(x) * MathS.Sqr(x));
        [Fact] public void TwoXSquareSinX() => Test(@"2 {x}^{2} \cdot \sin\left(x\right)", 2 * MathS.Sqr(x) * MathS.Sin(x));
        [Fact] public void SquareSquare() => Test(@"{\left({x}^{2}\right)}^{2}", MathS.Sqr(MathS.Sqr(x)));
        [Fact] public void SquareRoot() => Test(@"\sqrt{x}", MathS.Sqrt(x));
        [Fact] public void SquareRootAsPow() => Test(@"\sqrt{x}", MathS.Pow(x, 1m/2));
        [Fact] public void Cube() => Test(@"{x}^{3}", MathS.Pow(x, 3));
        [Fact] public void TwoXCubeX() => Test(@"2 {x}^{3} \cdot x", 2 * MathS.Pow(x, 3) * x);
        [Fact] public void TwoXXCube() => Test(@"2 x {x}^{3}", 2 * x * MathS.Pow(x, 3));
        [Fact] public void TwoXXCube2() => Test(@"2 x {x}^{3} \cdot 2", 2 * x * MathS.Pow(x, 3) * 2);
        [Fact] public void TwoXSquareCube() => Test(@"2 {x}^{2} {x}^{3}", 2 * MathS.Sqr(x) * MathS.Pow(x, 3));
        [Fact] public void CubeRoot() => Test(@"\sqrt[3]{x}", MathS.Pow(x, 1m/3));
        [Fact] public void FourthRoot() => Test(@"\sqrt[4]{x}", MathS.Pow(x, 1m/4));
        [Fact] public void FourthRootCube() => Test(@"\sqrt[4]{x}^{3}", MathS.Pow(x, 3m/4));
        [Fact] public void M3Root() => Test(@"\frac{1}{\sqrt[3]{x}}", MathS.Pow(x, 1m/-3));
        [Fact] public void M4RootCube() => Test(@"\frac{1}{\sqrt[4]{x}^{3}}", MathS.Pow(x, 3m/-4));
        [Fact] public void PowBase2() => Test("{2}^{x}", MathS.Pow(2, x));
        [Fact] public void PowBase10() => Test("{10}^{x}", MathS.Pow(10, x));
        [Fact] public void Pow() => Test("{x}^{x}", MathS.Pow(x, x));
        [Fact] public void PowPow() => Test("{x}^{{x}^{x}}", MathS.Pow(x, MathS.Pow(x, x)));
        [Fact] public void PowPowNumber() => Test(@"{\left({12}^{23}\right)}^{34}", MathS.Pow(MathS.Pow(12, 23), 34));
        [Fact] public void MPow() => Test(@"{\left(-x\right)}^{x}", MathS.Pow(-x, x));
        [Fact] public void AddMultiply() =>
            Test(@"\left(1+2\right) \left(3+4\right)", ((Entity)1 + 2) * ((Entity)3 + 4));
        [Fact] public void SubtractMultiply() =>
            Test(@"\left(1-2\right) \left(3-4\right)", ((Entity)1 - 2) * ((Entity)3 - 4));
        [Fact] public void AddPow() => Test(@"{\left(3+4\right)}^{x}", MathS.Pow("3+4", x));
        [Fact] public void SubtractPow() => Test(@"{\left(3-4\right)}^{x}", MathS.Pow("3-4", x));
        [Fact] public void MultiplyPow() => Test(@"{\left(3 \cdot 4\right)}^{x}", MathS.Pow("3*4", x));
        [Fact] public void DividePow() => Test(@"{\left(\frac{3}{4}\right)}^{x}", MathS.Pow("3/4", x));
        [Fact] public void XSquaredMinusX() => TestSimplify(@"{x}^{2}-x", "x^2-x");
        [Fact] public void XSquaredMinusXAlternate() => TestSimplify(@"{x}^{2}-x", "-x+x^2");
        [Fact] public void XSquaredMinusXAlternate2() => TestSimplify(@"{x}^{2}-x", "x^2+(-1)*x");
        [Fact] public void M1() => Test("-1", (Complex)(-1));
        [Fact] public void M1Entity() => Test("-1", m1);
        [Fact] public void M1Add() => Test(@"-1-1", m1 + m1);
        [Fact] public void M1Subtract() => Test(@"-1-\left(-1\right)", m1 - m1);
        [Fact] public void M1Multiply() => Test(@"-\left(-1\right)", m1 * m1);
        [Fact] public void M1Divide() => Test(@"\frac{-1}{-1}", m1 / m1);
        [Fact] public void PowM1() => Test(@"{x}^{-1}", MathS.Pow(x, m1));
        [Fact] public void M1Pow() => Test(@"{\left(-1\right)}^{x}", MathS.Pow(m1, x));
        [Fact] public void M2Pow() => Test(@"{\left(-2\right)}^{x}", MathS.Pow(-2, x));
        [Fact] public void PiPow() => Test(@"{\mathrm{\pi}}^{x}", MathS.Pow(MathS.pi, x));
        [Fact] public void MPiPow() => Test(@"{\left(-\mathrm{\pi}\right)}^{x}", MathS.Pow(-MathS.pi, x));
        [Fact] public void NumPiAdd() => Test(@"3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068+3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068", numPi + numPi);
        [Fact] public void NumPiSubtract() => Test(@"3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068-3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068", numPi - numPi);
        [Fact] public void NumPiMultiply() => Test(@"3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068 \cdot 3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068", numPi * numPi);
        [Fact] public void NumPiDivide() => Test(@"\frac{3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068}{3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068}", numPi / numPi);
        [Fact] public void NumPiPow() => Test(@"{3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068}^{x}", MathS.Pow(numPi, x));
        [Fact] public void PowNumPi() => Test(@"{x}^{3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068}", MathS.Pow(x, numPi));
        [Fact] public void MNumPiAdd() => Test(@"-3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068-3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068", mNumPi + mNumPi);
        [Fact] public void MNumPiSubtract() => Test(@"-3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068-\left(-3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068\right)", mNumPi - mNumPi);
        [Fact] public void MNumPiMultiply() => Test(@"-3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068 \left(-3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068\right)", mNumPi * mNumPi);
        [Fact] public void MNumPiDivide() => Test(@"\frac{-3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068}{-3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068}", mNumPi / mNumPi);
        [Fact] public void MNumPiPow() => Test(@"{\left(-3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068\right)}^{x}", MathS.Pow(mNumPi, x));
        [Fact] public void PowMNumPi() => Test(@"{x}^{-3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068}", MathS.Pow(x, mNumPi));
        [Fact] public void MI() => TestSimplify(@"-\mathrm{i}", MathS.Sqrt(-1) * -1);
        [Fact] public void M2I() => TestSimplify(@"-2\mathrm{i}", m2i);
        [Fact] public void IM1() => Test(@"-1 + \mathrm{i}", im1);
        [Fact] public void MIM1() => Test(@"-1 - \mathrm{i}", -MathS.i - 1);
        [Fact] public void M2IM1() => Test(@"-1 - 2\mathrm{i}", -2 * MathS.i - 1);
        [Fact] public void IP1() => Test(@"1 + \mathrm{i}", MathS.i + 1);
        [Fact] public void MIP1() => Test(@"1 - \mathrm{i}", -1 * MathS.i + 1);
        [Fact] public void M2IP1() => Test(@"1 - 2\mathrm{i}", -2 * MathS.i + 1);
        [Fact] public void M0IP1() => Test("1", 0 * -MathS.i + 1);
        [Fact] public void M0IM1() => Test("-1", 0 * -MathS.i - 1);
        [Fact] public void ISquare() => Test(@"{\mathrm{i}}^{2}", MathS.Sqr(MathS.i));
        [Fact] public void ISquareSimplified() => TestSimplify("-1", MathS.Sqr(MathS.i));
        [Fact] public void Frac34Add() => Test(@"\frac{3}{4}+\frac{3}{4}", frac34 + frac34);
        [Fact] public void Frac34Subtract() => Test(@"\frac{3}{4}-\frac{3}{4}", frac34 - frac34);
        [Fact] public void Frac34Multiply() => Test(@"\frac{3}{4} \cdot \frac{3}{4}", frac34 * frac34);
        [Fact] public void Frac34Divide() => Test(@"\frac{\frac{3}{4}}{\frac{3}{4}}", frac34 / frac34);
        [Fact] public void Frac34Square() => Test(@"{\left(\frac{3}{4}\right)}^{2}", MathS.Sqr(frac34));
        [Fact] public void Frac34SquareRoot() => Test(@"\sqrt{\frac{3}{4}}", MathS.Sqrt(frac34));
        [Fact] public void Frac34CubeRoot() => Test(@"\sqrt[3]{\frac{3}{4}}", MathS.Pow(frac34, 1m/3));
        [Fact] public void Frac34Pow() => Test(@"{\left(\frac{3}{4}\right)}^{x}", MathS.Pow(frac34, x));
        [Fact] public void PowFrac34() => Test(@"\sqrt[4]{2}^{3}", MathS.Pow(2, frac34));
        [Fact] public void M2IAdd() => Test(@"-2\mathrm{i}-2\mathrm{i}", m2i + m2i);
        [Fact] public void M2ISubtract() => Test(@"-2\mathrm{i}-\left(-2\mathrm{i}\right)", m2i - m2i);
        [Fact] public void M2IMultiply() => Test(@"-2\mathrm{i} \left(-2\mathrm{i}\right)", m2i * m2i);
        [Fact] public void M2IDivide() => Test(@"\frac{-2\mathrm{i}}{-2\mathrm{i}}", m2i / m2i);
        [Fact] public void M2ISquare() => Test(@"{\left(-2\mathrm{i}\right)}^{2}", MathS.Sqr(m2i));
        [Fact] public void M2ISquareRoot() => Test(@"\sqrt{-2\mathrm{i}}", MathS.Sqrt(m2i));
        [Fact] public void M2ICubeRoot() => Test(@"\sqrt[3]{-2\mathrm{i}}", MathS.Pow(m2i, 1m/3));
        [Fact] public void M2IPow() => Test(@"{\left(-2\mathrm{i}\right)}^{x}", MathS.Pow(m2i, x));
        [Fact] public void PowM2I() => Test(@"{2}^{-2\mathrm{i}}", MathS.Pow(2, m2i));
        [Fact] public void IM1Add() => Test(@"-1 + \mathrm{i}-1 + \mathrm{i}", im1 + im1);
        [Fact] public void IM1Subtract() => Test(@"-1 + \mathrm{i}-\left(-1 + \mathrm{i}\right)", im1 - im1);
        [Fact] public void IM1Multiply() => Test(@"\left(-1 + \mathrm{i}\right) \left(-1 + \mathrm{i}\right)", im1 * im1);
        [Fact] public void IM1Divide() => Test(@"\frac{-1 + \mathrm{i}}{-1 + \mathrm{i}}", im1 / im1);
        [Fact] public void IM1Square() => Test(@"{\left(-1 + \mathrm{i}\right)}^{2}", MathS.Sqr(im1));
        [Fact] public void IM1SquareRoot() => Test(@"\sqrt{-1 + \mathrm{i}}", MathS.Sqrt(im1));
        [Fact] public void IM1CubeRoot() => Test(@"\sqrt[3]{-1 + \mathrm{i}}", MathS.Pow(im1, 1m/3));
        [Fact] public void IM1Pow() => Test(@"{\left(-1 + \mathrm{i}\right)}^{x}", MathS.Pow(im1, x));
        [Fact] public void PowIM1() => Test(@"{2}^{-1 + \mathrm{i}}", MathS.Pow(2, im1));
        [Fact] public void Mi() => TestSimplify(@"-\mathrm{i}", -MathS.Sqrt(-1));
        [Fact] public void Trig() =>
            TestSimplify(@"\sin\left(\cos\left(\tan\left(\cot\left(x\right)\right)\right)\right)", MathS.Sin(MathS.Cos(MathS.Tan(MathS.Cotan(x)))));
        [Fact] public void SecCosec() =>
            TestSimplify(@"\sec\left(\csc\left(x\right)\right)", MathS.Sec(MathS.Cosec(x)));
        [Fact] public void ArcTrig() =>
            TestSimplify(@"\arcsin\left(\arccos\left(\arctan\left(\operatorname{arccot}\left(x\right)\right)\right)\right)", MathS.Arcsin(MathS.Arccos(MathS.Arctan(MathS.Arccotan(x)))));
        [Fact] public void ArcSecCosec() =>
            TestSimplify(@"\operatorname{arcsec}\left(\operatorname{arccsc}\left(x\right)\right)", MathS.Arcsec(MathS.Arccosec(x)));
        [Fact] public void Log10() => Test(@"\log\left(10\right)", MathS.Log(10, 10));
        [Fact] public void Ln() => Test(@"\ln\left(10\right)", MathS.Ln(10));
        [Fact] public void LnAlternate() => Test(@"\ln\left(10\right)", MathS.Log(MathS.e, 10));
        [Fact] public void Log() => Test(@"\log_{2}\left(10\right)", MathS.Log(2, 10));
        [Fact] public void Factorial1() => Test(@"1!", MathS.Factorial(1));
        [Fact] public void Factorial23() => Test(@"23!", MathS.Factorial(23));
        [Fact] public void FactorialM23() => Test(@"\left(-23\right)!", MathS.Factorial(-23));
        [Fact] public void FactorialI() => Test(@"\mathrm{i}!", MathS.Factorial(MathS.i));
        [Fact] public void FactorialMI() => Test(@"\left(-\mathrm{i}\right)!", MathS.Factorial(-MathS.i));
        [Fact] public void Factorial1MI() => Test(@"\left(1 - \mathrm{i}\right)!", MathS.Factorial(1 - MathS.i));
        [Fact] public void Factorial1PI() => Test(@"\left(1 + \mathrm{i}\right)!", MathS.Factorial(1 + MathS.i));
        [Fact] public void FactorialX() => Test(@"x!", MathS.Factorial(x));
        [Fact] public void FactorialSinX() => Test(@"\sin\left(x\right)!", MathS.Factorial(MathS.Sin(x)));
        // x!! is the double factorial, (x!)! is factorial applied twice which is different
        [Fact] public void FactorialFactorialX() => Test(@"\left(x!\right)!", MathS.Factorial(MathS.Factorial(x)));
        [Fact] public void FactorialXSquared() => Test(@"\left({x}^{2}\right)!", MathS.Factorial(MathS.Sqr(x)));
        [Fact] public void OO() => Test(@"\infty ", Real.PositiveInfinity);
        [Fact] public void MOO() => Test(@"-\infty ", Real.NegativeInfinity);
        [Fact] public void MOOAlternate() => Test(@"-\infty ", -Real.PositiveInfinity);
        [Fact] public void OOPI() => Test(@"\infty  + \mathrm{i}", Real.PositiveInfinity + MathS.i);
        [Fact] public void OOMI() => Test(@"\infty  - \mathrm{i}", Real.PositiveInfinity - MathS.i);
        [Fact] public void OOPOOI() => Test(@"\infty  + \infty \mathrm{i}", Real.PositiveInfinity * (1 + MathS.i));
        [Fact] public void OOMOOI() => Test(@"\infty  - \infty \mathrm{i}", Real.PositiveInfinity * (1 - MathS.i));
        [Fact] public void MOOPOOI() => Test(@"-\infty  + \infty \mathrm{i}", Real.PositiveInfinity * (-1 + MathS.i));
        [Fact] public void MOOMOOI() => Test(@"-\infty  - \infty \mathrm{i}", Real.PositiveInfinity * (-1 - MathS.i));
        [Fact] public void Undefined() => TestSimplify(@"\mathrm{undefined}", Real.PositiveInfinity / Real.PositiveInfinity);
        [Fact] public void Set0() => Test(@"\emptyset", MathS.Sets.Empty);
        [Fact] public void Set0Alternate() => Test(@"\emptyset", MathS.Sets.Finite());
        [Fact] public void Matrix() =>
            Test(@"\begin{bmatrix}11 & 12 \\ 21 & 22 \\ 31 & 32\end{bmatrix}", MathS.Matrix(new Entity[,]
                {
                    { 11, 12 },
                    { 21, 22 },
                    { 31, 32 }
                }));
        [Fact] public void MatrixRow() =>
            Test(@"\begin{bmatrix}11 & 12 & 21 & 22 & 31 & 32\end{bmatrix}", MathS.Vector(11, 12, 21, 22, 31, 32).T);
        [Fact] public void MatrixColumn() =>
            Test(@"\begin{bmatrix}11 \\ 12 \\ 21 \\ 22 \\ 31 \\ 32\end{bmatrix}", MathS.Vector(11, 12, 21, 22, 31, 32));
        [Fact] public void MatrixSingle() =>
            Test(@"\begin{bmatrix}x\end{bmatrix}", MathS.Vector(x));
        [Fact] public void Vector() =>
            Test(@"\begin{bmatrix}11 & 12 & 21 & 22 & 31 & 32\end{bmatrix}", MathS.Vector(11, 12, 21, 22, 31, 32).T);
        [Fact] public void VectorSingle() =>
            Test(@"\begin{bmatrix}x\end{bmatrix}", MathS.Vector(x));
        [Fact] public void Derivative1() => Test(@"\frac{\mathrm{d}}{\mathrm{d}x}x", MathS.Derivative(x, x));
        [Fact] public void Derivative2() => Test(@"\frac{\mathrm{d}^{2}}{\mathrm{d}x^{2}}x", MathS.Derivative(x, x, 2));
        [Fact] public void Derivative3() => Test(@"\frac{\mathrm{d}}{\mathrm{d}\left({x}^{2}\right)}x", MathS.Derivative(x, x.Pow(2)));
        [Fact] public void Derivative4() => Test(@"\frac{\mathrm{d}^{2}}{\mathrm{d}\left({x}^{2}\right)^{2}}x", MathS.Derivative(x, x.Pow(2), 2));
        [Fact] public void Derivative6() => Test(@"\frac{\mathrm{d}}{\mathrm{d}x}{x}^{2}", MathS.Derivative(x.Pow(2), x));
        [Fact] public void Derivative7() => Test(@"\frac{\mathrm{d}^{2}}{\mathrm{d}\left({x}^{2}\right)^{2}}{x}^{2}", MathS.Derivative(x.Pow(2), x.Pow(2), 2));
        [Fact] public void Derivative8() => Test(@"\frac{\mathrm{d}}{\mathrm{d}\left(\mathrm{xy}\right)}\mathrm{xy}", MathS.Derivative("xy", "xy"));
        [Fact] public void Derivative9() => Test(@"\frac{\mathrm{d}^{2}}{\mathrm{d}\left(\mathrm{xy}\right)^{2}}\mathrm{xy}", MathS.Derivative("xy", "xy", 2));
        [Fact] public void Derivative10() => Test(@"\frac{\mathrm{d}}{\mathrm{d}x}\frac{x}{2}", MathS.Derivative("x / 2", x));
        [Fact] public void Derivative11() => Test(@"\frac{\mathrm{d}}{\mathrm{d}\left(\mathrm{quack}\right)}\left(x+y\right)", MathS.Derivative("x + y", "quack"));
        [Fact] public void Derivative12() => Test(@"\frac{\mathrm{d}^{3}}{\mathrm{d}x_{f}^{3}}\left(x+1\right)", MathS.Derivative("x + 1", "x_f", 3));
        [Fact] public void Derivative13() => Test(@"\frac{\mathrm{d}}{\mathrm{d}x_{f}}\frac{1}{x}", MathS.Derivative("1/x", "x_f"));
        [Fact] public void Derivative14() => Test(@"\frac{\mathrm{d}}{\mathrm{d}\alpha}\left({x}^{23}-x_{\mathrm{16}}\right)", MathS.Derivative("x^23-x_16", "alpha"));
        [Fact] public void Derivative15() => Test(@"\frac{\mathrm{d}}{\mathrm{d}\alpha_{\beta}}\alpha", MathS.Derivative("alpha", "alpha_beta"));
        [Fact] public void Derivative16() => Test(@"\frac{\mathrm{d}}{\mathrm{d}\left(\mathrm{alphaa}_{\beta}\right)}\alpha", MathS.Derivative("alpha", "alphaa_beta"));
        [Fact] public void Integral1() => Test(@"\int x\,\mathrm{d}x", MathS.Integral(x, x));
        [Fact] public void Integral2() =>
            Test(@"\int \left(x+1\right)\,\mathrm{d}\left(x+1\right)", MathS.Integral("x + 1", "x+1"));
        [Fact] public void Integral3() =>
            Test(@"\int\int \left(x+1\right)\,\mathrm{d}x\,\mathrm{d}x", MathS.Derivative("x + 1", x, -2));
        [Fact] public void Integral4() =>
            Test(@"\int\int \left(x+1\right)\,\mathrm{d}\left(\mathrm{xf}\right)\,\mathrm{d}\left(\mathrm{xf}\right)", MathS.Derivative("x + 1", "xf", -2));
        [Fact] public void Integral5() =>
            Test(@"\int \frac{1}{x}\,\mathrm{d}x_{f}", MathS.Integral("1/x", "x_f"));
        [Fact] public void Integral6() =>
            Test(@"\int\int\int \left({x}^{23}-x_{\mathrm{16}}\right)\,\mathrm{d}\left({x_{f}}^{2}\right)\,\mathrm{d}\left({x_{f}}^{2}\right)\,\mathrm{d}\left({x_{f}}^{2}\right)", MathS.Derivative("x^23-x_16", "x_f^2", -3));
        [Fact] public void DerivativeAt0() => Test(@"\frac{\mathrm{d}^{0}}{\mathrm{d}x^{0}}\left(x+1\right)+1", MathS.Derivative("x+1", "x", 0) + 1);
        [Theory, CombinatorialData] public void DerivativeAsIntegral([CombinatorialValues("x", "x_f", "ab", "ab_cd", "e", "alpha", "alpha_beta", "alpha_e")] string v) =>
            Test(MathS.Integral(Integer.One + v, v).Latexize(), MathS.Derivative(Integer.One + v, v, -1));
        [Fact] public void Limit1() =>
            Test(@"\lim_{x\to 0^-} \left(x+y\right)", (Entity)"limitleft(x + y, x, 0)");
        [Fact] public void Limit2() =>
            Test(@"\lim_{x\to 0^+} {a}^{5}", (Entity)"limitright(a^5, x, 0)");
        [Fact] public void Limit3() =>
            Test(@"\lim_{x\to \infty } \left(x+y\right)", MathS.Limit("x + y", x, Real.PositiveInfinity));
        [Fact] public void Limit4() =>
            Test(@"\lim_{\mathrm{xf}\to 1+x} \frac{1}{x}", MathS.Limit("1/x", "xf", "1+x"));
        [Fact] public void Limit5() =>
            Test(@"\lim_{\mathrm{xf}\to {x_{2}}^{3}} \left({x}^{23}-x_{\mathrm{16}}\right)", MathS.Limit("x^23-x_16", "xf", "x_2^3"));
        [Theory, InlineData("+"), InlineData("-")] public void LimitOneSided1(string sign) =>
            Test($$"""\lim_{x\to \left(a+2\right)^{{sign}}} \left(x+y\right)""", (Entity)$"limit{(sign == "-" ? "left" : "right")}(x + y, x, a+2)");
        [Theory, InlineData("+"), InlineData("-")] public void LimitOneSided2(string sign) =>
            Test($$"""\lim_{x\to \left({a}^{2}\right)^{{sign}}} \left(x+y\right)""", (Entity)$"limit{(sign == "-" ? "left" : "right")}(x + y, x, a^2)");
        [Theory, InlineData("+"), InlineData("-")]
        public void LimitOneSided3(string sign) =>
            Test($$"""\lim_{x\to \left({a}^{2}\right)!^{{sign}}} \left(x+y\right)""", (Entity)$"limit{(sign == "-" ? "left" : "right")}(x + y, x, (a^2)!)");
        [Fact] public void LimitOfExponential() =>
            Test(@"\lim_{x\to \infty } {\sin\left(x\right)}^{x} = \infty ", MathS.Limit(MathS.Pow(MathS.Sin(x), x), x, Real.PositiveInfinity).EqualTo(Real.PositiveInfinity));
        [Fact] public void NestedLimitDerivative() =>
            Test(@"\left(\lim_{x\to 0} \frac{\mathrm{d}^{2}}{\mathrm{d}x^{2}}\lim_{y\to \infty } \frac{x}{y}\right) \cdot x", MathS.Limit(MathS.Derivative(MathS.Limit("x / y", "y", Real.PositiveInfinity), x, 2), x, 0) * x);
        [Fact] public void NestedDerivativeIntegral1() =>
            Test(@"\frac{\mathrm{d}}{\mathrm{d}x}\int \frac{\mathrm{d}}{\mathrm{d}y}\int {x}^{2} \cdot y\,\mathrm{d}y\,\mathrm{d}\left(x+1\right)+1", MathS.Derivative(MathS.Integral(MathS.Derivative(MathS.Integral(MathS.Pow(x, 2) * "y", "y"), "y"), x + 1), x) + 1);
        [Fact] public void NestedDerivativeIntegral2() =>
            Test(@"\int \frac{\mathrm{d}}{\mathrm{d}x}\int \frac{\mathrm{d}}{\mathrm{d}y}\left(1-{x}^{2} \cdot y\right)\,\mathrm{d}y\,\mathrm{d}x+1", MathS.Integral(MathS.Derivative(MathS.Integral(MathS.Derivative(1 - MathS.Pow(x, 2) * "y", "y"), "y"), x), x) + 1);
        [Fact] public void Interval1() =>
            Test(@"\left[3, 4\right]", MathS.Sets.Interval(3, true, 4, true));
        [Fact] public void Interval2() =>
            Test(@"\left(3, 4\right]", MathS.Sets.Interval(3, false, 4, true));
        [Fact] public void Interval3() =>
            Test(@"\left(3, 4\right)", MathS.Sets.Interval(3, false, 4, false));
        [Fact] public void Interval4() =>
            Test(@"\left[3, 4\right)", MathS.Sets.Interval(3, true, 4, false));
        [Fact] public void FiniteSet1() =>
            Test(@"\left\{ 1, 2, 3 \right\}", MathS.Sets.Finite(1, 2, 3));
        [Fact] public void FiniteSet2() =>
            Test(@"\left\{ 1 \right\}", MathS.Sets.Finite(1));
        [Fact] public void CSet1() =>
            Test(@"\left\{ x : x > 0 \right\}", new ConditionalSet("x", "x > 0"));
        [Fact] public void CSet2() =>
            Test(@"\left\{ x : \sqrt{2} \right\}", new ConditionalSet("x", "sqrt(2)"));
        [Fact] public void RR() =>
            Test(@"\mathbb{R}", (Entity)Domain.Real);
        [Fact] public void ZZ() =>
            Test(@"\mathbb{Z}", (Entity)Domain.Integer);
        [Fact] public void Signum()
            => Test(@"\operatorname{sgn}\left(x\right)", MathS.Signum(x));
        [Fact] public void Abs()
            => Test(@"\left|x\right|", MathS.Abs(x));
        [Fact] public void Phi1()
            => Test(@"\operatorname{\varphi}\left(x\right)", MathS.NumberTheory.Phi(x));
        [Fact] public void Phi2()
            => Test(@"\operatorname{\varphi}\left({x}^{2}\right)", MathS.NumberTheory.Phi(MathS.Pow(x, 2)));
        [Fact] public void Phi3()
            => Test(@"{\operatorname{\varphi}\left(x\right)}^{\operatorname{\varphi}\left({x}^{2}\right)}", MathS.Pow(MathS.NumberTheory.Phi(x), MathS.NumberTheory.Phi(MathS.Pow(x, 2))));
        [Fact] public void Piecewise1()
            => Test(@"\begin{cases}a & \text{for } b\\c & \text{for } \mathrm{e}\end{cases}", MathS.Piecewise(("a", "b"), ("c", "e")));
        [Fact] public void Piecewise2()
            => Test(@"\begin{cases}a & \text{for } b\\c & \text{for } \mathrm{e}\\g & \text{otherwise}\end{cases}", MathS.Piecewise(("a", "b"), ("c", "e"), ("g", true)));
        [Fact] public void Piecewise3()
            => Test(@"\begin{cases}\left(x \mapsto \top \right) \quad \text{for} \quad a & \text{for } b\\\left(c \mapsto \top \right) & \text{for } \left(d \quad \text{for} \quad \mathrm{e}\right)\\g & \text{otherwise}\end{cases}", MathS.Piecewise((MathS.Provided(MathS.Lambda("x", Entity.Boolean.True), "a"), "b"), (MathS.Lambda("c", Entity.Boolean.True), MathS.Provided("d", "e")), ("g", true)));
        [Fact] public void Provided1()
            => Test(@"x \quad \text{for} \quad y", MathS.Provided("x", "y"));
        [Fact] public void Provided2()
            => Test(@"x+1 \quad \text{for} \quad y > 0", MathS.Provided("x + 1", "y > 0"));
        // Both of these are left-nested, and `provided` folds to the right -- so the flat form
        // these used to expect is read back as the *right*-nested tree, which is a different
        // expression. The grouping is the assertion; the two used to pin its absence.
        [Fact] public void Provided3()
            => Test(@"\left(a \quad \text{for} \quad b\right) \quad \text{for} \quad c", MathS.Provided(MathS.Provided("a", "b"), "c"));
        [Fact] public void Provided4()
            => Test(@"\left(\left(a \quad \text{for} \quad b\right) \quad \text{for} \quad c \quad \text{for} \quad d\right) \to \top ", MathS.Provided(MathS.Provided("a", "b"), MathS.Provided("c", "d")).Implies(Entity.Boolean.True));
        // Juxtaposition tests
        [Fact] public void M1InTheMiddle() => Test(@"x \left(-1\right) \cdot x", (x * (-1)) * x);
        [Fact] public void MultiplyNumberWithPower() => Test(@"2 \cdot {3}^{4}", 2 * ((Entity)3).Pow(4));
        [Fact] public void Pie1() => Test(@"p \cdot \mathrm{i}", (Entity)"p*i");
        [Fact] public void Pie2() => Test(@"\mathrm{i} p", (Entity)"i*p");
        [Fact] public void Pie3() => Test(@"\mathrm{e} \cdot \mathrm{i}", (Entity)"e*i");
        [Fact] public void Pie4() => Test(@"\mathrm{i} \cdot \mathrm{e}", (Entity)"i*e");
        [Fact] public void Pie5() => Test(@"p \cdot \mathrm{i} \cdot \mathrm{e}", (Entity)"p*i*e");
        [Fact] public void Pie6() => Test(@"p \mathrm{ie}", (Entity)"p*ie");
        [Fact] public void Pie7() => Test(@"\mathrm{\pi} \cdot \mathrm{e}", (Entity)"pi*e");
        [Fact] public void Pie8() => Test(@"\mathrm{pie}", (Entity)"pie");
        [Fact] public void Pie9() => Test(@"2 \mathrm{pie}", 2 * (Entity)"pie");
        [Fact] public void Pie10() => Test(@"\mathrm{pie} \cdot 2", (Entity)"pie" * 2);
        [Fact] public void Pie11() => Test(@"a \mathrm{pie}", "a" * (Entity)"pie");
        [Fact] public void Pie12() => Test(@"\mathrm{pie} a", (Entity)"pie" * "a");
        [Fact] public void Pie13() => Test(@"\mathrm{\pi} \cdot \mathrm{pie}", MathS.pi * (Entity)"pie");
        [Fact] public void Pie14() => Test(@"\mathrm{pie} \cdot \mathrm{\pi}", (Entity)"pie" * MathS.pi);
        [Fact] public void Pie15() => Test(@"\mathrm{pie} \cdot \mathrm{cake}", (Entity)"pie" * "cake");
        [Fact] public void Pie16() => Test(@"2 \mathrm{pie} \cdot 2", 2 * (Entity)"pie" * 2);
        [Fact] public void Pie17() => Test(@"2 \mathrm{pie} a", 2 * (Entity)"pie" * "a");
        [Fact] public void Pie18() => Test(@"a \mathrm{pie} \cdot 2", "a" * (Entity)"pie" * 2);
        [Fact] public void Pie19() => Test(@"a \mathrm{pie} a", "a" * (Entity)"pie" * "a");
        [Fact] public void Pie20() => Test(@"\mathrm{\pi} \cdot \mathrm{pie} \cdot 2", MathS.pi * (Entity)"pie" * 2);
        [Fact] public void Pie21() => Test(@"2 \mathrm{pie} \cdot \mathrm{\pi}", 2 * (Entity)"pie" * MathS.pi);
        [Fact] public void Pie22() => Test(@"\mathrm{\pi} \cdot \mathrm{pie} \cdot \mathrm{\pi}", MathS.pi * (Entity)"pie" * MathS.pi);
        [Fact] public void Pie23() => Test(@"\mathrm{\pi} \cdot \mathrm{pie} \cdot \mathrm{cake}", MathS.pi * (Entity)"pie" * "cake");
        [Fact] public void Pie24() => Test(@"\mathrm{cake} \cdot \mathrm{pie} \cdot \mathrm{\pi}", "cake" * (Entity)"pie" * MathS.pi);
        [Fact] public void Pie25() => Test(@"\mathrm{cake} \cdot \mathrm{pie} \cdot \mathrm{cake}", "cake" * (Entity)"pie" * "cake");
        [Fact] public void Pie26() => // separate non-subscript roman-style characters with \cdot, no separator otherwise
            Test(@"\mathrm{\pi}_{\mathrm{e}} \cdot \mathrm{ca}_{\mathrm{ke}} \cdot \mathrm{\pi} \cdot \mathrm{e} \cdot \mathrm{cake} p_{\mathrm{ie}} c_{a} k_{\mathrm{e}}", 
                (Entity)"pi_e" * "ca_ke" * "pi" * "e" * "cake" * "p_ie" * "c_a" * "k_e");
        [Fact] public void Juxtaposition1() => Test(@"\mathrm{var} \cdot 2", (Entity)"var*2");
        [Fact] public void Juxtaposition1Rev() => Test(@"2 \mathrm{var}", (Entity)"2*var");
        [Fact] public void Juxtaposition2() => Test(@"\mathrm{ei}", (Entity)"ei");
        [Fact] public void Juxtaposition3() => Test(@"\mathrm{e} \cdot \mathrm{i}", (Entity)"e*i");
        [Fact] public void Juxtaposition3Alt() => Test(@"\mathrm{e} \cdot \mathrm{i}", (Entity)"e" * Complex.Create(0, 1));
        [Fact] public void Juxtaposition4() => Test(@"{\mathrm{ei}}^{2}", (Entity)"ei^2");
        [Fact] public void Juxtaposition5() => Test(@"\mathrm{e} \cdot {\mathrm{i}}^{2}", (Entity)"e*i^2");
        [Fact] public void Juxtaposition5Alt() => Test(@"\mathrm{e} \cdot {\mathrm{i}}^{2}", (Entity)"e" * Complex.Create(0, 1).Pow(2));
        [Fact] public void Juxtaposition5Rev() => Test(@"{\mathrm{i}}^{2} \cdot \mathrm{e}", (Entity)"i^2*e");
        [Fact] public void Juxtaposition5RevAlt() => Test(@"{\mathrm{i}}^{2} \cdot \mathrm{e}", Complex.Create(0, 1).Pow(2) * "e");
        [Fact] public void Juxtaposition6() => Test(@"\mathrm{e} \left(2+\mathrm{i}\right)", (Entity)"e" * "2+i");
        [Fact] public void Juxtaposition6Alt() => Test(@"\mathrm{e} \left(2 + \mathrm{i}\right)", (Entity)"e" * Complex.Create(2, 1));
        [Fact] public void Juxtaposition6Rev() => Test(@"\left(2+\mathrm{i}\right) \cdot \mathrm{e}", "2+i" * (Entity)"e");
        [Fact] public void Juxtaposition6RevAlt() => Test(@"\left(2 + \mathrm{i}\right) \cdot \mathrm{e}", Complex.Create(2, 1) * (Entity)"e");
        [Fact] public void Juxtaposition7() => Test(@"\mathrm{e} {\left(2+\mathrm{i}\right)}^{2}", (Entity)"e" * ((Entity)"2+i").Pow(2));
        [Fact] public void Juxtaposition7Alt() => Test(@"\mathrm{e} {\left(2 + \mathrm{i}\right)}^{2}", (Entity)"e" * Complex.Create(2, 1).Pow(2));
        [Fact] public void Juxtaposition7Rev() => Test(@"{\left(2+\mathrm{i}\right)}^{2} \cdot \mathrm{e}", ((Entity)"2+i").Pow(2) * "e");
        [Fact] public void Juxtaposition7RevAlt() => Test(@"{\left(2 + \mathrm{i}\right)}^{2} \cdot \mathrm{e}", Complex.Create(2, 1).Pow(2) * "e");
        [Fact] public void Juxtaposition8() => Test(@"\mathrm{e} \cdot {\mathrm{owo}}^{2}", (Entity)"e" * ((Entity)"owo").Pow(2));
        [Fact] public void Juxtaposition8Rev() => Test(@"{\mathrm{owo}}^{2} \cdot \mathrm{e}", ((Entity)"owo").Pow(2) * "e");
        [Fact] public void Juxtaposition9() => Test(@"\mathrm{i} \cdot {\mathrm{owo}}^{2}", (Entity)"i" * ((Entity)"owo").Pow(2));
        [Fact] public void Juxtaposition9Rev() => Test(@"{\mathrm{owo}}^{2} \cdot \mathrm{i}", ((Entity)"owo").Pow(2) * "i");
        [Fact] public void Juxtaposition9Alt() => Test(@"\mathrm{i} \cdot {\mathrm{owo}}^{2}", Complex.Create(0, 1) * ((Entity)"owo").Pow(2));
        [Fact] public void Juxtaposition9RevAlt() => Test(@"{\mathrm{owo}}^{2} \cdot \mathrm{i}", ((Entity)"owo").Pow(2) * Complex.Create(0, 1));
        [Fact] public void Juxtaposition10() => Test(@"\mathrm{e} \cdot \mathrm{i}!", (Entity)"e" * ((Entity)"i").Factorial());
        [Fact] public void Juxtaposition10Alt() => Test(@"\mathrm{e} \cdot \mathrm{i}!", (Entity)"e" * Complex.Create(0, 1).Factorial());
        [Fact] public void Juxtaposition10Rev() => Test(@"\mathrm{i}! \cdot \mathrm{e}", ((Entity)"i").Factorial() * "e");
        [Fact] public void Juxtaposition10RevAlt() => Test(@"\mathrm{i}! \cdot \mathrm{e}", Complex.Create(0, 1).Factorial() * "e");
        [Fact] public void Juxtaposition11() => Test(@"\mathrm{e} \left({\mathrm{i}}^{2}\right)!", (Entity)"e" * ((Entity)"i^2").Factorial());
        [Fact] public void Juxtaposition11Alt() => Test(@"\mathrm{e} \left({\mathrm{i}}^{2}\right)!", (Entity)"e" * Complex.Create(0, 1).Pow(2).Factorial());
        [Fact] public void Juxtaposition11Rev() => Test(@"\left({\mathrm{i}}^{2}\right)! \cdot \mathrm{e}", ((Entity)"i^2").Factorial() * "e");
        [Fact] public void Juxtaposition11RevAlt() => Test(@"\left({\mathrm{i}}^{2}\right)! \cdot \mathrm{e}", Complex.Create(0, 1).Pow(2).Factorial() * "e");
        [Fact] public void Juxtaposition12() => Test(@"\mathrm{e} \cdot \mathrm{owo}!", (Entity)"e" * ((Entity)"owo").Factorial());
        [Fact] public void Juxtaposition12Rev() => Test(@"\mathrm{owo}! \cdot \mathrm{e}", ((Entity)"owo").Factorial() * "e");
        [Fact] public void Juxtaposition13() => Test(@"\mathrm{i} \left({\mathrm{owo}}^{2}\right)!", (Entity)"i" * ((Entity)"owo^2").Factorial());
        [Fact] public void Juxtaposition13Rev() => Test(@"\left({\mathrm{owo}}^{2}\right)! \cdot \mathrm{i}", ((Entity)"owo^2").Factorial() * "i");
        [Fact] public void Juxtaposition13Alt() => Test(@"\mathrm{i} \left({\mathrm{owo}}^{2}\right)!", Complex.Create(0, 1) * ((Entity)"owo^2").Factorial());
        [Fact] public void Juxtaposition13RevAlt() => Test(@"\left({\mathrm{owo}}^{2}\right)! \cdot \mathrm{i}", ((Entity)"owo^2").Factorial() * Complex.Create(0, 1));
        [Fact] public void Juxtaposition14() => Test(@"\mathrm{i} \cdot {\mathrm{owo}!}^{2}", (Entity)"i" * (Entity)"owo!^2");
        [Fact] public void Juxtaposition14Rev() => Test(@"{\mathrm{owo}!}^{2} \cdot \mathrm{i}", (Entity)"owo!^2" * "i");
        [Fact] public void Juxtaposition14Alt() => Test(@"\mathrm{i} \cdot {\mathrm{owo}!}^{2}", Complex.Create(0, 1) * (Entity)"owo!^2");
        [Fact] public void Juxtaposition14RevAlt() => Test(@"{\mathrm{owo}!}^{2} \cdot \mathrm{i}", (Entity)"owo!^2" * Complex.Create(0, 1));
        [Fact] public void Juxtaposition15() => Test(@"\mathrm{i} \cdot {\mathrm{i}!}^{2}", (Entity)"i" * (Entity)"i!^2");
        [Fact] public void Juxtaposition15Rev() => Test(@"{\mathrm{i}!}^{2} \cdot \mathrm{i}", (Entity)"i!^2" * "i");
        [Fact] public void Juxtaposition15Alt() => Test(@"\mathrm{i} \cdot {\mathrm{i}!}^{2}", Complex.Create(0, 1) * (Entity)"i!^2");
        [Fact] public void Juxtaposition15RevAlt() => Test(@"{\mathrm{i}!}^{2} \cdot \mathrm{i}", (Entity)"i!^2" * Complex.Create(0, 1));
        [Fact] public void Juxtaposition15AltAlt() => Test(@"\mathrm{i} \cdot {\mathrm{i}!}^{2}", Complex.Create(0, 1) * Complex.Create(0, 1).Factorial().Pow(2));
        [Fact] public void Juxtaposition15AltRevAlt() => Test(@"{\mathrm{i}!}^{2} \cdot \mathrm{i}", Complex.Create(0, 1).Factorial().Pow(2) * Complex.Create(0, 1));
        [Fact] public void Juxtaposition16() => Test(@"-\mathrm{i} \cdot {\mathrm{i}!}^{2}", (Entity)"-i" * (Entity)"i!^2");
        [Fact] public void Juxtaposition16Rev() => Test(@"{\mathrm{i}!}^{2} \left(-\mathrm{i}\right)", (Entity)"i!^2" * "-i");
        [Fact] public void Juxtaposition16Alt() => Test(@"-\mathrm{i} \cdot {\mathrm{i}!}^{2}", Complex.Create(0, -1) * (Entity)"i!^2");
        [Fact] public void Juxtaposition16RevAlt() => Test(@"{\mathrm{i}!}^{2} \left(-\mathrm{i}\right)", (Entity)"i!^2" * Complex.Create(0, -1));
        [Fact] public void Juxtaposition16AltAlt() => Test(@"-\mathrm{i} \cdot {\left(-\mathrm{i}\right)!}^{2}", Complex.Create(0, -1) * Complex.Create(0, -1).Factorial().Pow(2));
        [Fact] public void Juxtaposition16AltRevAlt() => Test(@"{\left(-\mathrm{i}\right)!}^{2} \left(-\mathrm{i}\right)", Complex.Create(0, -1).Factorial().Pow(2) * Complex.Create(0, -1));
        [Fact] public void MixedNumber() => Test(@"2 \cdot \frac{3}{4}", (Entity)"2" * "3/4");
        [Fact] public void MixedNumberAlt() => Test(@"2 \cdot \frac{3}{4}", (Entity)"2" * Rational.Create(3, 4));
        [Fact] public void MixedNumberRev() => Test(@"\frac{3}{4} \cdot 2", (Entity)"3/4" * "2");
        [Fact] public void MixedNumberRevAlt() => Test(@"\frac{3}{4} \cdot 2", Rational.Create(3, 4) * "2");
        [Fact] public void LongFraction() => Test(@"\frac{3}{4} \cdot \frac{3}{4}", (Entity)"3/4" * "3/4");
        [Fact] public void LongFraction2() => Test(@"\frac{3}{4} \cdot \frac{3}{4}", Rational.Create(3, 4) * "3/4");
        [Fact] public void LongFraction3() => Test(@"\frac{3}{4} \cdot \frac{3}{4}", "3/4" * Rational.Create(3, 4));
        [Fact] public void LongFraction4() => Test(@"\frac{3}{4} \cdot \frac{3}{4}", (Entity)Rational.Create(3, 4) * Rational.Create(3, 4));
        [Theory]
        [InlineData(@"2 \cdot 2", "2*2")]
        [InlineData(@"2\mathrm{i} \cdot 2", "2i*2")]
        [InlineData(@"2 \cdot 2\mathrm{i}", "2*2i")]
        [InlineData(@"2\mathrm{i} \cdot 2\mathrm{i}", "2i*2i")]
        [InlineData(@"-2 \cdot 2", "-2*2")]
        [InlineData(@"-2\mathrm{i} \cdot 2", "-2i*2")]
        [InlineData(@"-2 \cdot 2\mathrm{i}", "-2*2i")]
        [InlineData(@"-2\mathrm{i} \cdot 2\mathrm{i}", "-2i*2i")]
        [InlineData(@"2 \left(-2\right)", "2*-2")]
        [InlineData(@"2\mathrm{i} \left(-2\right)", "2i*-2")]
        [InlineData(@"2 \left(-2\mathrm{i}\right)", "2*-2i")]
        [InlineData(@"2\mathrm{i} \left(-2\mathrm{i}\right)", "2i*-2i")]
        [InlineData(@"-2 \left(-2\right)", "-2*-2")]
        [InlineData(@"-2\mathrm{i} \left(-2\right)", "-2i*-2")]
        [InlineData(@"-2 \left(-2\mathrm{i}\right)", "-2*-2i")]
        [InlineData(@"-2\mathrm{i} \left(-2\mathrm{i}\right)", "-2i*-2i")]
        [InlineData(@"1 \cdot 2", "1*2")]
        [InlineData(@"\mathrm{i} \cdot 2", "1i*2")]
        [InlineData(@"1 \cdot 2\mathrm{i}", "1*2i")]
        [InlineData(@"\mathrm{i} \cdot 2\mathrm{i}", "1i*2i")]
        [InlineData(@"-2", "-1*2")]
        [InlineData(@"-\mathrm{i} \cdot 2", "-1i*2")]
        [InlineData(@"-2\mathrm{i}", "-1*2i")]
        [InlineData(@"-\mathrm{i} \cdot 2\mathrm{i}", "-1i*2i")]
        [InlineData(@"1 \left(-2\right)", "1*-2")]
        [InlineData(@"\mathrm{i} \left(-2\right)", "i*-2")]
        [InlineData(@"1 \left(-2\mathrm{i}\right)", "1*-2i")]
        [InlineData(@"-\mathrm{i} \left(-2\mathrm{i}\right)", "-i*-2i")]
        [InlineData(@"-\mathrm{i} \cdot 2", "-i*2")]
        [InlineData(@"-\left(-2\right)", "-1*-2")]
        [InlineData(@"-\left(-2\mathrm{i}\right)", "-1*-2i")]
        [InlineData(@"-\mathrm{i} \left(-2\mathrm{i}\right)", "-1i*-2i")]
        [InlineData(@"1 \cdot 2!", "1*2!")]
        [InlineData(@"0 \cdot 2!", "0*2!")]
        [InlineData(@"-2!", "-1*2!")]
        [InlineData(@"-2 \cdot 2!", "-2*2!")]
        [InlineData(@"1 \left(-2\right)!", "1*(-2)!")]
        [InlineData(@"0 \left(-2\right)!", "0*(-2)!")]
        [InlineData(@"-\left(-2\right)!", "-1*(-2)!")]
        [InlineData(@"-2 \cdot \left(-2\right)!", "-2*(-2)!")]
        [InlineData(@"-\mathrm{i} \cdot 2!", "-1i*2!")]
        [InlineData(@"-2\mathrm{i} \cdot 2!", "-2i*2!")]
        public void JuxtapositionWithNumbers(string latex, string entity) {
            Test(latex, (Entity)entity);
            // Adding items after shouldn't affect LaTeX before
            Test(latex + (latex.EndsWith("2") ? "" : @" \cdot") + " x", (Entity)(entity + "*x"));
            Test(latex + @" \cdot 3", (Entity)(entity + "*3"));
            Test(latex + @" \cdot {3}^{2}", (Entity)(entity + "*3^2"));
            Test(latex + @" \cdot 3!", (Entity)(entity + "*3!"));
            Test(latex + @" \left(-1\right)", (Entity)(entity + "*-1"));
            Test(latex + @" \left(-1\right) \cdot 1!", (Entity)(entity + "*-1!"));
            Test(latex + @" \left(-\mathrm{i}\right)", (Entity)(entity + "*-i"));
            Test(latex + @" \left(-1\right) \cdot \mathrm{i}!", (Entity)(entity + "*-i!"));
            Test(latex + @" \cdot {3!}^{4}", (Entity)(entity + "*3!^4"));
            // TODO: Check this case again later
            Test(latex + (latex.EndsWith(")") || latex.EndsWith("}") ? @" \cdot" : "") + @" \left({2}^{3}\right)!", (Entity)(entity + "*(2^3)!"));
        }

        // Tests for intervals with commas (ISO 80000-2)
        [Fact] public void IntervalWithVariables() => Test(@"\left[x, y\right]", MathS.Sets.Interval(x, true, x + 1, true).Substitute(x + 1, MathS.Var("y")));
        [Fact] public void IntervalWithPi() => Test(@"\left[0, \mathrm{\pi}\right]", MathS.Sets.Interval(0, true, MathS.pi, true));
        
        // Lambda calculus tests
        [Fact] public void Lambda1()
            => Test(@"x \mapsto x", x.LambdaOver(MathS.Var("x")));
        [Fact] public void Lambda2()
            => Test(@"x \mapsto {x}^{2}", MathS.Sqr(x).LambdaOver(MathS.Var("x")));
        [Fact] public void Lambda3()
        {
            var y = MathS.Var("y");
            Test(@"x \mapsto y \mapsto x+y", (x + y).LambdaOver((Entity.Variable)y).LambdaOver((Entity.Variable)x));
        }
        [Fact] public void LambdaWithPriority()
            => Test(@"x \mapsto x+1", (x + 1).LambdaOver(MathS.Var("x")));
            
        // Application tests
        [Fact] public void Application1()
            => Test(@"f\left(x\right)", MathS.Var("f").Apply(x));
        [Fact] public void Application2()
            => Test(@"f\left(x\right)\left(y\right)", MathS.Var("f").Apply(x, MathS.Var("y")));
        [Fact] public void Application3()
            => Test(@"f\left(x\right)\left(y\right)\left(z\right)", MathS.Var("f").Apply(x, MathS.Var("y"), MathS.Var("z")));
        [Fact] public void ApplicationLambda()
            => Test(@"\left(x \mapsto {x}^{2}\right)\left(3\right)", MathS.Sqr(x).LambdaOver((Entity.Variable)x).Apply(3));
            
        // Boolean operations
        [Fact] public void BooleanTrue()
            => Test(@"\top ", Entity.Boolean.True);
        [Fact] public void BooleanFalse()
            => Test(@"\bot ", Entity.Boolean.False);
        [Fact] public void BooleanAnd()
            => Test(@"x \land y", x & MathS.Var("y"));
        [Fact] public void BooleanOr()
            => Test(@"x \lor y", x | MathS.Var("y"));
        [Fact] public void BooleanXor()
            => Test(@"x \veebar y", x ^ MathS.Var("y"));
        [Fact] public void BooleanNot()
            => Test(@"\neg{x}", !x);
        [Fact] public void BooleanImplies()
            => Test(@"x \to y", x.Implies(MathS.Var("y")));
            
        // Comparison operations
        [Fact] public void Greater()
            => Test(@"x > y", x > MathS.Var("y"));
        [Fact] public void GreaterOrEqual()
            => Test(@"x \geq y", x >= MathS.Var("y"));
        [Fact] public void Less()
            => Test(@"x < y", x < MathS.Var("y"));
        [Fact] public void LessOrEqual()
            => Test(@"x \leq y", x <= MathS.Var("y"));
        [Fact] public void EqualsOperator()
            => Test(@"x = y", x.EqualTo(MathS.Var("y")));
            
        // Set operations
        [Fact] public void SetUnion()
            => Test(@"\left\{ 1, 2 \right\} \cup \left\{ 3, 4 \right\}", MathS.Sets.Finite(1, 2).Unite(MathS.Sets.Finite(3, 4)));
        [Fact] public void SetIntersection()
            => Test(@"\left\{ 1, 2 \right\} \cap \left\{ 2, 3 \right\}", MathS.Sets.Finite(1, 2).Intersect(MathS.Sets.Finite(2, 3)));
        [Fact] public void SetMinus()
            => Test(@"\left\{ 1, 2, 3 \right\} \setminus \left\{ 2 \right\}", MathS.Sets.Finite(1, 2, 3).SetSubtract(MathS.Sets.Finite(2)));
        [Fact] public void SetIn()
            => Test(@"x \in \left\{ 1, 2, 3 \right\}", x.In(MathS.Sets.Finite(1, 2, 3)));

        // Associativity, which LaTeX has to express too: CSharpMath.Evaluation reads the LaTeX
        // back, and it folds \cup, \setminus and \in to the left at the precedences the grammar
        // here uses, so a right operand at the same level must be bracketed or it comes back as
        // the outer operator. \to is the exception -- CSharpMath folds it to the *right*, the
        // usual convention for implication, so it is the assumption that gets the brackets.
        private static readonly Entity A = MathS.Sets.Finite(1, 2), B = MathS.Sets.Finite(2, 3), C = MathS.Sets.Finite(3);
        private static readonly string As = @"\left\{ 1, 2 \right\}", Bs = @"\left\{ 2, 3 \right\}", Cs = @"\left\{ 3 \right\}";

        [Fact] public void SetMinusOfSetMinusOnTheRight()
            => Test($@"{As} \setminus \left({Bs} \setminus {Cs}\right)", A.SetSubtract(B.SetSubtract(C)));
        [Fact] public void SetMinusOfSetMinusOnTheLeft()
            => Test($@"{As} \setminus {Bs} \setminus {Cs}", A.SetSubtract(B).SetSubtract(C));
        [Fact] public void SetMinusOfUnionOnTheRight()
            => Test($@"{As} \setminus \left({Bs} \cup {Cs}\right)", A.SetSubtract(B.Unite(C)));
        [Fact] public void UnionOfSetMinusOnTheRight()
            => Test($@"{As} \cup \left({Bs} \setminus {Cs}\right)", A.Unite(B.SetSubtract(C)));
        [Fact] public void UnionOfUnionOnTheRightStaysFlat()
            => Test($@"{As} \cup {Bs} \cup {Cs}", A.Unite(B.Unite(C)));
        [Fact] public void IntersectionOfIntersectionOnTheRightStaysFlat()
            => Test($@"{As} \cap {Bs} \cap {Cs}", A.Intersect(B.Intersect(C)));
        [Fact] public void InOfInOnTheRight()
            => Test(@"x \in \left(y \in z\right)", x.In(MathS.Var("y").In(MathS.Var("z"))));
        [Fact] public void ImpliesOfImpliesOnTheRightStaysFlat()
            => Test(@"x \to y \to z", x.Implies(MathS.Var("y").Implies(MathS.Var("z"))));
        [Fact] public void ImpliesOfImpliesOnTheLeft()
            => Test(@"\left(x \to y\right) \to z", x.Implies(MathS.Var("y")).Implies(MathS.Var("z")));
        [Fact] public void ModAsANonLeadingFactor()
            => Test(@"x \left(y \bmod z\right)", x * (MathS.Var("y") % MathS.Var("z")));
        [Fact] public void ModAsTheLeadingFactorStaysFlat()
            => Test(@"x \bmod y \cdot z", (x % MathS.Var("y")) * MathS.Var("z"));
            
        // Special sets
        [Fact] public void SetBooleans()
            => Test(@"\mathbb{B}", (Entity)Domain.Boolean);
        [Fact] public void SetRationals()
            => Test(@"\mathbb{Q}", (Entity)Domain.Rational);
        [Fact] public void SetComplexes()
            => Test(@"\mathbb{C}", (Entity)Domain.Complex);
            
        // Addition/Subtraction edge cases
        [Fact] public void AddNegativeNumber()
            => Test(@"x-1", x + (Entity)(-1));
        [Fact] public void SubtractNegativeNumber()
            => Test(@"x-\left(-1\right)", x - (Entity)(-1));
            
        // Complex edge cases
        [Fact] public void ComplexZeroReal()
            => Test(@"\mathrm{i}", Complex.Create(0, 1));
        [Fact] public void ComplexZeroImaginary()
            => Test(@"1", Complex.Create(1, 0));
        [Fact] public void ComplexBothPositive()
            => Test(@"1 + \mathrm{i}", Complex.Create(1, 1));
        [Fact] public void ComplexBothNegative()
            => Test(@"-1 - \mathrm{i}", Complex.Create(-1, -1));
            
        // Power edge cases
        [Fact] public void PowerNegativeBase()
            => Test(@"{\left(-2\right)}^{3}", MathS.Pow(-2, 3));
        [Fact] public void PowerComplexBase()
            => Test(@"{\left(1 + \mathrm{i}\right)}^{2}", MathS.Pow(Complex.Create(1, 1), 2));
        [Fact] public void PowerFractionExponent()
            => Test(@"\sqrt[4]{x}^{3}", MathS.Pow(x, Rational.Create(3, 4)));
        [Fact] public void PowerNegativeFraction()
            => Test(@"\frac{1}{\sqrt[4]{x}^{3}}", MathS.Pow(x, Rational.Create(-3, 4)));
            
        // Trigonometric hyperbolic functions
        [Fact] public void Sinh()
            => TestSimplify(@"\frac{{\mathrm{e}}^{x}-{\mathrm{e}}^{-x}}{2}", MathS.Hyperbolic.Sinh(x));
        [Fact] public void Cosh()
            => TestSimplify(@"\frac{{\mathrm{e}}^{x}+{\mathrm{e}}^{-x}}{2}", MathS.Hyperbolic.Cosh(x));
        [Fact] public void Tanh()
            => TestSimplify(@"\frac{{\mathrm{e}}^{2 x}-1}{{\mathrm{e}}^{2 x}+1}", MathS.Hyperbolic.Tanh(x));
        [Fact] public void Cotanh()
            => TestSimplify(@"\frac{{\mathrm{e}}^{2 x}+1}{{\mathrm{e}}^{2 x}-1}", MathS.Hyperbolic.Cotanh(x));
        [Fact] public void Sech()
            => TestSimplify(@"\frac{2}{{\mathrm{e}}^{x}+{\mathrm{e}}^{-x}}", MathS.Hyperbolic.Sech(x));
        [Fact] public void Cosech()
            => TestSimplify(@"\frac{2}{{\mathrm{e}}^{x}-{\mathrm{e}}^{-x}}", MathS.Hyperbolic.Cosech(x));
        [Fact] public void Arsinh()
            => TestSimplify(@"\ln\left(x+\sqrt{1+{x}^{2}}\right)", MathS.Hyperbolic.Arsinh(x));
        [Fact] public void Arcosh()
            => TestSimplify(@"\ln\left(x+\sqrt{x+1} \sqrt{x-1}\right)", MathS.Hyperbolic.Arcosh(x));
        [Fact] public void Artanh()
            => TestSimplify(@"\frac{\ln\left(\frac{2}{1-x}-1\right)}{2}", MathS.Hyperbolic.Artanh(x));
        [Fact] public void Arcotanh()
            => TestSimplify(@"\frac{\ln\left(1+\frac{2}{x-1}\right)}{2}", MathS.Hyperbolic.Arcotanh(x));
        [Fact] public void Arsech()
            => TestSimplify(@"\ln\left(\frac{1}{x}+\sqrt{\frac{1}{x}+1} \sqrt{\frac{1}{x}-1}\right)", MathS.Hyperbolic.Arsech(x));
        [Fact] public void Arcosech()
            => TestSimplify(@"\ln\left(\frac{1}{x}+\sqrt{1+\frac{1}{{x}^{2}}}\right)", MathS.Hyperbolic.Arcosech(x));

        // Priority tests for nested boolean expressions
        [Fact] public void BooleanAndOr()
            => Test(@"x \land y \lor z", (x & MathS.Var("y")) | MathS.Var("z"));
        [Fact] public void BooleanOrAnd()
            => Test(@"\left(x \lor y\right) \land z", (x | MathS.Var("y")) & MathS.Var("z"));
        [Fact] public void BooleanNotAnd()
            => Test(@"\neg{x} \land y", (!x) & MathS.Var("y"));
        [Fact] public void BooleanNotOr()
            => Test(@"\neg{x} \lor y", (!x) | MathS.Var("y"));
        [Fact] public void BooleanImpliesAnd()
            => Test(@"x \to y \land z", x.Implies(MathS.Var("y") & MathS.Var("z")));
        [Fact] public void BooleanAndImplies()
            => Test(@"x \land y \to z", (x & MathS.Var("y")).Implies(MathS.Var("z")));
        [Fact] public void BooleanComplexNesting()
            => Test(@"x \land y \lor z \land w", (x & MathS.Var("y")) | (MathS.Var("z") & MathS.Var("w")));
            
        // Priority tests for comparison operations
        [Fact] public void ComparisonAndBoolean()
            => Test(@"x > 0 \land y < 0", (x > (Entity)0) & (MathS.Var("y") < (Entity)0));
        [Fact] public void ComparisonOrBoolean()
            => Test(@"x > 0 \lor y < 0", (x > (Entity)0) | (MathS.Var("y") < (Entity)0));
            
        // Priority tests for mixed boolean and comparison operations
        [Fact] public void EqualityAndBoolean()
            => Test(@"x = y \land z = w", x.EqualTo(MathS.Var("y")) & (MathS.Var("z").EqualTo(MathS.Var("w"))));
        [Fact] public void EqualityOrBoolean()
            => Test(@"x = y \lor z = w", x.EqualTo(MathS.Var("y")) | (MathS.Var("z").EqualTo(MathS.Var("w"))));
        [Fact] public void InequalityChain()
            => Test(@"x < y < z < w", 
                (x < MathS.Var("y")) & (MathS.Var("y") < MathS.Var("z")) & (MathS.Var("z") < MathS.Var("w")));
        [Fact] public void InequalityChainAlt() => Test(@"x < y < z < w", x < "y" < "z" < "w");
        [Fact] public void InequalityChainString() => Test(@"x < y < z < w", (Entity)"x<y<z<w");
        [Fact] public void InequalityChainParenthesized()
            => Test(@"\left(x < y\right) < \left(z < w\right)", new Entity.Lessf(x < "y", (Entity)"z" < "w"));
        [Fact] public void EqualityChain()
            => Test(@"x = y = z = w",
                x.EqualTo(MathS.Var("y")) & (MathS.Var("y").EqualTo(MathS.Var("z"))) & MathS.Var("z").EqualTo(MathS.Var("w")));
        [Fact] public void EqualityChainAlt()
            => Test(@"x = y = z = w", x.Equalizes("y").Equalizes("z").Equalizes("w"));
        [Fact] public void EqualityChainString() => Test(@"x = y = z = w", (Entity)"x=y=z=w");
        [Fact] public void EqualityChainParenthesized()
            => Test(@"\left(x = y\right) = \left(z = w\right)", x.EqualTo("y").EqualTo(((Entity)"z").EqualTo("w")));
        [Fact] public void EqualityInequalityChain()
            => Test(@"x \geq y = z < w",
                (x >= MathS.Var("y")) & (MathS.Var("y").EqualTo(MathS.Var("z"))) & (MathS.Var("z") < MathS.Var("w")));
        [Fact] public void EqualityInequalityChainAlt() => Test(@"x \geq y = z < w", (x >= "y").Equalizes("z") < "w");
        [Fact] public void EqualityInequalityChainString1() => Test(@"\left(x \geq y = z\right) < w", (Entity)"(x>=y=z)<w");
        [Fact] public void EqualityInequalityChainString2() => Test(@"x \geq y = \left(z < w\right)", (Entity)"x>=y=(z<w)");
        [Fact] public void EqualityInequalityChainString3() => Test(@"x \geq y = z < w", (Entity)"x>=y=z<w");
        [Fact] public void EqualityInequalityChainParenthesized() => Test(@"x \geq \left(y = \left(z < w\right)\right)", new Entity.GreaterOrEqualf(x, new Entity.Equalsf("y", (Entity)"z" < "w")));
        [Fact] public void ParenthesizedComparisons()
            => Test(@"\left(x < x\right) \geq \left(x > \left(\left(x = x\right) \leq x\right)\right)",
#pragma warning disable 1718 // Disable self-comparison warning
                new Entity.GreaterOrEqualf(x < x, (x > new Entity.LessOrEqualf(new Entity.Equalsf(x, x), x))));
#pragma warning restore 1718
        [Fact] public void NotEqual() => Test(@"1 \neq 2", (Entity)"not 1 = 2");
        [Fact] public void ChainedNotEqual() => Test(@"1 \neq 2 \neq 3 = 4 \land 5", (Entity)"not 1 = 2 and not 2 = 3 and 3 = 4 and 5");
        [Fact] public void MixedChainedComparison() => Test(@"1 \land 2 > 3 < 4 \land \left(\top  \lor \bot \right) \land 5 \neq x = x \land x", (Entity)"1 and 2 > 3 and 3 < 4 and (true or false) and not 5 = x and x = x and x");
        [Fact] public void ImpliesChain()
            => Test(@"\left(\left(x \to y\right) \to z\right) \to x \to y \to z", x.Implies("y").Implies("z").Implies(x.Implies(MathS.Var("y").Implies("z"))));
    }
}

