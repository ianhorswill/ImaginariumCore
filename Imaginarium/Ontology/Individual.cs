﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Individual.cs" company="Ian Horswill">
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
using CatSAT;
using Imaginarium.Parsing;

namespace Imaginarium.Ontology
{
    /// <summary>
    /// Represents an object in the model (imagined scenario)
    /// </summary>
    public class Individual : Referent, IComparable
    {
        /// <summary>
        /// Returns the Property of this Individual named "name", if any
        /// </summary>
        /// <returns>The name Property, else null.</returns>
        public Property NameProperty(Solution model)
        {
            foreach (var pair in Properties)
                if (pair.Key.IsNamed(NameTokenString) && model.DefinesVariable(pair.Value))
                    return pair.Key;
            return null;
        }
        private static readonly string[] NameTokenString = new[] {"name"};

        /// <summary>
        /// The kinds (CommonNouns) of this Individual
        /// </summary>
        public readonly List<CommonNoun> Kinds = new List<CommonNoun>();

        /// <summary>
        /// The Adjectives that might apply to this Individual
        /// </summary>
        public readonly List<MonadicConceptLiteral> Modifiers = new List<MonadicConceptLiteral>();

        /// <summary>
        /// The Properties of this individual.
        /// </summary>
        public readonly Dictionary<Part, Individual[]> Parts = new Dictionary<Part, Individual[]>();
    
        /// <summary>
        /// The Properties of this individual.
        /// </summary>
        public readonly Dictionary<Property, Variable> Properties = new Dictionary<Property, Variable>();

        internal Individual(Ontology ontology, IEnumerable<MonadicConceptLiteral> concepts, string[] name, Individual container = null, Part containerPart = null, bool ephemeral = false) : base(ontology, name, ephemeral)
        {
            Name = name;
            Container = container;
            ContainerPart = containerPart;
            var enumerated = concepts as MonadicConceptLiteral[] ?? concepts.ToArray();
            Kinds.AddRange(enumerated.Where(l => l.IsPositive && l.Concept is CommonNoun).Select(l => (CommonNoun)l.Concept).Distinct());
            // Remove redundant kinds
            for (var i = Kinds.Count - 1; i >= 0; i--)
            {
                var kind = Kinds[i];
                foreach (var possibleSubKind in Kinds)
                    if (possibleSubKind != kind && possibleSubKind.IsSubKindOf(kind))
                    {
                        // Listing kind is redundant
                        Kinds.RemoveAt(i);
                        break;
                    }
            }
            Modifiers.AddRange(enumerated.Where(l => !l.IsPositive || !(l.Concept is CommonNoun)));
        }

        /// <summary>
        /// Name of the object within the ontology, for permanent individuals.
        /// Also used as a default name for ephemeral individuals, if they don't end up with any assigned
        /// name property.
        /// </summary>
        public readonly string[] Name;

        /// <summary>
        /// The Individual of which this is a part, if any, or null
        /// </summary>
        public readonly Individual Container;

        /// <summary>
        /// If this is a part of another Individual (its Container), what Part this is of that Container
        /// </summary>
        public readonly Part ContainerPart;

        /// <inheritdoc />
        public override string[] StandardName => Name;

        /// <summary>
        /// Cached description string from the last time it was generated for this individual.
        /// This will be invalid if switching between different Inventions that both contain this Individual.
        /// </summary>
        public string MostRecentDescription { get; set; }

        /// <inheritdoc />
        public override bool IsNamed(string[] tokens) => Name.SameAs(tokens);

        #region Comparisons
        private static int uidCounter;
        private readonly int uid = uidCounter++;

        /// <inheritdoc />
        public int CompareTo(object obj)
        {
            if (obj is Individual i)
                return uid.CompareTo(i.uid);
            return -1;
        }

        /// <summary>
        /// Implements an arbitrary total order on Individuals
        /// </summary>
        public static bool operator <(Individual a, Individual b) => a.uid < b.uid;
        /// <summary>
        /// Implements an arbitrary total order on Individuals
        /// </summary>
        public static bool operator >(Individual a, Individual b) => a.uid > b.uid;

        /// <summary>
        /// Implements an arbitrary total order on Individuals
        /// </summary>
        public static bool operator <=(Individual a, Individual b) => a.uid <= b.uid;
        /// <summary>
        /// Implements an arbitrary total order on Individuals
        /// </summary>
        public static bool operator >=(Individual a, Individual b) => a.uid >= b.uid;
        #endregion

        /// <inheritdoc />
        public override string ToString()
        {
            var normalAnswer = base.ToString();
            if (!string.IsNullOrEmpty(normalAnswer))
                return normalAnswer;
            if (Container != null)
                return $"{Container}'s {ContainerPart.Text}";
            return $"Individual{uid}";
        }
    }
}
