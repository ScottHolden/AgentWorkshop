using System.ComponentModel;
using System.Text.Json;
using AgentWorkshop.Core;
using OpenAI.Chat;

public sealed class Evaluator
{
    private static readonly string _prompt = """
        You are an AI agent that evaluates a provided response.
        Evaluate the following rules:
        - Each suburb name should be a single word
        - The response should contain at least 3 suburbs
        - The response should contain at most 15 suburbs
        - The response should not contain the word 'Melbourne'

        If the response meets the criteria, return IsCorrect = true,
        otherwise return IsCorrect = false with a reason why and a recommended fix.
        """;
    private readonly ChatClient _chatClient;
    private readonly ChatCompletionOptions _options;
    public Evaluator(ChatClient chatClient)
    {
        _chatClient = chatClient;
        _options = new()
        {
            ResponseFormat = SchemaBuilder.CreateJsonSchemaFormat<EvaluatorResponse>(true),
            MaxOutputTokenCount = 4096,
            Temperature = 0.5f
        };
    }

    public async Task<EvaluatorResponse> EvaluateAsync(AgentResponse resp)
    {
        ChatMessage[] messages = [
            ChatMessage.CreateSystemMessage(_prompt),
            ChatMessage.CreateUserMessage(JsonSerializer.Serialize(resp))
        ];
        var response = await _chatClient.CompleteChatAsync(messages, _options);
        var outputJson = response.Value.Content.First().Text;
        Console.WriteLine("Evaluator output: " + outputJson);

        return JsonSerializer.Deserialize<EvaluatorResponse>(outputJson)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }
}
public record EvaluatorResponse(
    [property: Description("Set to true if the provided input contains no errors")] 
    bool IsCorrect, 
    [property: Description("If IsCorrect is false, this should contain the reason why")]
    string Errors
);