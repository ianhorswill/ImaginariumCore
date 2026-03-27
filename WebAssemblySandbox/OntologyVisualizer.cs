using Imaginarium.Driver;
using Imaginarium.Generator;
using Imaginarium.Ontology;
using System.Text;

namespace WebAssemblySandbox
{
    public static class OntologyVisualizer
    {
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
                        yield return (p, kind, "is a", null);
                    break;

                case CommonNoun c:
                    foreach (var parent in c.Superkinds)
                        yield return (c, parent, "kind of", null);
                    foreach (var child in c.Subkinds)
                        yield return (child, c, "kind of", null);
                    foreach (var a in c.RelevantAdjectives)
                        yield return (c, a, "can be", null);
                    foreach (var s in c.AlternativeSets)
                        foreach (var a in s.Alternatives)
                            yield return (c, a.Concept, "can be", null);
                    foreach (var a in c.ImpliedAdjectives)
                        if (a.Conditions.Length == 0)
                            yield return (c, a.Modifier.Concept, a.Modifier.IsPositive ? "is always" : "is never", null);
                        else
                            yield return (c, a.Modifier.Concept, a.Modifier.IsPositive ? "can be" : "can be not", null);
                    foreach (var p in c.Parts)
                        yield return (c, p, "has part", null);
                    foreach (var prop in c.Properties)
                        yield return (c, prop, "has property", null);
                    break;

                case Verb v:
                    foreach (var m in v.SubjectAndObjectKindsAndModifiers)
                    {
                        if (m.Item1.Kind != null)
                            yield return (m.Item1.Kind, v, "subject", null);
                        if (m.Item2.Kind != null)
                            yield return (v, m.Item2.Kind, "object", null);
                    }
                   
                    foreach (var super in v.Generalizations)
                        yield return (v, super, "implies", null);
                    foreach (var super in v.Superspecies)
                        yield return (v, super, "is a way of", null);
                    foreach (var m in v.MutualExclusions)
                        yield return (v, m, "mutually exclusive", null);
                    break;

                case Part part:
                    yield return (part, part.Kind, "is a", null);
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
                    return (n.ToString(), "white");

                case Verb v:
                    return (v.ToString(), (v.ObjectKind == null || v.SubjectKind == null) ? "red" : "green");

                default:
                    return (node.ToString()!, "yellow");
            }
        }

        public static readonly StringBuilder GraphCode = new StringBuilder();
        public static readonly string[] EdgeColors = new[] { "red", "green", "blue", "orange", "purple", "brown", "cyan", "magenta" };

        public static string Mermaid(Ontology ontology)
        {
            var nouns = ontology.AllNouns.Select(pair => pair.Value).Cast<object>().ToArray();
            var verbs = ontology.AllVerbs.ToArray();
            var vocabulary = nouns.Concat(verbs);

            GraphCode.AppendLine("<div style=\"padding-top: 50px;\" class=\"mermaid\">");
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
                return $"{uid}[{name}]";
            }

            var edgeCounter = 0;
            foreach (var n in ontology.AllNouns.Values)
            foreach (var (from, to, label, style) in Edges(n))
            {
                var edgeDef = $"   {NodeReference(from)} -- {label} --> {NodeReference(to)}";
                //var edgeStyle = $"   linkStyle {edgeCounter++} stroke-width:2px,fill:none,stroke:{VerbColor(v)};";
                GraphCode.AppendLine(edgeDef);
                //GraphCode.AppendLine(edgeStyle);
            }

            foreach (var v in ontology.AllVerbs)
            foreach (var (from, to, label, style) in Edges(v))
            {
                var edgeDef = $"   {NodeReference(from)} -- {label} --> {NodeReference(to)}";
                //var edgeStyle = $"   linkStyle {edgeCounter++} stroke-width:2px,fill:none,stroke:{VerbColor(v)};";
                GraphCode.AppendLine(edgeDef);
                //GraphCode.AppendLine(edgeStyle);
            }

            GraphCode.AppendLine("</div>");

            return GraphCode.ToString();
        }
    }
}
