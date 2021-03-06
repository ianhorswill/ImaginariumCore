﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Adjective.cs" company="Ian Horswill">
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

using System.Linq;
using Imaginarium.Parsing;

namespace Imaginarium.Ontology
{
    /// <summary>
    /// A monadic predicate that is surfaced in English as an adjective.
    /// </summary>
    public class Adjective : MonadicConcept
    {
        /// <inheritdoc />
        public override string Description
        {
            get
            {
                var d = base.Description;
                return IsSilent ? $"{d} silent" : d;
            }
        }

        /// <inheritdoc />
        protected override string DictionaryStylePartOfSpeech => "adj.";

        internal Adjective(Ontology ontology, string[] name) : base(ontology, name)
        {
            Name = name;
            Ontology.AllAdjectives[name] = this;
            Ontology.Store(name, this);
            Driver.Driver.AppendResponseLine($"Learned the adjective <b><i>{name.Untokenize()}</i></b>.");
        }

        /// <summary>
        /// Number of alternative sets or implications this adjective is involved in
        /// </summary>
        public int ReferenceCount;

        /// <summary>
        /// True if this is an adjective that can apply to an individual of the specified kind.
        /// </summary>
        /// <param name="noun">Noun representing a kind of object</param>
        /// <returns>True if this adjective is allowed to apply to objects of the specified kind.</returns>
        public bool RelevantTo(CommonNoun noun)
        {
            if (noun.RelevantAdjectives.Contains(this))
                return true;
            return noun.Superkinds.Any(RelevantTo);
        }

        /// <summary>
        /// Token(s) that identify the adjective
        /// </summary>
        public readonly string[] Name;

        /// <inheritdoc />
        public override string[] StandardName => Name;

        /// <summary>
        /// Suppress this adjective during text generation.
        /// </summary>
        public bool IsSilent { get; set; }

        /// <inheritdoc />
        public override bool IsNamed(string[] tokens) => Name.SameAs(tokens);
    }
}
