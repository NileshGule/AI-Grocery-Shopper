using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common.ModelClient;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
// Ensure the app listens on all network interfaces inside the container
// builder.WebHost.UseUrls("http://0.0.0.0:80");
builder.WebHost.UseUrls("http://0.0.0.0:5004");

builder.Services.AddSingleton<IModelClient, Common.ModelClient.LocalModelClient>();

var app = builder.Build();

app.UseCors();

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
    var systemMsg = $@"You are an assistant that, given a JSON array of product names, returns ONLY a JSON object mapping each product to a short friendly description (one sentence). 
    The description should be lavish Michaline style names. 
    The output must be a single valid JSON object with product names as keys and descriptions as values. 
    No extra text. 
    Use the exact product strings as keys.";

    // Include a short example to encourage stable keys
    var example = new { example_items = new[] { "spaghetti", "tomato" }, example_output = new { spaghetti = "Long thin pasta, typically made from wheat.", tomato = "A versatile red fruit used in salads and sauces." } };

    var promptPayload = new
    {
        items = items
    };

    var endpoint = "https://ai-foundry-ai-hub.openai.azure.com/";
    var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_OPENAI_APIKEY");
    var model = "gpt-4.1-mini";

    JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(ShoppingResponse));

    ChatOptions chatOptions = new()
    {
        ResponseFormat = ChatResponseFormat.ForJsonSchema(
            schema: schema,
            schemaName: "ShoppingResponse",
            schemaDescription: "Information about a shopping list including its categories and items")
    };

    AIAgent agent = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureCliCredential())
        .GetChatClient(model)
        .CreateAIAgent(
            new ChatClientAgentOptions()
            {
                Name = "ShopperAgent",
                Instructions = systemMsg,
                ChatOptions = chatOptions
            }
    );

    var agentResponse = await agent.RunAsync(JsonSerializer.Serialize(promptPayload));

    Console.WriteLine($"LLM Response: {agentResponse.Text}");

    var shoppingResponse = agentResponse.Deserialize<ShoppingResponse>(JsonSerializerOptions.Web);

    Console.WriteLine($"Parsed {shoppingResponse.CategorizedItems.Count} categories from LLM response.");

    return Results.Ok(shoppingResponse);

});

app.Run();

public record ShoppingRequest(string[] Items);

public record ShoppingResponse(Dictionary<string, string> CategorizedItems);
