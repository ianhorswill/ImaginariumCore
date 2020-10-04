#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommonNoun.cs" company="Ian Horswill">
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
using Imaginarium.Parsing;

namespace Imaginarium.Ontology
{
    /// <summary>
    /// A noun that represents a kind of thing
    /// </summary>
    [DebuggerDisplay("{" + nameof(Text) + "}")]
    public class CommonNoun : Noun
    {
        internal CommonNoun(Ontology ontology) : base(ontology, null)
        { }

        /// <summary>
        /// Make a generator for this kind of object.
        /// </summary>
        /// <param name="count">Number of instances of the object to generate at a time</param>
        /// <param name="modifiers">And modifiers that must apply to generated objects</param>
        /// <returns></returns>
        public Generator.Generator MakeGenerator(int count = 1, params MonadicConceptLiteral[] modifiers)
            => new Generator.Generator(this, modifiers, count);

        /// <inheritdoc />
        public override string Description => $"<b>{ToString()}</b> <i>{DictionaryStylePartOfSpeech}</i> plural: <i>{PluralForm.Untokenize()}</i>";

        /// <inheritdoc />
        protected override string DictionaryStylePartOfSpeech => "n.";

        /// <inheritdoc />
        public override bool IsNamed(string[] tokens)
        {
            return SingularForm.SameAs(tokens) || PluralForm.SameAs(tokens);
        }

        // ReSharper disable InconsistentNaming
        private string[] _singular, _plural;
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Singular form of the noun
        /// </summary>
        public string[] SingularForm
        {
            get
            {
                EnsureSingularForm();
                return _singular;
            }
            set
            {
                if (_singular != null && ((TokenString) _singular).Equals((TokenString) value))
                    return;
                // If we're defining the plural to be identical to the singular, don't check for name collision
                if (_plural == null || !((TokenString) _plural).Equals((TokenString) value))
                    Ontology.CheckTerminologyCanBeAdded(value, GetType());
                if (_singular != null)
                {
                    Ontology.AllNouns.Remove(_singular);
                    Ontology.Store(_singular, null);
                }
                _singular = value;
                Ontology.AllNouns[_singular] = this;
                Ontology.Store(_singular, this);
                EnsurePluralForm();
            }
        }

        /// <summary>
        /// Make sure the noun has a singular form.
        /// </summary>
        private void EnsureSingularForm()
        {
            if (_singular == null)
                SingularForm = Inflection.SingularOfNoun(_plural);
        }

        /// <summary>
        /// Plural form of the noun, for common nouns
        /// </summary>
        public string[] PluralForm
        {
            get
            {
                EnsurePluralForm();
                return _plural;
            }
            set
            {
                if (_plural != null && ((TokenString) _plural).Equals((TokenString) value))
                    return;
                // If we're defining the plural to be identical to the singular, don't check for name collision
                if (_singular == null || !((TokenString) _singular).Equals((TokenString) value))
                    Ontology.CheckTerminologyCanBeAdded(value, GetType());
                if (_plural != null)
                {
                    Ontology.AllNouns.Remove(_plural);
                    Ontology.Store(_plural, null);
                }
                _plural = value;
                Ontology.AllNouns[_plural] = this;
                Ontology.Store(_plural, this, true);
                EnsureSingularForm();
            }
        }

        private void EnsurePluralForm()
        {
            if (_plural == null)
                PluralForm = Inflection.PluralOfNoun(_singular);
        }

        /// <inheritdoc />
        public override string[] StandardName => SingularForm ?? PluralForm;

        /// <summary>
        /// Template used to generate a reference to the object
        /// </summary>
        public string[] NameTemplate { get; set; }

        /// <summary>
        /// Template used to generate a a description of the object
        /// </summary>
        public string[] DescriptionTemplate { get; set; }

        /// <summary>
        /// The common nouns identifying the immediate subkinds of this noun
        /// </summary>
        public readonly List<CommonNoun> Subkinds = new List<CommonNoun>();
        /// <summary>
        /// The common nouns identifying the immediate superkinds of this noun
        /// </summary>
        public readonly List<CommonNoun> Superkinds = new List<CommonNoun>();
        /// <summary>
        /// Adjectives might apply to this kind of noun.
        /// Relevant adjectives of super- and subkinds might also apply but not be listed in this list.
        /// </summary>
        public readonly List<Adjective> RelevantAdjectives = new List<Adjective>();
        /// <summary>
        /// Sets of mutually exclusive concepts that apply to this kind of object
        /// </summary>
        public readonly List<AlternativeSet> AlternativeSets = new List<AlternativeSet>();
        /// <summary>
        /// Adjectives that are always true of this kind of object.
        /// </summary>
        public readonly List<ConditionalModifier> ImpliedAdjectives = new List<ConditionalModifier>();

        /// <summary>
        /// Components of this kind of object
        /// Objects of this kind may also have parts attached to sub- and superkinds.
        /// </summary>
        public readonly List<Part> Parts = new List<Part>();

        /// <summary>
        /// Return the Part of this noun with the specified name, or null.
        /// </summary>
        public Part PartNamed(string[] name) => Parts.FirstOrDefault(p => p.IsNamed(name));

        /// <summary>
        /// Properties attached to this kind of object
        /// Objects of this kind may also have properties attached to sub- and superkinds.
        /// </summary>
        public readonly List<Property> Properties = new List<Property>();

        /// <summary>
        /// Return the property of this noun with the specified name, or null.
        /// </summary>
        public Property PropertyNamed(string[] name) => Properties.FirstOrDefault(p => p.IsNamed(name));

        /// <summary>
        /// Search for and return the property with the specified name in this noun or one of its ancestor kinds
        /// </summary>
        public Property FindPropertyInAncestor(string[] name)
        {
            var p = PropertyNamed(name);
            if (p != null)
                return p;
            foreach (var k in Superkinds)
            {
                p = k.FindPropertyInAncestor(name);
                if (p != null)
                    return p;
            }

            return null;
        }

        /// <summary>
        /// Run action over all the ancestor kinds of this kind
        /// </summary>
        public void ForAllAncestorKinds(Action<CommonNoun> a, bool includeSelf = true)
        {
            if (includeSelf)
                a(this);
            foreach (var super in Superkinds)
                super.ForAllAncestorKinds(a);
        }

        /// <summary>
        /// Run action over all the descendant kinds of this kind
        /// </summary>
        public void ForAllDescendantKinds(Action<CommonNoun> a, bool includeSelf = true)
        {
            if (includeSelf)
                a(this);
            foreach (var super in Subkinds)
                super.ForAllDescendantKinds(a);
        }

        /// <summary>
        /// True if this is an immediate superkind of the specified subkind.
        /// </summary>
        public bool IsImmediateSuperKindOf(CommonNoun sub) => Subkinds.Contains(sub);

        /// <summary>
        /// True if this is an immediate subkind of the specified superkind.
        /// </summary>
        public bool IsImmediateSubKindOf(CommonNoun super) => Superkinds.Contains(super);

        /// <summary>
        /// This is a superkind of the specified subkind
        /// </summary>
        public bool IsSuperKindOf(CommonNoun sub) =>
            sub == this                         // A is a super kind of A
            || Subkinds.Any(s => s.IsSuperKindOf(sub));

        /// <summary>
        /// This is a subkind of the specified superkind
        /// </summary>
        public bool IsSubKindOf(CommonNoun super) => super.IsSuperKindOf(this);

        /// <summary>
        /// Returns the LUB of the two kinds in the kind lattice
        /// </summary>
        public static CommonNoun LeastUpperBound(CommonNoun a, CommonNoun b)
        {
            if (a == null)
                return b;
            if (b == null)
                return a;

            if (a.IsSuperKindOf(b))
                return a;
        
            foreach (var super in a.Superkinds)
            {
                var lub = LeastUpperBound(super, b);
                if (lub != null)
                    return lub;
            }

            return null;
        }

        /// <summary>
        /// Returns the LUB of the three kinds in the kind lattice
        /// </summary>
        public static CommonNoun LeastUpperBound(CommonNoun a, CommonNoun b, CommonNoun c) =>
            a == null ? LeastUpperBound(b, c) : LeastUpperBound(a, LeastUpperBound(b, c));

        /// <summary>
        /// Ensure super is an immediate super-kind of this kind.
        /// Does nothing if it is already a super-kind.
        /// </summary>
        public void DeclareSuperclass(CommonNoun super)
        {
            if (!Superkinds.Contains(super))
            {
                Superkinds.Add(super);
                super.Subkinds.Add(this);
            }
        }

        /// <summary>
        /// A set of mutually exclusive adjectives that can apply to a CommonNoun.
        /// </summary>
        public struct AlternativeSet
        {
            /// <summary>
            /// At most one of these may be true of the noun
            /// </summary>
            public readonly MonadicConceptLiteral[] Alternatives;

            /// <summary>
            /// Minimum number of literals that can be true
            /// </summary>
            public readonly int MinCount;
            /// <summary>
            /// Maximum number of literals that can be true
            /// </summary>
            public readonly int MaxCount;

            internal AlternativeSet(MonadicConceptLiteral[] alternatives, bool isRequired)
                : this(alternatives, isRequired ? 1 : 0, 1)
            { }

            internal AlternativeSet(MonadicConceptLiteral[] alternatives, int minCount, int maxCount)
            {
                Alternatives = alternatives;
                MinCount = minCount;
                MaxCount = maxCount;
            }
        }

        /// <summary>
        /// An adjective together with an optional list of modifiers that allow it to apply
        /// </summary>
        public class ConditionalModifier
        {
            private  readonly MonadicConceptLiteral[] emptyCondition = new MonadicConceptLiteral[0];

            /// <summary>
            /// Additional conditions on top of the CommonNoun in which this is stored, that must be true for the implication to hold
            /// </summary>
            public readonly MonadicConceptLiteral[] Conditions;
            /// <summary>
            /// Adjective that follows from the noun and conditions.
            /// </summary>
            public readonly MonadicConceptLiteral Modifier;

            internal ConditionalModifier(MonadicConceptLiteral[] conditions, MonadicConceptLiteral modifier)
            {
                Conditions = conditions??emptyCondition;
                Modifier = modifier;
            }
        }
    }
}
