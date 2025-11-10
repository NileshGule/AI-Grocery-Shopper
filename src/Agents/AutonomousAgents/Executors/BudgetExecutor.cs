using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using AutonomousAgents.Models;
using AutonomousAgents.Services;

namespace AutonomousAgents.Executors;

/// <summary>
/// Executor responsible for managing budget constraints and optimizing shopping lists
/// </summary>
internal sealed class BudgetExecutor : Executor<InventoryResponse, BudgetResponse>
{
    private readonly AIAgent _budgetAgent;
    private const float DefaultBudget = 100.0f;

    public BudgetExecutor(AIAgent budgetAgent) : base("BudgetExecutor")
    {
        _budgetAgent = budgetAgent;
    }

    public override async ValueTask<BudgetResponse> HandleAsync(
        InventoryResponse input, 
        IWorkflowContext context, 
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting BudgetExecutor...");

        var priceFile = Path.Combine(AppContext.BaseDirectory, "prices.json");
        
        // Fallback paths if not found in BaseDirectory
        if (!File.Exists(priceFile))
        {
            priceFile = Path.Combine(Directory.GetCurrentDirectory(), "prices.json");
        }
        
        if (!File.Exists(priceFile))
        {
            // Try looking in the project root
            var projectRoot = Directory.GetCurrentDirectory();
            while (projectRoot != null && !File.Exists(Path.Combine(projectRoot, "prices.json")))
            {
                var parent = Directory.GetParent(projectRoot);
                if (parent == null) break;
                projectRoot = parent.FullName;
            }
            if (projectRoot != null)
            {
                priceFile = Path.Combine(projectRoot, "prices.json");
            }
        }
        
        Console.WriteLine($"Loading prices from: {priceFile}");
        
        if (!File.Exists(priceFile))
        {
            Console.WriteLine($"ERROR: prices.json not found at {priceFile}");
            throw new FileNotFoundException($"prices.json not found. Searched in: {priceFile}", "prices.json");
        }
        
        var prices = BudgetService.LoadPricesFromFile(priceFile);

        Console.WriteLine($"Loaded {prices.Count} price entries from {priceFile}");

        var budgetService = new BudgetService(prices);
        Console.WriteLine($"BudgetService Prices count: {budgetService.Prices.Count}");

        Console.WriteLine($"Received budget request: Items=[{string.Join(", ", input.Available)}], Budget={DefaultBudget}");

        // Calculate total cost
        float total = budgetService.CalculateTotal(input.Available);

        Console.WriteLine($"Calculated total cost: {total}");

        // If within budget, return as-is
        if (total <= DefaultBudget)
        {
            return new BudgetResponse(input.Available, total, "Within budget - no changes needed");
        }

        // Otherwise, ask LLM to adjust
        var basePrompt = $@"Original items: [{string.Join(", ", input.Available)}]
Budget: {DefaultBudget}
CurrentTotal: {total}
Prices: {JsonSerializer.Serialize(budgetService.Prices)}

Task: Suggest an adjusted shopping list that meets or is closest to the Budget.
Requirements:
- Return ONLY the JSON object: {{ ""items"": [ string ], ""totalCost"": number, ""note"": string }}.
- items: array of product names.
- totalCost: numeric sum computed using the provided Prices (use default price 2.0 for unknown items).
- note: 1-2 sentence explanation.
- Prefer substitutions over removals. Make minimal changes needed to meet budget.";

        var agentResponse = await _budgetAgent.RunAsync(basePrompt);

        Console.WriteLine($"LLM Response: {agentResponse.Text}");

        var budgetResponse = agentResponse.Deserialize<BudgetResponse>(JsonSerializerOptions.Web);

        Console.WriteLine($"Parsed budget response: {budgetResponse.TotalCost} for {budgetResponse.Items.Length} items.");

        return new BudgetResponse(budgetResponse.Items, budgetResponse.TotalCost, budgetResponse.Note);
    }
}
