//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using System;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using static AngouriMath.Entity;

namespace AngouriMath.Core.Compilation.IntoLinq
{
    /// <summary>
    /// This class describes all the type conversions for 
    /// </summary>
#pragma warning disable SealedOrAbstract
    public partial record CompilationProtocol
#pragma warning restore SealedOrAbstract
    {

        
        
        


        /// <summary>
        /// This treats any number as <see cref="Complex"/> and any boolean as <see cref="bool"/>
        /// </summary>
        public virtual Expression ConvertConstant(Entity e)
        {
            
            return e switch
            {
                Number n => Expression.Constant(DownCast(n)),
                Entity.Boolean b => Expression.Constant((bool)b),
                _ => throw new AngouriBugException("Undefined constant type")
            };
        }
        
        /// <summary>
        /// This provides an appropriate representation of NaN using the given type
        /// </summary>
        public virtual Expression ConvertNaN(Type type)
        {
            
            type = Nullable.GetUnderlyingType(type) ?? type;
            
            // A statement with no truth value is null, as Kleene's logic has it: the NaN of a provided
            // statement where its predicate fails. https://github.com/asc-community/AngouriMath/issues/2224
            if (type == typeof(bool))
                return Expression.Constant(null, typeof(bool?));
            if (type == typeof(System.Numerics.Complex))
                return Expression.Constant(new System.Numerics.Complex(double.NaN, 0));
            if (type == typeof(double))
                return Expression.Constant(double.NaN);
            if (type == typeof(float))
                return Expression.Constant(float.NaN);
            if (type == typeof(long))
                return Expression.Constant(null, typeof(long?));
            if (type == typeof(BigInteger))
                return Expression.Constant(null, typeof(BigInteger?));
            if (type == typeof(int))
                return Expression.Constant(null, typeof(int?));
            if (type == typeof(object))
                return Expression.Constant(null, typeof(object));
            throw new InvalidProtocolProvided($"NaN conversion not implemented for {type}");
        }

        /// <summary>
        /// Converts an expression to a different type
        /// </summary>
        public virtual Expression ConvertType(Expression expr, Type type)
        {
            
            if (expr.Type == type)
                return expr;
            // A statement with no truth value is null as a bool?, and as a bool is not one that
            // holds. https://github.com/asc-community/AngouriMath/issues/2195
            if (expr.Type == typeof(bool?) && type == typeof(bool))
                return Expression.Call(ThreeValuedLogic.HoldsMethod, expr);
            if (expr.Type == typeof(bool) && type == typeof(bool?))
                return Expression.Convert(expr, type);
            // A statement as a number is its truth value: 1 where it holds, 0 where it does not and NaN where
            // it has none, as FastExpression and an Iverson bracket give it. It threw InvalidProtocolProvided,
            // there being no NaN of a bool. https://github.com/asc-community/AngouriMath/issues/2224
            if ((expr.Type == typeof(bool) || expr.Type == typeof(bool?)) && typeLevelInHierarchy.ContainsKey(Nullable.GetUnderlyingType(type) ?? type))
            {
                if (expr.Type == typeof(bool?) && Nullable.GetUnderlyingType(type) is null && ((ConstantExpression)ConvertNaN(type)).Value is null && type != typeof(object))
                    throw new UncompilableNodeException($"A statement that may have no truth value cannot be compiled as {type}, which has no NaN");
                return ConvertType(Expression.Call(ThreeValuedLogic.IversonMethod, ConvertType(expr, typeof(bool?))), type);
            }
            // A tensor converts to nothing but itself: a matrix over one element type is not
            // a matrix over another, and it is not a number.
            if (TensorCompilation.IsTensor(expr.Type) || TensorCompilation.IsTensor(type))
                throw new UncompilableNodeException($"A value of type {expr.Type} cannot be compiled as {type}");
                
            bool exprNullable = (Nullable.GetUnderlyingType(expr.Type) is not null) || (expr.Type == typeof(object));
            bool typeNullable = (Nullable.GetUnderlyingType(type) is not null) || (type == typeof(object));
            bool exprNaNisNaN = (((ConstantExpression)ConvertNaN(expr.Type)).Value is not null);
            bool typeNaNisNaN = (((ConstantExpression)ConvertNaN(type)).Value is not null);
            
            // ex. long? -> double
            if (exprNullable && typeNaNisNaN)
            {
                return Expression.Condition(Expression.Equal(expr, Expression.Constant(null, expr.Type)),
                                                             ConvertNaN(type),
                                                             Coerce(expr, type));
            }
            
            // ex. double -> long?
            if (!exprNullable && exprNaNisNaN && typeNullable && !typeNaNisNaN)
            {
                if (!nanChecks.TryGetValue(expr.Type, out var isNaN))
                    throw new AngouriBugException($"IsNaN method expected for type {expr.Type}");

                return Expression.Condition(Expression.Call(isNaN, expr),
                                            Expression.Constant(null, type),
                                            Coerce(expr, type));
            }

            return Coerce(expr, type);
        }
        
        /// <summary>
        /// This is a default converter for unary nodes (for those inherited from <see cref="IUnaryNode"/>)
        /// </summary>
        public virtual Expression ConvertUnaryNode(Expression e, Entity typeHolder)
        {
            e = NumericOperand(e);
            // A function of a matrix has no compiled form here: the tensor operations are the
            // arithmetic ones, and sin(A) is not an element-by-element sine of a matrix.
            if (TensorCompilation.IsTensor(e.Type))
                throw new UncompilableNodeException(
                    $"There is no compiled form for {typeHolder.GetType().Name} of a matrix.");
            return typeHolder switch
            {
                Sinf      when ShouldBeAtLeastDouble(e) is var newE  => Expression.Call(GetDef("Sin", 1, newE.Type), newE),
                Cosf      when ShouldBeAtLeastDouble(e) is var newE  => Expression.Call(GetDef("Cos", 1, newE.Type), newE),
                Tanf      when ShouldBeAtLeastDouble(e) is var newE  => Expression.Call(GetDef("Tan", 1, newE.Type), newE),
                Cotanf    when ShouldBeAtLeastDouble(e) is var newE  => Expression.Call(GetDef("Cot", 1, newE.Type), newE),
                Secantf   when ShouldBeAtLeastDouble(e) is var newE  => Expression.Call(GetDef("Sec", 1, newE.Type), newE),
                Cosecantf when ShouldBeAtLeastDouble(e) is var newE  => Expression.Call(GetDef("Csc", 1, newE.Type), newE),
                
                Arcsinf      when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Asin", 1, newE.Type), newE),
                Arccosf      when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Acos", 1, newE.Type), newE),
                Arctanf      when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Atan", 1, newE.Type), newE),
                Arccotanf    when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Acot", 1, newE.Type), newE),
                Arcsecantf   when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Asec", 1, newE.Type), newE),
                Arccosecantf when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Acsc", 1, newE.Type), newE),
                
                Absf =>         Expression.Call(GetDef("Abs", 1, e.Type), e),
                Signumf =>      Expression.Call(GetDef("Sgn", 1, e.Type), e),
                Floorf =>       Expression.Call(GetDef("Floor", 1, e.Type), e),
                Ceilf =>        Expression.Call(GetDef("Ceil", 1, e.Type), e),
                Roundf =>       Expression.Call(GetDef("Round", 1, e.Type), e),

                // The special functions, widened to double first as the trigonometry is.
                // https://github.com/asc-community/AngouriMath/issues/1607
                Erff  when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Erf", 1, newE.Type), newE),
                Erfcf when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Erfc", 1, newE.Type), newE),
                Erfif when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Erfi", 1, newE.Type), newE),
                Eif   when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Ei", 1, newE.Type), newE),
                Lif   when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Li", 1, newE.Type), newE),
                Sif   when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Si", 1, newE.Type), newE),
                Cif   when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Ci", 1, newE.Type), newE),
                Shif  when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Shi", 1, newE.Type), newE),
                Chif  when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Chi", 1, newE.Type), newE),
                Factorialf when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Factorial", 1, newE.Type), newE),
                DoubleFactorialf when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("DoubleFactorial", 1, newE.Type), newE),
                CatalanNumberf when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("CatalanNumber", 1, newE.Type), newE),
                // The number-theoretic functions, exact at a whole argument and NaN at any other.
                // https://github.com/asc-community/AngouriMath/issues/2195
                Mobiusf when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Mobius", 1, newE.Type), newE),
                PrimeNuf when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("PrimeNu", 1, newE.Type), newE),
                PrimeOmegaf when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("PrimeOmega", 1, newE.Type), newE),
                Primef when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Prime", 1, newE.Type), newE),
                PrimePif when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("PrimePi", 1, newE.Type), newE),
                Phif =>         Expression.Call(GetDef("Phi", 1, e.Type), e),

                // The conjugate, the argument and the parts, widened to double as the special
                // functions are; an Iverson bracket is 1 where its statement holds and 0 where it
                // does not. https://github.com/asc-community/AngouriMath/issues/2195
                Conjugatef when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Conj", 1, newE.Type), newE),
                Argf       when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Arg", 1, newE.Type), newE),
                Ref        when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Re", 1, newE.Type), newE),
                Imf        when ShouldBeAtLeastDouble(e) is var newE => Expression.Call(GetDef("Im", 1, newE.Type), newE),
                Iversonf when e.Type == typeof(bool) => Expression.Condition(e, Expression.Constant(1), Expression.Constant(0)),
                Iversonf when e.Type == typeof(bool?) => Expression.Call(ThreeValuedLogic.IversonMethod, e),
                
                Notf when e.Type == typeof(bool?) => Expression.Call(ThreeValuedLogic.NotMethod, e),
                Notf =>         Expression.Not(e),
                
                // A node this converter has no case for has no compiled form, and that is a gap
                // in coverage rather than an internal error. It threw AngouriBugException, which
                // asks the caller to report a bug -- so compiling floor(x), ceil(x), round(x),
                // x!, gamma(x) or phi(x) told the user to file an issue for something the
                // library already knows it cannot do. UncompilableNodeException is the exception
                // documented for exactly this, and #872 moved the other known gaps off
                // AngouriBugException for the same reason.
                // https://github.com/asc-community/AngouriMath/issues/894
                _ => throw new UncompilableNodeException(
                    $"There is no compiled form for {typeHolder.GetType().Name}. "
                    + "Define a CompilationProtocol which overrides ConvertUnaryNode to add one.")
            };
        }

        // A number that may have no value, an Iverson bracket's int? or a provided integer's long?, as a double, which
        // has NaN for none: no function takes the nullable integer, and widening one threw KeyNotFoundException or
        // AngouriBugException. A statement's bool? stays itself. https://github.com/asc-community/AngouriMath/issues/2237
        private Expression NumericOperand(Expression expr)
            => Nullable.GetUnderlyingType(expr.Type) is { } underlying && underlying != typeof(bool)
                && underlying != typeof(System.Numerics.Complex) && underlying != typeof(double)
                ? ConvertType(expr, typeof(double))
                : expr;

        private Expression ShouldBeAtLeastDouble(Expression expr)
        {
            if (typeLevelInHierarchy[expr.Type] < typeLevelInHierarchy[typeof(double)])
                return ConvertType(expr, typeof(double));
            return expr;
        }

        /// <summary>
        /// This is a default converter for binary nodes (for those inherited from <see cref="IBinaryNode"/>)
        /// </summary>
        public virtual Expression ConvertBinaryNode(Expression left, Expression right, Entity typeHolder)
        {
            // A matrix in either place: the tensor operation of the same name, the scalar side
            // converted to the element type. https://github.com/asc-community/AngouriMath/issues/526
            if (TensorCompilation.IsTensor(left.Type) || TensorCompilation.IsTensor(right.Type))
                return TensorCompilation.Binary(typeHolder, left, right, ConvertType)
                    ?? throw new UncompilableNodeException(
                        $"The node of type {typeHolder.GetType()} has no compiled form over a matrix.");

            (left, right) = EqualizeTypesIfAble(NumericOperand(left), NumericOperand(right));
            return typeHolder switch
            {
                Sumf => Operator(ExpressionType.Add, left, right),
                Minusf => Operator(ExpressionType.Subtract, left, right),
                Mulf => Operator(ExpressionType.Multiply, left, right),
                // Over the reals by MathAllMethods.Divide, which gives a division by zero no value, as
                // evaluation does. https://github.com/asc-community/AngouriMath/issues/2286
                Divf when ShouldBeAtLeastDouble(left) is var newLeft && ShouldBeAtLeastDouble(right) is var newRight
                          && (newLeft.Type == typeof(double) || newLeft.Type == typeof(float)) && newLeft.Type == newRight.Type
                    => Expression.Call(GetDef("Divide", 2, newLeft.Type), newLeft, newRight),
                Divf => Operator(ExpressionType.Divide, ShouldBeAtLeastDouble(left), ShouldBeAtLeastDouble(right)),
                // ((a % b) + b) % b, and not the bare a % b: the runtime truncates, where the
                // node is the floored remainder that takes the sign of the divisor, and this is
                // the shortest expression that turns the one into the other for every pair of
                // signs. Not widened to double the way division is -- the remainder of two
                // integers is an integer, and widening would only lose that.
                // Over the complex numbers the remainder FastExpression takes, the floored one where
                // both are real and NaN where either is not, as the interpreter leaves it unevaluated:
                // the operator is not defined for System.Numerics.Complex, and this was refused.
                // https://github.com/asc-community/AngouriMath/issues/2195
                Modf when left.Type == typeof(System.Numerics.Complex) && right.Type == typeof(System.Numerics.Complex)
                    => Expression.Call(complexRemainder, left, right),
                Modf => Operator(ExpressionType.Modulo,
                                 Operator(ExpressionType.Add, Operator(ExpressionType.Modulo, left, right), right),
                                 right),
                Powf when 
                    ShouldBeAtLeastDouble(left) is var newLeft 
                    && ShouldBeAtLeastDouble(right) is var newRight
                    => Expression.Call(GetDef("Pow", 2, newLeft.Type), newLeft, newRight),
                Logf when
                    ShouldBeAtLeastDouble(left) is var newLeft
                    && ShouldBeAtLeastDouble(right) is var newRight
                    => Expression.Call(GetDef("Log", 2, newRight.Type), newLeft, newRight),
                // The binomial coefficient and the factorial powers, widened to double first as the
                // special functions are. https://github.com/asc-community/AngouriMath/issues/2195
                Binomialf when ShouldBeAtLeastDouble(left) is var newLeft && ShouldBeAtLeastDouble(right) is var newRight
                    => Expression.Call(GetDef("Binomial", 2, newLeft.Type), newLeft, newRight),
                FallingFactorialf when ShouldBeAtLeastDouble(left) is var newLeft && ShouldBeAtLeastDouble(right) is var newRight
                    => Expression.Call(GetDef("FallingFactorial", 2, newLeft.Type), newLeft, newRight),
                RisingFactorialf when ShouldBeAtLeastDouble(left) is var newLeft && ShouldBeAtLeastDouble(right) is var newRight
                    => Expression.Call(GetDef("RisingFactorial", 2, newLeft.Type), newLeft, newRight),
                LambertWf when ShouldBeAtLeastDouble(left) is var newLeft && ShouldBeAtLeastDouble(right) is var newRight
                    => Expression.Call(GetDef("LambertW", 2, newLeft.Type), newLeft, newRight),
                Polygammaf when ShouldBeAtLeastDouble(left) is var newLeft && ShouldBeAtLeastDouble(right) is var newRight
                    => Expression.Call(GetDef("Polygamma", 2, newLeft.Type), newLeft, newRight),
                HarmonicNumberf when ShouldBeAtLeastDouble(left) is var newLeft && ShouldBeAtLeastDouble(right) is var newRight
                    => Expression.Call(GetDef("HarmonicNumber", 2, newLeft.Type), newLeft, newRight),
                DivisorSigmaf when ShouldBeAtLeastDouble(left) is var newLeft && ShouldBeAtLeastDouble(right) is var newRight
                    => Expression.Call(GetDef("DivisorSigma", 2, newLeft.Type), newLeft, newRight),
                Valuationf when ShouldBeAtLeastDouble(left) is var newLeft && ShouldBeAtLeastDouble(right) is var newRight
                    => Expression.Call(GetDef("Valuation", 2, newLeft.Type), newLeft, newRight),
                Dividesf when ShouldBeAtLeastDouble(left) is var newLeft && ShouldBeAtLeastDouble(right) is var newRight
                    => newLeft.Type == typeof(System.Numerics.Complex)
                        ? Expression.Call(complexDivisibility, newLeft, ConvertType(newRight, typeof(System.Numerics.Complex)))
                        : Expression.Call(realDivisibility, ConvertType(newLeft, typeof(double)), ConvertType(newRight, typeof(double))),
                Maxf => Expression.Call(GetDef("Max", 2, left.Type), left, right),
                Minf => Expression.Call(GetDef("Min", 2, left.Type), left, right),

                // A connective of a statement that may have no truth value follows Kleene's tables.
                // https://github.com/asc-community/AngouriMath/issues/2195
                Andf when IsUndecidable(left, right) => Expression.Call(ThreeValuedLogic.AndMethod, Undecidable(left), Undecidable(right)),
                Orf when IsUndecidable(left, right) => Expression.Call(ThreeValuedLogic.OrMethod, Undecidable(left), Undecidable(right)),
                Xorf when IsUndecidable(left, right) => Expression.Call(ThreeValuedLogic.XorMethod, Undecidable(left), Undecidable(right)),
                Impliesf when IsUndecidable(left, right) => Expression.Call(ThreeValuedLogic.ImpliesMethod, Undecidable(left), Undecidable(right)),
                Equalsf when IsUndecidable(left, right) => Expression.Equal(Undecidable(left), Undecidable(right), liftToNull: true, method: null),
                Andf => Expression.And(left, right),
                Orf => Expression.Or(left, right),
                Xorf => Expression.ExclusiveOr(left, right),
                Impliesf => Expression.Or(Expression.Not(left), right),

                Lessf => Compared(ExpressionType.LessThan, left, right),
                LessOrEqualf => Compared(ExpressionType.LessThanOrEqual, left, right),
                Greaterf => Compared(ExpressionType.GreaterThan, left, right),
                GreaterOrEqualf => Compared(ExpressionType.GreaterThanOrEqual, left, right),
                Equalsf => Compared(ExpressionType.Equal, left, right),
                
                Providedf => HandleProvidedf(left, right),

                _ => throw new UncompilableNodeException(
                    $"The node of type {typeHolder.GetType()} does not support compilation.")
            };
            
            Expression HandleProvidedf(Expression expr, Expression cond)
            {
                // A provided statement has no truth value where its predicate fails.
                if (expr.Type == typeof(bool))
                    expr = Expression.Convert(expr, typeof(bool?));
                var nan = ConvertNaN(expr.Type);
                (expr, nan) = EqualizeTypesIfAble(expr, nan);
                return Expression.Condition(Holds(cond), expr, nan);
            }
        }

        /// <summary>
        /// This is a default converter for other (non-unary and non-binary) nodes
        /// </summary>
        public virtual Expression ConvertOtherNode(IEnumerable<Expression> en, Entity typeHolder)
        {
            
            return typeHolder switch
            {
                Piecewise => HandlePiecewise(en),
                Congruentf => HandleCongruence(en.ToArray()),
                // A matrix is a tensor over its elements' common type, built from the compiled
                // elements in row-major order, which is the order DirectChildren gives them.
                // https://github.com/asc-community/AngouriMath/issues/526
                Entity.Matrix matrix => HandleMatrix(matrix, en),
                // TODO: finite set -> hash set
                _ => throw new UncompilableNodeException(
                    $"The node of type {typeHolder.GetType()} does not support compilation.")
                //FiniteSet => Expression.
            };
        }
            
        // A congruence, over Complex where any of its numbers is one and over double otherwise, with no truth value
        // where they are not whole. https://github.com/asc-community/AngouriMath/issues/2195
        private Expression HandleCongruence(Expression[] numbers)
        {
            var complex = numbers.Any(number => (Nullable.GetUnderlyingType(number.Type) ?? number.Type) == typeof(System.Numerics.Complex));
            var type = complex ? typeof(System.Numerics.Complex) : typeof(double);
            return Expression.Call(complex ? complexCongruence : realCongruence, numbers.Select(number => ConvertType(number, type)));
        }

        private Expression HandleMatrix(Entity.Matrix matrix, IEnumerable<Expression> en)
        {
            var elements = en.ToArray();
            if (elements.Length == 0)
                throw new UncompilableNodeException("An empty matrix has no compiled form");
            var common = elements[0].Type;
            foreach (var element in elements)
                common = MaxType(Nullable.GetUnderlyingType(common) ?? common, Nullable.GetUnderlyingType(element.Type) ?? element.Type);
            for (var i = 0; i < elements.Length; i++)
                elements[i] = ConvertType(elements[i], common);
            return TensorCompilation.Build(matrix, elements);
        }

        private Expression HandlePiecewise(IEnumerable<Expression> en)
        {
            Expression[] children = en.ToArray();
            
            // 0 cases
            if (children.Length == 0)
                return Expression.Constant(null, typeof(object));
            
            // A piecewise statement is one of its cases' truth values, and none where no case holds; one that
            // mixes statements and numbers has no type to compile to.
            // https://github.com/asc-community/AngouriMath/issues/2224
            var statements = 0;
            for (int i = 0; i < children.Length; i += 2)
                if (children[i].Type == typeof(bool) || children[i].Type == typeof(bool?))
                {
                    children[i] = ConvertType(children[i], typeof(bool?));
                    statements++;
                }
                // A case that is an Iverson bracket or a provided integer is widened to a double, as a function's
                // operand is: MaxType has no int? or long?. https://github.com/asc-community/AngouriMath/issues/2237
                else
                    children[i] = NumericOperand(children[i]);
            if (statements > 0 && statements * 2 != children.Length)
                throw new UncompilableNodeException("A piecewise whose cases are statements and numbers at once has no compiled form");

            Type maxType = statements > 0 ? typeof(bool?) : typeof(int);
            if (statements == 0)
                for (int i = 0; i < children.Length; i += 2)
                {
                    maxType = MaxType(maxType, children[i].Type);
                }
            
            // last case
            var expression = children[^2];
            var predicate = children[^1];
            var nan = ConvertNaN(maxType);
            
            (expression, nan) = EqualizeTypesIfAble(expression, nan);
            
            Expression piecewiseExpr = Expression.Condition(Holds(predicate), expression, nan); // could be ConstantExpression in certain cases if values of children could be known
            
            // additional cases
            for (int i = children.Length - 3; i >= 1; i -= 2)
            {
                expression = children[i - 1];
                predicate = children[i];
                
                (expression, piecewiseExpr) = EqualizeTypesIfAble(expression, piecewiseExpr);
                
                piecewiseExpr = Expression.Condition(Holds(predicate), expression, piecewiseExpr);
            }
            
            return piecewiseExpr;
        }
        
        private static object DownCast(Number num)
        {
            if (num is Integer)
                return (long)num;
            if (num is Real)
                return (double)num;
            if (num is Number.Complex)
                return (System.Numerics.Complex)num;
            throw new InvalidProtocolProvided("Undefined type, provide valid compilation protocol");
        }


        // MathAllMethods is a generated table of typed overloads, and this used to reach it with
        // typeof(MathAllMethods).GetMethod(name, ...) -- a name and a signature resolved at run
        // time. Both the trimmer and the NativeAOT compiler answer that by keeping every public
        // method of the class, used or not, since neither can tell which name will be asked for;
        // and the run paid an overload resolution per node of every expression compiled. The
        // dictionary is generated beside the methods, so the two cannot drift apart.
        // https://github.com/asc-community/AngouriMath/issues/363
        private static MethodInfo GetDef(string name, int argCount, Type type)
            => MathAllMethods.Definitions.TryGetValue((name, argCount, type), out var method)
                ? method
                : throw new AngouriBugException($"No compiled definition of {name} taking {argCount} argument(s) of type {type}");

        /// <summary>
        /// The types whose NaN is a value rather than <see langword="null"/> -- the only ones for
        /// which <see cref="ConvertType"/> ever needs to ask whether a value is NaN. Named rather
        /// than found by <c>Type.GetMethod("IsNaN")</c>, which a trimmed build has no way to keep.
        /// </summary>
        [ConstantField] private static readonly Dictionary<Type, MethodInfo> nanChecks = new()
            {
                { typeof(System.Numerics.Complex), Named<System.Numerics.Complex>(v => MathAllMethods.IsNaN(v)) },
                { typeof(double), Named<double>(v => double.IsNaN(v)) },
                { typeof(float), Named<float>(v => float.IsNaN(v)) },
            };

        /// <summary>
        /// The user-defined conversion operators between the types in
        /// <see cref="typeLevelInHierarchy"/>. <see cref="Expression.Convert(Expression, Type)"/>
        /// finds one by reflecting over the target type, and a trimmed or NativeAOT build has no
        /// metadata for an operator nothing referenced: <c>double -> Complex</c> came back as "no
        /// coercion operator is defined between types", so a compiled expression mixing an integer
        /// literal with a complex argument threw where the JIT build answered. Naming them keeps
        /// them, and spares the lookup.
        /// https://github.com/asc-community/AngouriMath/issues/363
        /// </summary>
        [ConstantField] private static readonly Dictionary<(Type From, Type To), MethodInfo> conversionOperators = new()
            {
                { (typeof(int), typeof(System.Numerics.Complex)), Conv<int, System.Numerics.Complex>(v => (System.Numerics.Complex)v) },
                { (typeof(long), typeof(System.Numerics.Complex)), Conv<long, System.Numerics.Complex>(v => (System.Numerics.Complex)v) },
                { (typeof(float), typeof(System.Numerics.Complex)), Conv<float, System.Numerics.Complex>(v => (System.Numerics.Complex)v) },
                { (typeof(double), typeof(System.Numerics.Complex)), Conv<double, System.Numerics.Complex>(v => (System.Numerics.Complex)v) },
                { (typeof(BigInteger), typeof(System.Numerics.Complex)), Conv<BigInteger, System.Numerics.Complex>(v => (System.Numerics.Complex)v) },

                { (typeof(int), typeof(BigInteger)), Conv<int, BigInteger>(v => (BigInteger)v) },
                { (typeof(long), typeof(BigInteger)), Conv<long, BigInteger>(v => (BigInteger)v) },
                { (typeof(float), typeof(BigInteger)), Conv<float, BigInteger>(v => (BigInteger)v) },
                { (typeof(double), typeof(BigInteger)), Conv<double, BigInteger>(v => (BigInteger)v) },

                { (typeof(BigInteger), typeof(int)), Conv<BigInteger, int>(v => (int)v) },
                { (typeof(BigInteger), typeof(long)), Conv<BigInteger, long>(v => (long)v) },
                { (typeof(BigInteger), typeof(float)), Conv<BigInteger, float>(v => (float)v) },
                { (typeof(BigInteger), typeof(double)), Conv<BigInteger, double>(v => (double)v) },
            };

        /// <summary>
        /// The nullable form of every type in <see cref="typeLevelInHierarchy"/> that has one, in
        /// place of <c>typeof(Nullable&lt;&gt;).MakeGenericType(t)</c>: constructing a generic over
        /// a value type at run time is the one thing NativeAOT cannot promise (IL3050), and the
        /// set of types this can be asked for is closed and six long.
        /// </summary>
        [ConstantField] private static readonly Dictionary<Type, Type> nullableForms = new()
            {
                { typeof(System.Numerics.Complex), typeof(System.Numerics.Complex?) },
                { typeof(double), typeof(double?) },
                { typeof(float), typeof(float?) },
                { typeof(long), typeof(long?) },
                { typeof(BigInteger), typeof(BigInteger?) },
                { typeof(int), typeof(int?) },
            };

        // An expression tree is how C# names a method without a string: the compiler emits an
        // ldtoken for the call or the conversion inside it, which resolves the overload at compile
        // time and roots the member for the trimmer.
        private static MethodInfo Named<T>(Expression<Func<T, bool>> e)
            => ((MethodCallExpression)e.Body).Method;

        private static MethodInfo Conv<TFrom, TTo>(Expression<Func<TFrom, TTo>> e)
            => ((UnaryExpression)e.Body).Method
               ?? throw new AngouriBugException($"{typeof(TFrom)} -> {typeof(TTo)} is not a user-defined conversion");

        /// <summary>
        /// The operators <see cref="System.Numerics.Complex"/> and <see cref="BigInteger"/> define
        /// as methods. <c>Expression.Add</c> and its neighbours look one up by reflecting over the
        /// operand type, which is the same hole as the conversions above: NativeAOT kept no
        /// metadata for <c>Complex.op_Addition</c>, so <c>x + 1</c> compiled over a complex
        /// argument threw "the binary operator Add is not defined". The built-in numeric types are
        /// absent on purpose -- their operators are IL instructions and there is nothing to find.
        /// https://github.com/asc-community/AngouriMath/issues/363
        /// </summary>
        [ConstantField] private static readonly Dictionary<(ExpressionType Kind, Type Type), MethodInfo> operators = new()
            {
                { (ExpressionType.Add, typeof(System.Numerics.Complex)), Op<System.Numerics.Complex>((a, b) => a + b) },
                { (ExpressionType.Subtract, typeof(System.Numerics.Complex)), Op<System.Numerics.Complex>((a, b) => a - b) },
                // Real where both sides are, so that an infinity keeps an imaginary part of 0:
                // https://github.com/asc-community/AngouriMath/issues/2222
                { (ExpressionType.Multiply, typeof(System.Numerics.Complex)), Call<System.Numerics.Complex>((a, b) => RealProducts.Product(a, b)) },
                { (ExpressionType.Divide, typeof(System.Numerics.Complex)), Call<System.Numerics.Complex>((a, b) => RealProducts.Quotient(a, b)) },
                { (ExpressionType.Equal, typeof(System.Numerics.Complex)), Cmp<System.Numerics.Complex>((a, b) => a == b) },

                { (ExpressionType.Add, typeof(BigInteger)), Op<BigInteger>((a, b) => a + b) },
                { (ExpressionType.Subtract, typeof(BigInteger)), Op<BigInteger>((a, b) => a - b) },
                { (ExpressionType.Multiply, typeof(BigInteger)), Op<BigInteger>((a, b) => a * b) },
                { (ExpressionType.Divide, typeof(BigInteger)), Op<BigInteger>((a, b) => a / b) },
                { (ExpressionType.Modulo, typeof(BigInteger)), Op<BigInteger>((a, b) => a % b) },
                { (ExpressionType.Equal, typeof(BigInteger)), Cmp<BigInteger>((a, b) => a == b) },
                { (ExpressionType.LessThan, typeof(BigInteger)), Cmp<BigInteger>((a, b) => a < b) },
                { (ExpressionType.LessThanOrEqual, typeof(BigInteger)), Cmp<BigInteger>((a, b) => a <= b) },
                { (ExpressionType.GreaterThan, typeof(BigInteger)), Cmp<BigInteger>((a, b) => a > b) },
                { (ExpressionType.GreaterThanOrEqual, typeof(BigInteger)), Cmp<BigInteger>((a, b) => a >= b) },
            };

        // Membership in a set of numbers and divisibility, with no truth value where evaluation has none.
        // https://github.com/asc-community/AngouriMath/issues/2195
        [ConstantField] private static readonly MethodInfo complexMembership =
            ((MethodCallExpression)((Expression<Func<System.Numerics.Complex, int, bool?>>)((z, set) => AngouriMath.Numerics.NumberStatements.In(z, set))).Body).Method;
        [ConstantField] private static readonly MethodInfo realMembership =
            ((MethodCallExpression)((Expression<Func<double, int, bool?>>)((x, set) => AngouriMath.Numerics.NumberStatements.In(x, set))).Body).Method;
        [ConstantField] private static readonly MethodInfo complexDivisibility =
            ((MethodCallExpression)((Expression<Func<System.Numerics.Complex, System.Numerics.Complex, bool?>>)((a, b) => AngouriMath.Numerics.NumberStatements.Divides(a, b))).Body).Method;
        [ConstantField] private static readonly MethodInfo complexCongruence =
            ((MethodCallExpression)((Expression<Func<System.Numerics.Complex, System.Numerics.Complex, System.Numerics.Complex, bool?>>)((a, b, n) => AngouriMath.Numerics.NumberStatements.Congruent(a, b, n))).Body).Method;
        [ConstantField] private static readonly MethodInfo realCongruence =
            ((MethodCallExpression)((Expression<Func<double, double, double, bool?>>)((a, b, n) => AngouriMath.Numerics.NumberStatements.Congruent(a, b, n))).Body).Method;
        [ConstantField] private static readonly MethodInfo realDivisibility =
            ((MethodCallExpression)((Expression<Func<double, double, bool?>>)((a, b) => AngouriMath.Numerics.NumberStatements.Divides(a, b))).Body).Method;

        /// <summary>
        /// Membership of a compiled element in a set of numbers, over <see cref="System.Numerics.Complex"/> where the
        /// element is one and over <see cref="double"/> otherwise.
        /// </summary>
        internal Expression ConvertMembership(Expression element, AngouriMath.Numerics.NumberStatements.NumberSet set)
        {
            var widened = ShouldBeAtLeastDouble(element);
            return widened.Type == typeof(System.Numerics.Complex)
                ? Expression.Call(complexMembership, widened, Expression.Constant((int)set))
                : Expression.Call(realMembership, ConvertType(widened, typeof(double)), Expression.Constant((int)set));
        }

        [ConstantField] private static readonly MethodInfo complexRemainder =
            ((MethodCallExpression)((Expression<Func<System.Numerics.Complex, System.Numerics.Complex, System.Numerics.Complex>>)((a, b) => RealOnly.Mod(a, b))).Body).Method;

        private static MethodInfo Call<T>(Expression<Func<T, T, T>> e) => ((MethodCallExpression)e.Body).Method;

        private static MethodInfo Op<T>(Expression<Func<T, T, T>> e)
            => ((BinaryExpression)e.Body).Method
               ?? throw new AngouriBugException($"{typeof(T)} defines this operator as an instruction, not a method");

        private static MethodInfo Cmp<T>(Expression<Func<T, T, bool>> e)
            => ((BinaryExpression)e.Body).Method
               ?? throw new AngouriBugException($"{typeof(T)} defines this operator as an instruction, not a method");

        /// <summary>
        /// A comparison, which has no truth value where an operand is NaN or the two are not both
        /// real, for the types that have a NaN: <c>i &gt; 1</c> is <see langword="null"/> over
        /// <see cref="System.Numerics.Complex"/>, where it was refused, and <c>NaN &gt; 1</c> over
        /// <see cref="double"/>, where it was false, so that its negation was true. Other types
        /// compare as they did. https://github.com/asc-community/AngouriMath/issues/2195
        /// </summary>
        private static Expression Compared(ExpressionType kind, Expression left, Expression right)
            => left.Type == right.Type && ThreeValuedLogic.Comparison(kind, left.Type) is { } method
                ? Expression.Call(method, left, right)
                : Operator(kind, left, right);

        private static bool IsUndecidable(Expression left, Expression right)
            => left.Type == typeof(bool?) || right.Type == typeof(bool?);

        private static Expression Undecidable(Expression statement)
            => statement.Type == typeof(bool) ? Expression.Convert(statement, typeof(bool?)) : statement;

        /// <summary>A predicate as a <see cref="bool"/>: one with no truth value does not hold.</summary>
        private static Expression Holds(Expression predicate)
            => predicate.Type == typeof(bool?) ? Expression.Call(ThreeValuedLogic.HoldsMethod, predicate) : predicate;

        // MakeBinary with a null method is exactly what Expression.Add and its neighbours do, so a
        // type that is not in the table is built the way it always was. liftToNull: false is their
        // default too, and MakeBinary ignores it for everything but the comparisons.
        private static Expression Operator(ExpressionType kind, Expression left, Expression right)
            => Expression.MakeBinary(
                kind, left, right, liftToNull: false,
                method: operators.TryGetValue((kind, Nullable.GetUnderlyingType(left.Type) ?? left.Type), out var op) ? op : null);

        private static Expression Coerce(Expression expr, Type type)
        {
            var from = Nullable.GetUnderlyingType(expr.Type) ?? expr.Type;
            var to = Nullable.GetUnderlyingType(type) ?? type;
            return conversionOperators.TryGetValue((from, to), out var op)
                ? Expression.Convert(expr, type, op)
                : Expression.Convert(expr, type);
        }


        [ConstantField] private static readonly Dictionary<Type, int> typeLevelInHierarchy = new()
            {
                { typeof(System.Numerics.Complex), 10 },
                { typeof(double), 9 },
                { typeof(float), 8 },
                { typeof(long), 8 },
                { typeof(BigInteger), 8 },
                { typeof(int), 7 },
                { typeof(object), 6}
            };
        private static Type MaxType(Type a, Type b)
        {
            if (a == b)
                return a;
            if (typeLevelInHierarchy[a] < typeLevelInHierarchy[b])
                return b;
            if (typeLevelInHierarchy[a] > typeLevelInHierarchy[b])
                return a;
            var level = typeLevelInHierarchy[a];
            var res = typeLevelInHierarchy.Where(p => p.Value == level + 1);
            if (res.Count() != 1)
                throw new AngouriBugException("Ambiguous upcast class");
            return res.First().Key;
        }
        
        private (Expression left, Expression right) EqualizeTypesIfAble(Expression left, Expression right)
        {
            if (left.Type == right.Type)
                return (left, right);
            
            Type? underlyingType = Nullable.GetUnderlyingType(left.Type);
            bool nullable = (underlyingType is not null);
            Type leftType = underlyingType ?? left.Type;

            underlyingType = Nullable.GetUnderlyingType(right.Type);
            nullable = nullable || (underlyingType is not null);
            Type rightType = underlyingType ?? right.Type;

            if (!typeLevelInHierarchy.ContainsKey(leftType) || !typeLevelInHierarchy.ContainsKey(rightType))
                return (left, right);

            var typeToCastTo = MaxType(leftType, rightType);
            if (nullable && ((ConstantExpression)ConvertNaN(typeToCastTo)).Value == null)
            {
                // object is the one key of typeLevelInHierarchy with no nullable form, and it
                // cannot arrive here: nullable is only true when one side unwrapped to a value
                // type, whose level is above object's, so MaxType never returns object.
                if (nullableForms.TryGetValue(typeToCastTo, out var nullableForm))
                    typeToCastTo = nullableForm;
            }
            
            left = ConvertType(left, typeToCastTo);
            right = ConvertType(right, typeToCastTo);
            
            return (left, right);
        }
    }
}
