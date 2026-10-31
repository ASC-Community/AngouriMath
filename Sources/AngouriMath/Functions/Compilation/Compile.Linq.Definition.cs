//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AngouriMath.Core;
using AngouriMath.Core.Compilation.IntoLinq;
using static AngouriMath.Core.FastExpression;

namespace AngouriMath
{
    partial record Entity
    {
        /// <summary>
        /// Compiles a given expression into a native lambda
        /// </summary>
        /// <typeparam name="TDelegate">
        /// The type of your delegate to convert to
        /// </typeparam>
        /// <param name="protocol">
        /// This is a protocol, according to which all nodes get compiled. Use this
        /// if you want to use the compilation for types different from those standard
        /// </param>
        /// <param name="returnType">
        /// The type to which the resulting type will be casted
        /// </param>
        /// <param name="typesAndNames">
        /// An <see cref="IEnumerable"/> of pairs, where the first element is the type of your argument,
        /// and the second one is the corresponding variable from the expression
        /// </param>
        /// <returns>
        /// Returnes a natively compiled expression of type <typeparamref name="TDelegate"/>
        /// </returns>
        /// <remarks>
        /// Into a function that returns <see cref="double"/> or <see cref="float"/>, a value that is not real is NaN, and
        /// so is every value computed from one, though the expression may be real there: <c>sin(arcsin(x))</c> at 2.
        /// <see cref="MathS.Settings.RefuseNonRealCompilation"/> refuses such an expression instead.
        /// </remarks>
        public TDelegate Compile<TDelegate>(CompilationProtocol protocol, Type returnType, IEnumerable<(Type type, Variable variable)> typesAndNames) where TDelegate : Delegate
            => IntoLinqCompiler.Compile<TDelegate>(this, returnType, protocol, typesAndNames);


        /// <summary>
        /// Compiles a given expression into a native lambda. We use the default protocol.
        /// If you plan using non-standard types, consider passing a custom compilation protocol.
        /// </summary>
        /// <typeparam name="TIn1">
        /// The type of the passed argument number 1
        /// </typeparam>
        /// <typeparam name="TOut">
        /// The return type
        /// </typeparam>
        /// <param name="var1">
        /// The variable corresponding to the function's argument number 1
        /// </param>
        /// <returns>
        /// Returns a natively-compiled delegate
        /// </returns>
        /// <remarks>
        /// Into a function that returns <see cref="double"/> or <see cref="float"/>, a value that is not real is NaN, and
        /// so is every value computed from one, though the expression may be real there: <c>sin(arcsin(x))</c> at 2.
        /// <see cref="MathS.Settings.RefuseNonRealCompilation"/> refuses such an expression instead.
        /// </remarks>
        public Func<TIn1, TOut> Compile<TIn1, TOut>(Variable var1)
            => IntoLinqCompiler.Compile<Func<TIn1, TOut>>(this, typeof(TOut), new(), 
                new[] { (typeof(TIn1), var1) });


        /// <summary>
        /// Compiles a given expression into a native lambda. We use the default protocol.
        /// If you plan using non-standard types, consider passing a custom compilation protocol.
        /// </summary>
        /// <typeparam name="TIn1">
        /// The type of the passed argument number 1
        /// </typeparam>
        /// <typeparam name="TIn2">
        /// The type of the passed argument number 2
        /// </typeparam>
        /// <typeparam name="TOut">
        /// The return type
        /// </typeparam>
        /// <param name="var1">
        /// The variable corresponding to the function's argument number 1
        /// </param>
        /// <param name="var2">
        /// The variable corresponding to the function's argument number 2
        /// </param>
        /// <returns>
        /// Returns a natively-compiled delegate
        /// </returns>
        /// <remarks>
        /// Into a function that returns <see cref="double"/> or <see cref="float"/>, a value that is not real is NaN, and
        /// so is every value computed from one, though the expression may be real there: <c>sin(arcsin(x))</c> at 2.
        /// <see cref="MathS.Settings.RefuseNonRealCompilation"/> refuses such an expression instead.
        /// </remarks>
        public Func<TIn1, TIn2, TOut> Compile<TIn1, TIn2, TOut>(Variable var1, Variable var2)
            => IntoLinqCompiler.Compile<Func<TIn1, TIn2, TOut>>(this, typeof(TOut), new(), 
                new[] { (typeof(TIn1), var1), (typeof(TIn2), var2)  });


        /// <summary>
        /// Compiles a given expression into a native lambda. We use the default protocol.
        /// If you plan using non-standard types, consider passing a custom compilation protocol.
        /// </summary>
        /// <typeparam name="TIn1">
        /// The type of the passed argument number 1
        /// </typeparam>
        /// <typeparam name="TIn2">
        /// The type of the passed argument number 2
        /// </typeparam>
        /// <typeparam name="TIn3">
        /// The type of the passed argument number 3
        /// </typeparam>
        /// <typeparam name="TOut">
        /// The return type
        /// </typeparam>
        /// <param name="var1">
        /// The variable corresponding to the function's argument number 1
        /// </param>
        /// <param name="var2">
        /// The variable corresponding to the function's argument number 2
        /// </param>
        /// <param name="var3">
        /// The variable corresponding to the function's argument number 3
        /// </param>
        /// <returns>
        /// Returns a natively-compiled delegate
        /// </returns>
        /// <remarks>
        /// Into a function that returns <see cref="double"/> or <see cref="float"/>, a value that is not real is NaN, and
        /// so is every value computed from one, though the expression may be real there: <c>sin(arcsin(x))</c> at 2.
        /// <see cref="MathS.Settings.RefuseNonRealCompilation"/> refuses such an expression instead.
        /// </remarks>
        public Func<TIn1, TIn2, TIn3, TOut> Compile<TIn1, TIn2, TIn3, TOut>(Variable var1, Variable var2, Variable var3)
            => IntoLinqCompiler.Compile<Func<TIn1, TIn2, TIn3, TOut>>(this, typeof(TOut), new(), 
                new[] { (typeof(TIn1), var1), (typeof(TIn2), var2) , (typeof(TIn3), var3)  });


        /// <summary>
        /// Compiles a given expression into a native lambda. We use the default protocol.
        /// If you plan using non-standard types, consider passing a custom compilation protocol.
        /// </summary>
        /// <typeparam name="TIn1">
        /// The type of the passed argument number 1
        /// </typeparam>
        /// <typeparam name="TIn2">
        /// The type of the passed argument number 2
        /// </typeparam>
        /// <typeparam name="TIn3">
        /// The type of the passed argument number 3
        /// </typeparam>
        /// <typeparam name="TIn4">
        /// The type of the passed argument number 4
        /// </typeparam>
        /// <typeparam name="TOut">
        /// The return type
        /// </typeparam>
        /// <param name="var1">
        /// The variable corresponding to the function's argument number 1
        /// </param>
        /// <param name="var2">
        /// The variable corresponding to the function's argument number 2
        /// </param>
        /// <param name="var3">
        /// The variable corresponding to the function's argument number 3
        /// </param>
        /// <param name="var4">
        /// The variable corresponding to the function's argument number 4
        /// </param>
        /// <returns>
        /// Returns a natively-compiled delegate
        /// </returns>
        /// <remarks>
        /// Into a function that returns <see cref="double"/> or <see cref="float"/>, a value that is not real is NaN, and
        /// so is every value computed from one, though the expression may be real there: <c>sin(arcsin(x))</c> at 2.
        /// <see cref="MathS.Settings.RefuseNonRealCompilation"/> refuses such an expression instead.
        /// </remarks>
        public Func<TIn1, TIn2, TIn3, TIn4, TOut> Compile<TIn1, TIn2, TIn3, TIn4, TOut>(Variable var1, Variable var2, Variable var3, Variable var4)
            => IntoLinqCompiler.Compile<Func<TIn1, TIn2, TIn3, TIn4, TOut>>(this, typeof(TOut), new(), 
                new[] { (typeof(TIn1), var1), (typeof(TIn2), var2) , (typeof(TIn3), var3) , (typeof(TIn4), var4)  });


        /// <summary>
        /// Compiles a given expression into a native lambda. We use the default protocol.
        /// If you plan using non-standard types, consider passing a custom compilation protocol.
        /// </summary>
        /// <typeparam name="TIn1">
        /// The type of the passed argument number 1
        /// </typeparam>
        /// <typeparam name="TIn2">
        /// The type of the passed argument number 2
        /// </typeparam>
        /// <typeparam name="TIn3">
        /// The type of the passed argument number 3
        /// </typeparam>
        /// <typeparam name="TIn4">
        /// The type of the passed argument number 4
        /// </typeparam>
        /// <typeparam name="TIn5">
        /// The type of the passed argument number 5
        /// </typeparam>
        /// <typeparam name="TOut">
        /// The return type
        /// </typeparam>
        /// <param name="var1">
        /// The variable corresponding to the function's argument number 1
        /// </param>
        /// <param name="var2">
        /// The variable corresponding to the function's argument number 2
        /// </param>
        /// <param name="var3">
        /// The variable corresponding to the function's argument number 3
        /// </param>
        /// <param name="var4">
        /// The variable corresponding to the function's argument number 4
        /// </param>
        /// <param name="var5">
        /// The variable corresponding to the function's argument number 5
        /// </param>
        /// <returns>
        /// Returns a natively-compiled delegate
        /// </returns>
        /// <remarks>
        /// Into a function that returns <see cref="double"/> or <see cref="float"/>, a value that is not real is NaN, and
        /// so is every value computed from one, though the expression may be real there: <c>sin(arcsin(x))</c> at 2.
        /// <see cref="MathS.Settings.RefuseNonRealCompilation"/> refuses such an expression instead.
        /// </remarks>
        public Func<TIn1, TIn2, TIn3, TIn4, TIn5, TOut> Compile<TIn1, TIn2, TIn3, TIn4, TIn5, TOut>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5)
            => IntoLinqCompiler.Compile<Func<TIn1, TIn2, TIn3, TIn4, TIn5, TOut>>(this, typeof(TOut), new(), 
                new[] { (typeof(TIn1), var1), (typeof(TIn2), var2) , (typeof(TIn3), var3) , (typeof(TIn4), var4) , (typeof(TIn5), var5)  });


        /// <summary>
        /// Compiles a given expression into a native lambda. We use the default protocol.
        /// If you plan using non-standard types, consider passing a custom compilation protocol.
        /// </summary>
        /// <typeparam name="TIn1">
        /// The type of the passed argument number 1
        /// </typeparam>
        /// <typeparam name="TIn2">
        /// The type of the passed argument number 2
        /// </typeparam>
        /// <typeparam name="TIn3">
        /// The type of the passed argument number 3
        /// </typeparam>
        /// <typeparam name="TIn4">
        /// The type of the passed argument number 4
        /// </typeparam>
        /// <typeparam name="TIn5">
        /// The type of the passed argument number 5
        /// </typeparam>
        /// <typeparam name="TIn6">
        /// The type of the passed argument number 6
        /// </typeparam>
        /// <typeparam name="TOut">
        /// The return type
        /// </typeparam>
        /// <param name="var1">
        /// The variable corresponding to the function's argument number 1
        /// </param>
        /// <param name="var2">
        /// The variable corresponding to the function's argument number 2
        /// </param>
        /// <param name="var3">
        /// The variable corresponding to the function's argument number 3
        /// </param>
        /// <param name="var4">
        /// The variable corresponding to the function's argument number 4
        /// </param>
        /// <param name="var5">
        /// The variable corresponding to the function's argument number 5
        /// </param>
        /// <param name="var6">
        /// The variable corresponding to the function's argument number 6
        /// </param>
        /// <returns>
        /// Returns a natively-compiled delegate
        /// </returns>
        /// <remarks>
        /// Into a function that returns <see cref="double"/> or <see cref="float"/>, a value that is not real is NaN, and
        /// so is every value computed from one, though the expression may be real there: <c>sin(arcsin(x))</c> at 2.
        /// <see cref="MathS.Settings.RefuseNonRealCompilation"/> refuses such an expression instead.
        /// </remarks>
        public Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut> Compile<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6)
            => IntoLinqCompiler.Compile<Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut>>(this, typeof(TOut), new(), 
                new[] { (typeof(TIn1), var1), (typeof(TIn2), var2) , (typeof(TIn3), var3) , (typeof(TIn4), var4) , (typeof(TIn5), var5) , (typeof(TIn6), var6)  });


        /// <summary>
        /// Compiles a given expression into a native lambda. We use the default protocol.
        /// If you plan using non-standard types, consider passing a custom compilation protocol.
        /// </summary>
        /// <typeparam name="TIn1">
        /// The type of the passed argument number 1
        /// </typeparam>
        /// <typeparam name="TIn2">
        /// The type of the passed argument number 2
        /// </typeparam>
        /// <typeparam name="TIn3">
        /// The type of the passed argument number 3
        /// </typeparam>
        /// <typeparam name="TIn4">
        /// The type of the passed argument number 4
        /// </typeparam>
        /// <typeparam name="TIn5">
        /// The type of the passed argument number 5
        /// </typeparam>
        /// <typeparam name="TIn6">
        /// The type of the passed argument number 6
        /// </typeparam>
        /// <typeparam name="TIn7">
        /// The type of the passed argument number 7
        /// </typeparam>
        /// <typeparam name="TOut">
        /// The return type
        /// </typeparam>
        /// <param name="var1">
        /// The variable corresponding to the function's argument number 1
        /// </param>
        /// <param name="var2">
        /// The variable corresponding to the function's argument number 2
        /// </param>
        /// <param name="var3">
        /// The variable corresponding to the function's argument number 3
        /// </param>
        /// <param name="var4">
        /// The variable corresponding to the function's argument number 4
        /// </param>
        /// <param name="var5">
        /// The variable corresponding to the function's argument number 5
        /// </param>
        /// <param name="var6">
        /// The variable corresponding to the function's argument number 6
        /// </param>
        /// <param name="var7">
        /// The variable corresponding to the function's argument number 7
        /// </param>
        /// <returns>
        /// Returns a natively-compiled delegate
        /// </returns>
        /// <remarks>
        /// Into a function that returns <see cref="double"/> or <see cref="float"/>, a value that is not real is NaN, and
        /// so is every value computed from one, though the expression may be real there: <c>sin(arcsin(x))</c> at 2.
        /// <see cref="MathS.Settings.RefuseNonRealCompilation"/> refuses such an expression instead.
        /// </remarks>
        public Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut> Compile<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6, Variable var7)
            => IntoLinqCompiler.Compile<Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut>>(this, typeof(TOut), new(), 
                new[] { (typeof(TIn1), var1), (typeof(TIn2), var2) , (typeof(TIn3), var3) , (typeof(TIn4), var4) , (typeof(TIn5), var5) , (typeof(TIn6), var6) , (typeof(TIn7), var7)  });


        /// <summary>
        /// Compiles a given expression into a native lambda. We use the default protocol.
        /// If you plan using non-standard types, consider passing a custom compilation protocol.
        /// </summary>
        /// <typeparam name="TIn1">
        /// The type of the passed argument number 1
        /// </typeparam>
        /// <typeparam name="TIn2">
        /// The type of the passed argument number 2
        /// </typeparam>
        /// <typeparam name="TIn3">
        /// The type of the passed argument number 3
        /// </typeparam>
        /// <typeparam name="TIn4">
        /// The type of the passed argument number 4
        /// </typeparam>
        /// <typeparam name="TIn5">
        /// The type of the passed argument number 5
        /// </typeparam>
        /// <typeparam name="TIn6">
        /// The type of the passed argument number 6
        /// </typeparam>
        /// <typeparam name="TIn7">
        /// The type of the passed argument number 7
        /// </typeparam>
        /// <typeparam name="TIn8">
        /// The type of the passed argument number 8
        /// </typeparam>
        /// <typeparam name="TOut">
        /// The return type
        /// </typeparam>
        /// <param name="var1">
        /// The variable corresponding to the function's argument number 1
        /// </param>
        /// <param name="var2">
        /// The variable corresponding to the function's argument number 2
        /// </param>
        /// <param name="var3">
        /// The variable corresponding to the function's argument number 3
        /// </param>
        /// <param name="var4">
        /// The variable corresponding to the function's argument number 4
        /// </param>
        /// <param name="var5">
        /// The variable corresponding to the function's argument number 5
        /// </param>
        /// <param name="var6">
        /// The variable corresponding to the function's argument number 6
        /// </param>
        /// <param name="var7">
        /// The variable corresponding to the function's argument number 7
        /// </param>
        /// <param name="var8">
        /// The variable corresponding to the function's argument number 8
        /// </param>
        /// <returns>
        /// Returns a natively-compiled delegate
        /// </returns>
        /// <remarks>
        /// Into a function that returns <see cref="double"/> or <see cref="float"/>, a value that is not real is NaN, and
        /// so is every value computed from one, though the expression may be real there: <c>sin(arcsin(x))</c> at 2.
        /// <see cref="MathS.Settings.RefuseNonRealCompilation"/> refuses such an expression instead.
        /// </remarks>
        public Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut> Compile<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6, Variable var7, Variable var8)
            => IntoLinqCompiler.Compile<Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut>>(this, typeof(TOut), new(), 
                new[] { (typeof(TIn1), var1), (typeof(TIn2), var2) , (typeof(TIn3), var3) , (typeof(TIn4), var4) , (typeof(TIn5), var5) , (typeof(TIn6), var6) , (typeof(TIn7), var7) , (typeof(TIn8), var8)  });

    }
}