using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

// Config
builder.Services.AddTransient(context
    => context.GetRequiredService<IConfiguration>().Get<Config>()
        ?? throw new Exception("Unable to bind config")
);

// Dependencies
builder.Services.AddSingleton<ChatClient>(services =>
{
    var config = services.GetRequiredService<Config>();
    var aoaiClient = string.IsNullOrEmpty(config.AzureOpenAIKey) ?
        new AzureOpenAIClient(config.AzureOpenAIEndpoint, new DefaultAzureCredential()) :
        new AzureOpenAIClient(config.AzureOpenAIEndpoint, new ApiKeyCredential(config.AzureOpenAIKey));
    return aoaiClient.GetChatClient(config.AzureOpenAIDeployment);
});

// Services
builder.Services.AddSingleton<Agent>();
builder.Services.AddSingleton<WeatherService>();

var app = builder.Build();

app.MapPost("api/agent/invoke", async (
    [FromBody] AgentRequest req,
    [FromServices] Agent agent
) => await agent.InvokeAsync(req));


// Warm up aoai in the background
_ = Task.Run(async () 
    => await app.Services.GetRequiredService<ChatClient>()
        .CompleteChatAsync([
            ChatMessage.CreateSystemMessage("Warmup"),
            ChatMessage.CreateUserMessage("Warmup")
        ], new ChatCompletionOptions{
            MaxOutputTokenCount = 10
        }).ContinueWith(task =>
        {
            if (task.IsFaulted)
                app.Services.GetService<ILogger<Program>>()?.LogError($"AOAI Warm Failed: {task.Exception}");
            else
                app.Services.GetService<ILogger<Program>>()?.LogInformation("AOAI Warm Succeeded");
        }
    ));

app.Run();