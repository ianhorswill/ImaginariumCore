using System.IO;
using Imaginarium.Parsing;

namespace Imaginarium.Driver
{
    internal class FileSystemResourceProvider : IResourceProvider
    {
        public bool IsValidResourceName(string path) => Parser.NameIsValidFilename(path);

        public string[] ReadAllLines(string path) => File.ReadAllLines(path);

        /// <summary>
        /// Returns the full path for the specified list file in the definition library.
        /// </summary>
        public string ListFilePath(string directory, string fileName)
        {
            var definitionFilePath = Path.Combine(directory, fileName + DataFiles.ListExtension);
            return definitionFilePath;
        }
    }
}
