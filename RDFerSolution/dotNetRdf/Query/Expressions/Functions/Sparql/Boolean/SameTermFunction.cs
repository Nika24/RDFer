﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Nodes;

namespace VDS.RDF.Query.Expressions.Functions.Sparql.Boolean
{
    /// <summary>
    /// Class representing the Sparql SameTerm() function
    /// </summary>
    public class SameTermFunction
        : BaseBinaryExpression
    {
        /// <summary>
        /// Creates a new SameTerm() function expression
        /// </summary>
        /// <param name="term1">First Term</param>
        /// <param name="term2">Second Term</param>
        public SameTermFunction(ISparqlExpression term1, ISparqlExpression term2)
            : base(term1, term2) { }

        /// <summary>
        /// Computes the Effective Boolean Value of this Expression as evaluated for a given Binding
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <param name="bindingID">Binding ID</param>
        /// <returns></returns>
        public override IValuedNode Evaluate(SparqlEvaluationContext context, int bindingID)
        {
            INode a, b;
            a = this._leftExpr.Evaluate(context, bindingID);
            b = this._rightExpr.Evaluate(context, bindingID);

            if (a == null)
            {
                if (b == null)
                {
                    return new BooleanNode(null, true);
                }
                else
                {
                    return new BooleanNode(null, false);
                }
            }
            else
            {
                return new BooleanNode(null, a.Equals(b));
            }
        }

        /// <summary>
        /// Gets the String representation of this Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "SAMETERM(" + this._leftExpr.ToString() + "," + this._rightExpr.ToString() + ")";
        }

        /// <summary>
        /// Gets the Type of the Expression
        /// </summary>
        public override SparqlExpressionType Type
        {
            get
            {
                return SparqlExpressionType.Function;
            }
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public override string Functor
        {
            get
            {
                return SparqlSpecsHelper.SparqlKeywordSameTerm;
            }
        }

        /// <summary>
        /// Transforms the Expression using the given Transformer
        /// </summary>
        /// <param name="transformer">Expression Transformer</param>
        /// <returns></returns>
        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new SameTermFunction(transformer.Transform(this._leftExpr), transformer.Transform(this._rightExpr));
        }
    }
}
