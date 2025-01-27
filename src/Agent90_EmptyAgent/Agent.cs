using System.Text.Json;
using AgentWorkshop.Core;
using OpenAI.Chat;

public class Agent
{
    private static readonly string _prompt = """
        You are an AI agent.
        """;
    private readonly ChatClient _chatClient;
    private readonly ChatCompletionOptions _options;
    private readonly ToolMapper _toolMapper;
    public Agent(
        ChatClient chatClient
    )
    {
        _chatClient = chatClient;
        _options = new()
        {
            ResponseFormat = SchemaBuilder.CreateJsonSchemaFormat<AgentResponse>(true),
            ToolChoice = ChatToolChoice.CreateAutoChoice(),
            AllowParallelToolCalls = true,
            MaxOutputTokenCount = 4096,
            Temperature = 0.5f
        };

        _toolMapper = new ToolMapper();
        /*
        _options.Tools.Add(
            _toolMapper.CreateTool<..., ...>("...", ...)
        );
        */
    }
    private record GetWeatherToolRequest(string Location);

    public async Task<AgentResponse> InvokeAsync(AgentRequest req)
    {
        List<ChatMessage> messages =
        [
            new SystemChatMessage(_prompt),
            new UserChatMessage($"Input: {req}")
        ];

        while (true)
        {
            var resp = await _chatClient.CompleteChatAsync(messages, _options);
            messages.Add(ChatMessage.CreateAssistantMessage(resp));

            if (resp.Value.ToolCalls.Count > 0)
            {
                foreach (var toolCall in resp.Value.ToolCalls)
                {
                    var result = await _toolMapper.InvokeToolAsync(toolCall.FunctionName, toolCall.FunctionArguments.ToString());
                    messages.Add(ChatMessage.CreateToolMessage(toolCall.Id, result));
                }
                continue;
            }

            var outputJson = resp.Value.Content.First().Text;
            return JsonSerializer.Deserialize<AgentResponse>(outputJson)
                ?? throw new Exception("Output was null");
        }
    }
}
public record AgentRequest();
public record AgentResponse();