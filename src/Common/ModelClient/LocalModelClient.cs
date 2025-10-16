using System.Threading.Tasks;
using System.ClientModel;
using System.Text;
using System.Text.Json;
using OpenAI;
using OpenAI.Chat;

namespace Common.ModelClient
{
    public class LocalModelClient : IModelClient
    {
        public async Task<string> GenerateTextAsync(string prompt)
        {
            // Dummy implementation for bootstrap. Replace with HTTP calls to Docker Model Runner or Azure Foundry.
            // return Task.FromResult($"[LOCAL MODEL] Echo: {prompt}");

            Console.WriteLine("Initializing Docker Model with OpenAIClient");

            var uri = new Uri(Environment.GetEnvironmentVariable("LLM_ENDPOINT") ?? "http://127.0.0.1:12434/engines/llama.cpp/v1");
            Console.WriteLine("LLM Endpoint: " + uri.ToString());

            // var aliasOrModelId = Environment.GetEnvironmentVariable("LLM_MODEL_ID") ?? "deepseek-r1-distill-qwen-7b-generic-gpu:3";
            // var aliasOrModelId = Environment.GetEnvironmentVariable("LLM_MODEL_ID") ?? "deepseek-r1-7b";
            var aliasOrModelId = Environment.GetEnvironmentVariable("LLM_MODEL_ID") ?? "ai/smollm2";

            Console.WriteLine("LLM Model/Deployment: " + aliasOrModelId);

            var _deploymentOrModelName = aliasOrModelId;

            Console.WriteLine("GenerateTextAsync called with prompt:");
            Console.WriteLine(prompt);

            // _deploymentOrModelName = "deepseek-r1-7b";
            Console.WriteLine("Deployment/Model: " + _deploymentOrModelName);

            // Foundry Local Initializations

            var key = new ApiKeyCredential("OPEN_API_KEY");

            var _openAiClient = new OpenAIClient(key, new OpenAIClientOptions
            {
                Endpoint = uri
            });

            // If SDK client available, use ChatCompletions (chat-style) via OpenAIClient
            if (_openAiClient is not null && !string.IsNullOrEmpty(_deploymentOrModelName))
            {
                try
                {
                    var chatClient = _openAiClient.GetChatClient(aliasOrModelId);

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
                    return outputBuilder.ToString();
                }
                catch (Exception ex)
                {
                    // Swallow and fall back to HTTP option if configured
                    Console.Error.WriteLine($"OpenAI SDK call failed: {ex.Message}");
                }
            }
            return "[ERROR] Unable to generate text via OpenAI SDK.";
        }

        public Task<float[]> GenerateEmbeddingsAsync(string input)
        {
            // Return a fake embedding
            return Task.FromResult(new float[] { 0.1f, 0.2f, 0.3f });
        }
    }
}
