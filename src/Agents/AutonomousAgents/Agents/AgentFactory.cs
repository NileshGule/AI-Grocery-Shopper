using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using AutonomousAgents.Models;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace AutonomousAgents.Agents;

/// <summary>
/// Factory class responsible for creating and configuring AI agents
/// </summary>
public static class AgentFactory
{
    /// <summary>
    /// Creates a meal planning agent that generates structured meal plans
    /// </summary>
    public static AIAgent CreateMealPlannerAgent(IChatClient chatClient)
    {
        // Create a TracerProvider that exports to the console
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("agent-telemetry-source")
            .AddConsoleExporter()
            .Build();
    
        var systemMessage = "You are a helpful meal planning assistant. Respond ONLY with a single JSON object (no surrounding text) that matches the schema described below.";

        JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(MealPlanResponse));

        ChatOptions chatOptions = new()
        {
            ResponseFormat = ChatResponseFormat.ForJsonSchema(
                schema: schema,
                schemaName: "MealPlanResponse",
                schemaDescription: "Information about a meal plan including its meals")
        };

        return chatClient.CreateAIAgent(
            new ChatClientAgentOptions()
            {
                Name = "MealPlannerAgent",
                Instructions = systemMessage,
                ChatOptions = chatOptions
            }
        )
        .AsBuilder()
        .UseOpenTelemetry(sourceName: "agent-telemetry-source")
        .Build();
    }

    /// <summary>
    /// Creates a budget agent that optimizes shopping lists based on budget constraints
    /// </summary>
    public static ChatClientAgent CreateBudgetAgent(IChatClient chatClient)
    {
        var systemMessage = @"You are an assistant whose ONLY allowed output is a single valid JSON object matching this schema: 
        { ""items"": [ string ], ""totalCost"": number, ""note"": string }. 
        Do not output any text, explanation, markup, or extra keys. Always use double quotes.";

        return new ChatClientAgent(chatClient, new ChatClientAgentOptions(instructions: systemMessage)
        {
            ChatOptions = new()
            {
                ResponseFormat = ChatResponseFormat.ForJsonSchema<BudgetResponse>()
            }
        });
    }

    /// <summary>
    /// Creates an inventory agent that checks ingredient availability
    /// </summary>
    public static ChatClientAgent CreateInventoryAgent(IChatClient chatClient)
    {
        return new ChatClientAgent(chatClient, new ChatClientAgentOptions(instructions: "You are a inventory checking agent.")
        {
            ChatOptions = new()
            {
                ResponseFormat = ChatResponseFormat.ForJsonSchema<InventoryResponse>()
            }
        });
    }

    /// <summary>
    /// Creates a shopping agent that generates friendly descriptions for products
    /// </summary>
    public static ChatClientAgent CreateShoppingAgent(IChatClient chatClient)
    {
        var systemMsg = @"You are an assistant that, given a JSON array of product names, returns ONLY a JSON object mapping each product to a short friendly description (one sentence). 
    The description should be lavish Michaline style names. 
    The output must be a single valid JSON object with product names as keys and descriptions as values. 
    No extra text. 
    Use the exact product strings as keys.";

        return new ChatClientAgent(chatClient, new ChatClientAgentOptions(instructions: systemMsg)
        {
            ChatOptions = new()
            {
                ResponseFormat = ChatResponseFormat.ForJsonSchema<ShoppingResponse>()
            }
        });
    }
}
