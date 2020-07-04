using System;

namespace Imaginarium.Driver
{
    public class StubRepl : IRepl
    {
        public void AddButton(string buttonName, string command)
        { }

        public void SetOutputWindow(string contents)
        {
            throw new Exception(contents);
        }
    }
}
