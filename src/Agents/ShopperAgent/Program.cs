using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common.ModelClient;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
// Ensure the app listens on all network interfaces inside the container
builder.WebHost.UseUrls("http://0.0.0.0:80");

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
    var systemMsg = "You are an assistant that, given a JSON array of product names, returns ONLY a JSON object mapping each product to a short friendly description (one sentence). The description should be lavish Michaline style names. The output must be a single valid JSON object with product names as keys and descriptions as values. No extra text. Use the exact product strings as keys.";

    // Include a short example to encourage stable keys
    var example = new { example_items = new[] { "spaghetti", "tomato" }, example_output = new { spaghetti = "Long thin pasta, typically made from wheat.", tomato = "A versatile red fruit used in salads and sauces." } };

    var promptPayload = new
    {
        items = items
    };

    var llmResp = await client.GenerateTextAsync(systemMsg, System.Text.Json.JsonSerializer.Serialize(promptPayload));

    // Parse LLM response defensively into a case-insensitive dictionary keyed by lower-case item name
    var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
                var key = prop.Name.Trim();
                var val = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() ?? string.Empty : prop.Value.ToString();
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val))
                {
                    descriptions[key.ToLowerInvariant()] = val.Trim();
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("ShopperAgent: failed to parse LLM descriptions: " + ex.Message);
    }

    // If any item is missing a description, call the LLM per-item as a fallback to ensure every item has a description
    foreach (var it in items)
    {
        var key = it?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(key)) continue;
        var lookup = key.ToLowerInvariant();
        if (!descriptions.ContainsKey(lookup) || string.IsNullOrWhiteSpace(descriptions[lookup]))
        {
            // Per-item prompt: ask for a single-sentence description only
            var perItemSystem = "You are a an assistant that returns a single sentence description for the provided product name. Only output the description sentence in Michelin style, no JSON or extra text.";
            var perItemPrompt = key;
            try
            {
                var singleResp = await client.GenerateTextAsync(perItemSystem, perItemPrompt);
                // trim quotes/braces and whitespace
                var cleaned = singleResp.Trim();
                if (cleaned.StartsWith("\"") && cleaned.EndsWith("\"")) cleaned = cleaned[1..^1].Trim();
                // If LLM returned JSON like { "item": "desc" }, attempt to extract string content
                if (cleaned.StartsWith("{") && cleaned.EndsWith("}"))
                {
                    try
                    {
                        using var d = JsonDocument.Parse(cleaned);
                        var r = d.RootElement;
                        if (r.ValueKind == JsonValueKind.Object && r.EnumerateObject().Any())
                        {
                            var first = r.EnumerateObject().First();
                            cleaned = first.Value.GetString() ?? cleaned;
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrWhiteSpace(cleaned)) descriptions[lookup] = cleaned.Trim();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ShopperAgent: per-item LLM call failed for '{key}': {ex.Message}");
                descriptions[lookup] = "No description available";
            }
        }
    }

    // Build final JSON structure, map descriptions case-insensitively back to original item casing
    var final = new
    {
        categories = categorized.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(i => new
        {
            name = i,
            description = descriptions.TryGetValue(i.ToLowerInvariant(), out var d) ? d : ""
        }))
    };

    Console.WriteLine("ShopperAgent: prepared shopping list with categories and descriptions.");
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(final));

    return Results.Ok(final);
});

app.Run();

public record ShoppingRequest(string[] Items);
