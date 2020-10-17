#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Ontology.cs" company="Ian Horswill">
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Imaginarium.Driver;
using Imaginarium.Generator;
using Imaginarium.Parsing;

namespace Imaginarium.Ontology
{
    /// <summary>
    /// Operations for accessing the ontology as a whole
    /// The ontology consists of all the Referent objects and the information within them (e.g. Property objects)
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class Ontology
    {
        /// <summary>
        /// Create a new Ontology and load the code in the specified directory
        /// </summary>
        /// <param name="name">Name for the ontology (for debugging purposes)</param>
        /// <param name="definitionsDirectory">Path to the directory containing code to load</param>
        public Ontology(string name, string definitionsDirectory = null)
        {
            AllReferentTables.Add(AllAdjectives);
            AllReferentTables.Add(AllPermanentIndividuals);
            AllReferentTables.Add(AllNouns);
            AllReferentTables.Add(AllParts);
            AllReferentTables.Add(AllProperties);

            VerbTrie = new TokenTrie<Verb>(this);
            MonadicConceptTrie = new TokenTrie<MonadicConcept>(this);
            Name = name;
            DefinitionsDirectory = definitionsDirectory;
            if (DefinitionsDirectory != null)
                Load();
        }

        /// <summary>
        /// If true, prevent new concepts from being added to the ontology.
        /// </summary>
        public bool IsLocked;

        /// <summary>
        /// Name of the ontology (for debugging purposes)
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Author(s) who wrote this generator
        /// </summary>
        public string Author;

        /// <summary>
        /// Description of the generator
        /// </summary>
        public string Description;

        /// <summary>
        /// Instructions for use
        /// </summary>
        public string Instructions;

        /// <summary>
        /// All the TokenTries used in this ontology, e.g. for monadic concepts and verbs.
        /// </summary>
        internal readonly List<TokenTrieBase> AllTokenTries = new List<TokenTrieBase>();

        /// <summary>
        /// List of all the tables of different kinds of referents.
        /// Used so we know what to clear when reinitializing the ontology.
        /// </summary>
        internal readonly List<IDictionary> AllReferentTables = new List<IDictionary>();

        internal readonly Dictionary<TokenString, Adjective> AllAdjectives = new Dictionary<TokenString, Adjective>();

        /// <summary>
        /// List of all common nouns (i.e. kinds/types) in the ontology.
        /// </summary>
        public IEnumerable<CommonNoun> AllCommonNouns => AllNouns.Select(pair => pair.Value)
            .Where(n => n is CommonNoun).Cast<CommonNoun>().Distinct();


        /// <summary>
        /// All the permanent individuals in this ontology.
        /// A permanent individual is made when one defines a proper name in the ontology.
        /// It is then produced by all Generators for this ontology, regardless of what the
        /// "imagine" command requires.
        /// </summary>
        public Dictionary<TokenString, Individual> AllPermanentIndividuals = new Dictionary<TokenString, Individual>();

        /// <summary>
        /// The trie used for monadic concepts (common nouns and adjectives).
        /// </summary>
        internal readonly TokenTrie<MonadicConcept> MonadicConceptTrie;

        /// <summary>
        /// True if the last lookup of the monadic concept trie was for a plural noun.
        /// </summary>
        internal bool LastMatchPlural => MonadicConceptTrie.LastMatchPlural;

        /// <summary>
        /// All nouns in this ontology (common or proper)
        /// </summary>
        public Dictionary<TokenString, Noun> AllNouns = new Dictionary<TokenString, Noun>();

        /// <summary>
        /// All Parts in this ontology, regardless of what common noun they're attached to
        /// </summary>
        internal readonly Dictionary<TokenString, Part> AllParts = new Dictionary<TokenString, Part>();

        /// <summary>
        /// All Properties in this ontology, regardless of what common noun they're attached to
        /// </summary>
        internal readonly Dictionary<TokenString, Property> AllProperties = new Dictionary<TokenString, Property>();

        internal readonly TokenTrie<Verb> VerbTrie;

        /// <summary>
        /// All verbs (binary relations) defined in this ontology.
        /// </summary>
        public IEnumerable<Verb> AllVerbs => VerbTrie.Contents.Distinct();

        internal void ClearAllTries()
        {
            foreach (var t in AllTokenTries)
                t.Clear();
        }

        /// <summary>
        /// Return the concept with the specified name, or null if there isn't one.
        /// </summary>
        public object Concept(TokenString name)
        {
            var dict = AllReferentTables.FirstOrDefault(t => t.Contains(name));
            var result = dict?[name];
            if (result == null)
                foreach (var t in AllTokenTries)
                {
                    result = t.Find(name);
                    if (result != null)
                        break;
                }

            return result;
        }

        /// <summary>
        /// Return the concept with the specified name, or null if there isn't one
        /// </summary>
        public object this[TokenString name] => Concept(name);

        /// <summary>
        /// Return the concept with the specified name
        /// </summary>
        public object this[string name] => Concept(name.Split());

        /// <summary>
        /// Returns the adjective with the specified name, or null if none
        /// </summary>
        public Adjective Adjective(params string[] tokens) => AllAdjectives.LookupOrDefault(tokens);

        /// <summary>
        /// Returns the common noun identified by the specified sequence of tokens, or null, if there is no such noun.
        /// </summary>
        /// <param name="name">Tokens identifying the noun</param>
        /// <returns>Identified noun, or null if none found.</returns>
        public CommonNoun CommonNoun(params string[] name)
        {
            return (CommonNoun)Noun(name);
        }

        /// <summary>
        /// Returns the common noun (kind) named by the specified string, or null if there is none.
        /// </summary>
        public CommonNoun CommonNoun(string name) => CommonNoun(name.Split());

        /// <summary>
        /// Return the (permanent) individual with the specified name
        /// An individual is the referent of a proper noun.
        /// </summary>
        /// <param name="name">The name of the individual</param>
        public Individual Individual(params string[] name) => AllPermanentIndividuals.LookupOrDefault(name);

        /// <summary>
        /// Returns the noun named by the specified token string, or null if there is none.
        /// </summary>
        public Noun Noun(params string[] tokens) => AllNouns.LookupOrDefault(tokens);

        /// <summary>
        /// Returns the noun named by the specified string, or null if there is none.
        /// </summary>
        public Noun Noun(string name) => Noun(name.Split());
    
        /// <summary>
        /// Return the property with the specified name, if any, otherwise null.
        /// </summary>
        public Part Part(params string[] tokens) => AllParts.LookupOrDefault(tokens);

        /// <summary>
        /// Return the property with the specified name, if any, otherwise null.
        /// </summary>
        public Property Property(params string[] tokens) => AllProperties.LookupOrDefault(tokens);

        /// <summary>
        /// Return the verb with the specified name
        /// </summary>
        public Verb Verb(params string[] name)
        {
            int index = 0;
            var v = VerbTrie.Lookup(name, ref index);
            if (index != name.Length)
                return null;
            return v;
        }

        /// <summary>
        /// Add this name and concept to the trie of all known names of all known monadic concepts.
        /// </summary>
        /// <param name="tokens">Name to add for the concept</param>
        /// <param name="c">Concept to add</param>
        /// <param name="isPlural">True when concept is a common noun and the name is its plural.</param>
        public void Store(string[] tokens, MonadicConcept c, bool isPlural = false) => MonadicConceptTrie.Store(tokens, c, isPlural);

        /// <summary>
        /// Search trie for a monadic concept named by some substring of tokens starting at the specified index.
        /// Updates index as it searches
        /// </summary>
        /// <param name="tokens">Sequence of tokens to search</param>
        /// <param name="index">Position within token sequence</param>
        /// <returns>Concept, if found, otherwise null.</returns>
        public MonadicConcept Lookup(IList<string> tokens, ref int index) => MonadicConceptTrie.Lookup(tokens, ref index);

        /// <summary>
        /// Makes an Individual that is not part of the ontology itself.
        /// This individual is local to a particular Invention.
        /// </summary>
        /// <param name="concepts">CommonNouns and Adjectives that must apply to the individual</param>
        /// <param name="name">Default name to give to the individual if no name property can be found.</param>
        /// <param name="container">The object of which this is a part, if any</param>
        /// <param name="containerPart">Part of container which this object represents</param>
        /// <returns></returns>
        public Individual EphemeralIndividual(IEnumerable<MonadicConceptLiteral> concepts, string[] name, Individual container = null, Part containerPart = null)
        {
            return new Individual(this, concepts, name, container, containerPart, true);
        }

        /// <summary>
        /// Makes an Individual that is part of the ontology itself.  It will appear in all Inventions.
        /// </summary>
        /// <param name="concepts">CommonNouns and Adjectives that must be true of this Individual</param>
        /// <param name="name">Default name for the individual if not name property can be found.</param>
        /// <returns></returns>
        public Individual PermanentIndividual(IEnumerable<MonadicConceptLiteral> concepts, string[] name)
        {
            var individual = new Individual(this, concepts, name);
            AllPermanentIndividuals[name] = individual;
            return individual;
        }


        /// <summary>
        /// Removes all concepts form the ontology.
        /// </summary>
        public void EraseConcepts()
        {
            foreach (var c in AllReferentTables)
                c.Clear();
        
            ClearAllTries();
            Parser.LoadedFiles.Clear();
            tests.Clear();
        }

        /// <summary>
        /// Reload the current project
        /// </summary>
        public void Reload()
        {
            EraseConcepts();
            Load();
        }

        private Parser parser;

        /// <summary>
        /// The default Parser for use with this ontology.
        /// </summary>
        public Parser Parser => parser ?? (parser = new Parser(this));

        /// <summary>
        /// Load the specified code into the ontology
        /// </summary>
        public void ParseAndExecute(string declaration) => Parser.ParseAndExecute(declaration);

        /// <summary>
        /// Load the specified code into the ontology
        /// </summary>
        public void ParseAndExecute(params string[] declarations) => Parser.ParseAndExecute(declarations);

        /// <summary>
        /// Load all the source files in the current project
        /// </summary>
        public void Load()
        {
            Driver.Driver.ClearLoadErrors();

            foreach (var file in Directory.GetFiles(DefinitionsDirectory))
                if (!Path.GetFileName(file).StartsWith(".")
                    && Path.GetExtension(file) == DataFiles.SourceExtension)
                {
                    try
                    {
                        Parser.LoadDefinitions(file);
                    }
                    catch (Exception e)
                    {
                        Driver.Driver.LogLoadError(Parser.CurrentSourceFile, Parser.CurrentSourceLine, e.Message);
                        throw;
                    }
                }
        }


        /// <summary>
        /// Directory holding definitions files and item lists.
        /// </summary>
        public string DefinitionsDirectory
        {
            get => _definitionsDirectory;
            set
            {
                _definitionsDirectory = value;
                // Throw away our state when we change projects
                EraseConcepts();
            }
        }

        private string _definitionsDirectory;

        /// <summary>
        /// Throw an exception if an object with a different type is already defined under this name.
        /// </summary>
        /// <param name="name">Name of the concept</param>
        /// <param name="type">C# type we think the referent should have (e.g. typeof(Verb) for verb)</param>
        /// <param name="addingToOntology">True if we are addingToOntology this referent for the first time, as opposed to adding a new inflection of an existing term</param>
        /// <exception cref="NameCollisionException">If there is already a concept with that name but a different type.</exception>
        internal void CheckTerminologyCanBeAdded(string[] name, Type type, bool addingToOntology = false)
        {
            if (addingToOntology && IsLocked)
                throw new UnknownReferentException(name, type);

            // Parts can have the same names as other things because there's no potential for ambiguity
            if (name == null || type == typeof(Part))
                return;

            var old = Concept((TokenString) name);
            if (old != null && old.GetType() != type)
                throw new NameCollisionException(name, old.GetType(), type);
        }

        #region Testing
        private readonly List<Test> tests = new List<Test>();

        /// <summary>
        /// Remove any tests defined for this ontology.
        /// </summary>
        public void ClearTests()
        {
            tests.Clear();
        }
    
        /// <summary>
        /// Add a test to the ontology
        /// </summary>
        /// <param name="noun">Kind of object to test (a common noun)</param>
        /// <param name="modifiers">other attributes it should have</param>
        /// <param name="shouldExist">If true, the test succeeds when a noun with those modifiers exists.  If false, it succeeds when it doesn't exist.</param>
        /// <param name="succeedMessage">Message to print when it succeeds</param>
        /// <param name="failMessage">Message to print when it fails</param>
        public void AddTest(CommonNoun noun, IEnumerable<MonadicConceptLiteral> modifiers, bool shouldExist, string succeedMessage, string failMessage)
        {
            tests.Add(new Test(noun, modifiers, shouldExist, succeedMessage, failMessage));
        }

        /// <summary>
        /// Run all the defined tests for this ontology and return their results
        /// </summary>
        /// <returns>A stream of results: test that was run, whether it succeeded, and the invention that's an example/counter-example</returns>
        public IEnumerable<(Test test, bool success, Invention example)> TestResults()
        {
            foreach (var test in tests)
            {
                var (success, example) = test.Run();
                yield return (test, success, example);
            }
        }
        #endregion

        #region Generators
        /// <summary>
        /// Make a generator to generate the specified noun and modifiers
        /// </summary>
        public Generator.Generator Generator(CommonNoun noun, params MonadicConceptLiteral[] modifiers) => new Generator.Generator(noun, modifiers);

        /// <summary>
        /// Make a generator to generate the specified noun and modifiers
        /// </summary>
        public Generator.Generator Generator(string noun, int count = 1) => new Generator.Generator(CommonNoun(noun), new MonadicConceptLiteral[0], count);
        #endregion
    }
}
