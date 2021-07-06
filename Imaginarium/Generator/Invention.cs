﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Invention.cs" company="Ian Horswill">
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
using System.Globalization;
using System.Linq;
using System.Text;
using CatSAT;
using Imaginarium.Ontology;
using Imaginarium.Parsing;

namespace Imaginarium.Generator
{
    /// <summary>
    /// Represents the output of the generator.
    /// This contains a model, which maps propositions to truth values
    /// </summary>
    public class Invention
    {
        /// <summary>
        /// The ontology in terms of which this Invention is defined.
        /// </summary>
        public readonly Ontology.Ontology Ontology;

        /// <summary>
        /// The output from the "imagine" command that created this invention.
        /// </summary>
        public List<Individual> Individuals => Generator.Individuals;

        /// <summary>
        /// The PossibleIndividuals in this invention (possible world).
        /// PossibleIndividuals are just combinations of Individuals and Inventions.
        /// </summary>
        public readonly List<PossibleIndividual> PossibleIndividuals;

        /// <summary>
        /// Returns the PossibleIndividual from within this Invention, corresponding to the specified individual.
        /// </summary>
        public PossibleIndividual PossibleIndividual(Individual i) =>
            PossibleIndividuals.FirstOrDefault(p => p.Individual == i);

        /// <summary>
        /// The index'th PossibleIndividual in this invention.
        /// </summary>
        /// <param name="index">Index of the individual.  This will be 0 if there is only one individual.</param>
        public PossibleIndividual this[int index] => PossibleIndividuals[index];

        /// <summary>
        /// Returns the PossibleIndividual within this invention of the specified individual
        /// </summary>
        public PossibleIndividual this[Individual i] => PossibleIndividual(i);

        /// <summary>
        /// Returns the PossibleIndividual with the specified name
        /// </summary>
        /// <param name="name">Name to search for</param>
        /// <returns>The PossibleIndividual or null</returns>
        public PossibleIndividual this[string name] 
            => PossibleIndividuals.Find(i => string.Compare(i.Name,name, StringComparison.InvariantCultureIgnoreCase) == 0);

        /// <summary>
        /// The Generator that created this invention
        /// </summary>
        public Generator Generator;
        /// <summary>
        /// The model of Problem most recently generated by CatSAT
        /// </summary>
        public Solution Model;

        internal Invention(Ontology.Ontology ontology, Generator generator, Solution model)
        {
            Ontology = ontology;
            Generator = generator;
            Model = model;
            PossibleIndividuals = new List<PossibleIndividual>(Individuals.Count);
            PossibleIndividuals.AddRange(Individuals.Select(i => new PossibleIndividual(i, this)));
        }

        #region Description generation
        private static readonly string[] DefaultDescriptionTemplate =
            { "[", "ContainerAndPart", "]", "[", "ProperNameIfDefined", "]", "is", "a", "[", "Modifiers", "]", "[", "Noun", "]", "[", "AllProperties", "]" };

        /// <summary>
        /// The StringBuilder currently has whitespace at the end
        /// </summary>
        internal static bool EndsWithSpace(StringBuilder b)
        {
            return b.Length > 0 && b[b.Length - 1] == ' ';
        }

        /// <summary>
        /// Remove trailing whitespace from the StringBuilder
        /// </summary>
        /// <param name="b"></param>
        internal static void RemoveEndingSpace(StringBuilder b)
        {
            if (EndsWithSpace(b))
                b.Length = b.Length - 1;
        }

        /// <summary>
        /// A description of all the individuals in this invention
        /// </summary>
        public string Description(string startEmphasis = "", string endEmphasis = "")
        {
            var b = new StringBuilder();
            foreach (var i in PossibleIndividuals)
            {
                b.Append(i.Description(startEmphasis, endEmphasis));
                b.Append('\n');
            }
            return b.ToString();
        }

        /// <summary>
        /// A textual description of the Individual's attributes within Model.
        /// </summary>
        public string Description(Individual i, string startEmphasis="", string endEmphasis="")
        {
            if (i.Kinds.Count == 0)
            {
                return i.MostRecentDescription = $"{i.Name.Untokenize()} has no nouns that apply to it";
            }

            var suppressedProperties = new List<Property>();

            var descriptionKind = FindKindOrSuperKind(i, k => k.DescriptionTemplate != null);
            var template = (descriptionKind != null) ? descriptionKind.DescriptionTemplate : DefaultDescriptionTemplate;

            if (descriptionKind == null)
                descriptionKind = i.Kinds[0];

            var b = new StringBuilder();
            var previousToken = "";
            for (var n = 0; n < template.Length; n++)
            {
                var token = template[n];
                if (!EndsWithSpace(b) && token != "-" && previousToken != "-")
                    b.Append(' ');
                if (token == "[")
                {
                    // Get a property name
                    var start = n+1;
                    var end = Array.IndexOf(template, "]", n);
                    if (end < 0)
                        end = template.Length;
                    var propertyName = new string[end - start];
                    Array.Copy(template, start, propertyName, 0, end - start);
                    if (propertyName.Length == 1)
                        AppendPropertyOrMetaPropertyValue(b, i, propertyName, suppressedProperties, descriptionKind, startEmphasis, endEmphasis);
                    else
                        AppendPropertyValue(b, i, propertyName, descriptionKind, suppressedProperties);

                    n = end;
                }
                else
                    b.Append(token);

                previousToken = token;
            }

            var description = b.ToString().Trim();
            i.MostRecentDescription = description;
            return description;
        }

        private void AppendPropertyOrMetaPropertyValue(StringBuilder b, Individual i, string[] propertyName,
            List<Property> suppressedProperties, CommonNoun templateKind, string startEmphasis = "",
            string endEmphasis = "", bool disallowNameString = false)
        {
            switch (propertyName[0])
            {
                case "Container":
                    if (i.Container != null)
                    {
                        b.Append(NameString(i.Container));
                        b.Append(" ");
                    }
                    break;
                
                case "ContainerAndPart":
                    if (i.Container != null)
                    {
                        b.Append(NameString(i.Container));
                        b.Append("'s ");
                        b.Append(i.ContainerPart.Name.Untokenize());
                        b.Append(" ");
                    }
                    break;

                case "NameString":
                    if (disallowNameString)
                        b.Append("<uh oh, name is defined in terms of itself!");
                    else
                        b.Append(NameString(i, suppressedProperties));
                    break;

                case "ProperNameIfDefined":
                    // Generate the name of i, but only if it's different from container+part
                    if (i.Container == null
                        || i.NameProperty(Model) != null
                        || FindKindOrSuperKind(i, kind => kind.NameTemplate != null) != null)
                        b.Append(NameString(i, suppressedProperties));
                    break;

                case "Modifiers":
                    b.Append(AdjectivesString(i));
                    break;

                case "Noun":
                    b.Append(NounsString(i, startEmphasis, endEmphasis));
                    break;

                case "AllProperties":
                    DescribeAllProperties(i, suppressedProperties, b);
                    break;

                default:
                    AppendPropertyValue(b, i, propertyName, templateKind, suppressedProperties);
                    break;
            }
        }

        private void DescribeAllProperties(Individual i, List<Property> suppressedProperties, StringBuilder b)
        {
            foreach (var pair in i.Properties)
            {
                var property = pair.Key;
                if (suppressedProperties.Contains(property))
                    continue;

                var pName = property.Text;
                var prop = pair.Value;
                if (Model.DefinesVariable(prop))
                {
                    var value = FormatPropertyValue(prop, Model[prop]);
                    RemoveEndingSpace(b);
                    b.Append($", {pName}: {value}");
                }
            }
        }

        /// <summary>
        /// Nouns describing i, as a string
        /// </summary>
        public string NounsString(Individual i, string startEmphasis="", string endEmphasis="")
        {
            return MostSpecificNouns(i).SelectMany(noun => noun.StandardName).Prepend(startEmphasis)
                .Append(endEmphasis).Untokenize();
        }

        /// <summary>
        /// Adjectives describing i, as a string
        /// </summary>
        public string AdjectivesString(Individual i)
        {
            var adjectivalPhrases = AdjectivesDescribing(i).Where(a => !a.IsSilent).Select(a => a.StandardName)
                .Cast<IEnumerable<string>>().ToList();
            // Add commas after all but the last adjectival phrase
            for (int j = 0; j < adjectivalPhrases.Count - 1; j++)
                adjectivalPhrases[j] = adjectivalPhrases[j].Append(",");
            var untokenize = adjectivalPhrases.SelectMany(list => list).Untokenize();
            return untokenize;
        }

        private void AppendPropertyValue(StringBuilder b, Individual i, string[] propertyName, CommonNoun templateKind,
            List<Property> suppressedProperties)
        {
            // Find the property
            var property = templateKind.PropertyNamed(propertyName);
            if (property != null)
            {
                // Print its value
                b.Append(PropertyValue(i, property));
                suppressedProperties?.Add(property);
            }
            else
            {
                var part = templateKind.PartNamed(propertyName);
                if (part != null)
                    foreach (var p in i.Parts[part])
                        b.Append(Description(p));
                else
                    b.Append($"<unknown property {propertyName.Untokenize()}>");
            }
        }

        /// <summary>
        /// The value of the specified property of the specified individual in this Invention.
        /// </summary>
        /// <param name="i">Individual whose property is needed</param>
        /// <param name="property">The Property object for the property requested.</param>
        /// <returns>The value of i's property property</returns>
        public object PropertyValue(Individual i, Property property)
        {
            return Model[i.Properties[property]];
        }

        // ReSharper disable once UnusedParameter.Local
        private string FormatPropertyValue(Variable prop, object value)
        {
            switch (value)
            {
                case float f:
                    return Math.Round(f).ToString(CultureInfo.InvariantCulture);

                default:
                    return value.ToString();
            }
        }

        /// <summary>
        /// All Adjectives that are true of the individual in Model.
        /// </summary>
        public IEnumerable<Adjective> AdjectivesDescribing(Individual i)
        {
            var kinds = TrueKinds(i);
            var relevantAdjectives = kinds.SelectMany(k => k.RelevantAdjectives.Concat(k.AlternativeSets.SelectMany(a => a.Alternatives).Select(a => a.Concept))).Where(a => a is Adjective).Cast<Adjective>().Distinct();
            return relevantAdjectives.Where(a => IsA(i, a));
        }

        /// <summary>
        /// Finds the minima of the sub-lattice of nouns satisfied by this individual.
        /// Translation: every noun that's true of ind but that doesn't have a more specific noun that's
        /// also true of it.  We suppress the more general nouns because they're implied by the truth of
        /// the more specified ones.
        /// </summary>
        public IEnumerable<CommonNoun> MostSpecificNouns(Individual ind)
        {
            var nouns = new HashSet<CommonNoun>();

            void MaybeAddNoun(CommonNoun n)
            {
                if (!IsA(ind, n) || nouns.Contains(n))
                    return;

                nouns.Add(n);
                foreach (var sub in n.Subkinds)
                    MaybeAddNoun(sub);
            }

            foreach (var n in ind.Kinds)
                MaybeAddNoun(n);

            // All the nouns that already have a more specific noun in nouns
            // We make a separate table of these rather than removing them from nouns
            // in order to avoid mutating a table while iterating over it, which is outlawed by
            // foreach and likely to be very buggy in this instance even if foreach would let us do it.
            var redundant = new HashSet<CommonNoun>();

            void MarkRedundant(CommonNoun n)
            {
                if (redundant.Contains(n))
                    return;

                redundant.Add(n);

                foreach (var sup in n.Superkinds)
                    MarkRedundant(sup);
            }

            foreach (var n in nouns)
            foreach (var sup in n.Superkinds)
                MarkRedundant(sup);

            return nouns.Where(n => !redundant.Contains(n));
        }

        private CommonNoun FindKindOrSuperKind(Individual i, Func<CommonNoun, bool> templateTest)
        {
            foreach (var kind in MostSpecificNouns(i))
            {
                var k = FindKindOrSuperKind(kind, templateTest);
                if (k != null)
                    return k;
            }

            return null;
        }

        private CommonNoun FindKindOrSuperKind(CommonNoun k, Func<CommonNoun, bool> templateTest)
        {
            if (templateTest(k))
                return k;
            foreach (var super in k.Superkinds)
            {
                var s = FindKindOrSuperKind(super, templateTest);
                if (s != null)
                    return s;
            }
            return null;
        }

        /// <summary>
        /// The name of the Individual within this Invention
        /// </summary>
        /// <param name="i">The individual</param>
        /// <param name="referencedProperties">(Optional) list of properties that were used to make this name.</param>
        /// <returns>Name as a single string</returns>
        public string NameString(Individual i, List<Property> referencedProperties = null)
        {
            var nameProperty1 = i.NameProperty(Model);
            if (nameProperty1 != null)
            {
                referencedProperties?.Add(nameProperty1);
                var prop1 = i.Properties[nameProperty1];
                if (Model.DefinesVariable(prop1))
                {
                    var name1 = Model[prop1];
                    if (name1 is float)
                        name1 = Convert.ToInt32(name1);
                    return name1.ToString();
                }
            }

            var k1 = FindKindOrSuperKind(i, kind => kind.NameTemplate != null);
            if (k1 != null)
                return FormatNameFromTemplate(i, referencedProperties, k1);
            if (i.Container != null)
                return $"{NameString(i.Container)}'s {i.ContainerPart.StandardName.Untokenize()}";
            return i.Text.Trim();
        }

        private string FormatNameFromTemplate(Individual i, List<Property> suppressedProperties, CommonNoun kind)
        {
            var b = new StringBuilder();
            var t = kind.NameTemplate;
            var firstOne = true;

            for (var n = 0; n < t.Length; n++)
            {
                if (firstOne)
                    firstOne = false;
                else
                    b.Append(' ');

                var token = t[n];
                if (token == "[")
                {
                    // Get a property name
                    var start = n+1;
                    var end = Array.IndexOf(t, "]", n);
                    if (end < 0)
                        end = t.Length;
                    var propertyName = new string[end - start];
                    Array.Copy(t, start, propertyName, 0, end - start);
                    AppendPropertyOrMetaPropertyValue(b, i, propertyName, suppressedProperties, kind);

                    n = end;
                }
                else
                    b.Append(token);
            }

            return b.ToString();
        }
        #endregion

        #region Model testing
        /// <summary>
        /// True if the concept with the specified name applies to the individual in the current Model.
        /// </summary>
        /// <param name="i">Individual to test</param>
        /// <param name="name">Name of concept to test</param>
        /// <returns>True if Individual is an instance of the named concept.</returns>
        public bool IsA(Individual i, params string[] name) => IsA(i, (CommonNoun) Ontology.Noun(name));

        /// <summary>
        /// True if concept applies to individual in the current Model.
        /// </summary>
        /// <param name="i">Individual to test</param>
        /// <param name="c">Concept to test the truth of</param>
        /// <returns>True if i is an instance of c in the current Model</returns>
        public bool IsA(Individual i, MonadicConcept c)
        {
            if (c is CommonNoun n)
                // In case we're doing a test for a noun that the generator had already determined
                // at compile time could not be an instance.
                return Generator.CanBeA(i, n) && Model[Generator.IsA(i, c)];
            return Model[Generator.IsA(i, c)];
        }

        /// <summary>
        /// True if i1 verbs i2 in this Invention (i.e. in this model)
        /// </summary>
        public bool Holds(Verb verb, Individual i1, Individual i2) => Model[Generator.Holds(verb, i1, i2)];

        /// <summary>
        /// True if i1 verbs i2 in this Invention (i.e. in this model)
        /// </summary>
        public bool Holds(string verb, Individual i1, Individual i2) => Holds(Ontology.Verb(verb), i1, i2);

        /// <summary>
        /// All the instances of all the relations in this Invention (i.e. in this model)
        /// </summary>
        public IEnumerable<Tuple<Verb, Individual, Individual>>  Relationships
        {
            get
            {
                var verbs = Ontology.AllVerbs.ToArray();
                foreach (var i1 in Individuals)
                foreach (var i2 in Individuals)
                foreach (var v in verbs)
                    if (i1 <= i2 || !v.IsSymmetric)
                        if (Generator.CanBeA(i1, v.SubjectKind) && Generator.CanBeA(i2, v.ObjectKind) && Holds(v, i1, i2))
                            yield return new Tuple<Verb, Individual, Individual>(v, i1, i2);
            }
        }

        /// <summary>
        /// All the kinds that apply to the individual in the current Model
        /// </summary>
        /// <param name="ind">Individual to look up the kinds of</param>
        /// <returns>All kinds that apply to individual</returns>
        public List<CommonNoun> TrueKinds(Individual ind)
        {
            var result = new List<CommonNoun>();

            void AddKindsDownward(List<CommonNoun> list, Individual i, CommonNoun k)
            {
                list.AddNew(k);
                foreach (var sub in k.Subkinds)
                    if (IsA(i, sub))
                    {
                        AddKindsDownward(list, i, sub);
                        return;
                    }
            }

            void AddKindsUpward(List<CommonNoun> list, Individual i, CommonNoun k)
            {
                list.AddNew(k);
                foreach (var super in k.Superkinds)
                    if (IsA(i, super))
                        AddKindsUpward(list, i, super);
            }

            foreach (var k in ind.Kinds)
                if (IsA(ind, k))
                {
                    AddKindsUpward(result, ind, k);
                    AddKindsDownward(result, ind, k);
                }

            return result;
        }
        #endregion
    }
}
