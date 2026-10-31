//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using static AngouriMath.Entity;

namespace AngouriMath.Functions
{
    internal static class Series
    {
        internal static Entity TaylorExpansion(Entity expr, int termCount, params (Variable exprVariable, Variable polyVariable, Entity point)[] exprToPolyVars)
        {
            if (exprToPolyVars.Length == 1)
                return SingleTaylorExpansion(expr, termCount, exprToPolyVars[0].exprVariable, exprToPolyVars[0].polyVariable, exprToPolyVars[0].point);
            else
                return MultivariableTaylorExpansion(expr, termCount, exprToPolyVars);
        }

        internal static Entity SingleTaylorExpansion(Entity expr, int termCount, Variable exprVariable, Variable polyVariable, Entity point)
        {
            if (termCount < 0)
                throw new InvalidNumberException($"{nameof(termCount)} is supposed to be at least 0, and {termCount} was given");
            return Sum(SingleTaylorExpansionTerms(expr, exprVariable, polyVariable, point), termCount);
        }

        /// <summary>
        /// The first <paramref name="termCount"/> terms added up, 0 where there are none, and
        /// <see cref="Number.Real.NaN"/> where a term has no value.
        /// </summary>
        /// <remarks>
        /// The term after the last is never asked for, since it costs a derivative more. The
        /// expansion of <c>sin(x) / x</c> around 0 has no value, as the function has none there,
        /// and neither has any derivative of it; it was the sum of the terms, <c>NaN + NaN + ...</c>.
        /// https://github.com/asc-community/AngouriMath/issues/2291
        /// </remarks>
        private static Entity Sum(IEnumerable<Entity> series, int termCount)
        {
            var terms = new List<Entity>();
            using var enumerator = series.GetEnumerator();
            while (terms.Count < termCount && enumerator.MoveNext())
            {
                if (enumerator.Current.IsNaN)
                    return Number.Real.NaN;
                terms.Add(enumerator.Current);
            }
            return terms.Count == 0 ? Number.Integer.Zero : TreeAnalyzer.MultiHangBinary(terms, (a, b) => a + b);
        }

        /// <remarks>
        /// Each derivative is simplified before the next is taken from it, and each coefficient
        /// once it is evaluated at the point. A quotient's derivative taken as it stands grows about
        /// eightfold a step, so the ninth of <c>sin(x) / x</c> had 7,797,110 nodes, and 87
        /// simplified, and the eleventh ran out of memory. Where a coefficient has no value, neither
        /// has any after it, so the derivatives stop there.
        /// https://github.com/asc-community/AngouriMath/issues/2291
        /// </remarks>
        internal static IEnumerable<Entity> SingleTaylorExpansionTerms(Entity expr, Variable exprVariable, Variable polyVariable, Entity point)
        {
            var currExpr = expr;
            var i = 0;
            while (true)
            {
                var coef = currExpr.Substitute(exprVariable, point).Simplify();
                var num = (i == 0 ? coef : coef * (polyVariable - point).Pow(i)).InnerSimplified;
                if (num.IsNaN)
                {
                    while (true)
                        yield return Number.Real.NaN;
                }
                if (num != Number.Integer.Zero)
                    if (i > 1)
                        yield return num / ((Entity)i).Factorial();
                    else
                        yield return num;
                else
                    yield return Number.Integer.Zero;
                currExpr = currExpr.Differentiate(exprVariable).Simplify();
                i++;
            }
        }

        internal static Entity MultivariableTaylorExpansion(Entity expr, int termCount, params (Variable exprVariable, Variable polyVariable, Entity point)[] exprToPolyVars)
        {
            if (termCount < 0)
                throw new InvalidNumberException($"{nameof(termCount)} is supposed to be at least 0, and {termCount} was given");
            return Sum(MultivariableTaylorExpansionTerms(expr, exprToPolyVars), termCount);
        }

        internal static IEnumerable<Entity> MultivariableTaylorExpansionTerms(Entity expr, params (Variable exprVariable, Variable polyVariable, Entity point)[] exprToPolyVars)
        {
            // This structure is just a deconstruction of the terms that are added together in a multi taylor polynomial.
            // Like n (x - a)^s (y - b)^t f'(x,y), where n is the coefficient, s and t are pointCoefficientDegrees, and then the partialDerivative
            // Many of these need to be computed and added together per computed expansion term.
            var lastTerms = new List<(int coefficient, int[] pointCoefficientDegrees, Entity partialDerivative)>();
            var order = 0;
            var variables = exprToPolyVars.Length;

            while (true)
            {
                var newTerms = new List<(int coefficient, int[] pointCoefficientDegrees, Entity partialDerivative)>();

                // Base case: initial term is just the given expression.
                if (order == 0)
                    newTerms.Add((1, new int[variables], expr));
                else
                {
                    // Iterative step: take the partial derivative of each of the previous terms with respect to each of the variables.
                    foreach (var (coefficient, pointCoefficientDegrees, partialDerivative) in lastTerms)
                    {
                        for (int variableIndex = 0; variableIndex < exprToPolyVars.Length; variableIndex++)
                        {
                            // Compute first what our point coefficient degrees will be, so that we don't repeat the computation of
                            // any partial derivatives, like f_xy vs f_yx.
                            var (exprVariable, polyVariable, point) = exprToPolyVars[variableIndex];
                            var newPointCoeffs = new int[variables];
                            // The new degree is the same as the parent term's, times an extra (x - a) or whichever variable.
                            pointCoefficientDegrees.CopyTo(newPointCoeffs, 0);
                            newPointCoeffs[variableIndex] += 1;

                            // The term is a repeat if it's coefficients match one previously computed.
                            bool foundRepeatFlag = false;
                            for (int newTermIndex = 0; newTermIndex < newTerms.Count; newTermIndex++)
                            {
                                var newTerm = newTerms[newTermIndex];

                                if (Enumerable.SequenceEqual(newPointCoeffs, newTerm.pointCoefficientDegrees))
                                {
                                    // If the match is found, instead of creating a whole new repeat term, we just add
                                    // to the match's coefficient. This ends up making a neat binomial coefficient pattern.
                                    newTerm.coefficient += coefficient;
                                    newTerms[newTermIndex] = newTerm;
                                    foundRepeatFlag = true;
                                    break;
                                }
                            }

                            // If no match is found, we add it to the new term list.
                            if (!foundRepeatFlag)
                            {
                                // Simplified before the next is taken from it, as in SingleTaylorExpansionTerms.
                                var newPartialDerivative = partialDerivative.Differentiate(exprVariable).Simplify();
                                newTerms.Add((coefficient, newPointCoeffs, newPartialDerivative));
                            }

                        }
                    }

                }


                // From the list of deconstructed terms, we put assemble the term and sum them all up.
                var fullExpressionTerms = new List<Entity>();
                foreach (var (coefficient, pointCoefficientDegrees, partialDerivative) in newTerms)
                {
                    // pointCoefficients are transformed into their proper (x - a)^t... forms.
                    Entity pointCoefficients = 1;
                    for (int variableIndex = 0; variableIndex < exprToPolyVars.Length; variableIndex++)
                    {
                        var (exprVariable, polyVariable, point) = exprToPolyVars[variableIndex];
                        if (pointCoefficientDegrees[variableIndex] != 0) // don't produce x^0 which is undefined for x=0
                            pointCoefficients *= (polyVariable - point).Pow(pointCoefficientDegrees[variableIndex]);
                    }

                    // Solve the partial derivative at the given point,
                    var solvedPartialDerivative = partialDerivative;
                    foreach (var (exprVariable, polyVariable, point) in exprToPolyVars)
                        solvedPartialDerivative = solvedPartialDerivative.Substitute(exprVariable, point);
                    solvedPartialDerivative = solvedPartialDerivative.Simplify();

                    var fullTerm = solvedPartialDerivative * coefficient * pointCoefficients;

                    // tack on the coefficient information,
                    fullExpressionTerms.Add(fullTerm);
                }

                Entity fullExpr = TreeAnalyzer.MultiHangBinary(fullExpressionTerms, (a, b) => a + b);
                fullExpr = fullExpr.InnerSimplified;

                if (fullExpr.Equals(Number.Integer.Zero))
                    yield return Number.Integer.Zero;
                else
                {
                    // and divide that all by the proper factorial.
                    if (order > 1)
                        fullExpr /= ((Entity)order).Factorial();

                    yield return fullExpr;
                }

                lastTerms = newTerms;
                order++;
            }
        }
    }
}
