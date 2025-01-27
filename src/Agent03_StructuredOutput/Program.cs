using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using AgentWorkshop.Core;
using OpenAI.Chat;

// Set up our chat client, see Agent01 for a detailed explanation
var chatClient = Config.LoadFrom("../../config.json").GetChatClient();

// Build our prompt
var prompt = """
    You are an AI agent that generates test data for a given structure.
""";

var input = "Create a test object for a user based in Australia";

// Set up our messages, output schema, and output options
List<ChatMessage> messages =
[
    new SystemChatMessage(prompt),
    new UserChatMessage(input)
];
var schema = JsonSchemaExporter.GetJsonSchemaAsNode(
    // Set "additionalProperties": false 
    new JsonSerializerOptions(JsonSerializerOptions.Default) {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    },
    typeof(TestData), 
    // Make sure root is not null
    new JsonSchemaExporterOptions() 
    { 
        TreatNullObliviousAsNonNullable = true
    }
);
ChatCompletionOptions options = new()
{
    // Instruct AOAI that we REQUIRE the output be structured with a schema
    ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
        jsonSchemaFormatName: "testData",
        jsonSchema: BinaryData.FromString(schema.ToString()),
        jsonSchemaFormatDescription: "Test object output",
        jsonSchemaIsStrict: true
    ),
    MaxOutputTokenCount = 4096,
    Temperature = 1.0f
};

// Run our agent
var resp = await chatClient.CompleteChatAsync(messages, options);

var outputJson = resp.Value.Content.First().Text;
var output = JsonSerializer.Deserialize<TestData>(outputJson);

Console.WriteLine(output);

// Structure for our output
record TestData(string Name, string City, string State, string Country, bool OnMailingList);