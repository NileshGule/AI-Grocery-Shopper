using System;
using System.IO;
using System.Collections.Generic;
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

        // var prompt = $@"Given the following list of ingredients, 
        // return a JSON object with two arrays: 'available' and 'missing'.
        // Ingredients: {JsonSerializer.Serialize(allIngredients)}";

        // var agentResponse = await _inventoryCheckAgent.RunAsync(prompt);

        // var inventoryResponse = agentResponse.Deserialize<InventoryResponse>(JsonSerializerOptions.Web);

        // Load local inventory JSON file
        var inventoryPath = Path.Combine(AppContext.BaseDirectory, "inventory.json");
        
        // Fallback paths if not found in BaseDirectory
        if (!File.Exists(inventoryPath))
        {
            inventoryPath = Path.Combine(Directory.GetCurrentDirectory(), "inventory.json");
        }
        
        if (!File.Exists(inventoryPath))
        {
            // Try looking in the project root
            var projectRoot = Directory.GetCurrentDirectory();
            while (projectRoot != null && !File.Exists(Path.Combine(projectRoot, "inventory.json")))
            {
                var parent = Directory.GetParent(projectRoot);
                if (parent == null) break;
                projectRoot = parent.FullName;
            }
            if (projectRoot != null)
            {
                inventoryPath = Path.Combine(projectRoot, "inventory.json");
            }
        }

        Console.WriteLine($"Loading inventory from: {inventoryPath}");
        
        if (!File.Exists(inventoryPath))
        {
            Console.WriteLine($"ERROR: inventory.json not found at {inventoryPath}");
            throw new FileNotFoundException($"inventory.json not found. Searched in: {inventoryPath}", "inventory.json");
        }

        var invJson = await File.ReadAllTextAsync(inventoryPath, cancellationToken);

        // Use case-insensitive property matching so JSON keys like "items" map to C# "Items"
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        InventoryFile? inventory = JsonSerializer.Deserialize<InventoryFile>(invJson, options);
        
        if (inventory == null || inventory.Items == null)
        {
            Console.WriteLine("ERROR: Failed to deserialize inventory.json or Items is null");
            throw new InvalidOperationException("Failed to deserialize inventory.json");
        }
        
        Console.WriteLine($"Successfully loaded {inventory.Items.Length} items from inventory");
        
        var missing = new List<string>();
        var available = new List<string>();

        foreach (var item in input.Meals.SelectMany(m => m.Ingredients).Distinct())
        {
            if (inventory.Items.Any(i => string.Equals(i.Name, item, StringComparison.OrdinalIgnoreCase)))
            {
                available.Add(item);
            }
            else
            {
                missing.Add(item);
            }
        }

        var inventoryResponse = new InventoryResponse(available.ToArray(), missing.ToArray());

        Console.WriteLine($"Parsed inventory response: {inventoryResponse.Available.Length} available, {inventoryResponse.Missing.Length} missing.");

        return inventoryResponse;
    }
}
