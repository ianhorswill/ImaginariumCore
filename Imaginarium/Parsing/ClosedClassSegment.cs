#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClosedClassSegment.cs" company="Ian Horswill">
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

using System;
using System.Collections.Generic;

namespace Imaginarium.Parsing
{
    /// <summary>
    /// Base class for segments that can only be filled by fixed words and phrases
    /// </summary>
    public abstract class ClosedClassSegment : Segment
    {
        /// <summary>
        /// The token that was used as a determiner;
        /// </summary>
        public string[] MatchedText;
        /// <summary>
        /// Tokens that can start a phrase this segment can match
        /// </summary>
        public string[] PossibleBeginnings;

        /// <summary>
        /// Tests if the token is one of the known quantifiers
        /// </summary>
        public Func<string, bool> IsPossibleStart;

        /// <summary>
        /// Closed-class words that can be used in this segment
        /// </summary>
        public abstract IEnumerable<string> Keywords { get; }

        /// <inheritdoc />
        protected ClosedClassSegment(Parser parser) : base(parser)
        { }
    }
}

