using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common.ModelClient;
using System.Security.AccessControl;

var builder = WebApplication.CreateBuilder(args);

// ...existing code...

builder.Services.AddSingleton<IModelClient, Common.ModelClient.LocalModelClient>();
// builder.Services.AddSingleton<IModelClient, Common.ModelClient.AzureFoundryModelClient>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("MealPlannerAgent OK"));

app.MapPost("/plan", async (MealPlanRequest req, IModelClient client) =>
{
    Console.WriteLine($"Received meal plan request: Preferences={req.Preferences}, Constraints={req.Constraints}");
    var prompt = $"Generate a 7-day meal plan for: {req.Preferences}\nConstraints: {req.Constraints}";
    var llmResponse = await client.GenerateTextAsync(prompt);
    // For bootstrap, return raw LLM response as part of structured response
    Console.WriteLine($"LLM Response: {llmResponse}");
    return Results.Ok(new MealPlanResponse (llmResponse ));
});

app.Run();

// DTOs
public record MealPlanRequest(string Preferences, string Constraints);
public record MealPlanResponse(string RawText);
