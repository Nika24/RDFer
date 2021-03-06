﻿/*

Copyright Robert Vesse 2009-12
rvesse@vdesign-studios.com

------------------------------------------------------------------------

This file is part of dotNetRDF.

dotNetRDF is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

dotNetRDF is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with dotNetRDF.  If not, see <http://www.gnu.org/licenses/>.

------------------------------------------------------------------------

dotNetRDF may alternatively be used under the LGPL or MIT License

http://www.gnu.org/licenses/lgpl.html
http://www.opensource.org/licenses/mit-license.php

If these licenses are not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Query.Aggregates;
using VDS.RDF.Nodes;

namespace VDS.RDF.Query.Expressions.Primary
{
    /// <summary>
    /// Class for representing Aggregate Expressions which have Numeric Results
    /// </summary>
    public class AggregateTerm 
        : BaseUnaryExpression
    {
        private ISparqlAggregate _aggregate;

        /// <summary>
        /// Creates a new Aggregate Expression Term that uses the given Aggregate
        /// </summary>
        /// <param name="aggregate">Aggregate</param>
        public AggregateTerm(ISparqlAggregate aggregate)
            : base(null)
        {
            this._aggregate = aggregate;
        }

        /// <summary>
        /// Evaluates the aggregate expression
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <param name="bindingID">Binding ID</param>
        /// <returns></returns>
        public override IValuedNode Evaluate(SparqlEvaluationContext context, int bindingID)
        {
            IValuedNode aggValue;
            if (context.Binder.IsGroup(bindingID))
            {
                BindingGroup group = context.Binder.Group(bindingID);
                context.Binder.SetGroupContext(true);
                aggValue = this._aggregate.Apply(context, group.BindingIDs);
                context.Binder.SetGroupContext(false);
            }
            else
            {
                aggValue = this._aggregate.Apply(context);
            }
            return aggValue;
        }

        /// <summary>
        /// Gets the Aggregate this Expression represents
        /// </summary>
        public ISparqlAggregate Aggregate
        {
            get
            {
                return this._aggregate;
            }
        }

        /// <summary>
        /// Gets the String representation of the Aggregate Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this._aggregate.ToString();
        }

        /// <summary>
        /// Gets the enumeration of variables that are used in the the aggregate expression
        /// </summary>
        public override IEnumerable<String> Variables
        {
            get
            {
                return this._aggregate.Expression.Variables;
            }
        }

        /// <summary>
        /// Gets the Type of the Expression
        /// </summary>
        public override SparqlExpressionType Type
        {
            get
            {
                return SparqlExpressionType.Aggregate;
            }
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public override string Functor
        {
            get
            {
                return this._aggregate.Functor;
            }
        }

        /// <summary>
        /// Gets the Arguments of the Expression
        /// </summary>
        public override IEnumerable<ISparqlExpression> Arguments
        {
            get
            {
                return this._aggregate.Arguments;
            }
        }

        /// <summary>
        /// Transforms the Expression using the given Transformer
        /// </summary>
        /// <param name="transformer">Expression Transformer</param>
        /// <returns></returns>
        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return this;
        }
    }
}
