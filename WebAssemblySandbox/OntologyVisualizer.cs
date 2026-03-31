using Imaginarium.Driver;
using Imaginarium.Generator;
using Imaginarium.Ontology;
using System.Text;

namespace WebAssemblySandbox
{
    public static class OntologyVisualizer
    {
        private const string NounColor = "orange";
        private const string VerbColor = "red";
        private const string AdjectiveColor = "green";
        private const string MiscColor = "grey";

        static IEnumerable<(object from, object to, string label, string? style)> Edges(object o)
        {
            switch (o)
            {
                //case Individual i:
                //    if (UIDriver.Invention != null)
                //        foreach (var kind in UIDriver.Invention.MostSpecificNouns(i))
                //            yield return (i, kind, "is a", null);
                //    break;

                case ProperNoun p:
                    foreach (var kind in p.Kinds)
                        yield return (p, kind, "is a", NounColor);
                    break;

                case CommonNoun c:
                    foreach (var parent in c.Superkinds)
                        yield return (c, parent, "kind of", NounColor);
                    foreach (var child in c.Subkinds)
                        yield return (child, c, "kind of", NounColor);
                    foreach (var a in c.RelevantAdjectives)
                        yield return (c, a, "can be", AdjectiveColor);
                    foreach (var s in c.AlternativeSets)
                        foreach (var a in s.Alternatives)
                            yield return (c, a.Concept, "can be", AdjectiveColor);
                    foreach (var a in c.ImpliedAdjectives)
                        if (a.Conditions.Length == 0)
                            yield return (c, a.Modifier.Concept, a.Modifier.IsPositive ? "is always" : "is never", AdjectiveColor);
                        else
                            yield return (c, a.Modifier.Concept, a.Modifier.IsPositive ? "can be" : "can be not", AdjectiveColor);
                    foreach (var p in c.Parts)
                        yield return (c, p, "has part", MiscColor);
                    foreach (var prop in c.Properties)
                        yield return (c, prop, "has property", MiscColor);
                    break;

                case Verb v:
                    foreach (var m in v.SubjectAndObjectKindsAndModifiers)
                    {
                        if (m.Item1.Kind != null)
                            yield return (m.Item1.Kind, v, "subject", VerbColor);
                        if (m.Item2.Kind != null)
                            yield return (v, m.Item2.Kind, "object", VerbColor);
                    }
                   
                    foreach (var super in v.Generalizations)
                        yield return (v, super, "implies", VerbColor);
                    foreach (var super in v.Superspecies)
                        yield return (v, super, "is a way of", VerbColor);
                    foreach (var m in v.MutualExclusions)
                        yield return (v, m, "mutually exclusive", VerbColor);
                    break;

                case Part part:
                    yield return (part, part.Kind, "is a", NounColor);
                    break;
            }
        }

        static (string label, string style) NodeLabel(object node)
        {
            switch (node)
            {
                //case Individual i:
                //    var iName = UIDriver.Invention?.NameString(i) ?? i.ToString();
                //    return (iName, nounStyle);
                case Noun n:
                    return (n.ToString(), NounColor);

                case Verb v:
                    return (v.ToString(), VerbColor);

                case Adjective a:
                    return (a.ToString(), AdjectiveColor);

                default:
                    return (node.ToString()!, MiscColor);
            }
        }

        static string NodeTooltip(object node)
        {
            switch (node)
            {
                //case Individual i:
                //    var iName = UIDriver.Invention?.NameString(i) ?? i.ToString();
                //    return (iName, nounStyle);
                case Noun n:
                    return n.Description;

                case Verb v:
                    return v.Description;

                case Adjective a:
                    return a.Description;

                case Property p:
                    return p.Description;

                case Part part:
                    return part.Description;

                default:
                    return "";
            }
        }

        public static readonly StringBuilder GraphCode = new StringBuilder();
        public static readonly string[] EdgeColors = new[] { "red", "green", "blue", "orange", "purple", "brown", "cyan", "magenta" };

        static readonly StringBuilder StyleCode = new StringBuilder();
        public static string Mermaid(Ontology ontology)
        {
            var nouns = ontology.AllNouns.Select(pair => pair.Value).Cast<object>().ToArray();
            var verbs = ontology.AllVerbs.ToArray();
            var vocabulary = nouns.Concat(verbs);

            GraphCode.Clear();
            StyleCode.Clear();
            GraphCode.AppendLine(ReplCommands.ShowMermaidCode?"<pre>":"<pre style=\"padding-top: 50px;\" class=\"mermaid\">");
            GraphCode.AppendLine("---\r\nconfig:\r\n  layout: dagre\r\n---");
            GraphCode.AppendLine("graph LR");
            var nodes = new Dictionary<object, string>();
            var uidCounter = 0;


            string NodeReference(object i)
            {
                if (nodes.TryGetValue(i, out var uid))
                    return uid;
                uid = nodes[i] = $"n{uidCounter++}";
                var (name, color) = NodeLabel(i);
                StyleCode.AppendLine($"style {uid} fill:{color},color:#000,stroke:#000");
                var nodeTooltip = NodeTooltip(i);
                if (!string.IsNullOrEmpty(nodeTooltip))
                    StyleCode.AppendLine($"click {uid} callback \"{nodeTooltip}\"");
                return $"{uid}[{name}]";
            }

            var edgeCounter = 0;
            var existingEdges = new HashSet<(object, object, string)>();
            foreach (var n in ontology.AllNouns.Values)
            foreach (var (from, to, label, style) in Edges(n))
            {
                if (existingEdges.Contains((from, to, label)))
                    continue;
                existingEdges.Add((from, to, label));
                    var edgeDef = $"   {NodeReference(from)} -- {label} --> {NodeReference(to)}";
                var edgeStyle = $"   linkStyle {edgeCounter++} stroke-width:2px,fill:none,stroke:{style};";
                GraphCode.AppendLine(edgeDef);
                GraphCode.AppendLine(edgeStyle);
            }

            foreach (var v in ontology.AllVerbs)
            foreach (var (from, to, label, style) in Edges(v))
            {
                if (existingEdges.Contains((from, to, label)))
                    continue;
                existingEdges.Add((from, to, label));
                    var edgeDef = $"   {NodeReference(from)} -- {label} --> {NodeReference(to)}";
                var edgeStyle = $"   linkStyle {edgeCounter++} stroke-width:2px,fill:none,stroke:{style};";
                GraphCode.AppendLine(edgeDef);
                GraphCode.AppendLine(edgeStyle);
            }

            GraphCode.Append(StyleCode);

            GraphCode.AppendLine("</pre>");

            return GraphCode.ToString();
        }
    }
}
