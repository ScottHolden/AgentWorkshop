using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using OpenAI.Chat;

namespace AgentWorkshop.Core;

public static class SchemaBuilder
{
    public static ChatResponseFormat CreateJsonSchemaFormat<T>(bool strict)
        => ChatResponseFormat.CreateJsonSchemaFormat(
            nameof(T),
            BinaryData.FromString(GetJsonSchema<T>()),
            GetDescription<T>(),
            strict
        );
    public static string GetJsonSchema<T>()
    {
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(
            new JsonSerializerOptions(JsonSerializerOptions.Default)
            {
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
            },
            typeof(T),
            new JsonSchemaExporterOptions()
            {
                TreatNullObliviousAsNonNullable = true,
                TransformSchemaNode = TransformSchemaNode
            }
        ).ToString();

        // Helpful to debug what is happening in tool schema/structured output
        //Console.WriteLine(schema);

        return schema;
    }
    private static JsonNode TransformSchemaNode(JsonSchemaExporterContext context, JsonNode node)
    {
        var description = context.PropertyInfo?
            .AttributeProvider?
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .OfType<DescriptionAttribute>()
            .FirstOrDefault()?.Description;

        if (!string.IsNullOrEmpty(description))
        {
            node["description"] = description;
        }

        return node;
    }
    internal static string GetDescription<T>()
    {
        var descriptionAttribute = typeof(T).GetCustomAttributes(typeof(DescriptionAttribute), false)
                                            .FirstOrDefault() as DescriptionAttribute;
        return descriptionAttribute?.Description ?? nameof(T);
    }
}