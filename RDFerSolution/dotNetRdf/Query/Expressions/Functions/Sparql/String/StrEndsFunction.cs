﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VDS.RDF.Query.Expressions.Functions.Sparql.String
{
    /// <summary>
    /// Represents the SPARQL STRENDS Function
    /// </summary>
    public class StrEndsFunction
        : BaseBinaryStringFunction
    {
        /// <summary>
        /// Creates a new STRENDS() function
        /// </summary>
        /// <param name="stringExpr">String Expression</param>
        /// <param name="endsExpr">Argument Expression</param>
        public StrEndsFunction(ISparqlExpression stringExpr, ISparqlExpression endsExpr)
            : base(stringExpr, endsExpr) { }

        /// <summary>
        /// Determines whether the given String Literal ends with the given Argument Literal
        /// </summary>
        /// <param name="stringLit">String Literal</param>
        /// <param name="argLit">Argument Literal</param>
        /// <returns></returns>
        protected override bool ValueInternal(ILiteralNode stringLit, ILiteralNode argLit)
        {
            return stringLit.Value.EndsWith(argLit.Value);
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public override string Functor
        {
            get
            {
                return SparqlSpecsHelper.SparqlKeywordStrEnds;
            }
        }

        /// <summary>
        /// Gets the String representation of the Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SparqlSpecsHelper.SparqlKeywordStrEnds + "(" + this._leftExpr.ToString() + ", " + this._rightExpr.ToString() + ")";
        }

        /// <summary>
        /// Transforms the Expression using the given Transformer
        /// </summary>
        /// <param name="transformer">Expression Transformer</param>
        /// <returns></returns>
        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new StrEndsFunction(transformer.Transform(this._leftExpr), transformer.Transform(this._rightExpr));
        }
    }
}
