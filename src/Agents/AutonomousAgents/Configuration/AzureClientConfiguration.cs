using System;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;

namespace AutonomousAgents.Configuration;

/// <summary>
/// Configuration helper for Azure OpenAI client setup
/// </summary>
public static class AzureClientConfiguration
{
    private const string DefaultEndpoint = "https://ai-foundry-ai-hub.openai.azure.com/";
    private const string DefaultModel = "gpt-4.1-mini";

    /// <summary>
    /// Creates and configures an Azure OpenAI chat client
    /// </summary>
    /// <param name="endpoint">Azure OpenAI endpoint URL</param>
    /// <param name="model">Model deployment name</param>
    /// <returns>Configured IChatClient instance</returns>
    public static IChatClient CreateChatClient(
        string? endpoint = null, 
        string? model = null)
    {
        var effectiveEndpoint = endpoint ?? DefaultEndpoint;
        var effectiveModel = model ?? DefaultModel;

        return new AzureOpenAIClient(
            new Uri(effectiveEndpoint),
            new AzureCliCredential())
            .GetChatClient(effectiveModel)
            .AsIChatClient();
    }
}
