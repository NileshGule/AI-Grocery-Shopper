using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using AutonomousAgents.Models;

namespace AutonomousAgents.Executors;

/// <summary>
/// Executor responsible for generating friendly shopping descriptions for products
/// </summary>
internal sealed class ShoppingExecutor : Executor<BudgetResponse, ShoppingResponse>
{
    private readonly AIAgent _shoppingAgent;

    public ShoppingExecutor(AIAgent shoppingAgent) : base("ShoppingExecutor")
    {
        _shoppingAgent = shoppingAgent;
    }

    public override async ValueTask<ShoppingResponse> HandleAsync(
        BudgetResponse input, 
        IWorkflowContext context, 
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting ShoppingExecutor...");

        var prompt = $@"Given the following list of items, return a JSON object mapping each item to a short friendly description.
        Items: {JsonSerializer.Serialize(input.Items)}";

        var agentResponse = await _shoppingAgent.RunAsync(prompt);

        Console.WriteLine($"LLM Response: {agentResponse.Text}");

        var shoppingResponse = agentResponse.Deserialize<ShoppingResponse>(JsonSerializerOptions.Web);

        Console.WriteLine($"Parsed shopping response: {shoppingResponse.CategorizedItems.Count} items.");

        return new ShoppingResponse(shoppingResponse.CategorizedItems);
    }
}
