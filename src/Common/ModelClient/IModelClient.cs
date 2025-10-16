using System.Threading.Tasks;

namespace Common.ModelClient
{
    public interface IModelClient
    {
        Task<string> GenerateTextAsync(string systemMessage, string prompt);
        Task<float[]> GenerateEmbeddingsAsync(string input);
    }
}
