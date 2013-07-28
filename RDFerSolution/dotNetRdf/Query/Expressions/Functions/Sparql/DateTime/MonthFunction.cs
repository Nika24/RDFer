﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VDS.RDF.Query.Expressions.Functions.Sparql.DateTime
{
    /// <summary>
    /// Represents the SPARQL MONTH() Function
    /// </summary>
    public class MonthFunction
        : XPath.DateTime.MonthFromDateTimeFunction
    {
        /// <summary>
        /// Creates a new SPARQL YEAR() Function
        /// </summary>
        /// <param name="expr">Argument Expression</param>
        public MonthFunction(ISparqlExpression expr)
            : base(expr) { }

        /// <summary>
        /// Gets the Functor of this Expression
        /// </summary>
        public override string Functor
        {
            get
            {
                return SparqlSpecsHelper.SparqlKeywordMonth;
            }
        }

        /// <summary>
        /// Gets the String representation of this Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SparqlSpecsHelper.SparqlKeywordMonth + "(" + this._expr.ToString() + ")";
        }

        /// <summary>
        /// Transforms the Expression using the given Transformer
        /// </summary>
        /// <param name="transformer">Expression Transformer</param>
        /// <returns></returns>
        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new MonthFunction(transformer.Transform(this._expr));
        }
    }
}
