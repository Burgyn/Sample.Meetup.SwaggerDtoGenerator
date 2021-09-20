using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Sample.Meetup.SwaggerDtoGenerator
{
    [Generator]
    public class DtoGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            var files = context.AdditionalFiles.Where(at => at.Path.EndsWith(".json"));
            foreach (var file in files)
            {
                var options = context.AnalyzerConfigOptions.GetOptions(file);
                if (options.TryGetValue("build_metadata.additionalfiles.IsSwaggerDocs", out var isSwagger) &&
                        bool.TryParse(isSwagger, out var isSwaggerDocs) && isSwaggerDocs)
                {
                    var content = file.GetText(context.CancellationToken).ToString();
                    GenerateDtoRecords(context, content);
                }
            }
        }

        private static void GenerateDtoRecords(GeneratorExecutionContext context, string content)
        {
            var swagger = JsonDocument.Parse(content);
            var schemes = swagger.RootElement.GetProperty("components").GetProperty("schemas").EnumerateObject();
            foreach (var scheme in schemes)
            {
                GenerateDto(context, scheme);
            }
        }

        private static void GenerateDto(GeneratorExecutionContext context, JsonProperty scheme)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendFormat("namespace {0} {{", context.Compilation.AssemblyName);
            sb.AppendFormat("public record {0}{{", scheme.Name);
            GenerateProperties(scheme, sb);
            sb.AppendLine("}}");

            context.AddSource(scheme.Name, Format(sb.ToString()));
        }

        private static void GenerateProperties(JsonProperty scheme, StringBuilder sb)
        {
            foreach (var property in scheme.Value.GetProperty("properties").EnumerateObject())
            {
                string t = GetType(property);

                sb.AppendFormat("public {0} {1}{{get; init;}}", t, property.Name.ToTitleCase());
            }
        }

        private static string GetType(JsonProperty property)
        {
            JsonElement type;
            if (property.Value.ValueKind == JsonValueKind.String)
            {
                type = property.Value;
            }
            else if (!property.Value.TryGetProperty("type", out type))
            {
                type = property.Value.GetProperty("$ref");
            }

            string t = type.GetString();
            if (t == "array")
            {
                return $"IEnumerable<{GetType(property.Value.GetProperty("items").EnumerateObject().First())}>";
            }

            return t.Replace("#/components/schemas/", string.Empty) switch
            {
                "integer" => "int",
                "boolean" => "bool",
                string s => s
            };
        }

        private static string Format(string output)
        {
            var tree = CSharpSyntaxTree.ParseText(output);
            var root = (CSharpSyntaxNode)tree.GetRoot();
            output = root.NormalizeWhitespace(elasticTrivia: true).ToFullString();

            return output;
        }
    }
}
