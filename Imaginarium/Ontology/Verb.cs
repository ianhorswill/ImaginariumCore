#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Verb.cs" company="Ian Horswill">
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
using Imaginarium.Parsing;

namespace Imaginarium.Ontology
{
    /// <summary>
    /// Represents a verb, i.e. a binary relation
    /// </summary>
    public class Verb : Concept
    {
        internal Verb(Ontology ontology) : base(ontology, null)
        { }

        /// <inheritdoc />
        public override string Description
        {
            get
            {
                var b = new StringBuilder();
                b.Append(base.Description);
                b.Append(
                    $" ({SingularForm.Untokenize()}/{PluralForm.Untokenize()}/{GerundForm.Untokenize()}/is {PassiveParticiple.Untokenize()} by)\n");
                if (IsReflexive || IsAntiReflexive || IsSymmetric || IsAntiSymmetric)
                {
                    if (IsReflexive)
                        b.Append("reflexive ");
                    if (IsAntiReflexive)
                        b.Append("anti-reflexive ");
                    if (IsSymmetric)
                        b.Append("symmetric ");
                    if (IsAntiSymmetric)
                        b.Append("anti-symmetric ");
                    b.AppendLine();
                }

                if (ObjectLowerBound > 0 || ObjectUpperBound < Unbounded)
                {
                    if (ObjectLowerBound == ObjectUpperBound)
                        b.Append($"Subjects {PluralForm.Untokenize()} {ObjectLowerBound} objects");
                    else if (ObjectUpperBound == Unbounded)
                        b.Append($"Subjects {PluralForm.Untokenize()} at least {ObjectLowerBound} objects");
                    else if (ObjectLowerBound == 0)
                        b.Append($"Subjects {PluralForm.Untokenize()} at most {ObjectUpperBound} objects");
                    else 
                        b.Append($"Subjects {PluralForm.Untokenize()} {ObjectLowerBound}-{ObjectUpperBound} objects");
                    b.AppendLine();
                }

                if (SubjectLowerBound > 0 || SubjectUpperBound < Unbounded)
                {
                    if (SubjectLowerBound == SubjectUpperBound)
                        b.Append($"Objects are {PassiveParticiple.Untokenize()} by {SubjectLowerBound} subjects");
                    else if (SubjectUpperBound == Unbounded)
                        b.Append($"Objects are {PassiveParticiple.Untokenize()} by at least {SubjectLowerBound} subjects");
                    else if (SubjectLowerBound == 0)
                        b.Append($"Objects are {PassiveParticiple.Untokenize()} by at most {SubjectUpperBound} subjects");
                    else 
                        b.Append($"Objects are {PassiveParticiple.Untokenize()} by {SubjectLowerBound}-{SubjectUpperBound} subjects");
                    b.AppendLine();
                }

                return b.ToString();
            }
        }

        /// <inheritdoc />
        protected override string DictionaryStylePartOfSpeech => "v.";

        /// <summary>
        /// Verbs that are implied by this verb
        /// </summary>
        public List<Verb> Generalizations = new List<Verb>();

        /// <summary>
        /// Verbs that are mutually exclusive with this one: A this B implies not A exclusion B
        /// </summary>
        public List<Verb> MutualExclusions = new List<Verb>();

        /// <summary>
        /// Verbs that are specializations of this verb
        /// </summary>
        public List<Verb> Subspecies = new List<Verb>();
        /// <summary>
        /// Verbs that are generalizations of this verb
        /// </summary>
        public List<Verb> Superspecies = new List<Verb>();

        /// <summary>
        /// The value for an upper bound that means there is no upper bound
        /// This can be any large value but must not be short.MaxValue, or there will be overflow errors.
        /// </summary>
        public const int Unbounded = 10000;

        /// <summary>
        /// The maximum number of elements in the Object domain, a given member of the Subject domain can be related to.
        /// </summary>
        public int ObjectUpperBound = Unbounded;
        /// <summary>
        /// The minimum number of elements in the Object domain, a given member of the Subject domain can be related to.
        /// </summary>
        public int ObjectLowerBound;

        /// <summary>
        /// The maximum number of elements in the Subject domain, a given member of the Object domain can be related to.
        /// </summary>
        public int SubjectUpperBound = Unbounded;
        /// <summary>
        /// The minimum number of elements in the Subject domain, a given member of the Object domain can be related to.
        /// </summary>
        public int SubjectLowerBound;

///// <summary>
///// There is an object for every possible subject.
///// </summary>
//public bool IsTotal
//    {
//        get => ObjectLowerBound == 1;
//        set => ObjectLowerBound = value?Math.Max(1, ObjectLowerBound):ObjectLowerBound;
//    }

        /// <summary>
        /// A verb A for all A in it's domain
        /// </summary>
        public bool IsReflexive;

        /// <summary>
        /// This verb and/or one of its superspecies is reflexive
        /// </summary>
        public bool AncestorIsReflexive => IsReflexive || Superspecies.Any(sup => sup.AncestorIsReflexive);

        /// <summary>
        /// A verb A for NO A in its domain
        /// </summary>
        public bool IsAntiReflexive;

        /// <summary>
        /// This verb and/or one of its superspecies is anti-reflexive
        /// </summary>
        public bool AncestorIsAntiReflexive => IsAntiReflexive || Superspecies.Any(sup => sup.AncestorIsAntiReflexive);

        /// <summary>
        /// X verb Y implies Y verb X
        /// </summary>
        public bool IsSymmetric;

        /// <summary>
        /// X verb Y implies not Y verb X
        /// </summary>
        public bool IsAntiSymmetric;

        /// <summary>
        /// The initial probability of the relation.
        /// </summary>
        public float Density = 0.5f;

        /// <inheritdoc />
        public override bool IsNamed(string[] tokens) => tokens.SameAs(SingularForm) || tokens.SameAs(PluralForm);

        // ReSharper disable InconsistentNaming
        private string[] _baseForm;
        private string[] _gerundForm;

        // ReSharper restore InconsistentNaming

        /// <summary>
        /// The base form of the verb (e.g. eat, rather than eats, eating,
        /// to eat, eaten by, etc.).
        /// This is most commonly the same as the third person plural.
        /// </summary>
        public string[] BaseForm
        {
            get => _baseForm;
            set
            {
                _baseForm = value;
                Ontology.VerbTrie.Store(value, this);
                EnsureGerundForm();
                EnsurePassiveParticiple();
                EnsurePluralForm();
                EnsureSingularForm();
            }
        }

        /// <summary>
        /// Passive participle of the verb (eat => eaten).
        /// In English, this is the same as the past participle, but
        /// since Imaginarium doesn't do tenses, we refer to it as the
        /// passive participle.
        /// </summary>
        public string[] PassiveParticiple { get; private set;  }

        /// <summary>
        /// The gerund/present participle form (eat => eating)
        /// </summary>
        public string[] GerundForm
        {
            get => _gerundForm;
            set
            {
                _gerundForm = value;
                Ontology.VerbTrie.Store(value, this);
                EnsureBaseForm();
                EnsurePluralForm();
                EnsureSingularForm();
            }
        }

        /// <inheritdoc />
        public override string[] StandardName => BaseForm;

        // ReSharper disable InconsistentNaming
        private string[] _singular, _plural;

        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Singular form of the verb
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
                Ontology.CheckTerminologyCanBeAdded(value, GetType());
                if (_singular != null) Ontology.VerbTrie.Store(_singular, null);
                _singular = value;
                Ontology.VerbTrie.Store(_singular, this);
                EnsurePluralForm();
                EnsureGerundForm();
                EnsureBaseForm();
                EnsurePassiveParticiple();
            }
        }

        /// <summary>
        /// Add likely spellings of the gerund of this verb.
        /// They are stored as if they are plural inflections.
        /// </summary>
        private void EnsureGerundForm()
        {
            if (_gerundForm != null)
                return;
            EnsureBaseForm();
            foreach (var form in Inflection.GerundsOfVerb(_baseForm))
            {
                if (_gerundForm == null)
                    _gerundForm = form;
                Ontology.VerbTrie.Store(form, this, true);
            }
        }

        /// <summary>
        /// Add likely spellings of the gerund of this verb.
        /// They are stored as if they are plural inflections.
        /// </summary>
        private void EnsurePassiveParticiple()
        {
            if (PassiveParticiple != null)
                return;
            EnsureBaseForm();
            PassiveParticiple = Inflection.PassiveParticiple(BaseForm);
            Ontology.VerbTrie.Store(PassiveParticiple, this, true);
        }

        private void EnsureBaseForm()
        {
            if (_baseForm != null)
                return;
            if (_gerundForm != null)
                _baseForm = Inflection.BaseFormOfGerund(_gerundForm);
            Debug.Assert(_plural != null || _singular != null || _baseForm != null);
            EnsurePluralForm();
            EnsureSingularForm();
            if (_baseForm != null)
                _baseForm = Inflection.ReplaceCopula(_plural, "be");
            EnsureGerundForm();
        }

        /// <summary>
        /// Make sure the noun has a singular verb.
        /// </summary>
        private void EnsureSingularForm()
        {
            if (_singular == null)
                SingularForm = Inflection.SingularOfVerb(_plural);
        }

        /// <summary>
        /// Plural form of the verb
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
                Ontology.CheckTerminologyCanBeAdded(value, GetType());
                if (_plural != null) Ontology.VerbTrie.Store(_plural, null);
                _plural = value;
                Ontology.VerbTrie.Store(_plural, this, true);
                EnsureSingularForm();
                EnsureBaseForm();
                EnsureGerundForm();
                EnsurePassiveParticiple();
            }
        }

        /// <summary>
        /// Type/kind/noun of the subject argument to the verb
        /// </summary>
        public CommonNoun SubjectKind { get; set; }
        /// <summary>
        /// Modifiers that must also be true of subjects of the verb
        /// </summary>
        public MonadicConceptLiteral[] SubjectModifiers { get; set; }
        /// <summary>
        /// Type/kind/noun of the object argument of the verb
        /// </summary>
        public CommonNoun ObjectKind { get; set; }
        /// <summary>
        /// Modifiers that must also be true of the objects of the verb.
        /// </summary>
        public MonadicConceptLiteral[] ObjectModifiers { get; set; }

        /// <summary>
        /// A struct representing a verb's kind and modifiers.
        /// </summary>
        public struct ModifiedKind
        {
            /// <summary>
            /// The verb's kind.
            /// </summary>
            public CommonNoun Kind;
            
            /// <summary>
            /// The verb's modifiers.
            /// </summary>
            public MonadicConceptLiteral[] Modifiers;
        
            /// <summary>
            /// Constructs a ModifiedKind.
            /// </summary>
            /// <param name="kind">The kind.</param>
            /// <param name="modifiers">The list of modifiers.</param>
            public ModifiedKind(CommonNoun kind, MonadicConceptLiteral[] modifiers)
            {
                Kind = kind;
                Modifiers = modifiers;
            }
        
            /// <summary>
            /// Deconstructs a ModifiedKind.
            /// </summary>
            /// <param name="kind">The kind.</param>
            /// <param name="modifiers">The modifiers.</param>
            public void Deconstruct(out CommonNoun kind, out MonadicConceptLiteral[] modifiers)
            {
                kind = Kind;
                modifiers = Modifiers;
            }
        }
        
        /// <summary>
        /// The list of subject kinds (and modifiers) and object kinds (and modifiers).
        /// </summary>
        public List<Tuple<ModifiedKind, ModifiedKind>> SubjectAndObjectKindsAndModifiers =
            new List<Tuple<ModifiedKind, ModifiedKind>>();
        
        /// <summary>
        /// Adds a new tuple to SubjectAndObjectKindsAndModifiers.
        /// </summary>
        /// <param name="subj">The subject ModifiedKind.</param>
        /// <param name="obj">The object ModifiedKind.</param>
        public void AddSubjectObject(ModifiedKind subj, ModifiedKind obj)
        {
            SubjectAndObjectKindsAndModifiers.Add(new Tuple<ModifiedKind, ModifiedKind>(subj, obj));
        }

        private void EnsurePluralForm()
        {
            if (_plural != null)
                return;
            PluralForm = _baseForm != null ? Inflection.ReplaceCopula(_baseForm, "are") : Inflection.PluralOfVerb(_singular);
        }
    }

    /// <summary>
    /// Possible conjugations of a verb
    /// </summary>
    public enum VerbConjugation
    {
        /// <summary>
        /// Third person
        /// </summary>
        ThirdPerson,
        /// <summary>
        /// Base form
        /// </summary>
        BaseForm,
        /// <summary>
        /// Gerund form / present participle
        /// </summary>
        Gerund,
        /// <summary>
        /// Passive participle / past participle
        /// </summary>
        PassiveParticiple
    };
}