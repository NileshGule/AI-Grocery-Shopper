using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common.ModelClient;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using BudgetAgent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

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

        var endpoint = "https://ai-foundry-ai-hub.openai.azure.com/";
        var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_OPENAI_APIKEY");
        var model = "gpt-4.1-mini";

        lastRaw = await client.GenerateTextAsync(systemMsg, attemptPrompt);

        JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(BudgetResponse));

    ChatOptions chatOptions = new()
    {
        ResponseFormat = ChatResponseFormat.ForJsonSchema(
            schema: schema,
            schemaName: "BudgetResponse",
            schemaDescription: "Information about a budget response including its items, total cost, and note")
    };

    AIAgent agent = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureCliCredential())
        .GetChatClient(model)
        .CreateAIAgent(
            new ChatClientAgentOptions()
            {
                Name = "BudgetAgent",
                Instructions = systemMsg,
                ChatOptions = chatOptions
            }
    );

    var agentResponse = await agent.RunAsync(attemptPrompt);

    Console.WriteLine($"LLM Response: {agentResponse.Text}");

    var budgetResponse = agentResponse.Deserialize<BudgetResponse>(JsonSerializerOptions.Web);

    Console.WriteLine($"Parsed budget response: {budgetResponse.TotalCost} for {budgetResponse.Items.Length} items.");

    return Results.Ok(new BudgetResponse(budgetResponse.Items, budgetResponse.TotalCost, budgetResponse.Note)); 

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
