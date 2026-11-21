using Imaginarium.Driver;

namespace WebAssemblySandbox
{
    public class ResourceProvider : IResourceProvider
    {
        public static readonly Dictionary<string, string> ResourceCache = new();

        public bool IsValidResourceName(string path) => true;

        public string[] ReadAllLines(string path)
        {
            if (ResourceCache.TryGetValue(path, out var content))
                return content.Split('\n');
            return (path.ToLower() switch
            {
                "female cat names" => Resources.FemaleCatNames,
                "male cat names" => Resources.MaleCatNames,
                "english surnames" => Resources.EnglishSurnames,
                "female names" => Resources.FemaleNames,
                "male names" => Resources.MaleNames,
                "bear names" => Resources.BearNames,
                "prizes" => Resources.Prizes,
                "surprises" => Resources.Surprises,
                "dog names" => Resources.DogNames,
                _ => throw new FileNotFoundException($"No such resource file: {path}", path)
            }).Split('\n');

        }

        public string ListFilePath(string directory, string filename) => filename;
    }
}
