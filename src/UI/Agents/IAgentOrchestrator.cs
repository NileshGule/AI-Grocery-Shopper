using System.Threading.Tasks;
using UI.Models;

namespace UI.Agents
{
    public interface IAgentOrchestrator
    {
        Task<OrchestrationResult> RunAsync(UserInput input);
    }
}
