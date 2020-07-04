using System;

namespace Imaginarium.Driver
{
    /// <summary>
    /// A stub implementation of IRepl that does nothing
    /// Used when Imaginarium is being run non-interactively, e.g. in a game.
    /// </summary>
    public class StubRepl : IRepl
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        public void AddButton(string buttonName, string command)
        { }

        /// <summary>
        /// Throws an exception with the specified message, since there isn't a REPL to write to.
        /// </summary>
        public void SetOutputWindow(string contents)
        {
            throw new Exception(contents);
        }
    }
}
