using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using AutonomousAgents.Models;

namespace AutonomousAgents.Executors;

/// <summary>
/// Executor responsible for checking ingredient availability in inventory
/// </summary>
internal sealed class InventoryCheckExecutor : Executor<MealPlanResponse, InventoryResponse>
{
    private readonly AIAgent _inventoryCheckAgent;

    public InventoryCheckExecutor(AIAgent inventoryCheckAgent) : base("InventoryCheckExecutor")
    {
        _inventoryCheckAgent = inventoryCheckAgent;
    }

    public override async ValueTask<InventoryResponse> HandleAsync(
        MealPlanResponse input, 
        IWorkflowContext context, 
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting InventoryCheckExecutor...");

        var allIngredients = input.Meals
            .SelectMany(m => m.Ingredients)
            .Distinct()
            .ToArray();

        var prompt = $@"Given the following list of ingredients, 
        return a JSON object with two arrays: 'available' and 'missing'.
        Ingredients: {JsonSerializer.Serialize(allIngredients)}";

        var agentResponse = await _inventoryCheckAgent.RunAsync(prompt);

        var inventoryResponse = agentResponse.Deserialize<InventoryResponse>(JsonSerializerOptions.Web);

        Console.WriteLine($"Parsed inventory response: {inventoryResponse.Available.Length} available, {inventoryResponse.Missing.Length} missing.");

        return inventoryResponse;
    }
}
