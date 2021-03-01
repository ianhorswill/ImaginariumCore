#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AP.cs" company="Ian Horswill">
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

using Imaginarium.Ontology;

namespace Imaginarium.Parsing
{
    /// <summary>
    /// Represents a phrase denoting an adjective
    /// </summary>
// ReSharper disable once InconsistentNaming
    public class AP : ReferringExpression<Adjective>
    {
        /// <summary>
        /// The Adjective object referred to by the parsed phrase
        /// </summary>
        public Adjective Adjective => Concept;
        /// <summary>
        /// True if the adjectival phrase is prefixed by "not"  or "non-"
        /// </summary>
        public bool IsNegated;

        /// <summary>
        /// How often this adjective it to be chosen compared to other adjectives in the same list.
        /// </summary>
        public float RelativeFrequency = 1;

        /// <summary>
        /// The adjective referred to, but in the form of a MCL.
        /// </summary>
        public MonadicConceptLiteral MonadicConceptLiteral => new MonadicConceptLiteral(Adjective, !IsNegated);

        /// <inheritdoc />
        protected override Adjective GetConcept()
        {
            var (text, relativeFrequency) = Parser.ParseRelativeFrequencyFromText(Text);
            RelativeFrequency = relativeFrequency;
            return Ontology.Adjective(text) ?? new Adjective(Ontology, text);
        }

        /// <inheritdoc />
        public override bool ValidBeginning(string firstToken) => firstToken != "a" && firstToken != "an";

        /// <inheritdoc />
        public override void ParseModifiers()
        {
            var tok = CurrentToken.ToLower();
            if (tok == "not" || tok == "non" || tok == "never")
            {
                IsNegated = true;
                SkipToken();
                if (!EndOfInput && CurrentToken == "-")
                    SkipToken();
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            IsNegated = false;
            RelativeFrequency = 1;
        }

        /// <inheritdoc />
        public AP(Parser parser) : base(parser)
        {
        }
    }
}