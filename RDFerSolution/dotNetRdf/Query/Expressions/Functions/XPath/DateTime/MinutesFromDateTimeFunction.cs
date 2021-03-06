﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Nodes;

namespace VDS.RDF.Query.Expressions.Functions.XPath.DateTime
{
    /// <summary>
    /// Represents the XPath minutes-from-dateTime() function
    /// </summary>
    public class MinutesFromDateTimeFunction
        : BaseUnaryDateTimeFunction
    {
        /// <summary>
        /// Creates a new XPath Minutes from Date Time function
        /// </summary>
        /// <param name="expr">Expression</param>
        public MinutesFromDateTimeFunction(ISparqlExpression expr)
            : base(expr) { }

        /// <summary>
        /// Calculates the numeric value of the function from the given Date Time
        /// </summary>
        /// <param name="dateTime">Date Time</param>
        /// <returns></returns>
        protected override IValuedNode ValueInternal(DateTimeOffset dateTime)
        {
            return new LongNode(null, Convert.ToInt64(dateTime.Minute));
        }

        /// <summary>
        /// Gets the String representation of the function
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "<" + XPathFunctionFactory.XPathFunctionsNamespace + XPathFunctionFactory.MinutesFromDateTime + ">(" + this._expr.ToString() + ")";
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public override string Functor
        {
            get
            {
                return XPathFunctionFactory.XPathFunctionsNamespace + XPathFunctionFactory.MinutesFromDateTime;
            }
        }

        /// <summary>
        /// Transforms the Expression using the given Transformer
        /// </summary>
        /// <param name="transformer">Expression Transformer</param>
        /// <returns></returns>
        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new MinutesFromDateTimeFunction(transformer.Transform(this._expr));
        }
    }
}
