using System.ComponentModel;
using System.Text.Json;
using AgentWorkshop.Core;
using OpenAI.Chat;

var chatClient = Config.LoadFrom("../../config.json").GetChatClient();

// Build our prompt
var prompt = """
    You are an AI agent that fills weather requests using tools.
    Only provide data that is retrieved via a tool, do not make up data.
    You have been provided a tool to escalate questions if something is unclear
""";

var input = "Provide the weather for 3 suburbs within 15km of my home town";

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

ToolMapper toolMapper = new();
options.Tools.Add(toolMapper.CreateTool<GetWeatherRequest, string>("GetWeather", GetWeatherRequest.Invoke));

options.Tools.Add(ChatTool.CreateFunctionTool(
    "AskOperator", 
    "Ask a question if something is unclear or missing",
    // Tool schema always needs to be an object
    BinaryData.FromString(SchemaBuilder.GetJsonSchema<AskOperator>()),
    true
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
            if (toolCall.FunctionName == "AskOperator")
            {
                var question = toolCall.FunctionArguments.ToObjectFromJson<AskOperator>();
                Console.WriteLine("Question: " + question?.Question);

                // At this point we could halt operations, save messages somewhere, and pick up later
                var answer = Console.ReadLine();
                messages.Add(ChatMessage.CreateToolMessage(toolCall.Id, answer));
            }
            else
            {
                // Use our mapper to invoke any tools we might need
                Console.WriteLine("Invoking tool " + toolCall.FunctionName);
                var result = await toolMapper.InvokeToolAsync(toolCall.FunctionName, toolCall.FunctionArguments.ToString());
                messages.Add(ChatMessage.CreateToolMessage(toolCall.Id, result));
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



// Tool types
public record AskOperator(string Question);
public record GetWeatherRequest(string Location)
{
    public static string Invoke(GetWeatherRequest request)
        => $"{request.Location}: {Random.Shared.NextDouble() * 20 + 10} C";
}

// Output types
[Description("Details about the weather")]
record Output(
    [property: Description("An array of weather at locations")] OutputWeather[] Weather
);
record OutputWeather(string Country, string State, string Suburb, float DegreesC);