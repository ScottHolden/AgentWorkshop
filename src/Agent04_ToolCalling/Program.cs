using System.ComponentModel;
using System.Text.Json;
using AgentWorkshop.Core;
using OpenAI.Chat;

// Set up our chat client, see Agent01 for a detailed explanation
var chatClient = Config.LoadFrom("../../config.json").GetChatClient();

// Build our prompt
var prompt = """
    You are an AI agent that fills weather requests using tools.
    Only provide weather data that is retrieved via a tool, do not make up weather data.
""";

var input = "Provide the weather for 3 suburbs within 15km of Melbourne, Australia";

// Set up our messages, tools, and output options
List<ChatMessage> messages =
[
    new SystemChatMessage(prompt),
    new UserChatMessage(input)
];
ChatCompletionOptions options = new()
{
    // Instruct AOAI that we REQUIRE the output be structured with a schema
    ResponseFormat = SchemaBuilder.CreateJsonSchemaFormat<Output>(true),
    // Allow the model to call tools
    ToolChoice = ChatToolChoice.CreateAutoChoice(),
    // Allow multiple tool calls at once
    AllowParallelToolCalls = true,
    MaxOutputTokenCount = 4096,
    Temperature = 0.5f
};
// Add our tool
options.Tools.Add(ChatTool.CreateFunctionTool(
    functionName: "GetWeather",
    functionDescription: "Given the name of a location returns the weather",
    functionParameters: BinaryData.FromString(SchemaBuilder.GetJsonSchema<WeatherToolInput>()),
    functionSchemaIsStrict: true
));

// Run our conversation loop!
// We need a loop as tool calls happen locally, just like the agent asking us a question
while (true)
{
    var resp = await chatClient.CompleteChatAsync(messages, options);
    messages.Add(ChatMessage.CreateAssistantMessage(resp));
    if (resp.Value.ToolCalls.Count > 0)
    {
        // Run the tool requests!
        foreach (var toolCall in resp.Value.ToolCalls)
        {
            if (toolCall.FunctionName == "GetWeather")
            {
                // Parse the provided function args
                var toolInput = toolCall.FunctionArguments.ToObjectFromJson<WeatherToolInput>();
                if (toolInput == null)
                {
                    Console.WriteLine("Tool call args for GetWeather was null");
                    continue;
                }

                // Run our tool
                Console.WriteLine("Running GetWeather for " + toolInput);
                var toolOutput = GetWeather(toolInput);
                Console.WriteLine("Result: " + toolOutput);

                // Add the response to our messages to pass back to model, in this case we are just serializing the output
                messages.Add(ChatMessage.CreateToolMessage(toolCall.Id, JsonSerializer.Serialize(toolOutput)));
            }
            else
            {
                Console.WriteLine("Unknown tool call " + toolCall.FunctionName);
            }
        }
    }
    else
    {
        // Parse our final output
        var outputJson = resp.Value.Content.First().Text;
        var output = JsonSerializer.Deserialize<Output>(outputJson)
            ?? throw new Exception("Output was null");

        Console.WriteLine("Final response: \n" + string.Join<OutputWeather>("\n", output.Weather));
        break;
    }
}

// Example functions
static WeatherToolOutput GetWeather(WeatherToolInput input)
    => new(Random.Shared.NextDouble() * 20 + 10);

// Tool types
[Description("Input to get weather for a location")]
record WeatherToolInput(
    [property: Description("Location to get the weather for")] string Location
);
record WeatherToolOutput(double DegreesC);

// Output types
[Description("Details about the weather")]
record Output(
    [property: Description("An array of weather at locations")] OutputWeather[] Weather
);
record OutputWeather(string Country, string State, string Suburb, float DegreesC);