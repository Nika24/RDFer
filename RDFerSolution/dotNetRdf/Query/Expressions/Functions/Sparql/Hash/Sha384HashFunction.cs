﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HashLib.Crypto;

namespace VDS.RDF.Query.Expressions.Functions.Sparql.Hash
{
#if !SILVERLIGHT

    /// <summary>
    /// Represents the SPARQL SHA384() Function
    /// </summary>
    public class Sha384HashFunction 
        : BaseHashFunction
    {
        /// <summary>
        /// Creates a new SHA384() Function
        /// </summary>
        /// <param name="expr">Argument Expression</param>
        public Sha384HashFunction(ISparqlExpression expr)
            : base(expr, new SHA384Managed()) { }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public override string Functor
        {
            get 
            {
                return SparqlSpecsHelper.SparqlKeywordSha384; 
            }
        }

        /// <summary>
        /// Gets the String representation of the Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SparqlSpecsHelper.SparqlKeywordSha384 + "(" + this._expr.ToString() + ")";
        }

        /// <summary>
        /// Transforms the Expression using the given Transformer
        /// </summary>
        /// <param name="transformer">Expression Transformer</param>
        /// <returns></returns>
        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new Sha384HashFunction(transformer.Transform(this._expr));
        }
    }

#else

        /// <summary>
    /// Represents the SPARQL SHA384() Function
    /// </summary>
    public class Sha384HashFunction 
        : BaseHashLibFunction
    {
        /// <summary>
        /// Creates a new SHA384() Function
        /// </summary>
        /// <param name="expr">Argument Expression</param>
        public Sha384HashFunction(ISparqlExpression expr)
            : base(expr, new SHA384()) { }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public override string Functor
        {
            get 
            {
                return SparqlSpecsHelper.SparqlKeywordSha384; 
            }
        }

        /// <summary>
        /// Gets the String representation of the Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SparqlSpecsHelper.SparqlKeywordSha384 + "(" + this._expr.ToString() + ")";
        }

        /// <summary>
        /// Transforms the Expression using the given Transformer
        /// </summary>
        /// <param name="transformer">Expression Transformer</param>
        /// <returns></returns>
        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new Sha384HashFunction(transformer.Transform(this._expr));
        }
    }

#endif
}
