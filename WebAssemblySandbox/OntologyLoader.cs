using System.Text.RegularExpressions;
using Imaginarium.Driver;
using Imaginarium.Parsing;

namespace WebAssemblySandbox
{
    public class OntologyLoader(Parser parser)
    {
        private static readonly HttpClient Http = new();
        private Parser parser = parser;

        public Task<Parser> SplitAndLoad(string commands, string? currentContext = null) => Load(SplitAndCanonicalize(commands).ToArray());
        public async Task<Parser> Load(string[] commands, string? currentContext = null)
        {
            //Driver.AppendResponseLine($"Running {commands.Length} commands");
            //Driver.AppendResponseLine("Starting load...");
            // Proactively load all the files referenced by usings
            var loadingResources = new Dictionary<string, Task<string>>(
                commands.Where(IsUsingCommand).Select(command =>
            {
                var path = UsingToPath(command, currentContext);
                //Driver.AppendResponseLine($"Starting load of {path}");
                return new KeyValuePair<string, Task<string>>(path, Http.GetStringAsync(path));
            }));

            //Driver.AppendResponseLine("loads started...");

            // Now execute the commands one by one
            foreach (var command in commands)
            {
                //Driver.AppendResponseLine($"command={command}");
                if (IsUsingCommand(command))
                {
                    var path = UsingToPath(command);
                    var content = await loadingResources[path];
                    //Driver.AppendResponseLine($"content={content}");
                    var extension = Path.GetExtension(path);
                    switch (extension)
                    {
                        case ".gen":
                            await SplitAndLoad(content);
                            break;

                        case ".txt":
                            ResourceProvider.ResourceCache[Path.GetFileNameWithoutExtension(path)] = content;
                            break;

                        default:
                            throw new Exception($"{extension} is not a valid file type in command '{command}'");
                    }
                } else
                    LoadCommand(command);
            }

            return parser;
        }

        private static bool IsUsingCommand(string command)
        {
            return command.StartsWith("using ");
        }

        private void LoadCommand(string command)
        {
            parser.ParseAndExecute(command);
        }

        public static IEnumerable<string> SplitAndCanonicalize(string lines)
        {
            foreach (var line in lines.Split('\n'))
            {
                var trimmed = TrimLine(line);
                if (trimmed != null)
                    yield return trimmed;
            }
        }

        private const string Comment = @"(?:#|//).*$";

        public static string? TrimLine(string line)
        {
            //Driver.AppendResponseLine($"Line='{line}'");
            var noComment = Regex.Replace(line.Replace("\r", ""), Comment, "");
            var trimmed = noComment.Trim();
            //Driver.AppendResponseLine($"Trimmed='{trimmed}'");
            return (trimmed != "") ? trimmed : null;
        }

        private static string UsingToPath(string usingCommand, string? currentContext = null) =>
            CanonicalizeUrl(Regex.Replace(usingCommand, "^using\\s+", ""), currentContext);

        public static string CanonicalizeUrl(string url, string? currentContext = null)
        {
            if (url.StartsWith("github/"))
                url = ToRawGitHubUrl(url);
            if (currentContext != null && !url.StartsWith("http"))
                url = Path.Combine(currentContext, url);
            if (Path.GetExtension(url) == "")
                return url + (url.EndsWith("/") ? "" : "/") + "main.gen";
            return url;
        }

        static string ToRawGitHubUrl(string spec)
        {
            // Expected: "github/{owner}/{repo}/{path...}"
            var parts = spec.Split('/', 4);

            if (parts.Length < 4 || parts[0] != "github" ||
                parts.Skip(1).Any(string.IsNullOrEmpty))
                throw new FormatException($"Expected 'github/{{owner}}/{{repo}}/{{path}}', got '{spec}'.");

            var (owner, repo, path) = (parts[1], parts[2], parts[3]);
            return $"https://raw.githubusercontent.com/{owner}/{repo}/HEAD/{path}";
        }
    }
}
