﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GeneratorTests.cs" company="Ian Horswill">
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
using Imaginarium.Driver;
using Imaginarium.Generator;
using Imaginarium.Ontology;
using Imaginarium.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class GeneratorTests
    {
        public static Ontology Ontology = new Ontology("Test", null);
        public Parser Parser = new Parser(Ontology);

        public GeneratorTests()
        {
            DataFiles.DataHome = "../../../Imaginarium/";
        }

        [TestMethod]
        public void CatTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("a cat is a kind of person",
                    "a persian is a kind of cat",
                    "a tabby is a kind of cat",
                    "a siamese is a kind of cat",
                    "a cat can be haughty",
                    "a cat can be cuddly",
                    "a cat can be crazy",
                    "a persian can be matted");
            var cat = Ontology.CommonNoun("cat");
            var g = new Generator(cat);
            for (var n = 0; n < 100; n++)
            {
                var i = g.Generate();
                Assert.IsTrue(i.IsA(i.Individuals[0], cat));
                Assert.IsTrue(i.IsA(i.Individuals[0], "persian")
                              || i.IsA(i.Individuals[0], "tabby")
                              || i.IsA(i.Individuals[0], "siamese"));
                Console.WriteLine(i.Model.Model);
                Console.WriteLine(i.Description(i.Individuals[0]));
            }
        }

        [TestMethod]
        public void PartTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("a cat is a kind of person",
                "a persian is a kind of cat",
                "a tabby is a kind of cat",
                "a siamese is a kind of cat",
                "a cat can be haughty",
                "a cat can be cuddly",
                "a cat can be crazy",
                "a persian can be matted",
                "red, blue, and green are kinds of color",
                "a cat has a color called its favorite color");
            var cat = Ontology.CommonNoun("cat");
            var color = Ontology.CommonNoun("color");
            var g = new Generator(cat);
            for (var n = 0; n < 100; n++)
            {
                var i = g.Generate();
                Assert.IsTrue(i.IsA(i.Individuals[0], cat));
                Assert.IsTrue(i.IsA(i.Individuals[0], "persian")
                              || i.IsA(i.Individuals[0], "tabby")
                              || i.IsA(i.Individuals[0], "siamese"));
                Assert.AreEqual(i.Individuals[0], i.Individuals[1].Container);
                Assert.IsTrue(i.IsA(i.Individuals[1], color));
                Console.WriteLine(i.Model.Model);
                Console.WriteLine(i.Description(i.Individuals[0]));
            }
        }

        [TestMethod]
        public void MultiPartTest()
        {
            var o = new Ontology("MultiPartTest", null);
            o.Parser.ParseAndExecute("A person has 4 pastimes called their hobbies");
            var invention = o.Generator("person").Generate();
            Assert.AreEqual(5, invention.Individuals.Count);
        }

        [TestMethod]
        public void  CompoundNounTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("a cat is a kind of person",
                "a persian is a kind of cat",
                "a tabby is a kind of cat",
                "a siamese is a kind of cat",
                "a cat can be haughty",
                "a cat can be cuddly",
                "a cat can be crazy",
                "a persian can be matted",
                "thaumaturge and necromancer are kinds of magic user");
            var cat = Ontology.CommonNoun("cat");
            var magicUser = Ontology.CommonNoun("magic", "user");
            var g = new Generator(cat, magicUser);
            for (var n = 0; n < 100; n++)
            {
                var i = g.Generate();
                Assert.IsTrue(i.IsA(i.Individuals[0], cat));
                Assert.IsTrue(i.IsA(i.Individuals[0], "persian")
                              || i.IsA(i.Individuals[0], "tabby")
                              || i.IsA(i.Individuals[0], "siamese"));
                Console.WriteLine(i.Model.Model);
                Console.WriteLine(i.Description(i.Individuals[0]));
            }
        }

        [TestMethod]
        public void ImpliedAdjectiveTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("cats are fuzzy");
            var cat = Ontology.CommonNoun("cat");
            var fuzzy = Ontology.Adjective("fuzzy");
            var g = new Generator(cat);
            for (var n = 0; n < 100; n++)
            {
                var i = g.Generate();
                Assert.IsTrue(i.IsA(i.Individuals[0], fuzzy));
            }
        }

        [TestMethod]
        public void NumericPropertyTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("cats have an age between 1 and 20");
            var cat = Ontology.CommonNoun("cat");
            var age = cat.Properties[0];
            var g = new Generator(cat);
            for (var n = 0; n < 100; n++)
            {
                var i = g.Generate();
                var ageVar = i.Individuals[0].Properties[age];
                var ageValue = (float)i.Model[ageVar];
                Assert.IsTrue(ageValue >= 1 && ageValue <= 20);
            }
        }

        [TestMethod]
        public void ProperNameTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("a cat is a kind of person",
                "a persian is a kind of cat",
                "a tabby is a kind of cat",
                "a siamese is a kind of cat",
                "a cat can be haughty",
                "a cat can be cuddly",
                "a cat can be crazy",
                "a persian can be matted",
                "thaumaturgy is a form of magic",
                "necromancy is a form of magic",
                "a magic user must practice one form of magic");
            var cat = Ontology.CommonNoun("cat");
            var magicUser = Ontology.CommonNoun("magic", "user");
            var thaumaturgy = Ontology.Individual("thaumaturgy");
            var necromancy = Ontology.Individual("necromancy");
            var g = new Generator(cat, magicUser);
            for (var n = 0; n < 100; n++)
            {
                var i = g.Generate();
                Assert.IsTrue(i.Holds("practices", i.Individuals[0], thaumaturgy)
                              || i.Holds("practices", i.Individuals[0], necromancy));
                Console.WriteLine(i.Model.Model);
                Console.WriteLine(i.Description(i.Individuals[0]));
            }
        }
    }
}
