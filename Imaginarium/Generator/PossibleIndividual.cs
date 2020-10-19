using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Imaginarium.Ontology;

namespace Imaginarium.Generator
{
    /// <summary>
    /// Represents the instantiation of a specific Individual within a specific Invention (possible world).
    /// </summary>
    public class PossibleIndividual
    {
        /// <summary>
        /// The Generator that generated this possible individual
        /// </summary>
        public Generator Generator => Invention.Generator;

        /// <summary>
        /// The Ontology describing this possible individual
        /// </summary>
        public Ontology.Ontology Ontology => Invention.Ontology;

        /// <summary>
        /// The underlying Individual
        /// </summary>
        public readonly Individual Individual;
        /// <summary>
        /// The Invention (model, possible world) from which this PossibleIndividual takes its attributes
        /// </summary>
        public readonly Invention Invention;

        internal PossibleIndividual(Individual individual, Invention invention)
        {
            Individual = individual;
            Invention = invention;
        }

        /// <summary>
        /// A textual description of the Individual's attributes within Model.
        /// </summary>
        public string Description(string startEmphasis = "", string endEmphasis = "") =>
            Invention.Description(Individual, startEmphasis, endEmphasis);

        /// <summary>
        /// Nouns describing i, as a string
        /// </summary>
        public string NounsString(string startEmphasis = "", string endEmphasis = "") =>
            Invention.NounsString(Individual, startEmphasis, endEmphasis);

        /// <summary>
        /// Adjectives describing i, as a string
        /// </summary>
        public string AdjectivesString() => Invention.AdjectivesString(Individual);

        /// <summary>
        /// The value of the specified property.
        /// </summary>
        /// <param name="property">The Property object for the property requested.</param>
        /// <returns>The value of property</returns>
        public object PropertyValue(Property property) => Invention.PropertyValue(Individual, property);

        /// <summary>
        /// All Adjectives that are true of the individual in Model.
        /// </summary>
        public IEnumerable<Adjective> AdjectivesDescribing() => Invention.AdjectivesDescribing(Individual);

        /// <summary>
        /// Finds the minima of the sub-lattice of nouns satisfied by this individual.
        /// Translation: every noun that's true of ind but that doesn't have a more specific noun that's
        /// also true of it.  We suppress the more general nouns because they're implied by the truth of
        /// the more specified ones.
        /// </summary>
        public IEnumerable<CommonNoun> MostSpecificNouns() => Invention.MostSpecificNouns(Individual);

        /// <summary>
        /// The name of the Individual within this Invention
        /// </summary>
        /// <returns>Name as a single string</returns>
        public string NameString() => Invention.NameString(Individual);

        private string cachedName;

        /// <summary>
        /// NameString for the object.
        /// Caches name, so the name is only computed once.
        /// </summary>
        public string Name => cachedName ?? (cachedName = NameString());

        /// <summary>
        /// True if the concept with the specified name applies to the individual in the current Model.
        /// </summary>
        /// <param name="name">Name of concept to test</param>
        /// <returns>True if Individual is an instance of the named concept.</returns>
        public bool IsA(params string[] name) => Invention.IsA(Individual, name);
        
        /// <summary>
        /// True if concept applies to individual in the current Model.
        /// </summary>
        /// <param name="c">Concept to test the truth of</param>
        /// <returns>True if i is an instance of c in the current Model</returns>
        public bool IsA(MonadicConcept c) => Invention.IsA(Individual, c);

        /// <summary>
        /// All the kinds that apply to the individual in the current Model
        /// </summary>
        /// <returns>All kinds that apply to individual</returns>
        public List<CommonNoun> TrueKinds() => Invention.TrueKinds(Individual);

        /// <summary>
        /// True if this is related to other via the verb.  That is, if "this verbs other"
        /// </summary>
        public bool RelatesTo(PossibleIndividual other, Verb verb)
        {
            Debug.Assert(other.Invention == Invention, "PossibleIndividuals are from different Inventions");
            return Invention.Holds(verb, Individual, other.Individual);
        }

        /// <summary>
        /// True if this is related to other via the verb.  That is, if "this verbs other"
        /// </summary>
        public bool RelatesTo(PossibleIndividual other, string verb)
        {
            Debug.Assert(other.Invention == Invention, "PossibleIndividuals are from different Inventions");
            return Invention.Holds(verb, Individual, other.Individual);
        }

        /// <summary>
        /// Returns the PossibleIndividual(s) representing the specified Part of this possible individual
        /// </summary>
        public PossibleIndividual[] Part(Part p) => Individual.Parts[p].Select(i => Invention.PossibleIndividual(i)).ToArray();

        /// <summary>
        /// Returns the PossibleIndividual(s) representing the specified Part of this possible individual
        /// </summary>
        public PossibleIndividual[] Part(params string[] name) => Part(Ontology.Part(name));

        /// <summary>
        /// Returns the relationships in which this individual is involved.
        /// </summary>
        public IEnumerable<(Verb, PossibleIndividual, PossibleIndividual)> Relationships
        {
            get
            {
                return Invention.Relationships.Where(r => r.Item2 == Individual || r.Item3 == Individual)
                    .Select(r => (r.Item1, Invention[r.Item2], Invention[r.Item3]));
            }
        }

        /// <inheritdoc />
        public override string ToString() => NameString();
    }
}
