public record Config(
    Uri AzureOpenAIEndpoint,
    string AzureOpenAIDeployment,
    string? AzureOpenAIKey
);