#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Referent.cs" company="Ian Horswill">
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
using Imaginarium.Parsing;

namespace Imaginarium.Ontology
{
    /// <summary>
    /// An object within an ontology (e.g. a Concept or Individual)
    /// </summary>
    [DebuggerDisplay("{" + nameof(Text) + "}")]
    public abstract class Referent
    {
        /// <summary>
        /// The ontology to which this belongs
        /// </summary>
        public readonly Ontology Ontology;

        /// <summary>
        /// Make a new referent.
        /// </summary>
        /// <param name="name">Name of the referent</param>
        /// <param name="ephemeral">True if this referent will exist only in one generator, and is not being added to the ontology</param>
        /// <param name="ontology">Ontology within which the referent exists</param>
        protected Referent(Ontology ontology, string[] name, bool ephemeral = false)
        {
            Ontology = ontology;
            Ontology.CheckTerminologyCanBeAdded(name, GetType(), !ephemeral);
        }

        /// <summary>
        /// True if this object's name matches the specified token string.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMemberInSuper.Global
        public abstract bool IsNamed(string[] tokens);

        /// <summary>
        /// String form of standard name of this object
        /// </summary>
        public virtual string Text => Tokenizer.Untokenize(StandardName);

        /// <summary>
        /// Tokenized form of the standard name of this object
        /// </summary>
        public abstract string[] StandardName { get; }
    
        /// <inheritdoc />
        public override string ToString() => Text;
    }
}