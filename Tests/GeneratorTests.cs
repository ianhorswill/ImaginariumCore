#region Copyright
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
using System.Linq;
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
        public static Ontology Ontology = new Ontology("Test");
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
        public void RelativeFrequencyTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("a cat is a kind of person",
                "persian, tabby (10), and siamese are kinds of cat",
                "a cat is grey, white, ginger (10), or black.",
                "a cat can be haughty",
                "a cat can be cuddly",
                "a cat can be crazy",
                "a persian can be matted");
            var cat = Ontology.CommonNoun("cat");
            var tabby = Ontology.CommonNoun("tabby");
            var ginger = Ontology.Adjective("ginger");
            Assert.AreEqual(tabby, cat.Subkinds[1]);
            //cat.SubkindFrequencies[1] = 10; // tabby
            var g = new Generator(cat);
            var tabbyCount = 0;
            var gingerCount = 0;
            for (var n = 0; n < 1000; n++)
            {
                var i = g.Generate();
                if (i.IsA(i.Individuals[0], tabby))
                    tabbyCount++;
                if (i.IsA(i.Individuals[0], ginger))
                    gingerCount++;
            }
            Assert.IsTrue(tabbyCount > 700);
            Assert.IsTrue(tabbyCount < 1000);
            Assert.IsTrue(gingerCount > 700);
            Assert.IsTrue(gingerCount < 1000);
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
        public void UnnamedPartTest()
        {
            var o = new Ontology("UnnamedPartTest");
            o.Parser.ParseAndExecute("a cat is a kind of person",
                "a cat has a color");
            var cat = o.CommonNoun("cat");
            var color = o.CommonNoun("color");
            var i = o.Generator(cat).Generate();
            Assert.AreEqual(1, cat.Parts.Count);
            Assert.AreEqual("color", cat.Parts[0].StandardName.Untokenize());
            Assert.IsTrue(i.IsA(i.Individuals[0], cat));
            Assert.IsTrue(i.IsA(i.Individuals[1], color));
        }

        [TestMethod]
        public void MultiPartTest()
        {
            var o = new Ontology("MultiPartTest");
            o.Parser.ParseAndExecute("A person has 4 pastimes called their hobbies");
            var invention = o.Generator("person").Generate();
            Assert.AreEqual(5, invention.Individuals.Count);
        }

        [TestMethod]
        public void PartNamingTest()
        {
            var o = new Ontology("PartNamingTest");
            o.Parser.ParseAndExecute("A face has eyes",
                "A face has a mouth",
                "A face has a nose",
                "A face has hair");
            var invention = o.Generator("face").Generate();
            Assert.AreEqual("the face", invention.NameString(invention.Individuals[0]));
            Assert.AreEqual("the face's eye", invention.NameString(invention.Individuals[1]));
            Assert.AreEqual("the face's mouth", invention.NameString(invention.Individuals[2]));
            Assert.AreEqual("the face's nose", invention.NameString(invention.Individuals[3]));
            Assert.AreEqual("the face's hair", invention.NameString(invention.Individuals[4]));
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
        public void MultipleRangePropertyTest()
        {
            var o = new Ontology("MultipleRangePropertyTest");
            o.Parser.ParseAndExecute("cats have an age between 1 and 20",
                "kittens are a kind of cat",
                "kittens have an age between 1 and 2",
                "adults are a kind of cat",
                "adults have an age between 2 and 20");
            var g = o.Generator("cat");
            var age = o.Property("age");
            for (var n = 0; n < 200; n++)
            {
                var i = g.Generate();
                var cat = i.Individuals[0];
                var catAge = (float)(i.PropertyValue(cat, age));
                if (i.IsA(cat, "kitten"))
                {
                    Assert.IsTrue(catAge >= 1);
                    Assert.IsTrue(catAge <= 2);
                }
                else
                {
                    Assert.IsTrue(catAge >= 2);
                    Assert.IsTrue(catAge <= 20);
                }

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

        [TestMethod]
        public void PossibleIndividualTest()
        {
            var o = new Ontology("PossibleIndividualTest");
            o.ParseAndExecute("a cat is a kind of person",
                "a persian is a kind of cat",
                "a tabby is a kind of cat",
                "a siamese is a kind of cat",
                "a cat can be haughty",
                "a cat can be cuddly",
                "a cat can be crazy",
                "a persian can be matted");
            var i = o.Generator("cat", 10).Generate();
            foreach (var pi in i.PossibleIndividuals)
                Assert.IsTrue(pi.IsA("cat"));
        }

        [TestMethod]
        public void PossibleIndividualPartTest()
        {
            var o = new Ontology("PartNamingTest");
            o.ParseAndExecute("A face has eyes",
                "A face has a mouth",
                "A face has a nose",
                "A face has hair");
            var invention = o.Generator("face").Generate();
            var face = invention.PossibleIndividuals[0];
            Assert.IsTrue(face.IsA("face"));
            Assert.IsTrue(face.Part("eye")[0].IsA("eye"));
            Assert.IsTrue(face.Part("mouth")[0].IsA("mouth"));
            Assert.IsTrue(face.Part("nose")[0].IsA("nose"));
            Assert.IsTrue(face.Part("hair")[0].IsA("hair"));
        }

        [TestMethod, ExpectedException(typeof(GrammaticalError))]
        public void ImagineNotKnowingTest()
        {
            var o = new Ontology("Not knowing test") { IsLocked = true};
            o.ParseAndExecute("imagine not knowing what a car is");
        }

        [TestMethod]
        public void MonsterTest()
        {
            var o = new Ontology(nameof(MonsterTest));
            foreach (var decl in MonsterSource.Replace("\r", "").Split(new [] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                o.ParseAndExecute(decl);
            var g = o.CommonNoun("monster").MakeGenerator(20);
            for (var n = 0; n < 100; n++) 
                g.Generate();
            Console.WriteLine(g.Problem.PerformanceStatistics);
            Console.WriteLine($"Average flips per solve: {g.Problem.SolveFlips.Average}");
        }

        private const string MonsterSource = @"
fish, bird, plant, fungus, reptile, mammal (5), insect, and crustacean are kinds of monster.
the plural of fungus is fungi.

humanoid, carnivoid, rodent, bat, marsupial, shrew, ungulate, and rabbitoid are kinds of mammal. 

felinoid, caninoid, and bearoid are kinds of carnivoid.
carnivoids are quadrapedal.

A monster is flat, ameboid, legged, serpentine, polyhedral, or spherical.
A legged monster is bipedal, tripedal, quadrapedal, hexapodal, octopodal, or centipedal.
Do not mention being legged.

mammals are legged.
reptiles are legged.
birds are legged.
insects can be legged.
crustaceans are legged.
fishes are not legged.

monsters can be aerial or aquatic.
do not mention being aerial.
fishes are aquatic.
crustaceans are aquatic.

monsters can be winged.
birds are winged.

monsters are very small, small, dog-sized, human-sized, large, very large, or building-sized.
monsters are teleporting, burrowing, slithering, flying, swimming, walking, or hovering.

flying monsters are winged.
Aerial monsters are flying or hovering.
A burrowing monster is legged.


flying monsters are aerial.
hovering monsters are aerial.

a swimming monster is always aquatic.
walking monsters are legged.
a slithering monster is always a reptile.
Do not mention being walking.

monsters are spitting, biting, clawing, bashing, or fire breathing.
aquatic monsters are not fire breathing.
aquatic monsters are not spitting.";

        [TestMethod]
        public void IdentifiedAsTest()
        {
            var o = new Ontology(nameof(IdentifiedAsTest));
            o.ParseAndExecute("esoteric crops, dangerous crops, and psychoactive crops are kinds of plant",
                "wolf's bane, and foo are kinds of esoteric crops",
                "triffids, poison ivy, and venus flytrap are kinds of dangerous crops",
                "dartura, wormwood, peyote, ayahuasca, tobacco, and chocolate are kinds of psychoactive crops",
                "a plant is described as \"[Modifiers] [Noun]\"",
                "vodka, gin, bourbon, and absinthe are kinds of alcohol base",
                "an alcohol base is described as \"[Modifiers] [Noun]\"",
                "an infusion has an alcohol base",
                "an infusion has a plant",
                "an infusion is described as \"[plant]-infused [alcohol]\"");
            var g = o.CommonNoun("infusion").MakeGenerator();
            for (var i = 0; i < 100; i++)
            {
                var invention = g.Generate();
                invention.PossibleIndividuals[0].Description();
            }
        }

        [TestMethod]
        public void OverlappingAdjectivesTest()
        {
            var o = new Ontology(nameof(OverlappingAdjectivesTest));
            o.ParseAndExecute("x, y, and z are kinds of thing",
                "a x is between 4 and 5 of b, c, d, e, f, or g",
                "a y is between 1 and 2 of b, c, d, e, f, or g",
                "a z is any 3 of b, c, d, e, f, or g");
            var adjectives = new[] {"b", "c", "d", "e", "f", "g"}.Select(a => o.Adjective(a)).ToArray();
            var g = o.CommonNoun("thing").MakeGenerator();
            for (var i = 0; i < 100; i++)
            {
                var thing = g.Generate().PossibleIndividuals[0];
                var count = adjectives.Count(a => thing.IsA(a));
                Assert.IsTrue(
                    (thing.IsA("x") && count >= 4 && count <= 5)
                    || (thing.IsA("y") && count >= 1 && count <= 2)
                    || (thing.IsA("z") && count == 3)
                );
            }
        }

        [TestMethod]
        public void ForcedCycleTest()
        {
            var o = new Ontology(nameof(ForcedCycleTest));
            o.ParseAndExecute("cat, dog, and mouse are kinds of animals",
                "an animal can chase some animal",
                "animals cannot chase each other",
                "animals cannot chase themselves",
                "a dog must dchase one cat",
                "a cat must cchase one mouse",
                "a mouse must mchase one dog",
                "mchasing is a way of chasing",
                "cchasing is a way of chasing",
                "dchasing is a way of chasing");

            o.Generator("animal", 3).Generate();
        }
    }
}
