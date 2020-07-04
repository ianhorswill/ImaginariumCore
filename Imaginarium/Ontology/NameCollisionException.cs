#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NameCollisionException.cs" company="Ian Horswill">
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
using Imaginarium.Parsing;

namespace Imaginarium.Ontology
{
    /// <summary>
    /// Represents an error in which the same name is used for two different Concepts of different types (e.g. noun and verb)
    /// </summary>
    public class NameCollisionException : UserException
    {
        /// <summary>
        /// Name that was inconsistently defined
        /// </summary>
        public readonly string[] Name;
        /// <summary>
        /// First type assigned to the name
        /// </summary>
        public readonly Type OldType;
        /// <summary>
        /// New type the user tried to assign to it.
        /// </summary>
        public readonly Type NewType;

        /// <summary>
        /// Make a new NameCollisionException
        /// </summary>
        public NameCollisionException(string[] name, Type oldType, Type newType)
            : base(
                $"Can't create a new {newType}, {name.Untokenize()}, because there is already {oldType} of the same name.",
                $"You appear to be using {name.Untokenize()} as if it were a {Concept.EnglishTypeName(newType)}, but I thought it was a {Concept.EnglishTypeName(oldType)}")
        {
            Name = name;
            OldType = oldType;
            NewType = newType;
        }
    }
}
