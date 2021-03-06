﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Test.cs" company="Ian Horswill">
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

using System.Collections.Generic;
using System.Linq;
using Imaginarium.Generator;

namespace Imaginarium.Ontology
{
    /// <summary>
    /// A unit test for generators
    /// Consists of an NP and whether it should exist or not.
    /// </summary>
    public class Test
    {
        /// <summary>
        /// The kind that ought to exist or not exist
        /// </summary>
        public readonly CommonNoun Noun;
        /// <summary>
        /// Any additional attributes it should have
        /// </summary>
        public readonly MonadicConceptLiteral[] Modifiers;
        /// <summary>
        ///  Whether this is a test that the Noun+Modifiers does exist, or a test that it doesn't.
        /// </summary>
        public readonly bool ShouldExist;
        /// <summary>
        /// Message to print if the test succeeds
        /// </summary>
        public readonly string SucceedMessage;
        /// <summary>
        /// Message to print if it fails
        /// </summary>
        public readonly string FailMessage;

        internal Test(CommonNoun noun, IEnumerable<MonadicConceptLiteral> modifiers, bool shouldExist, string succeedMessage, string failMessage)
        {
            Noun = noun;
            ShouldExist = shouldExist;
            SucceedMessage = succeedMessage;
            FailMessage = failMessage;
            Modifiers = modifiers.ToArray();
        }

        /// <summary>
        /// Run this test
        /// </summary>
        /// <returns>Whether it succeeded and the example/counter-example that was generated (or null, if nothing was generated)</returns>
        public (bool success, Invention example) Run()
        {
            var example = TestExistence();
            var success = ShouldExist == (example != null);
            return (success, example);
        }

        private Invention TestExistence()
        {
            try
            {
                var g = new Generator.Generator(Noun, Modifiers, 1);
                return g.Generate();
            }
            catch (CatSAT.ContradictionException)
            {
                return null;
            }
            catch (CatSAT.TimeoutException)
            {
                return null;
            }
        }
    }
}
