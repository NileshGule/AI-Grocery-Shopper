using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common.ModelClient;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using BudgetAgent;

// Simple in-memory price list (dummy prices)
// var priceList = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
// {
//     { "egg", 0.2f },
//     { "milk", 1.5f },
//     { "bread", 2.0f },
//     { "chicken", 5.0f },
//     { "rice", 1.0f },
//     { "beans", 1.2f },
//     { "tomato", 0.5f },
//     { "onion", 0.4f },
//     { "cheese", 3.0f },
//     { "lettuce", 1.0f }
// };

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:80");

// Load prices from prices.json in app base dir
var priceFile = Path.Combine(AppContext.BaseDirectory, "prices.json");
var prices = BudgetService.LoadPricesFromFile(priceFile);

Console.WriteLine($"Loaded {prices.Count} price entries from {priceFile}");

var budgetService = new BudgetService(prices);

builder.Services.AddSingleton(budgetService);
builder.Services.AddSingleton<IModelClient, Common.ModelClient.LocalModelClient>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("BudgetAgent OK"));

app.MapPost("/check-budget", async (BudgetRequest req, IModelClient client, BudgetService svc) =>
{
    Console.WriteLine($"BudgetService Prices count: {svc.Prices.Count}");

    Console.WriteLine($"Received budget request: Items=[{string.Join(", ", req.Items)}], Budget={req.Budget}");

    // Calculate total cost
    float total = svc.CalculateTotal(req.Items);

    Console.WriteLine($"Calculated total cost: {total}");

    if (total <= req.Budget)
    {
        return Results.Ok(new BudgetResponse(req.Items, total, "Within budget - no changes needed"));
    }

    // Over budget - ask LLM for suggestions to reduce cost
    var systemMsg = "You are an assistant whose ONLY allowed output is a single valid JSON object matching this schema: { \"items\": [ string ], \"totalCost\": number, \"note\": string }. Do not output any text, explanation, markup, or extra keys. Always use double quotes.";

    var basePrompt = $@"Original items: [{string.Join(", ", req.Items)}]
Budget: {req.Budget}
CurrentTotal: {total}
Prices: {JsonSerializer.Serialize(svc.Prices)}

Task: Suggest an adjusted shopping list that meets or is closest to the Budget.
Requirements:
- Return ONLY the JSON object: {{ ""items"": [ string ], ""totalCost"": number, ""note"": string }}.
- items: array of product names.
- totalCost: numeric sum computed using the provided Prices (use default price 2.0 for unknown items).
- note: 1-2 sentence explanation.
- Prefer substitutions over removals. Make minimal changes needed to meet budget.";

    string lastRaw = string.Empty;
    List<string> adjustedItems = new List<string>();
    string adjustedNote = string.Empty;
    float adjustedTotal = 0f;
    bool parsed = false;

    var allowedKeys = new HashSet<string>(new[] { "items", "totalCost", "note" }, StringComparer.OrdinalIgnoreCase);
    int maxAttempts = 3;

    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        var attemptPrompt = basePrompt;
        if (attempt > 1)
        {
            attemptPrompt += "\n\nFollow-up: Output must be EXACT JSON matching schema only. No extra text. Return only the JSON object.";
        }

        lastRaw = await client.GenerateTextAsync(systemMsg, attemptPrompt);

        // Extract JSON object substring
        var start = lastRaw.IndexOf('{');
        var end = lastRaw.LastIndexOf('}');
        var adjustedJson = lastRaw;
        if (start >= 0 && end > start)
            adjustedJson = lastRaw[start..(end + 1)];

        try
        {
            using var doc = JsonDocument.Parse(adjustedJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new Exception("root is not an object");

            // Ensure no extra keys
            var keySet = new HashSet<string>(root.EnumerateObject().Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
            if (!allowedKeys.SetEquals(keySet))
                throw new Exception("unexpected or missing top-level keys");

            // Validate items
            var itemsElem = root.GetProperty("items");
            if (itemsElem.ValueKind != JsonValueKind.Array)
                throw new Exception("items is not an array");

            var newItems = new List<string>();
            foreach (var el in itemsElem.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.String)
                    throw new Exception("items must be strings");
                newItems.Add(el.GetString() ?? string.Empty);
            }

            // Validate totalCost
            var totalElem = root.GetProperty("totalCost");
            if (totalElem.ValueKind != JsonValueKind.Number)
                throw new Exception("totalCost is not a number");

            // Validate note
            var note = root.GetProperty("note").GetString() ?? string.Empty;

            // Recalculate total using local prices to be authoritative
            var recalculated = svc.CalculateTotal(newItems);

            adjustedItems = newItems;
            adjustedNote = note;
            adjustedTotal = recalculated;
            parsed = true;
            break;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"BudgetAgent: parse attempt {attempt} failed: {ex.Message}");
            // try again until maxAttempts
        }
    }

    if (!parsed)
    {
        return Results.Ok(new BudgetResponse(req.Items, total, "LLM response could not be parsed after retries; original list returned. Raw LLM: " + lastRaw));
    }

    return Results.Ok(new BudgetResponse(adjustedItems.ToArray(), adjustedTotal, adjustedNote));
});

app.Run();

// DTOs
public record BudgetRequest(string[] Items, float Budget);
public record BudgetResponse(string[] Items, float TotalCost, string Note);
