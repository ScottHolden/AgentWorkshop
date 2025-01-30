using System.ClientModel;
using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;

namespace AgentWorkshop.Core;

public record Config(
    Uri AzureOpenAIEndpoint,
    string AzureOpenAIDeployment,
    string? AzureOpenAIKey
)
{
    private static readonly JsonSerializerOptions serializerOptions 
        = new(JsonSerializerDefaults.Web);
    public static Config LoadFrom(string path)
        => JsonSerializer.Deserialize<Config>(File.ReadAllText(path), serializerOptions)
            ?? throw new Exception("Unable to load config");
    public ChatClient GetChatClient()
    {
        var azureOpenAIClient = string.IsNullOrEmpty(AzureOpenAIKey) ?
            new AzureOpenAIClient(AzureOpenAIEndpoint, new DefaultAzureCredential()) :
            new AzureOpenAIClient(AzureOpenAIEndpoint, new ApiKeyCredential(AzureOpenAIKey));

        return azureOpenAIClient.GetChatClient(AzureOpenAIDeployment);
    }
}
