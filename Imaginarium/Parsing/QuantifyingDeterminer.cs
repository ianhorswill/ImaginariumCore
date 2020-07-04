#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QuantifyingDeterminer.cs" company="Ian Horswill">
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
using System.Linq;
using static Imaginarium.Parsing.Parser;

namespace Imaginarium.Parsing
{
    /// <summary>
    /// A matches a determiner that quantifies a noun phrase
    /// For example, a/an/one is singular, many is plural, and 3 is plural but a specific number.
    /// </summary>
    public class QuantifyingDeterminer : ClosedClassSegment
    {
        /// <summary>
        /// The token that was used as a determiner;
        /// </summary>
        public string Quantifier;

        /// <summary>
        /// Tests if the token is one of the known quantifiers
        /// </summary>
        public readonly Func<string, bool> IsQuantifier = token => 
            NonNumberQuantifierWord(token) || IntFromWord(token).HasValue;

        private static bool NonNumberQuantifierWord(string token)
        {
            if (token == "one")
                return false;

            return SingularQuantifiers.Contains(token)
                   || PluralQuantifiers.Contains(token)
                   || InvalidQuantifiers.Contains(token);
        }

        private static readonly string[] SingularQuantifiers =
        {
            "one",
            "another"
        };

        private static readonly string[] PluralQuantifiers =
        {
            "many",
            "some",
            "other"
        };

        /// <inheritdoc />
        public override IEnumerable<string> Keywords
        {
            get
            {
                foreach (var s in SingularQuantifiers)
                    yield return s;
                foreach (var s in PluralQuantifiers)
                    yield return s;
            }
        }

        private static readonly string[] InvalidQuantifiers =
        {
            "a",
        };

        /// <summary>
        /// True if this quantifier is indicating the NP is plural
        /// </summary>
        public bool IsPlural => PluralQuantifiers.Contains(Quantifier) || (ExplicitCount.HasValue && ExplicitCount.Value > 1);
        /// <summary>
        /// The quantifier includes the word "other", as in "one other", "many other", or just "other".
        /// </summary>
        public bool IsOther;
        /// <summary>
        /// The token contains a word specifically disallowed as a quantifier
        /// </summary>
        public bool IsInvalid => InvalidQuantifiers.Contains(Quantifier);
        /// <summary>
        /// When the quantifier specifies a specific number, rather than just "many", the specific number.
        /// </summary>
        public int? ExplicitCount;

        /// <inheritdoc />
        public override bool ScanTo(Func<string, bool> endPredicate)
        {
            Quantifier = CurrentToken;
            if (!Match(NonNumberQuantifierWord))
            {
                ExplicitCount = IntFromWord(Quantifier);
                if (ExplicitCount == null)
                    return false;
                SkipToken();
                Quantifier = CurrentToken;
            }
            else
                IsOther = Quantifier == "other";

            if (!IsOther && !EndOfInput && CurrentToken == "other")
            {
                IsOther = true;
                SkipToken();
            }

            return !EndOfInput && endPredicate(CurrentToken);
        }

        /// <inheritdoc />
        public override bool ScanTo(string token)
        {
            if (EndOfInput)
                return false;

            Quantifier = CurrentToken;
            if (!Match(IsQuantifier))
                return false;

            IsOther = Quantifier == "other";

            if (!IsOther && !EndOfInput && CurrentToken == "other")
            {
                IsOther = true;
                SkipToken();
            }

            return !EndOfInput && CurrentToken == token;
        }

        /// <inheritdoc />
        public override bool ScanToEnd(bool failOnConjunction = true)
        {
            Quantifier = CurrentToken;
            return Match(IsQuantifier) && EndOfInput;
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            IsOther = false;
            ExplicitCount = null;
        }

        /// <inheritdoc />
        public QuantifyingDeterminer(Parser parser) : base(parser)
        {
        }
    }
}
