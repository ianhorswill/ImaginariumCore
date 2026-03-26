namespace Imaginarium.Driver
{
    /// <summary>
    /// Abstract interface to file system
    /// </summary>
    public interface IResourceProvider
    {
        /// <summary>
        /// True if this is a valid source (file) name
        /// </summary>
        bool IsValidResourceName(string path);

        /// <summary>
        /// Return the file as an array of strings, one per line.
        /// </summary>
        string[] ReadAllLines(string path);

        /// <summary>
        /// Return the internal path to use for a list file.
        /// </summary>
        string ListFilePath(string directory, string filename);

    }
}
