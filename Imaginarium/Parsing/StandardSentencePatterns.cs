#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StandardSentencePatterns.cs" company="Ian Horswill">
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
using CatSAT;
using CatSAT.NonBoolean.SMT.Float;
using CatSAT.NonBoolean.SMT.MenuVariables;
using Imaginarium.Ontology;

namespace Imaginarium.Parsing
{
    /// <summary>
    /// Rules for parsing the top-level syntax of sentences.
    /// </summary>
    public partial class Parser
    {
        internal void StandardSentencePatterns(Ontology.Ontology ontology)
        {
            SentencePatterns.AddRange(new[]
            {
                new SentencePattern(this, OptionalAll, Object, CanMustButShouldBeCan, "be", Verb, "by", AtMost, "!", UpperBound,
                        OptionalOther, Subject)
                    .Action(() =>
                    {
                        if (CanMustButShouldBeCan.WasMatchedTo("must"))
                            throw new GrammaticalError(
                                "Using must here is confusing; please use can ... at most or can ... between 1 and ...");
                        
                        var verb = ConfigureVerb(Verb, Subject, Object);

                        verb.SubjectUpperBound = (int) ParsedUpperBound;
                        verb.IsAntiReflexive |= OptionalOther.Matched;
                    })
                    .Check(VerbBaseForm, SubjectCommonNoun, ObjectCommonNoun)
                    .Documentation("Specifies how many Subject a given Object can be Verb'ed by."),

                new SentencePattern(this, OptionalAll, Subject, CanMustButShouldBeCan, Verb, AtMost, "!", UpperBound, OptionalOther, Object)
                    .Action(() =>
                    {
                        if (CanMustButShouldBeCan.WasMatchedTo("must"))
                            throw new GrammaticalError(
                                "Using must here is confusing; please use can ... at most or can ... between 1 and ...");

                        var verb = ConfigureVerb(Verb, Subject, Object);

                        verb.ObjectUpperBound = (int) ParsedUpperBound;
                        verb.IsAntiReflexive |= OptionalOther.Matched;
                    })
                    .Check(VerbBaseForm, SubjectCommonNoun, ObjectCommonNoun)
                    .Documentation("Specifies how many Objects a given Subject can Verb."),

                new SentencePattern(this, OptionalAll, Object, CanMustButShouldBeMust, "be", Verb, "by", "at", "least", "!", LowerBound,
                        OptionalOther, Subject)
                    .Action(() =>
                    {
                        if (CanMustButShouldBeMust.WasMatchedTo("can"))
                            throw new GrammaticalError(
                                "Using can here is confusing; please use must ... at least ...");
                        
                        
                        var verb = ConfigureVerb(Verb, Subject, Object);
                        verb.SubjectLowerBound = (int) ParsedLowerBound;
                        verb.IsAntiReflexive |= OptionalOther.Matched;
                    })
                    .Check(VerbBaseForm, SubjectCommonNoun, ObjectCommonNoun)
                    .Documentation("Specifies how many Subject a given Object can be Verb'ed by"),

                new SentencePattern(this, OptionalAll, Subject, CanMustButShouldBeMust, Verb, "at", "least", "!", LowerBound, OptionalOther, Object)
                    .Action(() =>
                    {
                        if (CanMustButShouldBeMust.WasMatchedTo("can"))
                            throw new GrammaticalError(
                                "Using can here is confusing; please use must ... at least ...");
                        
                        var verb = ConfigureVerb(Verb, Subject, Object);
                        verb.ObjectLowerBound = (int) ParsedLowerBound;
                        verb.IsAntiReflexive |= OptionalOther.Matched;
                    })
                    .Check(VerbBaseForm, SubjectCommonNoun, ObjectCommonNoun)
                    .Documentation("Specifies how many Objects a given Subject can Verb."),

                new SentencePattern(this, OptionalAll, Subject, CanMustButShouldBeMust, Verb, "between", LowerBound, "and", UpperBound, OptionalOther, Object)
                    .Action(() =>
                    {
                        var verb = ConfigureVerb(Verb, Subject, Object);
                        verb.ObjectLowerBound = (int) ParsedLowerBound;
                        verb.ObjectUpperBound = (int) ParsedUpperBound;
                        verb.IsAntiReflexive |= OptionalOther.Matched;
                    })
                    .Check(VerbBaseForm, SubjectCommonNoun, ObjectCommonNoun)
                    .Documentation("Specifies how many Objects a given Subject can Verb."),

                new SentencePattern(this, OptionalAll, Object, CanMustButShouldBeMust, "be", Verb, "by", "between", "!", LowerBound, "and", UpperBound,
                        OptionalOther, Subject)
                    .Action(() =>
                    {
                        var verb = ConfigureVerb(Verb, Subject, Object);
                        verb.SubjectLowerBound = (int) ParsedLowerBound;
                        verb.SubjectUpperBound = (int) ParsedUpperBound;
                        verb.IsAntiReflexive |= OptionalOther.Matched;
                    })
                    .Check(VerbBaseForm, SubjectCommonNoun, ObjectCommonNoun)
                    .Documentation("Specifies how many Subject a given Object can be Verb'ed by"),

                new SentencePattern(this, OptionalAll, Object, CanMust, "be", Verb, "by", Quantifier, "!", Subject)
                    .Action(() =>
                    {
                        var verb = ConfigureVerb(Verb, Subject, Object);

                        if (Quantifier.ExplicitCount.HasValue)
                            verb.SubjectUpperBound = Quantifier.ExplicitCount.Value;
                        // "Cats can love other cats" means anti-reflexive, whereas "cats can love many cats" doesn't.
                        verb.IsAntiReflexive |= Quantifier.IsOther;
                        if (CanMust.WasMatchedTo("must"))
                            verb.SubjectLowerBound = Quantifier.ExplicitCount ?? 1;
                    })
                    .Check(VerbBaseForm, SubjectCommonNoun, ObjectCommonNoun)
                    .Documentation("Specifies how many Subjects a given Object can be Verb'ed by."),

                new SentencePattern(this, OptionalAll, Subject, CanMust, Verb, Quantifier, "!", Object)
                    .Action(() =>
                    {
                        var verb = ConfigureVerb(Verb, Subject, Object);

                        if (Quantifier.ExplicitCount.HasValue)
                            verb.ObjectUpperBound = Quantifier.ExplicitCount.Value;
                        // "Cats can love other cats" means anti-reflexive, whereas "cats can love many cats" doesn't.
                        verb.IsAntiReflexive |= Quantifier.IsOther;
                        if (CanMust.WasMatchedTo("must"))
                            verb.ObjectLowerBound = Quantifier.ExplicitCount ?? 1;
                    })
                    .Check(VerbBaseForm, SubjectCommonNoun, ObjectCommonNoun)
                    .Documentation("Specifies how many Objects a given Subject can Verb."),

                new SentencePattern(this, Verb, "is", RareCommon)
                    .Action(() => Verb.Verb.Density = RareCommon.Value)
                    // ReSharper disable once StringLiteralTypo
                    .Documentation("States that Verb'ing is rare or common."),

                new SentencePattern(this, Verb, "and", Verb2, "are", "!", "mutually", "exclusive")
                    .Action(() => { Verb.Verb.MutualExclusions.AddNew(Verb2.Verb); })
                    .Documentation("States that two objects cannot be related by both verbs at once."),

                new SentencePattern(this, Verb, "is", "mutually", "!", "exclusive", "with", Verb2)
                    .Action(() => { Verb.Verb.MutualExclusions.AddNew(Verb2.Verb); })
                    .Documentation("States that two objects cannot be related by both verbs at once."),

                new SentencePattern(this, Verb, "implies", "!", Verb2)
                    .Action(() => { Verb.Verb.Generalizations.AddNew(Verb2.Verb); })
                    .Check(VerbGerundForm, Verb2GerundForm)
                    .Documentation(
                        "States that two objects being related by the first verb means they must also be related by the second."),
                // todo: is this the issue with "happily getting married to is a way of knowing"?
                new SentencePattern(this, Verb, "is", "a", "way", "!", "of", Verb2)
                    .Action(() =>
                    {
                        var sub = Verb.Verb;
                        var super = Verb2.Verb;
                        // old
                        if (sub.SubjectKind == null)
                        {
                            sub.SubjectKind = super.SubjectKind;
                            sub.SubjectModifiers = super.SubjectModifiers;
                        }
                        
                        if (sub.ObjectKind == null)
                        {
                            sub.ObjectKind = super.ObjectKind;
                            sub.ObjectModifiers = super.ObjectModifiers;
                        }
                        
                        if (super.SubjectKind == null)
                        {
                            super.SubjectKind = sub.SubjectKind;
                            super.SubjectModifiers = sub.SubjectModifiers;
                        }
                        
                        if (super.ObjectKind == null)
                        {
                            super.ObjectKind = sub.ObjectKind;
                            super.ObjectModifiers = sub.ObjectModifiers;
                        }
                        
                        // new
                        // note: this very well may be wrong. and not good. sorry.
                        // if (sub.SubjectAndObjectKindsAndModifiers.Count == 0)
                        // {
                        //     sub.SubjectAndObjectKindsAndModifiers = super.SubjectAndObjectKindsAndModifiers;
                        // }
                        //
                        // if (super.SubjectAndObjectKindsAndModifiers.Count == 0)
                        // {
                        //     super.SubjectAndObjectKindsAndModifiers = sub.SubjectAndObjectKindsAndModifiers;
                        // }

                        sub.Superspecies.AddNew(super);
                        super.Subspecies.AddNew(sub);
                    })
                    .Check(VerbGerundForm, Verb2GerundForm)
                    .Documentation(
                        "Like 'is a kind of' but for verbs.  Verb1 implies Verb2, but Verb2 implies that one of the Verbs that is a way of Verb2ing is also true."),

                new SentencePattern(this, Subject, "are", RareCommon)
                    .Action(() => Subject.CommonNoun.InitialProbability = RareCommon.Value)
                    .Check(SubjectPlural, SubjectUnmodified)
                    .Documentation("States that the specified kind of object is rare/common."),

                new SentencePattern(this, OptionalAll, Subject, CanNot, Verb, Reflexive)
                    .Action(() =>
                    {
                        var verb = ConfigureVerb(Verb, Subject, Subject);
                        verb.IsAntiReflexive = true;
                    })
                    .Check(VerbBaseForm, SubjectCommonNoun)
                    .Documentation("States that the verb can't hold between an object and itself."),

                new SentencePattern(this, OptionalAll, Subject, CanNot, Verb, "each", "other")
                    .Action(() =>
                    {
                        var verb = ConfigureVerb(Verb, Subject, Subject);
                        verb.IsAntiSymmetric = true;
                    })
                    .Check(VerbBaseForm, SubjectCommonNoun)
                    .Documentation("States that if A Verbs B, then B can't verb A."),

                new SentencePattern(this, OptionalAll, Subject, Always, Verb, Reflexive)
                    .Action(() =>
                    {
                        var verb = ConfigureVerb(Verb, Subject, Subject);
                        verb.IsReflexive = true;
                    })
                    .Check(VerbBaseForm, SubjectCommonNoun)
                    .Documentation("States that the verb always holds between objects and themselves."),

                new SentencePattern(this, OptionalAll, Subject, CanMustButShouldBeCan, Verb, EachOther)
                    .Action(() =>
                    {
                        if (CanMustButShouldBeCan.WasMatchedTo("must"))
                            throw new GrammaticalError("Using must here is confusing; it suggests everything must verb everything else.");
                        
                        var verb = ConfigureVerb(Verb, Subject, Subject);
                        verb.IsSymmetric = true;
                    })
                    .Check(VerbBaseForm, SubjectCommonNoun)
                    .Documentation("States that the verb is symmetric: if a verbs b, then b verbs a."),

                new SentencePattern(this, Subject, Is, "a", "kind", "!", "of", Object)
                    .Action(() =>
                    {
                        Subject.CommonNoun.DeclareSuperclass(Object.CommonNoun);
                        foreach (var mod in Object.Modifiers)
                            Subject.CommonNoun.ImpliedAdjectives.Add(new CommonNoun.ConditionalModifier(null, mod));
                    })
                    .Check(SubjectVerbAgree, ObjectSingular, SubjectUnmodified, SubjectCommonNoun, ObjectCommonNoun)
                    .Documentation(
                        "Declares that all Subjects are also Objects.  For example, 'cat is a kind of animal' says anything that is a cat is also an animal."),

                new SentencePattern(this, SubjectNounList, Is, "kinds", "!", "of", Object)
                    .Action(() =>
                    {
                        foreach (var e in SubjectNounList.Expressions)
                        {
                            var c = e.Concept as CommonNoun;
                            if (c == null)
                                throw new GrammaticalError(
                                    $"The noun '{e.Concept.StandardName}' is a proper noun (a name of a specific thing), but I need a common noun (a kind of thing) here",
                                    $"The noun '<i>{e.Concept.StandardName}</i>' is a proper noun (a name of a specific thing), but I need a common noun (a kind of thing) here");
                            c.DeclareSuperclass(Object.CommonNoun, e.RelativeFrequency);
                            foreach (var mod in Object.Modifiers)
                                c.ImpliedAdjectives.Add(new CommonNoun.ConditionalModifier(null, mod));

                        }
                    })
                    .Check(ObjectCommonNoun)
                    .Documentation(
                        "Declares that all the different nouns in the subject list are also kinds of the object noun.  So 'dogs and cats are kinds of animal' states that all dogs and all cats are also animals."),

                new SentencePattern(this, "the", "plural", "!", "of", Subject, "is", Object)
                    .Action(() =>
                    {
                        Subject.Number = Number.Singular;
                        Object.Number = Number.Plural;
                        Subject.CommonNoun.PluralForm = Object.Text;
                    })
                    .Check(SubjectUnmodified, ObjectUnmodified, SubjectCommonNoun, ObjectCommonNoun)
                    .Documentation("Lets you correct the system's guess as to the plural of a noun."),

                new SentencePattern(this, "the", "singular", "!", "of", Subject, "is", Object)
                    .Action(() =>
                    {
                        Subject.Number = Number.Plural;
                        Object.Number = Number.Singular;
                        Subject.CommonNoun.SingularForm = Object.Text;
                    })
                    .Check(SubjectUnmodified, ObjectUnmodified, SubjectCommonNoun, ObjectCommonNoun)
                    .Documentation("Lets you correct the system's guess as to the singular form of a noun."),

                new SentencePattern(this, Subject, Is, "identified", "!", "as", "\"", Text, "\"")
                    .Action(() => Subject.CommonNoun.NameTemplate = Text.Text)
                    .Check(SubjectUnmodified, SubjectCommonNoun)
                    .Documentation("Tells the system how to print the name of an object."),

                new SentencePattern(this, Subject, Is, "described", "!", "as", "\"", Text, "\"")
                    .Action(() => Subject.CommonNoun.DescriptionTemplate = Text.Text)
                    .Check(SubjectUnmodified, SubjectCommonNoun)
                    .Documentation("Tells the system how to generate the description of an object."),

                new SentencePattern(this, OptionalAll, Subject, "can", "be", PredicateAP)
                    .Action(() =>
                    {
                        if (!PredicateAP.Adjective.RelevantTo(Subject.CommonNoun))
                            Subject.CommonNoun.RelevantAdjectives.Add(PredicateAP.Adjective);
                    })
                    .Check(SubjectUnmodified)
                    .Documentation("Declares that Subjects can be Adjectives, but don't have to be."),

                new SentencePattern(this, "Do", "not", "mention", "!", "being", PredicateAP)
                    .Action(() => { PredicateAP.Adjective.IsSilent = true; })
                    .Documentation("Declares that the specified adjective shouldn't be mentioned in descriptions."),
                
                new SentencePattern(this, "Do", "not", "print", "!", Object)
                    .Check(ObjectCommonNoun)
                    .Action(() =>
                    {
                        Object.CommonNoun.SuppressDescription = true;
                    })
                    .Documentation("Declares that descriptions of the specified kinds should not be printed as separate lines."),

                new SentencePattern(this, Subject, "is", Object)
                    .Action(() =>
                    {
                        var proper = (ProperNoun) Subject.Noun;
                        proper.Kinds.AddNew(Object.CommonNoun);
                    })
                    .Check(SubjectProperNoun, ObjectCommonNoun, ObjectExplicitlySingular)
                    .Documentation(
                        "States that proper noun Subject is of the type Object.  For example, 'Ben is a person'."),

                new SentencePattern(this, Subject, "is", OptionalAlways, Object)
                    .Action(() =>
                    {
                        var subject = Subject.CommonNoun;
                        subject.ImpliedAdjectives.Add(new CommonNoun.ConditionalModifier(Subject.Modifiers.ToArray(),
                            Object.CommonNoun));
                    })
                    .Check(SubjectCommonNoun, ObjectCommonNoun, SubjectExplicitlySingular, ObjectExplicitlySingular)
                    .Documentation(
                        "States that a Subject is always also a Object.  Important: object *must* be singular, as in 'a noun', not just 'nouns'.  This is different from saying 'a kind of' which says that Objects must also be one of their subkinds.  This just says if you see a Subject, also make it be an Object."),

                new SentencePattern(this, OptionalAll, Subject, Is, OptionalAlways, PredicateAP)
                    .Action(() =>
                    {
                        switch (Subject.Noun)
                        {
                            case CommonNoun c:
                                c.ImpliedAdjectives.Add(new CommonNoun.ConditionalModifier(Subject.Modifiers.ToArray(),
                                    PredicateAP.MonadicConceptLiteral));
                                break;

                            case ProperNoun n:
                                if (n.Kinds.Count == 0)
                                    throw new Exception(
                                        $"Using a new proper noun <i>{n.Name.Untokenize()}</i>, which hasn't been been given a kind.  If you intend to use this term, please first add a line of the form \"{n.Name.Untokenize()} is <i>noun</i>\".");
                                n.Individual.Modifiers.AddNew(PredicateAP.MonadicConceptLiteral);
                                break;

                            default:
                                throw new Exception(
                                    $"Unknown kind of noun ({Subject.Noun.GetType().Name}: '{Subject.Noun.StandardName.Untokenize()}'");
                        }
                    })
                    .Check(SubjectVerbAgree)
                    .Documentation(
                        "Declares that Subjects are always Adjective.  For example, 'cats are fuzzy' declares that all cats are also fuzzy."),

                new SentencePattern(this, OptionalAll, Subject, Is, "any", "!", LowerBound, "of", PredicateAPList)
                    .Action(() =>
                    {
                        var alternatives = PredicateAPList.Expressions.Select(ap => ap.MonadicConceptLiteral)
                            .Concat(Subject.Modifiers.Select(l => l.Inverse()))
                            .ToArray();
                        var frequencies = PredicateAPList.Expressions.Select(ap => ap.RelativeFrequency)
                            .Concat(Subject.Modifiers.Select(l => 1f))
                            .ToArray();
                        var alternativeSet =
                            new CommonNoun.AlternativeSet(alternatives, frequencies,(int) ParsedLowerBound, (int) ParsedLowerBound,
                                Subject.Modifiers.Count == 0);
                        Subject.CommonNoun.AlternativeSets.Add(alternativeSet);
                    })
                    .Check(SubjectVerbAgree)
                    .Documentation("Declares the specified number of Adjectives must be true of Subjects."),

                new SentencePattern(this, OptionalAll, Subject, Is, "between", "!", LowerBound, "and", UpperBound, "of",
                        PredicateAPList)
                    .Action(() =>
                    {
                        var (alternatives, frequencies) = AdjectiveAlternatives(Subject, PredicateAPList);
                        var alternativeSet =
                            new CommonNoun.AlternativeSet(alternatives, frequencies, (int) ParsedLowerBound, (int) ParsedUpperBound,
                                Subject.Modifiers.Count == 0);
                        Subject.CommonNoun.AlternativeSets.Add(alternativeSet);
                    })
                    .Check(SubjectVerbAgree)
                    .Documentation(
                        "Declares the number of Adjectives true of Subjects must be in the specified range."),

                new SentencePattern(this, OptionalAll, Subject, CanMust, "be", AtMost, "!", LowerBound, "of",
                        PredicateAPList)
                    .Action(() =>
                    {
                        var (alternatives, frequencies) = AdjectiveAlternatives(Subject, PredicateAPList);
                        var alternativeSet = new CommonNoun.AlternativeSet(alternatives, frequencies, 0, (int) ParsedLowerBound,
                            Subject.Modifiers.Count == 0);
                        Subject.CommonNoun.AlternativeSets.Add(alternativeSet);
                    })
                    .Check(SubjectVerbAgree)
                    .Documentation(
                        "Declares the number of Adjectives true of Subjects can never be more than the specified number."),

                new SentencePattern(this, OptionalAll, Subject, Is, PredicateAPList)
                    .Action(() =>
                    {
                        var (alternatives, frequencies) = AdjectiveAlternatives(Subject, PredicateAPList);
                        Subject.CommonNoun.AlternativeSets.Add(new CommonNoun.AlternativeSet(alternatives, frequencies, true,
                            Subject.Modifiers.Count == 0));
                    })
                    .Check(SubjectVerbAgree)
                    .Documentation(
                        "Declares that Subjects must be one of the Adjectives.  So 'cats are big or small' says cats are always either big or small, but not both or neither."),

                new SentencePattern(this, OptionalAll, Subject, "can", "be", PredicateAPList)
                    .Action(() =>
                    {
                        var (alternatives, frequencies) = AdjectiveAlternatives(Subject, PredicateAPList);
                        Subject.CommonNoun.AlternativeSets.Add(
                            new CommonNoun.AlternativeSet(alternatives, frequencies, false,
                                Subject.Modifiers.Count == 0));
                    })
                    .Check(SubjectDefaultPlural)
                    .Documentation(
                        "Declares that Subjects can be any one of the Adjectives, but don't have to be.  So 'cats can be big or small' says cats can be big, small, or neither, but not both."),

                new SentencePattern(this, OptionalAll, Subject, Has, Object, "between", "!", LowerBound, "and", UpperBound)
                    .Action(() =>
                    {
                        var subject = Subject.CommonNoun;
                        var propertyName = Object.Text;

                        var existingProperty = subject.FindPropertyInAncestor(propertyName);
                        if (existingProperty == null)
                        {
                            if (Subject.Modifiers.Count > 0)
                                throw new InvalidOperationException($"Can't define a range for property {Object.Text.Untokenize()} of {Subject.Text.Untokenize()} without first defining it for {subject.StandardName.Untokenize()}");
                            subject.Properties.Add(new Property(ontology, propertyName,
                                new FloatDomain(propertyName.Untokenize(), ParsedLowerBound, ParsedUpperBound)));
                        }
                        else
                        {
                            var rule = new Property.IntervalRule(Subject.Modifiers.Append(subject).ToArray(),
                                                                    new Interval(ParsedLowerBound, ParsedUpperBound));
                            existingProperty.IntervalRules.Add(rule);
                        }
                    })
                    .Check(SubjectVerbAgree, ObjectUnmodified)
                    .Documentation(
                        "Says Subjects have a property, Object, that is a number in the specified range.  For example, 'cats have an age between 1 and 20'"),

                new SentencePattern(this, OptionalAll, Subject, Has, Object, "from", "!", ListName)
                    .Action(() =>
                    {
                        var menuName = ListName.Text.Untokenize();
                        if (!NameIsValidFilename(menuName))
                            throw new Exception($"The list name \"{menuName}\" is not a valid file name.");
                        var possibleValues = File.ReadAllLines(ListFilePath(menuName)).Select(s => s.Trim())
                            .Where(s => !String.IsNullOrEmpty(s)).ToArray();
                        if (possibleValues.Length == 0)
                            throw new ArgumentException($"The file {menuName} has no entries in it!");
                        var menu = new Menu<string>(menuName, possibleValues);
                        var propertyName = Object.Text;
                        var prop = Subject.CommonNoun.Properties.FirstOrDefault(p => p.IsNamed(propertyName));
                        if (prop == null)
                        {
                            prop = new Property(ontology, propertyName, null);
                            Subject.CommonNoun.Properties.Add(prop);
                        }

                        prop.MenuRules.Add(new Property.MenuRule(Subject.Modifiers.ToArray(), menu));
                    })
                    .Check(SubjectVerbAgree, ObjectUnmodified)
                    .Documentation(
                        "States that Subjects have a property whose possible values are given in the specified file.  For example 'cats have a name from cat names', or 'French cats have a name from French cat names'"),

                new SentencePattern(this, OptionalAll, Subject, Has, Count, Object, "called", "!", PossessivePronoun, Text)
                    .Action(() =>
                    {
                        var count = ParsedCount;
                        var partName = count == 1 ? Text.Text : Inflection.SingularOfNoun(Text.Text);
                        var part = Subject.CommonNoun.Parts.FirstOrDefault(p => p.IsNamed(partName));
                        if (part == null)
                            InstallPart(ontology, partName, count, Object.CommonNoun, Object.Modifiers,
                                Subject.CommonNoun);
                    })
                    .Check(SubjectVerbAgree, SubjectUnmodified, ObjectCommonNoun)
                    .Documentation("States that Subjects have part called Text that is a Object."),
                
                new SentencePattern(this, OptionalAll, Subject, Has, Object, "called", "!", PossessivePronoun, Text)
                    .Action(() =>
                    {

                        var partName = Text.Text;
                        var part = Subject.CommonNoun.Parts.FirstOrDefault(p => p.IsNamed(partName));
                        if (part == null)
                            InstallPart(ontology, partName, 1, Object.CommonNoun, Object.Modifiers,
                                Subject.CommonNoun);
                    })
                    .Check(SubjectVerbAgree, SubjectUnmodified, ObjectCommonNoun)
                    .Documentation("States that Subjects have part called Text that is a Object."),

                new SentencePattern(this, OptionalAll, Subject, Has, Count, Object)
                    .Action(() =>
                    {
                        var part = Subject.CommonNoun.Parts.FirstOrDefault(p => p.IsNamed(Object.CommonNoun.StandardName));
                        if (part == null)
                            InstallPart(ontology, Object.CommonNoun.StandardName, ParsedCount, Object.CommonNoun, Object.Modifiers,
                                Subject.CommonNoun);
                    })
                    .Check(SubjectVerbAgree, SubjectUnmodified, ObjectCommonNoun)
                    .Documentation("States that Subjects have part called Text that is a Object."),
                
                new SentencePattern(this, OptionalAll, Subject, Has, Object)
                    .Action(() =>
                    {
                        var part = Subject.CommonNoun.Parts.FirstOrDefault(p => p.IsNamed(Object.CommonNoun.StandardName));
                        if (part == null)
                            InstallPart(ontology, Object.CommonNoun.StandardName, 1, Object.CommonNoun, Object.Modifiers,
                                Subject.CommonNoun);
                    })
                    .Check(SubjectVerbAgree, SubjectUnmodified, ObjectCommonNoun)
                    .Documentation("States that Subjects have part called Text that is a Object."),

                new SentencePattern(this, "every", "kind", "of", "!", Subject, "should", "exist")
                    .Action(() =>
                    {
                        void Walk(CommonNoun kind)
                        {
                            if (kind.Subkinds.Count == 0)
                            {
                                var name = kind.PluralForm.Untokenize();
                                var modifiers = Subject.Modifiers.SelectMany(lit =>
                                        lit.IsPositive
                                            ? lit.Concept.StandardName
                                            : lit.Concept.StandardName.Prepend("not"))
                                    .Untokenize();
                                ontology.AddTest(kind, Subject.Modifiers, true,
                                    $"Test succeeded:{modifiers} {name} should exist",
                                    $"Test failed:{modifiers} {name} should exist");
                            }
                            else
                                foreach (var sub in kind.Subkinds)
                                    Walk(sub);
                        }

                        Walk(Subject.CommonNoun);
                    })
                    .Documentation("Adds a set of tests for the existence of the various subkinds of Subject."),

                new SentencePattern(this, Subject, "should", "!", ExistNotExist)
                    .Action(() =>
                    {
                        var shouldExist = ExistNotExist.MatchedText[0] == "exist";
                        var input = Input.Untokenize();
                        ontology.AddTest(Subject.CommonNoun, Subject.Modifiers, shouldExist,
                            $"Test succeeded: {input}",
                            $"Test failed: {input}");
                    })
                    .Documentation("Adds a new test to the list of tests to perform when the test command is used."),

                new SentencePattern(this, "every", "kind", "of", "!", Subject, "should", "exist")
                    .Action(() =>
                    {
                        void Walk(CommonNoun kind)
                        {
                            if (kind.Subkinds.Count == 0)
                            {
                                var name = kind.PluralForm.Untokenize();
                                var modifiers = Subject.Modifiers.SelectMany(lit =>
                                        lit.IsPositive
                                            ? lit.Concept.StandardName
                                            : lit.Concept.StandardName.Prepend("not"))
                                    .Untokenize();
                                ontology.AddTest(kind, Subject.Modifiers, true,
                                    $"Test succeeded:{modifiers} {name} should exist",
                                    $"Test failed:{modifiers} {name}");
                            }
                            else
                                foreach (var sub in kind.Subkinds)
                                    Walk(sub);
                        }

                        Walk(Subject.CommonNoun);
                    })
                    .Documentation("Adds a set of tests for the existence of the various subkinds of Subject."),

                new SentencePattern(this, SubjectNounList, "should", "!", ExistNotExist)
                    .Action(() =>
                    {
                        var shouldExist = ExistNotExist.MatchedText[0] == "exist";
                        var input = Input.Untokenize();
                        foreach (var noun in SubjectNounList.Concepts)
                        {
                            ontology.AddTest(noun as CommonNoun, new MonadicConceptLiteral[0], shouldExist,
                                $"Test succeeded: {input}",
                                $"Test failed: {input}");
                        }
                    })
                    .Documentation("Adds a new test to the list of tests to perform when the test command is used."),

                new SentencePattern(this, "pressing", "!", "\"", ButtonName, "\"", "means", "\"", Text, "\"")
                    .Action(() => { Driver.Driver.Repl.AddButton(ButtonName.Text.Untokenize(), Text.Text.Untokenize()); })
                    .Documentation(
                        "Instructs the system to add a new button to the button bar with the specified name.  When it is pressed, it will execute the specified text."),

                new SentencePattern(this, "author", ":", Text)
                    .Action(() => { ontology.Author = Text.Text.Untokenize(); })
                    .Documentation(
                        "Adds the name of the author to the generator."),

                new SentencePattern(this, "description", ":", Text)
                    .Action(() => { ontology.Description = Text.Text.Untokenize(); })
                    .Documentation(
                        "Adds a short description to the generator."),

                new SentencePattern(this, "instructions", ":", Text)
                    .Action(() => { ontology.Instructions = Text.Text.Untokenize(); })
                    .Documentation(
                        "Adds some brief instructions to the generator."),
            });
        }

        private static (MonadicConceptLiteral[], float[]) AdjectiveAlternatives(NP subject, ReferringExpressionList<AP, Adjective> predicateAPList)
        {
            var alternatives = predicateAPList.Expressions.Select(ap => ap.MonadicConceptLiteral)
                .Concat(subject.Modifiers.Select(l => l.Inverse()))
                .ToArray();
            var frequencies = predicateAPList.Expressions.Select(ap => ap.RelativeFrequency)
                .Concat(subject.Modifiers.Select(l => 1f))
                .ToArray();
            return (alternatives, frequencies);
        }

        private static void InstallPart(Ontology.Ontology ontology, string[] partName, int count, CommonNoun kind, List<MonadicConceptLiteral> modifiers,
            CommonNoun subject)
        {
            Part part;
            part = new Part(ontology, partName, count, kind, modifiers);
            subject.Parts.Add(part);
        }

        /// <summary>
        /// Set the subject and object kind and modifiers of Verb based on Subject and Object.
        /// </summary>
        /// <returns>The verb</returns>
        private static Verb ConfigureVerb(VerbSegment verbSegment, NP subjectNP, NP objectNP)
        {
            // old
            // var verb = verbSegment.Verb;
            // var verbSubjectKind = CommonNoun.LeastUpperBound(verb.SubjectKind, subjectNP.CommonNoun);
            // if (verbSubjectKind == null)
            //     throw new ContradictionException(null,
            //         $"Verb {verb.BaseForm.Untokenize()} was previously declared to take subjects that were {verb.SubjectKind.PluralForm.Untokenize()}, but is now being declared to take subjects of the unrelated type {subjectNP.CommonNoun.SingularForm.Untokenize()}.");
            // verb.SubjectKind = verbSubjectKind;
            // if (verb.SubjectModifiers == null)
            //     verb.SubjectModifiers = subjectNP.Modifiers.ToArray();
            //
            // var verbObjectKind = CommonNoun.LeastUpperBound(verb.ObjectKind, objectNP.CommonNoun);
            // if (verbObjectKind == null)
            //     throw new ContradictionException(null,
            //         $"Verb {verb.BaseForm.Untokenize()} was previously declared to take objects that were {verb.ObjectKind.PluralForm.Untokenize()}, but is now being declared to take objects of the unrelated type {objectNP.CommonNoun.SingularForm.Untokenize()}.");
            // verb.ObjectKind = verbObjectKind;
            // if (verb.ObjectModifiers == null)
            //     verb.ObjectModifiers = objectNP.Modifiers.ToArray();
            // return verb;
            
            // new
            var verb = verbSegment.Verb;
            if (verb.SubjectAndObjectKindsAndModifiers.Count == 0)
            {
                Verb.ModifiedKind subj = new Verb.ModifiedKind(subjectNP.CommonNoun, subjectNP.Modifiers.ToArray());
                Verb.ModifiedKind obj = new Verb.ModifiedKind(objectNP.CommonNoun, objectNP.Modifiers.ToArray());
                verb.AddSubjectObject(subj, obj);
                return verb;
            }
            
            for (int i = 0; i < verb.SubjectAndObjectKindsAndModifiers.Count; i++)
            {
                var ((sKind, sModifiers), 
                    (oKind, oModifiers)) = verb.SubjectAndObjectKindsAndModifiers[i];
                // if the exact same subject and object kinds and modifiers are already in the list, do nothing
                if (subjectNP.CommonNoun == sKind && subjectNP.Modifiers.ToArray() == sModifiers &&
                    objectNP.CommonNoun == oKind && objectNP.Modifiers.ToArray() == oModifiers)
                {
                    return verb;
                }
                
                // if the subject and object kinds are the same, but the modifiers are different, add the new modifiers to the list?
                
                // if the subject and object kinds subsume the existing kinds, replace the existing kinds with the new ones
                // a subsumes b if a's modifier list is empty and a is the same or a superkind of b
                if (subjectNP.CommonNoun.Superkinds.Contains(sKind) && objectNP.CommonNoun.Superkinds.Contains(oKind))
                {
                    verb.SubjectAndObjectKindsAndModifiers[i] = new Tuple<Verb.ModifiedKind, Verb.ModifiedKind>
                        (new Verb.ModifiedKind(subjectNP.CommonNoun, subjectNP.Modifiers.ToArray()), 
                            new Verb.ModifiedKind(objectNP.CommonNoun, objectNP.Modifiers.ToArray()));
                    return verb;
                }
            }
            
            // otherwise, just add it to the list
            Verb.ModifiedKind newSubj = new Verb.ModifiedKind(subjectNP.CommonNoun, subjectNP.Modifiers.ToArray());
            Verb.ModifiedKind newObj = new Verb.ModifiedKind(objectNP.CommonNoun, objectNP.Modifiers.ToArray());
            verb.AddSubjectObject(newSubj, newObj);
            
            return verb;
        }
    }
}