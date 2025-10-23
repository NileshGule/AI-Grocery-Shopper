using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common.ModelClient;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IModelClient, Common.ModelClient.LocalModelClient>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("ShopperAgent OK"));

app.MapPost("/prepare-shopping-list", async (ShoppingRequest req, IModelClient client) =>
{
    Console.WriteLine($"Received shopping prepare request: BudgetItemsCount={req.Items?.Length ?? 0}");

    // Expect req.Items to be the adjusted list from BudgetAgent
    var items = req.Items ?? new string[0];

    // Group items by simple category heuristics (basic map)
    var categoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "egg", "Dairy & Eggs" },
        { "milk", "Dairy & Eggs" },
        { "cheese", "Dairy & Eggs" },
        { "yogurt", "Dairy & Eggs" },
        { "bread", "Bakery" },
        { "rice", "Grains & Pasta" },
        { "pasta", "Grains & Pasta" },
        { "beans", "Canned & Dry Goods" },
        { "tomato", "Produce" },
        { "lettuce", "Produce" },
        { "onion", "Produce" },
        { "chicken", "Meat & Seafood" },
        { "beef", "Meat & Seafood" },
        { "fish", "Meat & Seafood" },
        { "apple", "Produce" },
        { "banana", "Produce" },
    };

    var categorized = new Dictionary<string, List<string>>();
    foreach (var it in items)
    {
        var key = it?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(key)) continue;
        var canonical = key.ToLowerInvariant();
        var cat = categoryMap.ContainsKey(canonical) ? categoryMap[canonical] : "Other";
        if (!categorized.ContainsKey(cat)) categorized[cat] = new List<string>();
        categorized[cat].Add(key);
    }

    // Ask LLM to provide friendly descriptions for each item
    var systemMsg = "You are an assistant that, given a JSON array of product names, returns ONLY a JSON object mapping each product to a short friendly description (one sentence). The output must be a single valid JSON object with product names as keys and descriptions as values. No extra text.";

    var prompt = JsonSerializer.Serialize(items);
    var llmResp = await client.GenerateTextAsync(systemMsg, prompt);

    // Parse LLM response defensively
    var descriptions = new Dictionary<string, string>();
    try
    {
        var start = llmResp.IndexOf('{');
        var end = llmResp.LastIndexOf('}');
        var json = llmResp;
        if (start >= 0 && end > start) json = llmResp[start..(end + 1)];
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
            {
                descriptions[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("ShopperAgent: failed to parse LLM descriptions: " + ex.Message);
    }

    // Build final JSON structure
    var final = new
    {
        categories = categorized.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(i => new {
            name = i,
            description = descriptions.ContainsKey(i) ? descriptions[i] : (descriptions.ContainsKey(i.ToLowerInvariant()) ? descriptions[i.ToLowerInvariant()] : "")
        }))
    };

    return Results.Ok(final);
});

app.Run();

public record ShoppingRequest(string[] Items);
