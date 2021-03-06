﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Parser.cs" company="Ian Horswill">
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Imaginarium.Driver;
using Imaginarium.Ontology;

namespace Imaginarium.Parsing
{
    /// <summary>
    /// Implements methods for scanning input tokens and backtracking.
    /// </summary>
    public partial class Parser
    {
        /// <inheritdoc />
        public Parser(Ontology.Ontology o, params Func<Parser, IEnumerable<SentencePattern>>[] commandSets)
        {
            Ontology = o;
            InitializeConstituents();
            Is = MatchCopula;
            Has = MatchHave;
            Count = () => MatchInt(out ParsedCount);
            UpperBound = () => MatchFloat(out ParsedUpperBound);
            LowerBound = () => MatchFloat(out ParsedLowerBound);
            StandardSentencePatterns(o);
            foreach (var set in commandSets)
                SentencePatterns.AddRange(set(this));
        }

        /// <summary>
        /// Ontology into which this parser loads new content
        /// </summary>
        public readonly Ontology.Ontology Ontology;
    
        internal static bool SingularDeterminer(string word) => word == "a" || word == "an";

        /// <summary>
        /// Grammatical number feature
        /// </summary>
        public enum Number
        {
            /// <summary>
            /// The noun or verb is in its singular form
            /// </summary>
            Singular,
            /// <summary>
            /// The noun or verb is in its plural form
            /// </summary>
            Plural
        }

        /// <summary>
        /// Files from the current project (generator) currently being loaded
        /// </summary>
        public static List<string> LoadedFiles = new List<string>();
        /// <summary>
        /// File currently being loaded
        /// </summary>
        public static string CurrentSourceFile;
        /// <summary>
        /// Line of the file we're currently loading
        /// </summary>
        public static int CurrentSourceLine;

        /// <summary>
        /// Rules for the different sentential forms understood by the system.
        /// Each consists of a pattern to recognize the form and store its components in static fields such
        /// as Subject, Object, and VerbNumber, and an Action to perform updates to the ontology based on
        /// the data stored in those static fields.  The Check option is used to insure features are properly
        /// set.
        /// </summary>
        public readonly List<SentencePattern> SentencePatterns = new List<SentencePattern>();

        /// <summary>
        /// Return all rules whose keywords overlap the specified set of tokens
        /// </summary>
        /// <param name="tokens">Words to check against rule keywords</param>
        /// <returns>Rules with keywords in common</returns>
        public IEnumerable<SentencePattern> RulesMatchingKeywords(IEnumerable<string> tokens) =>
            SentencePatterns.Where(r => r.HasCommonKeywords(tokens));

        /// <summary>
        /// Rule in which grammatical error was detected.
        /// </summary>
        public static SentencePattern RuleTriggeringException;

        /// <summary>
        /// When parsing throws an exception, the specific sentence being parsed at the time can be found here.
        /// </summary>
        public static string InputTriggeringException;

        /// <summary>
        /// Finds the matching Syntax rule for sentence and runs its associated action.
        /// </summary>
        /// <param name="sentence">User input (either an ontology statement or a command)</param>
        /// <returns>True if command altered the ontology.</returns>
        public bool ParseAndExecute(string sentence)
        {
            sentence = sentence.TrimEnd(' ', '.');
            InputTriggeringException = sentence;
            // Load text
            Input.Clear();
            Input.AddRange(Tokenizer.Tokenize(sentence));

            RuleTriggeringException = null;
            var rule = SentencePatterns.FirstOrDefault(r =>
            {
                RuleTriggeringException = r;
                return r.Try();
            });
            RuleTriggeringException = null;

            if (rule == null)
                throw new GrammaticalError($"Unknown sentence pattern: {sentence}", 
                    "This doesn't match any of the sentence patterns I know");

            InputTriggeringException = null;

            return !rule.IsCommand;
        }

        /// <summary>
        /// Parse and execute a series of statements
        /// </summary>
        public void ParseAndExecute(params string[] statements)
        {
            foreach (var sentence in statements)
                ParseAndExecute(sentence);
        }

        /// <summary>
        /// Re-initializes all information associated with parsing.
        /// </summary>
        public void ResetParser()
        {
            ResetConstituentInformation();

            // Initialize state
            currentTokenIndex = 0;
        }

        #region Token matching
        /// <summary>
        /// Attempt to match next token to TOKEN.  If successful, returns true and advances to next token.
        /// </summary>
        /// <param name="token">Token to match to next token in input</param>
        /// <returns>Success</returns>
        public bool Match(string token)
        {
            if (EndOfInput)
                return false;

            if (String.Equals(CurrentToken, token, StringComparison.OrdinalIgnoreCase))
            {
                currentTokenIndex++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to match the specified series of tokens.
        /// Each token must match in order.  If any token fails to match, state is reset
        /// to the state before the call.
        /// </summary>
        /// <param name="tokens">Tokens to match</param>
        /// <returns>True on success</returns>
        public bool Match(params string[] tokens)
        {
            var s = State;
            foreach (var t in tokens)
                if (!Match(t))
                {
                    ResetTo(s);
                    return false;
                }

            return true;
        }

        /// <summary>
        /// Attempt to match next token to TOKEN.  If successful, returns true and advances to next token.
        /// </summary>
        /// <param name="tokenPredicate">Predicate to apply to next token</param>
        /// <returns>Success</returns>
        public bool Match(Func<string, bool> tokenPredicate)
        {
            if (tokenPredicate(CurrentToken))
            {
                currentTokenIndex++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempt to match token to a conjugation of "be"
        /// </summary>
        /// <returns>True on success</returns>
        public bool MatchCopula()
        {
            if (Match("is"))
            {
                VerbNumber = Number.Singular;
                return true;
            }
            if (Match("are"))
            {
                VerbNumber = Number.Plural;
                return true;
            }

            return false;
        }

        /// <summary>
        /// True if argument is a conjugation of "be"
        /// </summary>
        public static bool IsCopula(string s)
        {
            return s == "is" || s == "are";
        }

        /// <summary>
        /// Attempt to match token to a conjugation of "have"
        /// </summary>
        /// <returns>True on success</returns>
        public bool MatchHave()
        {
            if (Match("has"))
            {
                VerbNumber = Number.Singular;
                return true;
            }
            if (Match("have"))
            {
                VerbNumber = Number.Plural;
                return true;
            }

            return false;
        }

        /// <summary>
        /// True if argument is a conjugation of "have"
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsHave(string s)
        {
            return s == "has" || s == "have";
        }

        private static readonly string[] NumberWords =
            { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten"};
    
        internal static int? IntFromWord(string s)
        {
            var value = Array.IndexOf(NumberWords, s);
            if (value >= 0 || Int32.TryParse(s, out value))
                return value;

            return null;
        }

        /// <summary>
        /// Attempt to match token to an integer.  If successful, writes number to out arg.
        /// </summary>
        /// <param name="number">Variable or field to write result back to</param>
        /// <returns>True on success</returns>
        public bool MatchInt(out int number)
        {
            var token = CurrentToken;
            if (int.TryParse(token, out number))
            {
                SkipToken();
                return true;
            }

            number = Array.IndexOf(NumberWords, token);

            var success = number >= 0;
            if (success)
                SkipToken();

            return success;
        }

        /// <summary>
        /// Attempt to match token to a floating-point number.  If successful, writes number to out arg.
        /// </summary>
        /// <param name="number">Variable or field to write result back to</param>
        /// <returns>True on success</returns>
        public bool MatchFloat(out float number)
        {
            var token = CurrentToken;
            if (Single.TryParse(token, out number))
            {
                SkipToken();
                return true;
            }

            number = Array.IndexOf(NumberWords, token);

            var success = number >= 0;
            if (success)
                SkipToken();

            return success;
        }

        /// <summary>
        /// Attempts to match tokens to the name of a known monadic concept (CommonNoun or Adjective)
        /// </summary>
        /// <returns>The concept, if successful, or null</returns>
        public TReferent MatchTrie<TReferent>(TokenTrie<TReferent> trie)
            where TReferent : Referent
        {
            var old = State;
            var concept = trie.Lookup(Input, ref CurrentTokenIndex);
            if (concept != null)
                return concept;
            ResetTo(old);
            return null;
        }

        /// <summary>
        /// Skip to the next token in the input
        /// </summary>
        public void SkipToken()
        {
            if (EndOfInput)
                throw new InvalidOperationException("Attempt to skip past end of input");
            currentTokenIndex++;
        }

        /// <summary>
        /// Skip over all remaining tokens, to the end of the input.
        /// </summary>
        public void SkipToEnd()
        {
            currentTokenIndex = Input.Count;
        }

        /// <summary>
        /// "Unread" the last token
        /// </summary>
        public void Backup()
        {
            currentTokenIndex--;
        }
        #endregion

        #region State variables
        /// <summary>
        /// List of tokens to be parsed
        /// </summary>
        public List<string> Input => TokenStream;

        /// <summary>
        /// Index within input of the next token to be matched
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private int currentTokenIndex
        {
            get => CurrentTokenIndex;
            set => CurrentTokenIndex = value;
        }

        /// <summary>
        /// True if all tokens have already been read
        /// </summary>
        public bool EndOfInput => currentTokenIndex == Input.Count;
        /// <summary>
        /// Token currently being processed.
        /// Fails if EndOfInput.
        /// </summary>
        public string CurrentToken => Input[currentTokenIndex];
        #endregion

        #region State maintenance
        /// <summary>
        /// Current state of the parser.
        /// </summary>
        internal ScannerState State => new ScannerState(currentTokenIndex);

        /// <summary>
        /// The words from the current sentence that have not yet been matched by the sentence pattern currently being tried.
        /// </summary>
        public string RemainingInput
        {
            get
            {
                var b = new StringBuilder();
                for (var i = currentTokenIndex; i < Input.Count; i++)
                {
                    b.Append(Input[i]);
                    b.Append(' ');
                }

                return b.ToString();
            }
        }

        /// <summary>
        /// Saved position within the input sentence being parsed
        /// </summary>
        internal struct ScannerState
        {
            /// <summary>
            /// Save index within TokenStream of the token that was currently being matched
            /// </summary>
            public readonly int CurrentTokenIndex;

            internal ScannerState(int currentTokenIndex)
            {
                CurrentTokenIndex = currentTokenIndex;
            }
        }

        /// <summary>
        /// Back up to the specified position in the input
        /// </summary>
        /// <param name="s"></param>
        internal void ResetTo(ScannerState s)
        {
            currentTokenIndex = s.CurrentTokenIndex;
        }


        /// <summary>
        /// Sentence currently being parsed
        /// </summary>
        public readonly List<string> TokenStream = new List<string>();
        /// <summary>
        /// Index
        /// </summary>
        public int CurrentTokenIndex;

        #endregion

        #region Definition files
        /// <summary>
        /// Returns full path for library definitions for the specified noun.
        /// </summary>
        public string DefinitionFilePath(Referent referent)
        {
            var fileName = referent.Text;
            return DefinitionFilePath(fileName);
        }

        internal static bool NameIsValidFilename(Referent referent) =>
            NameIsValidFilename(referent.Text);

        internal static bool NameIsValidFilename(string fileName) =>
            fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

        /// <summary>
        /// Returns the full path for the specified file in the definition library.
        /// </summary>
        public string DefinitionFilePath(string fileName) =>
            Path.Combine(Ontology.DefinitionsDirectory, fileName + DataFiles.SourceExtension);

        /// <summary>
        /// Returns the full path for the specified list file in the definition library.
        /// </summary>
        public string ListFilePath(string fileName)
        {
            var definitionFilePath = Path.Combine(Ontology.DefinitionsDirectory, fileName + DataFiles.ListExtension);
            return definitionFilePath;
        }

        /// <summary>
        /// Load definitions for noun, if there is a definition file for it.
        /// Called when noun is first added to ontology.
        /// </summary>
        public void MaybeLoadDefinitions(Referent referent)
        {
            if (Ontology.DefinitionsDirectory != null && NameIsValidFilename(referent) && File.Exists(DefinitionFilePath(referent)))
            {
                var p = new Parser(referent.Ontology);
                p.LoadDefinitions(referent);
            }
        }

        /// <summary>
        /// Add all the statements from the definition file for noun to the ontology
        /// </summary>
        public void LoadDefinitions(Referent referent)
        {
            var path = DefinitionFilePath(referent);
            LoadDefinitions(path);
        }

        /// <summary>
        /// Load the definitions found in the specified file
        /// </summary>
        /// <param name="path">Path to the file to load</param>
        /// <param name="throwOnErrors">True if errors should throw exceptions.  False, if list of exceptions should be returned instead</param>
        /// <returns>List of exceptions if throwOnErrors is false and there were exceptions.  Otherwise null.</returns>
        public List<Exception> LoadDefinitions(string path, bool throwOnErrors = true)
        {
            var downCased = path.ToLower();
            if (LoadedFiles.Contains(downCased))
                return null;

            LogFile.Log("Loading " + path);
            LoadedFiles.Add(downCased);
            var oldPath = CurrentSourceFile;
            var oldLine = CurrentSourceLine;
            CurrentSourceFile = path;
            CurrentSourceLine = 0;

            var assertions = File.ReadAllLines(path);
            List<Exception> errors = throwOnErrors ? null : new List<Exception>();
            foreach (var def in assertions)
            {
                CurrentSourceLine++;
                var uncommented = RemoveAfter(RemoveAfter(def, "#"), "//");
                var trimmed = uncommented.Trim();
                if (trimmed != "")
                {
                    if (throwOnErrors)
                        ParseAndExecute(trimmed);
                    else
                    {
                        try
                        {
                            ParseAndExecute(trimmed);
                        }
                        catch (Exception e)
                        {
                            errors.Add(e);
                        }
                    }

                }
            }

            LogFile.Log("Finished loading of " + path);
            CurrentSourceFile = oldPath;
            CurrentSourceLine = oldLine;

            return errors;
        }

        /// <summary>
        /// Remove all text from s starting with the first occurrence of commentMarker.
        /// If commentMarker doesn't appear, string is left unchanged.
        /// </summary>
        private static string RemoveAfter(string s, string commentMarker)
        {
            var index = s.IndexOf(commentMarker,StringComparison.InvariantCulture);
            if (index < 0)
                return s;
            return s.Substring(0, index);
        }
        #endregion

        #region Utilities

        /// <summary>
        /// Take a chunk of tokens, and remove "(number)" from the end, returning the stripped
        /// text and the number.  If there is no number at the end, just return the original
        /// text and 1.
        /// </summary>
        internal (string[] strippedText, float relativeFrequency) ParseRelativeFrequencyFromText(string[] text)
        {
            var length = text.Length;
            if (length > 2 && text[length - 3] == "(")
            {
                if (text[length - 1] != ")" || !float.TryParse(text[length - 2], out var relativeFrequency))
                    throw new GrammaticalError($"I don't understand the noun phrase {text.Untokenize()}.");
                var stripped = text.Take(length - 3).ToArray();
                return (stripped, relativeFrequency);
            }

            return (text, 1);
        }

        /// <summary>
        /// Check for '(number)' in the input stream.  If it's there, consume those tokens
        /// and return the number.  Otherwise, leave the input stream unchanged and return 1.
        /// </summary>
        internal float ParseRelativeFrequency()
        {
            if (EndOfInput || !Match("("))
                // There's no relative frequency annotation
                return 1;
            if (EndOfInput || !float.TryParse(CurrentToken, out var relativeFrequency))
                throw new GrammaticalError($"I expected a number after the '(', but instead got {(EndOfInput?"end of line":CurrentToken)}");
            currentTokenIndex++;
            if (EndOfInput)
                throw new GrammaticalError($"Expected a ')' after '({relativeFrequency}' but instead the line ended");
            if (!Match(")"))
                throw new GrammaticalError($"Expected a ')' after '({relativeFrequency}' but instead got {CurrentToken}");
            return relativeFrequency;
        }
        #endregion
    }
}