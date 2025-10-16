using System.Threading.Tasks;

namespace Common.ModelClient
{
    public class LocalModelClient : IModelClient
    {
        public Task<string> GenerateTextAsync(string prompt)
        {
            // Dummy implementation for bootstrap. Replace with HTTP calls to Docker Model Runner or Azure Foundry.
            return Task.FromResult($"[LOCAL MODEL] Echo: {prompt}");
        }

        public Task<float[]> GenerateEmbeddingsAsync(string input)
        {
            // Return a fake embedding
            return Task.FromResult(new float[] { 0.1f, 0.2f, 0.3f });
        }
    }
}
