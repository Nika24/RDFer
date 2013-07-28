﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Nodes;

namespace VDS.RDF.Query.Expressions.Functions.Sparql.Boolean
{
    /// <summary>
    /// Class representing the Sparql IsBlank() function
    /// </summary>
    public class IsBlankFunction
        : BaseUnaryExpression
    {
        /// <summary>
        /// Creates a new IsBlank() function expression
        /// </summary>
        /// <param name="expr">Expression to apply the function to</param>
        public IsBlankFunction(ISparqlExpression expr) : base(expr) { }

        /// <summary>
        /// Computes the Effective Boolean Value of this Expression as evaluated for a given Binding
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <param name="bindingID">Binding ID</param>
        /// <returns></returns>
        public override IValuedNode Evaluate(SparqlEvaluationContext context, int bindingID)
        {
            INode result = this._expr.Evaluate(context, bindingID);
            if (result == null)
            {
                return new BooleanNode(null, false);
            }
            else
            {
                return new BooleanNode(null, result.NodeType == NodeType.Blank);
            }
        }

        /// <summary>
        /// Gets the String representation of this Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "ISBLANK(" + this._expr.ToString() + ")";
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
                return SparqlSpecsHelper.SparqlKeywordIsBlank;
            }
        }

        /// <summary>
        /// Transforms the Expression using the given Transformer
        /// </summary>
        /// <param name="transformer">Expression Transformer</param>
        /// <returns></returns>
        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new IsBlankFunction(transformer.Transform(this._expr));
        }
    }
}
