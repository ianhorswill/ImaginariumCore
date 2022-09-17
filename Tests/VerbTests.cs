#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VerbTests.cs" company="Ian Horswill">
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Imaginarium.Driver;
using Imaginarium.Generator;
using Imaginarium.Ontology;
using Imaginarium.Parsing;

namespace Tests
{
    [TestClass]
    public class VerbTests
    {
        public static Ontology Ontology = new Ontology("Test", null);
        public Parser Parser = new Parser(Ontology);

        public VerbTests()
        {
            DataFiles.DataHome = "../../../Imaginarium/";
        }

        [TestMethod]
        public void AntiReflexive()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("people can love other people");
            Parser.ParseAndExecute("people cannot love themselves");
            var v = Ontology.Verb("loves");
            var g = new Generator(Ontology.CommonNoun("person"), new MonadicConceptLiteral[0], 10);
            for (int n = 0; n < 100; n++)
            {
                var s = g.Generate();
                foreach (var i in s.Individuals) Assert.IsFalse(s.Holds(v, i, i));
            }
        }

        [TestMethod]
        public void Reflexive()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("people must love themselves");
            var v = Ontology.Verb("loves");
            var g = new Generator(Ontology.CommonNoun("person"), new MonadicConceptLiteral[0], 10);
            for (int n = 0; n < 100; n++)
            {
                var s = g.Generate();
                foreach (var i in s.Individuals) Assert.IsTrue(s.Holds(v, i, i));
            }
        }

        [TestMethod]
        public void PartialFunction()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("people can love one person");
            var v = Ontology.Verb("loves");
            var g = new Generator(Ontology.CommonNoun("person"), new MonadicConceptLiteral[0], 3);
            bool sawNonTotal = false;

            for (var n = 0; n < 300; n++)
            {
                var s = g.Generate();
                foreach (var i in s.Individuals)
                {
                    var count = s.Individuals.Count(i2 => s.Holds(v, i, i2));
                    Assert.IsFalse(count > 1);
                    sawNonTotal |= count == 0;
                }
            }
            Assert.IsTrue(sawNonTotal);
        }

        [TestMethod]
        public void TotalFunction()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("people must love one person");
            var v = Ontology.Verb("loves");
            var g = new Generator(Ontology.CommonNoun("person"), new MonadicConceptLiteral[0], 10);

            for (var n = 0; n < 100; n++)
            {
                var s = g.Generate();
                foreach (var i in s.Individuals)
                {
                    var count = s.Individuals.Count(i2 => s.Holds(v, i, i2));
                    Assert.IsTrue(count == 1);
                }
            }
        }

        [TestMethod]
        public void Symmetric()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("people can love each other");
            var v = Ontology.Verb("loves");
            var g = new Generator(Ontology.CommonNoun("person"), new MonadicConceptLiteral[0], 10);

            foreach (var i1 in g.Individuals)
            foreach (var i2 in g.Individuals)
                Assert.IsTrue(ReferenceEquals(g.Holds(v, i1, i2), g.Holds(v, i2, i1)));
        }

        [TestMethod]
        public void VerbQuantificationTest()
        {
            var o = new Ontology(nameof(VerbQuantificationTest));
            o.ParseAndExecute(
                "employee and employer are kinds of person",
                "an employee must work for one employer",
                "an employer must be worked for by at least two employees");
            var g = o.CommonNoun("person").MakeGenerator(4);
            var employee = o.CommonNoun("employee");
            var employer = o.CommonNoun("employer");
            var workFor = o.Verb("work", "for");
            for (var count = 0; count < 100; count++)
            {
                var invention = g.Generate();
                Assert.IsNotNull(invention, "Generator failed, count = "+count);
                foreach (var person in invention.PossibleIndividuals)
                {
                    if (person.IsA(employee))
                        Assert.AreEqual(1, invention.PossibleIndividuals.Count(p => person.RelatesTo(p, workFor)));
                    else if (person.IsA(employer))
                        Assert.IsTrue(2 <= invention.PossibleIndividuals.Count(p => p.RelatesTo(person, workFor)));
                    else
                        throw new Exception("Object in model that is neither an employee or employer");
                }
            }

        }
    }
}
