#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ParserTests.cs" company="Ian Horswill">
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

using Imaginarium.Driver;
using Imaginarium.Ontology;
using Imaginarium.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class ParserTests
    {
        public static Ontology Ontology = new Ontology("Test", null);
        public Parser Parser = new Parser(Ontology);

        public ParserTests()
        {
            DataFiles.DataHome = "../../../Imaginarium/";
        }

        [TestMethod, ExpectedException(typeof(GrammaticalError))]
        public void GibberishTest()
        {
            Parser.ParseAndExecute("foo bar baz");
        }

        [TestMethod]
        public void PluralDeclarationTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("the plural of person is people");
            Assert.AreEqual("people",(Ontology.CommonNoun("person")).PluralForm[0]);
        }

        [TestMethod]
        public void KindOfTestSingular()
        {
            Parser.ParseAndExecute("a cat is a kind of person");
            Assert.IsTrue(Parser.Subject.CommonNoun.IsImmediateSubKindOf(Parser.Object.CommonNoun));
        }

        [TestMethod]
        public void KindOfTestPlural()
        {
            Parser.ParseAndExecute("cats are a kind of person");
            Assert.IsTrue(Parser.Subject.CommonNoun.IsImmediateSubKindOf(Parser.Object.CommonNoun));
        }

        [TestMethod]
        public void AdjectiveDeclarationTestPlural()
        {
            Parser.ParseAndExecute("cats can be lovely");
            Assert.IsTrue(Parser.PredicateAP.Adjective.RelevantTo(Parser.Subject.CommonNoun));
        }

        [TestMethod]
        // ReSharper disable once InconsistentNaming
        public void NPListTest()
        {
            Parser.ParseAndExecute("tabby, persian, and siamese are kinds of cat");
            Assert.IsTrue(Ontology.CommonNoun("tabby").IsImmediateSubKindOf(Ontology.CommonNoun("cat")));
            Assert.IsTrue(Ontology.CommonNoun("persian").IsImmediateSubKindOf(Ontology.CommonNoun("cat")));
            Assert.IsTrue(Ontology.CommonNoun("siamese").IsImmediateSubKindOf(Ontology.CommonNoun("cat")));
        }

        [TestMethod]
        // ReSharper disable once InconsistentNaming
        public void APListTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("Cats can be white, black, or ginger");
        }

        [TestMethod]
        public void RequiredAlternativeSetTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("cats are long haired or short haired");
            var cat = Ontology.CommonNoun("cat");
            Assert.AreEqual(1, cat.AlternativeSets.Count);
        }

        [TestMethod]
        public void OptionalAlternativeSetTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("cats can be big or small");
            var cat = Ontology.CommonNoun("cat");
            Assert.AreEqual(1, cat.AlternativeSets.Count);
        }

        [TestMethod]
        public void InterningNounTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("Tabby, Persian, and Maine Coon are kinds of cat");
            Assert.IsNotNull(Ontology.CommonNoun("Persian"));
            Assert.IsNotNull(Ontology.CommonNoun("Persians"));
        }

        [TestMethod]
        public void ParseAntiReflexiveTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("Cats are a kind of person.",
                "Cats cannot love themselves");
            var love = Ontology.Verb("love");
            Assert.IsNotNull(love);
        }

        [TestMethod]
        public void ParseReflexiveTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("Cats are a kind of person.",
                "Cats must love themselves");
            var love = Ontology.Verb("love");
            Assert.IsNotNull(love);
        }

        [TestMethod]
        public void BeRelatedToTest()
        {
            Ontology.EraseConcepts();
            Parser.ParseAndExecute("A character can be related to other characters");
            var love = Ontology.Verb("be","related","to");
            Assert.IsNotNull(love);
        }
    }
}
