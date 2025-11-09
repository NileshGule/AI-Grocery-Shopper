using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common.ModelClient;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

// Declare the WebApplication builder before using it to fix undefined 'builder' errors.
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
builder.WebHost.UseUrls("http://0.0.0.0:5003");

// builder.Services.AddSingleton<IModelClient, Common.ModelClient.LocalModelClient>();
builder.Services.AddSingleton<IModelClient, Common.ModelClient.AzureFoundryModelClient>();

var app = builder.Build();
app.UseCors();

app.MapGet("/health", () => Results.Ok("MealPlannerAgent OK"));

app.MapPost("/plan", async (MealPlanRequest req, IModelClient client) =>
{
    Console.WriteLine($"Received meal plan request: Preferences={req.Preferences}, Constraints={req.Constraints}");

    var systemMessage = "You are a helpful meal planning assistant. Respond ONLY with a single JSON object (no surrounding text) that matches the schema described below.";
    var schemaInstruction = @"Return a JSON object with this shape:
{
  ""meals"": [
    {
      ""name"": string,
      ""ingredients"": [ string ],
      ""notes"": string (optional)
    }
  ]
}
Only output valid JSON and no other text.";

    var prompt = $"Generate a meal plan for: {req.Preferences}\nConstraints: {req.Constraints}\n\n{schemaInstruction}";

    var endpoint = "https://ai-foundry-ai-hub.openai.azure.com/";
    var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_OPENAI_APIKEY");
    var model = "gpt-4.1-mini";

    JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(MealPlanResponse));

    ChatOptions chatOptions = new()
    {
        ResponseFormat = ChatResponseFormat.ForJsonSchema(
            schema: schema,
            schemaName: "MealPlanResponse",
            schemaDescription: "Information about a meal plan including its meals")
    };

    AIAgent agent = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureCliCredential())
        .GetChatClient(model)
        .CreateAIAgent(
            new ChatClientAgentOptions()
            {
                Name = "MealPlannerAgent",
                Instructions = systemMessage,
                ChatOptions = chatOptions
            }
    );

    var agentResponse = await agent.RunAsync(prompt);

    Console.WriteLine($"LLM Response: {agentResponse.Text}");

    var meals = agentResponse.Deserialize<MealPlanResponse>(JsonSerializerOptions.Web);
    
    Console.WriteLine($"Parsed {meals.Meals.Count} meals from LLM response.");

    return Results.Ok(new MealPlanResponse(meals.Meals));
});

app.Run();

// DTOs and helper types
public record MealPlanRequest(string Preferences, string Constraints);
public record MealPlanResponse(List<MealDto> Meals);
public class MealDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("ingredients")]
    public List<string> Ingredients { get; set; } = new();

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

// Internal types to parse the LLM JSON
public class StructuredPlan
{
    [JsonPropertyName("meals")]
    public List<StructuredMeal>? Meals { get; set; }
}
public class StructuredMeal
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("ingredients")]
    public List<string>? Ingredients { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
