﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Inflection.cs" company="Ian Horswill">
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
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Channels;
using Imaginarium.Driver;
using File = System.IO.File;

namespace Imaginarium.Parsing
{
    /// <summary>
    /// Implements a best effort to convert between English plural and singular noun inflections
    /// </summary>
    public static class Inflection
    {
        static Inflection()
        {
            Inflections = InflectionProcess.FromFile(DataFiles.PathTo("Inflections", "Regular nouns"));

            foreach (var entry in File.ReadAllLines(DataFiles.PathTo("Inflections", "Irregular nouns")))
            {
                var split = entry.Split('\t');
                var singular = split[0];
                var plural = split[1];
                IrregularPlurals[singular] = plural;
                IrregularSingulars[plural] = singular;
            }

            IrregularVerbs = new Spreadsheet(DataFiles.PathTo(
                    "Inflections", "Irregular verbs", ".csv"),
                "Base form");
        }

        private static readonly Spreadsheet IrregularVerbs;

        private static readonly HashSet<string> Prepositions = new HashSet<string>() 
        {
            "on", "in", "at", "since", "for", "ago", "before", "to", "past", "til", "until", "by",
            "next", "beside", "over", "under", "above", "below", "across", "through", "into", "onto",
            "towards", "toward", "from", "of",  "about", "with"
        };

        /// <summary>
        /// True if the word can be used as a preposition
        /// </summary>
        public static bool IsPreposition(string word) => Prepositions.Contains(word);

        /// <summary>
        /// The plural form of a singular noun
        /// </summary>
        public static string[] PluralOfNoun(string[] singular)
        {
            var plural = new string[singular.Length];
            singular.CopyTo(plural,0);
            var last = singular.Length - 1;
            plural[last] = PluralOfNoun(singular[last]);
            return plural;
        }

        /// <summary>
        /// The plural form of a one-word singular noun
        /// </summary>
        public static string PluralOfNoun(string singular)
        {
            if (IrregularPlurals.TryGetValue(singular, out string plural))
                return plural;
            foreach (var i in Inflections)
                if (i.MatchSingularForPlural(singular))
                    return i.InflectSingularForPlural(singular);
            throw new GrammaticalError($"'{singular}' appears to be a singular noun, but I can't find a plural inflection for it",
                $"In this context, the term '<i>{singular}</i>' appears to be a singular noun, but I can't find a plural inflection for it");
        }


        /// <summary>
        /// The singular form of a one-word plural noun
        /// </summary>
        public static string SingularOfNoun(string plural)
        {
            if (IrregularSingulars.TryGetValue(plural, out string singular))
                return singular;
            foreach (var i in Inflections)
                if (i.MatchPluralForSingular(plural))
                    return i.InflectPluralForSingular(plural);
            throw new GrammaticalError($"'{plural}' appears to be a plural noun, but I can't find a singular inflection for it",
                $"In this context, the term '<i>{plural}</i>' appears to be a plural noun, but I can't find a singular inflection for it");
        }

        /// <summary>
        /// The singular form of a plural noun
        /// </summary>
        public static string[] SingularOfNoun(string[] plural)
        {
            var singular = new string[plural.Length];
            plural.CopyTo(singular,0);
            var last = plural.Length - 1;
            singular[last] = SingularOfNoun(plural[last]);
            return singular;
        }

        /// <summary>
        /// Heuristically guess is this one-word noun is in plural form
        /// </summary>
        public static bool NounAppearsPlural(string plural)
        {
            if (IrregularSingulars.ContainsKey(plural))
                return true;
            foreach (var i in Inflections)
                if (i.MatchPluralForSingular(plural))
                    return true;
            return false;
        }

        /// <summary>
        /// Heuristically guess is this noun is in plural form
        /// </summary>
        public static bool NounAppearsPlural(string[] plural)
        {
            return NounAppearsPlural(plural[plural.Length-1]);
        }

        /// <summary>
        /// The singular form of a plural form verb
        /// </summary>
        public static string[] SingularOfVerb(string[] plural)
        {
            if (ContainsCopula(plural))
                return ReplaceCopula(plural, "is");        return PluralOfNoun(plural);
        }

        /// <summary>
        /// The plural form of a singular form verb
        /// </summary>
        public static string[] PluralOfVerb(string[] singular)
        {
            if (ContainsCopula(singular))
                return ReplaceCopula(singular, "are");
            return SingularOfNoun(singular);
        }
        
        /// <summary>
        /// Heuristically guess if this verb is in gerund form
        /// </summary>
        public static bool IsGerund(string[] verbal)
        {
            var wordsWithIng = verbal.Where(w => w.EndsWith("ing")).ToArray();
            return ContainsCopula(verbal) || wordsWithIng.Length > 0;
        }

        /// <summary>
        /// Enumerate every potential gerund form of a (third person) plural verb
        /// It's hard to know algorithmically which is correct, so we just allow
        /// all of them.
        /// </summary>
        public static IEnumerable<string[]> GerundsOfVerb(string[] plural)
        {
            if (ContainsCopula(plural))
                yield return ReplaceCopula(plural, "being");
            else if (plural.Length == 1)
            {
                foreach (var gerund in RegularGerundsOfWord(plural[0]))
                    yield return new [] { gerund };
            }
            else if (plural.Length >= 2 && IsPreposition(plural.Last()))
            {
                foreach (var gerund in RegularGerundsOfWord(plural[0]))
                    yield return new[] { gerund, plural.Last() };
            }
        }

        /// <summary>
        /// Enumerate every possible gerund form of a single-word verb.
        /// It's hard to know algorithmically which one is correct, so we allow
        /// all of them.
        /// </summary>
        private static IEnumerable<string> RegularGerundsOfWord(string s)
        {
            if (EndsWithVowel(s))
                yield return WithoutFinalCharacter(s) + "ing";
            else
                yield return s + "ing";

            if (EndingConsonant(s, out var terminalConsonant))
            {
                yield return s + terminalConsonant + "ing";
            }
            else
            {
                yield return s.Substring(0, s.Length - 1) + "ing";
            }
        }

        /// <summary>
        /// Convert the gerund form of a verb to its base form
        /// </summary>
        public static string[] BaseFormOfGerund(string[] gerund)
        {
            var gerundList = gerund.ToList();
            if (gerund.Contains("being"))
            {
                return gerund.Replace("being", "be").ToArray();
            }

            if (gerund.Length == 1)
            {
                // Cut trailing -ing
                return new[] { BaseFormOfRegularGerundWord(gerund[0]) };
                // old
                // } else if (gerund.Length == 2 && IsPreposition(gerund[1])) {}
                //     return new [] { BaseFormOfRegularGerundWord(gerund[0]), gerund[1] };
            }

            // new
            else if (gerund.Length >= 2 && IsPreposition(gerund.Last())) {
                // apply BaseFormOfRegularGerundWord to the item in gerund that ends with "ing"
                return gerund.Where(word => word.EndsWith("ing"))
                    .Select(word => BaseFormOfRegularGerundWord(word))
                    .Concat(gerund.Where(word => !word.EndsWith("ing")))
                    .ToArray();
            }

            throw new SyntaxErrorException($"Can't determine the stem verb of gerund {gerund.Untokenize()}");
        }

        private static string BaseFormOfRegularGerundWord(string s)
        {
            if (s.EndsWith("ing"))
                s = s.Substring(0, s.Length - 3);
            var len = s.Length;
            // Removed doubled consonant
            if (len > 2 && s[len - 1] == s[len - 2])
                s = s.Substring(0, len - 1);
            return s;
        }

        private static readonly char[] Vowels = {'a', 'e', 'i', 'o', 'u'};
        private static bool IsVowel(char c) => Vowels.Contains(c);
        private static bool IsConsonant(char c) => !IsVowel(c);
        private static bool EndsWithVowel(string s) => IsVowel(FinalCharacter(s));
        //private static bool EndsWithConsonant(string s) => IsConsonant(FinalCharacter(s));

        private static bool EndingConsonant(string s, out char c)
        {
            System.Diagnostics.Debug.Assert(s.Length > 0);
            c = FinalCharacter(s);
            return IsVowel(c);
        }

        private static char FinalCharacter(string s)
        {
            return s[s.Length - 1];
        }

        private static string WithoutFinalCharacter(string s) => s.Substring(0, s.Length - 1);

        // ReSharper disable once IdentifierTypo
        private static readonly string[] CopularForms = {"is", "are", "being", "be" };
        private static bool ContainsCopula(string[] tokens) => tokens.Any(word => CopularForms.Contains(word));

        /// <summary>
        /// Replace any occurence of the copula (e.g. is/are/be/being) with the specified replacement
        /// </summary>
        public static string[] ReplaceCopula(string[] tokens, string replacement) => tokens.Select(word => CopularForms.Contains(word) ? replacement : word).ToArray();

        private static IEnumerable<T> Replace<T>(this IEnumerable<T> seq, T from, T to) =>
            seq.Select(e => e.Equals(from) ? to : e);

        private static readonly Dictionary<string, string> IrregularPlurals = new Dictionary<string, string>();

        private static readonly  Dictionary<string, string> IrregularSingulars = new Dictionary<string, string>();

        private static readonly InflectionProcess[] Inflections;

        class InflectionProcess
        {
            private readonly string singularEnding;
            private readonly string pluralEnding;

            private InflectionProcess(string singularEnding, string pluralEnding)
            {
                this.singularEnding = singularEnding;
                this.pluralEnding = pluralEnding;
            }

            public bool MatchSingularForPlural(string singular) => singular.EndsWith(singularEnding);
            public string InflectSingularForPlural(string singular) =>
                singular.Substring(0, singular.Length - singularEnding.Length) + pluralEnding;

            public bool MatchPluralForSingular(string plural) => plural.EndsWith(pluralEnding);
            public string InflectPluralForSingular(string plural) =>
                plural.Substring(0, plural.Length - pluralEnding.Length) + singularEnding;

            public static InflectionProcess[] FromFile(string path)
            {
                var lines = File.ReadAllLines(path).Where(line => !line.StartsWith("#"));
                var columns = lines.Select(line => line.Split('\t'));
                return columns.Select(line => new InflectionProcess(line[0], line[1])).ToArray();
            }
        }

        /// <summary>
        /// Determine the passive participle of a verb given its base form
        /// </summary>
        public static string[] PassiveParticiple(string[] baseForm)
        {
            if (baseForm.Length == 1 && IrregularVerbs.LookupOrNull(baseForm[0], "Passive participle") is string irregular)
            {
                return new[] {irregular};
            }

            var passive = (string[]) baseForm.Clone();
            var headPosition = passive.Length - 1;
            if (IsPreposition(passive[headPosition]))
                headPosition--;
            var head = passive[headPosition];
            var len = head.Length;

            if (head.EndsWith("e"))
                head = head + "d";
            else if (head.EndsWith("y") && len > 1 && IsConsonant(head[len - 2]))
                head = head.Substring(0, len - 1) + "ied";
            else if (head.EndsWith("c"))
                head = head + "ked";
            else if (IsConsonant(head[len - 1]) && head[len-1] != 'y' && len > 1 && IsVowel(head[len - 2]))
                head = head + head[len - 1] + "ed";
            else
                head = head + "ed";

            passive[headPosition] = head;

            return passive;
        }
    }
}
