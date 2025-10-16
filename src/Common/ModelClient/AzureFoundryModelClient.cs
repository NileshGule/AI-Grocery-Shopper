using System;
using System.ClientModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Optional SDK namespaces - include the Azure.AI.OpenAI package when ready
using Microsoft.AI.Foundry.Local;
using OpenAI;
using OpenAI.Chat;


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

        // Constructor for HTTP-based Foundry endpoints (keeps existing behavior)
        public AzureFoundryModelClient(HttpClient? http = null, string? endpoint = null, string? apiKey = null)
        {
            _http = http ?? new HttpClient();
            _endpoint = endpoint ?? string.Empty;
            if (!string.IsNullOrEmpty(apiKey))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
        }

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

        public async Task<string> GenerateTextAsync(string prompt)
        {
            Console.WriteLine("Initializing AzureFoundryModelClient with OpenAIClient");

            var uri = new Uri(Environment.GetEnvironmentVariable("LLM_ENDPOINT") ?? "http://localhost:56030");
            Console.WriteLine("LLM Endpoint: " + uri.ToString());

            // var aliasOrModelId = Environment.GetEnvironmentVariable("LLM_MODEL_ID") ?? "deepseek-r1-distill-qwen-7b-generic-gpu:3";
            // var aliasOrModelId = Environment.GetEnvironmentVariable("LLM_MODEL_ID") ?? "deepseek-r1-7b";
            var aliasOrModelId = Environment.GetEnvironmentVariable("LLM_MODEL_ID") ?? "Phi-3.5-mini-instruct-generic-gpu:1";
            
            Console.WriteLine("LLM Model/Deployment: " + aliasOrModelId);

            _deploymentOrModelName = aliasOrModelId;

            Console.WriteLine("GenerateTextAsync called with prompt:");
            Console.WriteLine(prompt);

            // _deploymentOrModelName = "deepseek-r1-7b";
            Console.WriteLine("Deployment/Model: " + _deploymentOrModelName);
            
            // Foundry Local Initializations
            var manager = await FoundryLocalManager.StartModelAsync(_deploymentOrModelName);

            if (manager == null)
                throw new ArgumentNullException("Trouble initializing model");

            var localEndpoint = manager.Endpoint;
            var localApiKey = manager.ApiKey;

            Console.WriteLine($"Foundry Local Manager initialized. Endpoint: {localEndpoint}, API Key: {localApiKey}");


            var key = new ApiKeyCredential(manager.ApiKey);

            _openAiClient = new OpenAIClient(key, new OpenAIClientOptions
            {
                Endpoint = manager.Endpoint
            });
            
            // If SDK client available, use ChatCompletions (chat-style) via OpenAIClient
            if (_openAiClient is not null && !string.IsNullOrEmpty(_deploymentOrModelName))
            {
                try
                {
                    var model = await manager.GetModelInfoAsync(_deploymentOrModelName);
                    var chatClient = _openAiClient.GetChatClient(model?.ModelId);

                    var systemMessage = "You are a helpful meal planning assistant. Respond with a list of grocery items needed for the meal plan. Limit the response to about 2000 words";

                    List<ChatMessage> messages =
                    [
                        // System messages represent instructions or other guidance about how the assistant should behave
                        new SystemChatMessage($"{systemMessage }"),

                        // User messages represent user input, whether historical or the most recent input
                        new UserChatMessage(prompt)
                    ];

                    var chatCompletionsOptions = new ChatCompletionOptions
                    {
                        MaxOutputTokenCount = 1024 * 3, // Adjust as needed
                    };

                    // Prepare to collect streamed chunks so we can write them to a file later
                    var outputBuilder = new StringBuilder();

                    AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates = chatClient.CompleteChatStreamingAsync(messages, chatCompletionsOptions);

                    await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                    {
                        if (completionUpdate.ContentUpdate.Count > 0)
                        {
                            var chunk = completionUpdate.ContentUpdate[0].Text;
                            // Print to console (use plain Console to avoid markup parsing issues with arbitrary text)
                            Console.Write(chunk);
                            // Append to builder
                            outputBuilder.Append(chunk);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Swallow and fall back to HTTP option if configured
                    Console.Error.WriteLine($"OpenAI SDK call failed: {ex.Message}");
                }
            }
        }

        public async Task<float[]> GenerateEmbeddingsAsync(string input)
        {
            // Final fallback: small deterministic embedding
            return new float[] { 0.0f };
        }
    }
}
