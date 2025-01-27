using System.ComponentModel;
using System.Text.Json;
using AgentWorkshop.Core;
using OpenAI.Chat;

public sealed class Agent
{
    private static readonly string _prompt = """
        You are an AI agent that fills weather requests using tools.
        You will be provided with a location, you must find the weather for a count of suburbs within distance of the location.
        Only provide data that is retrieved via a tool, do not make up data.
        """;
    private readonly ChatClient _chatClient;
    private readonly Evaluator _evaluator;
    private readonly ChatCompletionOptions _options;
    private readonly ToolMapper _toolMapper;

    public Agent(ChatClient chatClient, Evaluator evaluator)
    {
        _chatClient = chatClient;
        _evaluator = evaluator;

        _options = new()
        {
            ResponseFormat = SchemaBuilder.CreateJsonSchemaFormat<AgentResponse>(true),
            ToolChoice = ChatToolChoice.CreateAutoChoice(),
            AllowParallelToolCalls = true,
            MaxOutputTokenCount = 4096,
            Temperature = 0.5f
        };

        _toolMapper = new ToolMapper();
        _options.Tools.Add(
            _toolMapper.CreateTool<GetWeatherToolRequest, double>(
                "GetWeather",
                x => Random.Shared.NextDouble() * 20 + 10
            )
        );
    }
    private record GetWeatherToolRequest(string Location);

    public async Task<AgentResponse> InvokeAsync(AgentRequest req)
    {
        List<ChatMessage> messages =
        [
            new SystemChatMessage(_prompt),
            new UserChatMessage($"Provide the weather for {req.Count} suburbs within {req.Distance}km of {req.Location}")
        ];

        while (true)
        {
            var resp = await _chatClient.CompleteChatAsync(messages, _options);
            messages.Add(ChatMessage.CreateAssistantMessage(resp));

            // Process tool calls if they exist
            if (resp.Value.ToolCalls.Count > 0)
            {
                foreach (var toolCall in resp.Value.ToolCalls)
                {
                    Console.WriteLine($"Tool call: {toolCall.FunctionName}");
                    var result = await _toolMapper.InvokeToolAsync(toolCall.FunctionName, toolCall.FunctionArguments.ToString());
                    messages.Add(ChatMessage.CreateToolMessage(toolCall.Id, result));
                }
                continue;
            }

            // Parse the output
            var outputJson = resp.Value.Content.First().Text;
            Console.WriteLine("Agent output: " + outputJson);
            var output = JsonSerializer.Deserialize<AgentResponse>(outputJson)
                    ?? throw new Exception("Output was null");

            // Evaluate the response
            var evaluatorResponse = await _evaluator.EvaluateAsync(output);
            Console.WriteLine($"Evaluation result: {evaluatorResponse}");

            if (!evaluatorResponse.IsCorrect)
            {
                // Feed the error back to the agent
                messages.Add(ChatMessage.CreateAssistantMessage(
                    "The following issues have been found with the previous output, please fix: "
                        +evaluatorResponse.Errors));
                continue;
            }

            return output;
        }
    }
}

public record AgentRequest(int Count, double Distance, string Location);
public record AgentResponse(
    [property: Description("An array of weather at locations")]
    OutputWeather[] Weather
);
public record OutputWeather(string Country, string State, string Suburb, float DegreesC);