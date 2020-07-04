#region Copyright
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
    /// <summary>
    /// Abstracts console that can be written to.
    /// This is filled in with something of type IRepl, the default being StubRepl, that does nothing.
    /// </summary>
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

        /// <summary>
        /// Insert string at the beginning of the output on the screen
        /// </summary>
        public static void PrependResponseLine(string s)
        {
            CommandBuffer.Insert(0, s);
        }

        /// <summary>
        /// Add string to the end of the output on the screen
        /// </summary>
        public static void AppendResponseLine(string s)
        {
            CommandBuffer.AppendLine(s);
        }

        /// <summary>
        /// The accumulated results of the all the calls to PrependRespondLine and AppendResponseLine
        /// since last call to ClearCommandBuffer.
        /// </summary>
        public static string CommandResponse => CommandBuffer.ToString();
        #endregion

        #region Load error tracking
        /// <summary>
        /// Separate buffer for diagnostics from file loading
        /// </summary>
        private static readonly StringBuilder LoadErrorBuffer = new StringBuilder();

        /// <summary>
        /// Clear the buffer for file loading diagnostics.
        /// This is separate from the "CommandOutput" buffer.
        /// </summary>
        public static void ClearLoadErrors()
        {
            LoadErrorBuffer.Length = 0;
        }

        /// <summary>
        /// Return any load error diagnostics that have been generated since the last call to ClearLoadErrors.
        /// </summary>
        public static string LoadErrors => 
            LoadErrorBuffer.Length>0 ? LoadErrorBuffer.ToString() : null;

        /// <summary>
        /// Add another line to the load errors buffer
        /// </summary>
        /// <param name="filename">File where the problem occured</param>
        /// <param name="lineNumber">Line number where it occured</param>
        /// <param name="message">Problem description</param>
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

        /// <summary>
        /// The IRepl object that does the actual output to the user
        /// </summary>
        public static IRepl Repl = new StubRepl();

        /// <summary>
        /// Completely replace the current output with the specified string
        /// </summary>
        /// <param name="contents">New output</param>
        public static void SetOutputWindow(string contents) => Repl.SetOutputWindow(contents);
    }

    /// <summary>
    /// Interface for Read/Eval/Print loop driving this library, if any.
    /// If this is running headless, then StubRepl is used.
    /// </summary>
    public interface IRepl
    {
        /// <summary>
        /// Add a button the user can press to the screen
        /// </summary>
        /// <param name="buttonName">Text for the button</param>
        /// <param name="command">Imaginarium command to run when the button is pressed</param>
        void AddButton(string buttonName, string command);

        /// <summary>
        /// Completely replace the current output with the specified string
        /// </summary>
        /// <param name="contents">New output</param>
        void SetOutputWindow(string contents);
    }
}