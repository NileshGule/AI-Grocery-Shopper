using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;


namespace Common.ModelClient
{
    /// <summary>
    /// Model client that supports using the Azure OpenAI SDK (recommended for Azure Foundry/OpenAI deployments)
    /// and falls back to simple HTTP POST calls for custom Foundry endpoints (developer/local runner).
    /// </summary>
    public class AzureFoundryModelClient : IModelClient
    {
        private HttpClient _http;
        private string _endpoint;
        private OpenAIClient _openAiClient;
        private string? _deploymentOrModelName;

        

        // Constructor that accepts an Azure OpenAI SDK client and a deployment/model name.
        // This is the recommended constructor when using Azure OpenAI or Azure Foundry SDKs.
        public AzureFoundryModelClient()
        {
            Console.WriteLine("Initializing AzureFoundryModelClient with OpenAIClient");

            var uri = new Uri(Environment.GetEnvironmentVariable("LLM_ENDPOINT") ?? "http://localhost:51842");
            Console.WriteLine("LLM Endpoint: " + uri.ToString());

            // var aliasOrModelId = Environment.GetEnvironmentVariable("LLM_MODEL_ID") ?? "deepseek-r1-distill-qwen-7b-generic-gpu:3";
            var aliasOrModelId = Environment.GetEnvironmentVariable("LLM_MODEL_ID") ?? "deepseek-r1-7b";
            Console.WriteLine("LLM Model/Deployment: " + aliasOrModelId);

            _deploymentOrModelName = aliasOrModelId;
            
        }

        public async Task<string> GenerateTextAsync(string systemMessage, string prompt)
        {
            Console.WriteLine("Initializing AzureFoundryModelClient with OpenAIClient");

            var endpoint = "https://ai-foundry-ai-hub.openai.azure.com/";
var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_OPENAI_APIKEY");
var model = "gpt-4.1-mini";

            AIAgent agent = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureCliCredential())
        .GetChatClient(model)
        .CreateAIAgent(instructions: systemMessage, name: "Agent");

        var agentResponse = await agent.RunAsync(prompt);
        Console.WriteLine(agentResponse.Text);
        return agentResponse.Text;
            
        }

        public async Task<float[]> GenerateEmbeddingsAsync(string input)
        {
            // Final fallback: small deterministic embedding
            return new float[] { 0.0f };
        }
    }
}
