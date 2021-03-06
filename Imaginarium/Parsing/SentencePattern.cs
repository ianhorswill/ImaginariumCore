﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SentencePattern.cs" company="Ian Horswill">
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using static Imaginarium.Parsing.Parser;

namespace Imaginarium.Parsing
{
    /// <summary>
    /// Possible pattern for a sentence that can be matched against it
    /// </summary>
    [DebuggerDisplay("{" + nameof(HelpDescription) + "}")]
    public class SentencePattern
    {
        /// <summary>
        /// Parser to which this sentence pattern belongs
        /// </summary>
        public readonly Parser Parser;

        /// <summary>
        /// Used in SubjectNounList to ensure all NPs are in base form (singular but no determiner)
        /// </summary>
        public static bool ForceBaseForm(NP np)
        {
            np.ForceCommonNoun = true;
            if (np.Number == Number.Plural)
                return false;
            np.Number = Number.Singular;
            return true;
        }

        #region Constructors
        /// <summary>
        /// Makes a new sentence pattern
        /// </summary>
        /// <param name="p">Parser to which the pattern will be added</param>
        /// <param name="constituents">Segments and other elements to match against the input, in order</param>
        public SentencePattern(Parser p, params object[] constituents)
        {
            Parser = p;
            this.constituents = constituents;
        }

        /// <summary>
        /// Adds an action to a Syntax rule.
        /// This is here only so that the syntax constructor can take the constituents as a params arg,
        /// which makes the code a little more readable.
        /// </summary>
        public SentencePattern Action(Action a)
        {
            action = a;
            return this;
        }

        /// <summary>
        /// Adds a set of feature checks to a Syntax rule.
        /// This is here only so that the syntax constructor can take the constituents as a params arg,
        /// which makes the code a little more readable.
        /// </summary>
        public SentencePattern Check(params Func<bool>[] checks)
        {
            validityTests = checks;
            return this;
        }
        #endregion

        /// <summary>
        /// Closed class words used in this sentence template
        /// </summary>
        public IEnumerable<string> Keywords
        {
            get
            {
                foreach (var c in constituents)
                    switch (c)
                    {
                        case string s:
                            yield return s;
                            break;

                        case ClosedClassSegment ccs:
                            foreach (var s in ccs.Keywords)
                                yield return s;
                            break;
                    }
            }
        }

        /// <summary>
        /// True if the tokens have a word in common with the keywords of this rule
        /// </summary>
        /// <param name="tokens">Words to check against the keywords of this rule.</param>
        /// <returns>True if there is a word in common</returns>
        public bool HasCommonKeywords(IEnumerable<string> tokens) => tokens.Any(t => Keywords.Contains(t));

        /// <summary>
        /// Try to make a syntax rule and run its action if successful.
        /// </summary>
        /// <returns>True on success</returns>
        public bool Try()
        {
            Parser.ResetParser();
            var old = Parser.State;

            if (MatchConstituents())
                if (Parser.EndOfInput)
                {
                    // Check validity tests and fail if one fails
                    if (validityTests != null)
                    {
                        foreach (var test in validityTests)
                            if (!test())
                            {
                                if (LogMatch)
                                {
                                    var d = (Delegate)test;
                                    Driver.Driver.AppendResponseLine("Validity test failed: "+d.Method.Name);
                                }

                                goto fail;
                            }
                    }

                    action();
                    return true;
                }
                else if (LogMatch)
                {
                    Driver.Driver.AppendResponseLine("Remaining input blocked match: "+Parser.CurrentToken);
                }

            fail:
            Parser.ResetTo(old);
            return false;
        }

        /// <summary>
        /// Try to match the constituents of a syntax rule, resetting the parser on failure.
        /// </summary>
        /// <returns>True if successful</returns>
        private bool MatchConstituents()
        {
            if (constituents[0] is string firstToken 
                && string.Compare(Parser.CurrentToken, firstToken, StringComparison.InvariantCultureIgnoreCase) != 0)
                // Fast path.  This also reduces spam in the logging output
                return false;

            if (LogMatch) Driver.Driver.AppendResponseLine("Try parse rule: " + SentencePatternDescription);

            var cut = false;
            for (int i = 0; i < constituents.Length; i++)
            {
                var c = constituents[i];

                if (c.Equals("!"))
                {
                    cut = true;
                    continue;
                }

                if (LogMatch)
                {
                    var conName = ConstituentToString(c);
                    Driver.Driver.AppendResponseLine($"Constituent {conName}");
                    Driver.Driver.AppendResponseLine($"Remaining input: {Parser.RemainingInput}");
                }
                if (BreakOnMatch)
                    Debugger.Break();
                if (c is string str)
                {
                    if (!Parser.Match(str))
                    {
                        if (cut)
                            ThrowFailedMatch($"I expected to see '{str}' but got '{Parser.CurrentToken}'");
                        return false;
                    }
                }
                else if (c is Segment seg)
                {
                    if (i == constituents.Length - 1)
                    {
                        // Last one
                        if (!seg.ScanToEnd())
                        {
                            if (cut)
                                ThrowFailedMatch($"I could not match {seg.Name}");
                            return false;
                        }
                    }
                    else
                    {
                        // Look up the next constituent, skipping "!".
                        if (constituents[i + 1].Equals("!"))
                            i++;
                        var next = constituents[i + 1];
                        if (next is string nextStr)
                        {
                            if (!seg.ScanTo(nextStr))
                            {
                                if (cut)
                                    ThrowFailedMatch($"I could not match {seg.Name}");
                                return false;
                            }
                        }
                        else if (ReferenceEquals(next, Parser.Is))
                        {
                            if (!seg.ScanTo(IsCopula))
                            {
                                if (cut)
                                    ThrowFailedMatch($"I could not match {seg.Name}");
                                return false;
                            }
                        }
                        else if (ReferenceEquals(next, Parser.Has))
                        {
                            if (!seg.ScanTo(IsHave))
                            {
                                if (cut)
                                    ThrowFailedMatch($"I could not match {seg.Name}");
                                return false;
                            }
                        }
                        else if (next is SimpleClosedClassSegment s)
                        {
                            if (!seg.ScanTo(s.IsPossibleStart))
                            {
                                if (cut)
                                    ThrowFailedMatch($"I could not match {seg.Name}");
                                return false;
                            }
                        }
                        else if (seg is SimpleClosedClassSegment)
                        {
                            if (!seg.ScanTo(tok => true))
                            {
                                if (cut)
                                    ThrowFailedMatch($"I could not match {seg.Name}");
                                return false;
                            }
                        }
                        else if (next is QuantifyingDeterminer q)
                        {
                            if (!seg.ScanTo(q.IsQuantifier))
                            {
                                if (cut)
                                    ThrowFailedMatch($"I could not match {seg.Name}");
                                return false;
                            }
                        }
                        else if (seg is QuantifyingDeterminer)
                        {
                            if (!seg.ScanTo(tok => true))
                            {
                                if (cut)
                                    ThrowFailedMatch($"I could not match {seg.Name}");
                                return false;
                            }
                        }
                        else throw new ArgumentException("Don't know how to scan to the next constituent type");
                    }

                    if (LogMatch)
                    {
                        var text = seg.Text;
                        var asString = text != null ? text.Untokenize() : "(null)";
                        Driver.Driver.AppendResponseLine($"{seg.Name} matches {asString}");
                    }
                }
                else if (c is Func<bool> test)
                {
                    if (!test())
                        return false;
                }
                else throw new ArgumentException($"Unknown type of constituent {c}");

            }

            if (LogMatch) Driver.Driver.AppendResponseLine("Succeeded parsing rule: " + SentencePatternDescription);
            return true;
        }

        private void ThrowFailedMatch(string s)
        {
            throw new GrammaticalError(s, s);
        }

        private static object ConstituentToString(object c)
        {
            var conName = c is Segment seg ? seg.Name : c;
            return conName;
        }

        /// <summary>
        /// Matching routines for the constituents of the sentential form, in order.
        /// For example: Subject, Is, Object
        /// </summary>
        private readonly object[] constituents;
        /// <summary>
        /// Procedure to run if this sentential form matches the input.
        /// This procedure should update the ontology based on the data stored in the constituents
        /// during the matching phase.
        /// </summary>
        private Action action;
        /// <summary>
        /// Additional sanity checks to perform, e.g. for checking plurality.
        /// </summary>
        private Func<bool>[] validityTests;

        /// <summary>
        /// True if this is a command rather than a declaration
        /// </summary>
        public bool IsCommand;
        /// <summary>
        /// True if we should trigger a breakpoint whenever the parser tries to match this sentence pattern to an input.
        /// </summary>
        public bool BreakOnMatch;
        /// <summary>
        /// True if logging all parsing
        /// </summary>
        public static bool LogAllParsing;
        /// <summary>
        /// True if logging this one rule, regardless of LogAllParsing
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public bool _logMatch;

        /// <summary>
        /// True if we should log the parsing of this rule right now.
        /// </summary>
        public bool LogMatch => _logMatch || LogAllParsing;

        /// <summary>
        /// Force parser to breakpoint when trying to match this pattern.
        /// </summary>
        public SentencePattern DebugMatch()
        {
            BreakOnMatch = true;
            return this;
        }

        /// <summary>
        /// Elaborately log the matching process for this pattern
        /// </summary>
        /// <returns></returns>
        public SentencePattern Log()
        {
            _logMatch = true;
            return this;
        }

        /// <summary>
        /// Mark this pattern as a command rather than an assertion.
        /// </summary>
        /// <returns></returns>
        public SentencePattern Command()
        {
            IsCommand = true;
            return this;
        }

        /// <summary>
        /// User-facing description of this form.
        /// </summary>
        public string DocString;

        /// <summary>
        /// Adds the specified documentation string to the Syntax form.
        /// </summary>
        public SentencePattern Documentation(string doc)
        {
            DocString = doc;
            return this;
        }

        private static readonly StringBuilder Buffer = new StringBuilder();
        /// <summary>
        /// Text describing this pattern and what it does
        /// </summary>
        public string HelpDescription
        {
            get
            {
                Buffer.Length = 0;
                var firstOne = true;
                Buffer.Append("<b>");
                foreach (var c in constituents)
                {
                    if (c.Equals("!"))
                        continue;

                    if (firstOne)
                        firstOne = false;
                    else Buffer.Append(' ');

                    Buffer.Append(ConstituentName(c));
                }

                Buffer.Append("</b>\n");
                Buffer.Append(DocString??"");
                Buffer.AppendLine();
                return Buffer.ToString();
            }
        }

        /// <summary>
        /// Text describing this pattern and what it does
        /// </summary>
        public string SentencePatternDescription
        {
            get
            {
                Buffer.Length = 0;
                var firstOne = true;
                Buffer.Append("<b>");
                foreach (var c in constituents)
                {
                    if (c.Equals("!"))
                        continue;

                    if (firstOne)
                        firstOne = false;
                    else Buffer.Append(' ');

                    Buffer.Append(ConstituentName(c));
                }

                Buffer.Append("</b>");
                return Buffer.ToString();
            }
        }

        private string ConstituentName(object c)
        {
            switch (c)
            {
                case string s:
                    return s;

                case ClosedClassSegment ccs:
                    return ccs.Name;

                case Segment seg:
                    return $"<i><color=grey>{seg.Name}</color></i>";

                case Func<bool> f:
                    if (f == Parser.Is)
                        return "is/are";
                    if (f == Parser.Has)
                        return "have/has";
                    if (f == Parser.Count)
                        return "<i><color=grey>Count</color></i>";
                    if (f == Parser.LowerBound)
                        return "<i><color=grey>LowerBound</color></i>";
                    if (f == Parser.UpperBound)
                        return "<i><color=grey>UpperBound</color></i>";
                    return $"<i>{f}</i>";

                default:
                    return $"<i>{c}</i>";
            }
        }
    }
}