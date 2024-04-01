#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Generator.cs" company="Ian Horswill">
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
using System.Xml.XPath;
using CatSAT;
using CatSAT.NonBoolean.SMT.MenuVariables;
using Imaginarium.Ontology;
using Imaginarium.Parsing;
using static CatSAT.Language;

namespace Imaginarium.Generator
{
    /// <summary>
    /// Generates a specified type of object based on the information in the ontology
    /// </summary>
    public class Generator
    {
        /// <summary>
        /// The output from the "imagine" command that created this Generator
        /// </summary>
        public static Generator Current;

        /// <summary>
        /// The Ontology containing the concepts used by this generator
        /// </summary>
        public readonly Ontology.Ontology Ontology;

        #region Instance variables

        /// <summary>
        /// The object(s) being constructed by this generator
        /// </summary>
        public List<Individual> EphemeralIndividuals = new List<Individual>();

        /// <summary>
        /// All Individuals in the model being constructed (ephemeral and permanent)
        /// </summary>
        public List<Individual> Individuals = new List<Individual>();

        /// <summary>
        /// The CatSAT problem that will generate the values for the Creation's attributes
        /// </summary>
        public Problem Problem;

        /// <summary>
        /// How many objects the user requested
        /// </summary>
        public int Count;

        #endregion

        /// <summary>
        /// Creates a generator for objects of the specified types
        /// </summary>
        /// <param name="noun">Base common noun for the object</param>
        /// <param name="concepts">Other monadic concepts that must be true of the object</param>
        public Generator(CommonNoun noun, params MonadicConceptLiteral[] concepts) : this(noun,
            (IEnumerable<MonadicConceptLiteral>)concepts)
        {
        }

        /// <summary>
        /// Creates a generator for objects of the specified types
        /// </summary>
        /// <param name="noun">Base common noun for the object</param>
        /// <param name="concepts">Other monadic concepts that must be true of the object</param>
        /// <param name="count">Number of objects of the specified type to include</param>
        public Generator(CommonNoun noun, IEnumerable<MonadicConceptLiteral> concepts, int count = 1)
        {
            Ontology = noun.Ontology;
            Count = count;
            Noun = noun;
            Concepts = concepts.ToArray();
            Rebuild();
        }

        /// <summary>
        /// The kind of object(s) to be generated
        /// </summary>
        public readonly CommonNoun Noun;

        /// <summary>
        /// Any other attributes they the generated object(s) should have, beyond Noun.
        /// </summary>
        public readonly MonadicConceptLiteral[] Concepts;

        /// <summary>
        /// Rebuild and re-solve the CatSAT problem
        /// </summary>
        public void Rebuild()
        {
            // Do this first so that Problem.Current gets set.
            Problem = new Problem("invention");
            Problem.InitializeTruthAssignment += RandomlySelectKinds;

            DetermineIndividuals();

            foreach (var i in Individuals)
                AddFormalization(i);

            BuildVerbPropositionsAndClauses();

            Problem.Optimize();
        }

        /// <summary>
        /// For each individual, randomly select its subkinds, if any.
        /// </summary>
        private void RandomlySelectKinds(Problem _)
        {
            foreach (var i in Individuals)
            foreach (var k in i.Kinds)
            {
                SelectInitialSubkind(i, k);
                //Console.WriteLine();
            }
        }

        /// <summary>
        /// Individual i is of kind k; randomly choose a subkind, if k has subkinds.
        /// </summary>
        private void SelectInitialSubkind(Individual i, CommonNoun k)
        {
            if (k.Subkinds.Count == 0)
                // nothing to do
                return;

            var sub = k.RandomSubkind;
            //Console.Write(sub); Console.Write(' ');

            foreach (var s in k.Subkinds)
            {
                SetIsA(i, s, s == sub);
                if (s == sub)
                    // This is the chosen subkind, so choose sub-sub-kinds
                    SelectInitialSubkind(i, s);
                else
                    // This one wasn't chosen, so set it and all its children to false
                    DeselectSubkinds(i, s);
            }

            foreach (var set in k.AlternativeSets)
            {
                if (set.AllowPreInitialization && set.AllSingleReferenceAdjectives)
                {
                    var chosen = set.RandomAlternative;
                    //Console.Write(chosen);
                    //Console.Write(' ');
                    if (set.MinCount == 1 && set.MaxCount == 1)
                        foreach (var alt in set.Alternatives)
                            SetLiteral(i, alt, alt == chosen);
                }
            }
        }

        /// <summary>
        /// Individual i is known not to be of kind k.  Initialize to also not
        /// be one of its subkinds
        /// </summary>
        private void DeselectSubkinds(Individual i, CommonNoun k)
        {
            if (k.Subkinds.Count == 0)
                // Nothing to do
                return;

            foreach (var sub in k.Subkinds)
            {
                SetIsA(i, sub, false);
                DeselectSubkinds(i, sub);
            }
        }

        private void SetIsA(Individual i, MonadicConcept n, bool truth) => Problem.Initialize(IsA(i, n), truth);

        private void SetLiteral(Individual i, MonadicConceptLiteral l, bool truth) =>
            SetIsA(i, l.Concept, l.IsPositive ? truth : !truth);

        /// <summary>
        /// Find all the individuals that need to exist in these inventions
        /// </summary>
        private void DetermineIndividuals()
        {
            EphemeralIndividuals.Clear();

            var ca = Concepts;
            if (Count == 1)
                EphemeralIndividuals.Add(Ontology.EphemeralIndividual(ca.Append(Noun),
                    Noun.SingularForm.Prepend("the").ToArray()));
            else
                for (var i = 0; i < Count; i++)
                    EphemeralIndividuals.Add(Ontology.EphemeralIndividual(ca.Append(Noun),
                        Noun.SingularForm.Append(i.ToString()).ToArray()));

            foreach (var i in EphemeralIndividuals.ToArray())
                AddParts(i);

            Individuals.Clear();
            Individuals.AddRange(EphemeralIndividuals);
            Individuals.AddRange(Ontology.AllPermanentIndividuals.Select(pair => pair.Value));
            ResetPredicateTables();
        }

        /// <summary>
        /// Add all the propositions and clauses for the verbs
        /// </summary>
        private void BuildVerbPropositionsAndClauses()
        {
            var verbs = Ontology.AllVerbs.ToArray();

            foreach (var subj in Individuals)
            foreach (var obj in Individuals)
            foreach (var v in verbs)
                // new
                foreach (var ((sKind, sModifiers), 
                             (oKind, oModifiers)) in v.SubjectAndObjectKindsAndModifiers)
                {
                    if (CanBeA(subj, sKind) && CanBeA(obj, oKind))
                    {
                        var related = Holds(v, subj, obj);
                        related.InitialProbability = v.Density;
                        AddImplication(IsA(subj, sKind), related);
                        if (sModifiers != null) // todo: check for empty instead of check for null?
                            foreach (var lit in sModifiers)
                                AddImplication(Satisfies(subj, lit), related);
                        AddImplication(IsA(obj, oKind), related);
                        if (oModifiers != null) // todo: check for empty instead of check for null?
                            foreach (var lit in oModifiers)
                                AddImplication(Satisfies(obj, lit), related);
                    }
                }

            foreach (var v in verbs)
            {
                BuildVerbClauses(v);
            }
        }

        private void BuildVerbClauses(Verb v)
        {
            // new
            foreach (var ((sKind, sModifiers), (oKind, oModifiers)) in v.SubjectAndObjectKindsAndModifiers)
            {
                foreach (var (s, o) in Domain(v))
                {
                    var h = Holds(v, s, o);
                    AddImplication(IsA(s, sKind), h);
                    foreach (var m in sModifiers)
                        AddImplication(Satisfies(s, m), h);
                    
                    AddImplication(IsA(o, oKind), h);
                    foreach (var m in oModifiers)
                        AddImplication(Satisfies(o, m), h);
                }
                
                var subjectDomain = Individuals.Where(i => CanBeA(i, sKind)).ToArray();
                var objectDomain = Individuals.Where(i => CanBeA(i, oKind)).ToArray();
                
                // Bound instantiations
                if (v.ObjectUpperBound < Verb.Unbounded || v.ObjectLowerBound > 0)
                    foreach (var i1 in subjectDomain)
                    {
                        if (objectDomain.Length < v.ObjectLowerBound)
                            // todo: is this the problem with domain/codomain???
                            throw new ContradictionException(Problem,
                                $"Each {sKind.SingularForm.Untokenize()} must {v.SingularForm.Untokenize()} at least {v.ObjectLowerBound} {oKind.PluralForm.Untokenize()}, but there are only {objectDomain.Length} total {oKind.PluralForm.Untokenize()}.");
                        Problem.QuantifyIf(IsA(i1, sKind), v.ObjectLowerBound, v.ObjectUpperBound, 
                            objectDomain.Select(i2 => (Literal)Holds(v, i1, i2)).ToArray());
                    }
                
                if (v.SubjectUpperBound < Verb.Unbounded || v.SubjectLowerBound > 0)
                    foreach (var i1 in objectDomain)
                    {
                        if (subjectDomain.Length < v.SubjectLowerBound)
                            // todo: is this the problem with domain/codomain???
                            throw new ContradictionException(Problem,
                                $"Each {sKind.SingularForm.Untokenize()} must be {v.PassiveParticiple.Untokenize()} by at least {v.ObjectLowerBound} {oKind.PluralForm.Untokenize()}, but there are only {subjectDomain.Length} total {oKind.PluralForm.Untokenize()}.");
                        Problem.QuantifyIf(IsA(i1, oKind), v.SubjectLowerBound, v.SubjectUpperBound,
                            subjectDomain.Select(i2 => (Literal)Holds(v, i2, i1)).ToArray());
                    }
                
                // Force diagonal values if (anti-)reflexive
                if (v.AncestorIsAntiReflexive)
                    // No individuals can self-relate
                {
                    foreach (var i in subjectDomain)
                        Problem.Assert(Not(Holds(v, i, i)));
                }
                
                if (v.AncestorIsReflexive)
                {
                    // All eligible individuals must self-relate
                    foreach (var i in subjectDomain)
                        Problem.Assert(Holds(v, i, i));
                }
                
                if (v.IsAntiSymmetric)
                {
                    for (int i = 0; i < subjectDomain.Length; i++)
                    {
                        var i1 = subjectDomain[i];
                        for (var j = i + 1; j < subjectDomain.Length; j++)
                        {
                            var i2 = subjectDomain[j];
                            Problem.AtLeast(1, Not(Holds(v, i1, i2)), Not(Holds(v, i1, i2)));
                        }
                    }
                }
                
                // Implications and exclusions
                if (v.Generalizations.Count > 0 || v.MutualExclusions.Count > 0)
                    foreach (var (s, o) in Domain(v))
                    {
                        var vHolds = Holds(v, s, o);
                        foreach (var g in v.Generalizations)
                            AddImplication(Holds(g, s, o), vHolds);
                        foreach (var e in v.MutualExclusions)
                            Problem.AtMost(1, vHolds, Holds(e, s, o));
                    }
                
                // Link to super-species and subspecies
                if (v.Superspecies.Count > 0 || v.Subspecies.Count > 0)
                    foreach (var (s, o) in Domain(v))
                    {
                        var vHolds = Holds(v, s, o);
                        foreach (var g in v.Superspecies)
                            // Subspecies implies super-species
                            AddImplication(Holds(g, s, o), vHolds);
                
                        if (v.Subspecies.Count > 0)
                        {
                            // Super-species implies some subspecies
                            if (v.IsSymmetric)
                            {
                                var literals = v.Subspecies.Select(sub => Holds(sub, s, o))
                                    .Concat(v.Subspecies.Select(sub => Holds(sub, o, s)))
                                    .Append(Not(vHolds)).Distinct().ToArray();
                                Problem.Exactly(1, literals);
                            }
                            else
                            {
                                var literals = v.Subspecies.Select(sub => Holds(sub, s, o)).Append(Not(vHolds)).ToArray();
                                Problem.Exactly(1, literals);
                            }
                        }
                    }
            }
        }

        private void AddParts(Individual i)
        {
            foreach (var k in i.Kinds) AddParts(i, k);
        }

        private void AddParts(Individual i, CommonNoun k)
        {
            foreach (var part in k.Parts)
            {
                var partSet = Enumerable.Range(1, part.Count)
                    .Select(index => Ontology.EphemeralIndividual(part.MonadicConcepts, null, i, part)).ToArray();
                i.Parts[part] = partSet;
                EphemeralIndividuals.AddRange(partSet);
                foreach (var p in partSet)
                    AddParts(p);
            }

            foreach (var super in k.Superkinds)
                AddParts(i, super);
        }

        IEnumerable<(Individual, Individual)> Domain(Verb v)
        {
            // new
            foreach (var i1 in Individuals)
                foreach (var ((sKind, sModifiers), (oKind, oModifiers)) in v.SubjectAndObjectKindsAndModifiers)
                    if (CanBeA(i1, sKind))
                        foreach (var i2 in Individuals)
                            if ((i1 != i2 || !v.IsAntiReflexive) && CanBeA(i1, oKind))
                                yield return (i1, i2);
        }

        /// <summary>
        /// Make a new Model
        /// </summary>
        public Invention Generate(int retries = 100, int timeout = 50000)
        {
            Problem.Timeout = timeout;
            Solution solution = null;
            for (var retry = 0; solution == null && retry < retries; retry++)
            {
                solution = Problem.Solve(false);
                //Console.WriteLine(solution == null ? "fail" : "succeed");
            }

            return solution == null ? null : new Invention(Ontology, this, solution);
        }

        /// <summary>
        /// Add all clauses and variables relevant to the individual
        /// </summary>
        private void AddFormalization(Individual ind)
        {
            // We know that i IS of kind k, so assert that and its implications
            // Returns true if we hadn't already generated the code for i and k.
            bool AssertKind(Individual i, CommonNoun k)
            {
                var isK = IsA(i, k);
                if (MaybeAssert(isK))
                {
                    // We haven't already processed the constraints for i being a k)
                    foreach (var super in k.Superkinds)
                        AssertKind(i, super);
                    MaybeFormalizeKindInstance(i, k);

                    MaybeAddProperties(k, i, isK);

                    return true;
                }

                return false;
            }

            void MaybeAddProperties(CommonNoun commonNoun, Individual individual, Proposition isK)
            {
                foreach (var p in commonNoun.Properties)
                {
                    if (!individual.Properties.ContainsKey(p))
                        // Create SMT variable
                    {
                        var v = p.Type == null
                            ? new MenuVariable<string>(p.Text, null, Problem, isK)
                            : p.Type.Instantiate(p.Text, Problem, isK);
                        individual.Properties[p] = v;
                        var menuV = v as MenuVariable<string>;
                        foreach (var r in p.MenuRules)
                            // ReSharper disable once PossibleNullReferenceException
                            AddRule(individual, r.Conditions.Append(commonNoun), menuV.In(r.Menu));

                        var floatV = v as FloatVariable;
                        foreach (var r in p.IntervalRules)
                        {
                            AddImplication(individual, r.Conditions.Append(commonNoun), floatV > r.Interval.Lower);
                            AddImplication(individual, r.Conditions.Append(commonNoun), floatV < r.Interval.Upper);
                        }
                    }
                }
            }

            // We know that i MIGHT BE of kind k so add clauses stating that if it is, i
            void SolveForSubclass(Individual i, CommonNoun k)
            {
                // Setting the probability to 0 means that it only has to make one subclass true
                // To satisfy the uniqueness constraint below.  Otherwise, the probability is 0.5f
                // and so it has to make separate moves to make a bunch of them false.
                IsA(i, k).InitialProbability = 0;
                MaybeFormalizeKindInstance(i, k);
                if (k.Subkinds.Count == 0)
                    return;
                Problem.Unique(k.Subkinds.Select(sub => IsA(i, sub)).Append(Not(IsA(i, k))));
                foreach (var sub in k.Subkinds)
                    SolveForSubclass(i, sub);
            }

            // Add clauses for implications that follow from i being of kind k
            void MaybeFormalizeKindInstance(Individual i, CommonNoun k)
            {
                if (kindsFormalized.Contains(new Tuple<Individual, CommonNoun>(i, k)))
                    return;

                foreach (var a in k.ImpliedAdjectives)
                {
                    AddImplication(i, a.Conditions.Append(k), a.Modifier);
                    //AddClause(a.Conditions.Select(c => Not(MembershipProposition(i, c))).Append(Not(MembershipProposition(i, k)))
                    //        .Append(MembershipProposition(i, a.Adjective)));
                }

                foreach (var adj in k.RelevantAdjectives)
                    // Force the creation of the Proposition representing the possibility of the adjective
                    // being true of the individual
                    IsA(i, adj);
                foreach (var set in k.AlternativeSets)
                {
                    if (set.MaxCount < 3)
                        foreach (var lit in set.Alternatives)
                            // Try to ensure that all the alternatives start false so it only has to set one or two of them
                            // rather than clear a bunch of them.
                            IsA(i, lit.Concept).InitialProbability = lit.IsPositive ? 0 : 1;
                    Problem.QuantifyIf(IsA(i, k),
                        set.MinCount, set.MaxCount,
                        set.Alternatives.Select(a => Satisfies(i, a)));
                }

                MaybeAddProperties(k, i, IsA(i, k));

                kindsFormalized.Add(new Tuple<Individual, CommonNoun>(i, k));
            }

            ind.Properties.Clear();
            foreach (var k in ind.Kinds)
            {
                if (AssertKind(ind, k))
                    SolveForSubclass(ind, k);
            }

            foreach (var a in ind.Modifiers)
                MaybeAssert(Satisfies(ind, a));
        }

        #region Predicate and Proposition tracking

        private void ResetPredicateTables()
        {
            predicates.Clear();
            asserted.Clear();
            kindsFormalized.Clear();
        }

        /// <summary>
        /// Assert p is true, unless we've already asserted it
        /// </summary>
        /// <param name="l">Proposition to assert</param>
        /// <returns>True if it had not already been asserted</returns>
        private bool MaybeAssert(Proposition l)
        {
            if (asserted.Contains(l))
                return false;
            Problem.Assert(l);
            asserted.Add(l);
            return true;
        }

        /// <summary>
        /// Assert p is true, unless we've already asserted it
        /// </summary>
        /// <param name="l">Proposition to assert</param>
        /// <returns>True if it had not already been asserted</returns>
        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool MaybeAssert(Literal l)
        {
            if (asserted.Contains(l))
                return false;
            Problem.Assert(l);
            asserted.Add(l);
            return true;
        }

        /// <summary>
        /// The proposition representing that concept k applies to individual i
        /// </summary>
        public Proposition IsA(Individual i, MonadicConcept k)
        {
            if (k is CommonNoun n && !CanBeA(i, n))
                return false;

            var p = PredicateOf(k)(i);
            p.InitialProbability = k.InitialProbability;
            return p;
        }

        /// <summary>
        /// The literal representing that concept k or its negation applies to individual i
        /// </summary>
        private Literal Satisfies(Individual i, MonadicConceptLiteral l)
        {
            var prop = IsA(i, l.Concept);
            return l.IsPositive ? prop : Not(prop);
        }

        /// <summary>
        /// It is potentially possible for this individual to be of this kind.
        /// That is, there are probably models in which it does.
        /// </summary>
        public bool CanBeA(Individual i, CommonNoun kind)
        {
            bool SearchUp(CommonNoun k)
            {
                if (k == kind)
                    return true;
                foreach (var super in k.Superkinds)
                    if (SearchUp(super))
                        return true;
                return false;
            }

            bool SearchDown(CommonNoun k)
            {
                if (k == kind)
                    return true;
                foreach (var sub in k.Subkinds)
                    if (SearchDown(sub))
                        return true;
                return false;
            }

            foreach (var k in i.Kinds)
                if (SearchUp(k) || SearchDown(k))
                    return true;

            return false;
        }

        /// <summary>
        /// Proposition representing that i1 verbs i2.
        /// </summary>
        /// <param name="verb">Verb</param>
        /// <param name="i1">Subject (left) argument to the verb</param>
        /// <param name="i2">Object (right) argument to the verb</param>
        /// <returns>A CatSAT Proposition object representing that i1 verbs i2.</returns>
        public Proposition Holds(Verb verb, Individual i1, Individual i2) => PredicateOf(verb)(i1, i2);

        /// <summary>
        /// The predicate used to represent concept in the CatSAT problem
        /// </summary>
        private Func<Individual, Proposition> PredicateOf(MonadicConcept c)
        {
            if (predicates.TryGetValue(c, out Func<Individual, Proposition> p))
                return p;
            return predicates[c] = Predicate<Individual>(c.StandardName.Untokenize());
        }

        private Func<Individual, Individual, Proposition> PredicateOf(Verb v)
        {
            if (relations.TryGetValue(v, out Func<Individual, Individual, Proposition> p))
                return p;
            var name = v.StandardName.Untokenize();
            return relations[v] = v.IsSymmetric
                ? SymmetricPredicate<Individual>(name)
                : Predicate<Individual, Individual>(name);
        }

        /// <summary>
        /// Which individual/kind pairs we've already generated clauses for
        /// </summary>
        private readonly HashSet<Tuple<Individual, CommonNoun>> kindsFormalized =
            new HashSet<Tuple<Individual, CommonNoun>>();

        /// <summary>
        /// Propositions already asserted in Problem
        /// </summary>
        private readonly HashSet<Literal> asserted = new HashSet<Literal>();

        /// <summary>
        /// Predicates created within Problem
        /// </summary>
        private readonly Dictionary<object, Func<Individual, Proposition>> predicates =
            new Dictionary<object, Func<Individual, Proposition>>();

        private readonly Dictionary<Verb, Func<Individual, Individual, Proposition>> relations =
            new Dictionary<Verb, Func<Individual, Individual, Proposition>>();

        #endregion

        #region Clause generation

        /// <summary>
        /// Add clause to Problem stating that consequent(i) follow from antecedent(i)
        /// </summary>
        /// <param name="i">Individual for which this implication holds</param>
        /// <param name="antecedents">A set of conditions on i</param>
        /// <param name="consequent">A concept that must be true of i when the antecedents are true.</param>
        // ReSharper disable once UnusedMember.Local
        private void AddImplication(Individual i, IEnumerable<MonadicConcept> antecedents, MonadicConcept consequent)
        {
            AddClause(antecedents.Select(a => Not(IsA(i, a))).Append(IsA(i, consequent)));
        }

        /// <summary>
        /// Add clause to Problem stating that consequent(i) follow from antecedent(i)
        /// </summary>
        /// <param name="i">Individual for which this implication holds</param>
        /// <param name="antecedents">A set of conditions on i</param>
        /// <param name="consequent">A proposition that must follow from the antecedent applying to i.</param>
        // ReSharper disable once UnusedMember.Local
        private void AddImplication(Individual i, IEnumerable<MonadicConcept> antecedents, Literal consequent)
        {
            AddClause(antecedents.Select(a => Not(IsA(i, a))).Append(consequent));
        }

        /// <summary>
        /// Add clause to Problem stating that consequent(i) follow from antecedent(i)
        /// </summary>
        /// <param name="i">Individual for which this implication holds</param>
        /// <param name="antecedents">A set of conditions on i</param>
        /// <param name="consequent">A concept that must be true of i when the antecedents are true.</param>
        void AddImplication(Individual i, IEnumerable<MonadicConceptLiteral> antecedents,
            MonadicConceptLiteral consequent)
        {
            AddClause(antecedents.Select(a => Not(Satisfies(i, a))).Append(Satisfies(i, consequent)));
        }

        /// <summary>
        /// Add clause to Problem stating that consequent(i) follow from antecedent(i)
        /// </summary>
        /// <param name="i">Individual for which this implication holds</param>
        /// <param name="antecedents">A set of conditions on i</param>
        /// <param name="consequent">A proposition that must follow from the antecedent applying to i.</param>
        // ReSharper disable once UnusedMember.Local
        void AddImplication(Individual i, IEnumerable<MonadicConceptLiteral> antecedents, Literal consequent)
        {
            AddClause(antecedents.Select(a => Not(Satisfies(i, a))).Append(consequent));
        }


        /// <summary>
        /// Assert that all antecedents being true implies consequent
        /// </summary>
        void AddImplication(Literal consequent, params Literal[] antecedents)
        {
            AddClause(antecedents.Select(Not).Append(consequent));
        }

        /// <summary>
        /// Add a CNF clause to the problem.  This states that at least one the literals must be true.
        /// </summary>
        /// <param name="literals"></param>
        void AddClause(IEnumerable<Literal> literals)
        {
            Problem.AtLeast(1, literals);
        }

        /// <summary>
        /// Add a CatSAT rule with completion semantics to Problem stating that consequent(i) follow from antecedent(i)
        /// </summary>
        /// <param name="i">Individual for which this implication holds</param>
        /// <param name="antecedents">A set of conditions on i</param>
        /// <param name="consequent">A concept that must be true of i when the antecedents are true.</param>
        private void AddRule(Individual i, IEnumerable<MonadicConceptLiteral> antecedents, Proposition consequent)
        {
            Problem.Assert(consequent <= Conjunction(antecedents.Select(a => Satisfies(i, a))));
        }

        /// <summary>
        /// Convert a set of literals into a CatSAT Expression object.
        /// </summary>
        private static Expression Conjunction(IEnumerable<Literal> literals)
        {
            Expression result = null;
            foreach (var p in literals)
            {
                if (result == null)
                    result = p;
                else result = result & p;
            }

            return result;
        }

        #endregion
    }
}