#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Part.cs" company="Ian Horswill">
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

using System.Collections.Generic;
using System.Linq;
using Imaginarium.Parsing;

namespace Imaginarium.Ontology
{
    /// <summary>
    /// An Individual that is an intrinsic component of another individual
    /// </summary>
    public class Part : Concept
    {
        internal Part(Ontology ontology, string[] name, int count, CommonNoun kind, IEnumerable<MonadicConceptLiteral> modifiers) : base(ontology, name)
        {
            Name = name;
            Ontology.AllParts[name] = this;
            Count = count;
            Kind = kind;
            Modifiers = modifiers.ToArray();
        }

        /// <summary>
        /// Number of instances of this part in a given instance of Kind.
        /// </summary>
        public readonly int Count;
    
        /// <summary>
        /// The CatSAT domain of this variable
        /// </summary>
        public readonly CommonNoun Kind;

        /// <summary>
        /// Modifiers attached to the Kind
        /// </summary>
        public readonly MonadicConceptLiteral[] Modifiers;

        /// <summary>
        /// All Monadic concepts (Kind and Modifiers) attached to this Part.
        /// </summary>
        public IEnumerable<MonadicConceptLiteral> MonadicConcepts => Modifiers.Append(new MonadicConceptLiteral(Kind));

        /// <summary>
        /// Token string used to refer to this property
        /// </summary>
        public readonly string[] Name;

        /// <inheritdoc />
        public override string[] StandardName => Name;

        /// <inheritdoc />
        public override bool IsNamed(string[] tokens) => Name.SameAs(tokens);
    }
}