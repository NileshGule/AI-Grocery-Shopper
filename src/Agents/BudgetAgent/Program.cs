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

    Console.WriteLine($"Received budget request: Items=[{string.Join(", ", req.Items)}], Budget={req.Budget}");

    // Calculate total cost
    float total = svc.CalculateTotal(req.Items);

    Console.WriteLine($"Calculated total cost: {total}");

    if (total <= req.Budget)
    {
        return Results.Ok(new BudgetResponse(req.Items, total, "Within budget - no changes needed"));
    }

    // Over budget - ask LLM for suggestions to reduce cost
    Console.WriteLine("Over budget - requesting LLM for adjustments");

    var systemMsg = "You are an assistant that suggests lower-cost substitutions or removals to meet a shopping budget. Respond with a JSON object { \"items\": [ ... ] , \"note\": \"...\" } where items is the adjusted list.";
    var prompt = $"Original items: [{string.Join(", ", req.Items)}]\nBudget: {req.Budget}\nTotalCost: {total}\nPlease suggest an adjusted shopping list to meet the budget and include a short note explaining changes.";

    var llmResp = await client.GenerateTextAsync(systemMsg, prompt);

    var (newItems, note) = svc.ParseAdjustedFromLLM(llmResp);

    Console.WriteLine($"Parsed LLM response: {newItems.Count} items, Note: {note}");
    
    if (!newItems.Any())
    {
        return Results.Ok(new BudgetResponse(req.Items, total, "LLM response could not be parsed; original list returned. Raw LLM: " + llmResp));
    }

    var newTotal = svc.CalculateTotal(newItems);

    Console.WriteLine($"LLM suggested {newItems.Count} items with total cost: {newTotal}");

    return Results.Ok(new BudgetResponse(newItems.ToArray(), newTotal, note));
});

app.Run();

// DTOs
public record BudgetRequest(string[] Items, float Budget);
public record BudgetResponse(string[] Items, float TotalCost, string Note);
