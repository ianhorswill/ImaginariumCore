﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MonadicConceptLiteral.cs" company="Ian Horswill">
// Copyright (C) 2019, 2020 Ian Horswill
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion

using System.Diagnostics;

namespace Imaginarium.Ontology
{
    /// <summary>
    /// Represents a monadic concept or its negation
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebugString) + "}")]
    public class MonadicConceptLiteral
    {
        /// <summary>
        /// Concept referred to by this literal
        /// </summary>
        public readonly MonadicConcept Concept;
        /// <summary>
        /// Polarity of the literal.
        /// If true, then this means Concept, else !Concept.
        /// </summary>
        public readonly bool IsPositive;

        internal MonadicConceptLiteral(MonadicConcept concept, bool isPositive = true)
        {
            Concept = concept;
            IsPositive = isPositive;
        }

        internal void IncrementReferenceCount()
        {
            if (Concept is Adjective a)
                a.ReferenceCount++;
        }

        /// <summary>
        /// Makes a literal sating this monadic concept is true
        /// </summary>
        public static implicit operator MonadicConceptLiteral(MonadicConcept c)
        {
            return new MonadicConceptLiteral(c);
        }

        /// <summary>
        /// Inverts a monadic concept literal
        /// </summary>
        public MonadicConceptLiteral Inverse()
        {
            return new MonadicConceptLiteral(Concept, !IsPositive);
        }

        /// <summary>
        /// Printed string representation of this literal
        /// </summary>
        public string DebugString => IsPositive ? Concept.ToString() : "!" + Concept;

        /// <inheritdoc />
        public override string ToString() => DebugString;
    }
}
