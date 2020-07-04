﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Driver.cs" company="Ian Horswill">
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

using System.IO;
using System.Text;
using Imaginarium.Parsing;

namespace Imaginarium.Driver
{
    public static class Driver
    {
        #region Command output
        private static readonly StringBuilder CommandBuffer = new StringBuilder();

        /// <summary>
        /// Remove any pending output
        /// </summary>
        public static void ClearCommandBuffer()
        {
            CommandBuffer.Length = 0;
        }

        public static void PrependResponseLine(string s)
        {
            CommandBuffer.Insert(0, s);
        }

        public static void AppendResponseLine(string s)
        {
            CommandBuffer.AppendLine(s);
        }

        public static string CommandResponse => CommandBuffer.ToString();
        #endregion

        #region Load error tracking
        private static readonly StringBuilder LoadErrorBuffer = new StringBuilder();

        public static void ClearLoadErrors()
        {
            LoadErrorBuffer.Length = 0;
        }

        public static string LoadErrors => 
            LoadErrorBuffer.Length>0 ? LoadErrorBuffer.ToString() : null;

        public static void LogLoadError(string filename, int lineNumber, string message)
        {
            if (Parser.InputTriggeringException == null)
                LoadErrorBuffer.AppendLine($"File {Path.GetFileName(filename)}, line {lineNumber}:\n\t<b>{message}</b>");
            else
            {
                LoadErrorBuffer.AppendLine($"File {Path.GetFileName(filename)}, line {lineNumber}:");
                LoadErrorBuffer.AppendLine($"While processing the command:\n\t<b>{Parser.InputTriggeringException}</b>");
                if (Parser.RuleTriggeringException != null)
                    LoadErrorBuffer.AppendLine(
                        $"And matching it against the sentence pattern:\n\t{Parser.RuleTriggeringException.SentencePatternDescription}");
                LoadErrorBuffer.AppendLine($"The following error occured:\n\t<b>{message}</b>");
            }
        }
        #endregion

        public static IRepl Repl = new StubRepl();

        public static void SetOutputWindow(string contents) => Repl.SetOutputWindow(contents);
    }

    public interface IRepl
    {
        void AddButton(string buttonName, string command);
        void SetOutputWindow(string contents);
    }
}