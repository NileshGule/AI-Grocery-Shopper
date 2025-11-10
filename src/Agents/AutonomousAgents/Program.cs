using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.Workflows;

using Azure.AI.OpenAI;
using Azure.Identity;

using OpenAI;
using System.Diagnostics;

public static class Program
{
    private static async Task Main()
    {
        // Set up the Azure Foundry client
        // var endpoint = "https://ai-foundry-ai-hub.services.ai.azure.com/api/projects/ai-hub-Project1";
        var endpoint = "https://ai-foundry-ai-hub.openai.azure.com/";
        var model = "gpt-4.1-mini";

        var chatClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new AzureCliCredential())
            .GetChatClient(model)
            .AsIChatClient();

        // Create agents
        AIAgent mealPlannerAgent = GetMealPlannerAgent(chatClient);
        AIAgent inventoryAgent = GetInventoryAgent(chatClient);
        AIAgent budgetAgent = GetBudgetAgent(chatClient);

        MealPlanExecutor mealPlanExecutor = new(mealPlannerAgent);
        InventoryCheckExecutor inventoryCheckExecutor = new(inventoryAgent);
        BudgetExecutor budgetExecutor = new(budgetAgent);

        // Execute the workflow

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

        var prompt = @$"Generate a meal plan for: 
        Diwali \nConstraints: Glutten free \n\n{schemaInstruction}";

        // Build the workflow by adding executors and connecting them
        var workflow = new WorkflowBuilder(mealPlanExecutor)
            .AddEdge<InventoryResponse>(mealPlanExecutor, inventoryCheckExecutor)
            .WithOutputFrom(mealPlanExecutor)
            .AddEdge<BudgetResponse>(inventoryCheckExecutor, budgetExecutor)
            .WithOutputFrom(inventoryCheckExecutor)
            .Build();

        Console.WriteLine("Starting workflow execution...");

        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, new ChatMessage(ChatRole.User, prompt));

        Console.WriteLine("Workflow execution started.");

        // Must send the turn token to trigger the agents.
        // The agents are wrapped as executors. When they receive messages,
        // they will cache the messages and only start processing when they receive a TurnToken.
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        // await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
        // {
        //     if (evt is AgentRunUpdateEvent executorComplete)
        //     {
        //         Console.WriteLine($"{executorComplete.ExecutorId}: {executorComplete.Data}");
        //     }
        // }

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is WorkflowOutputEvent outputEvent)
            {
                Console.WriteLine("Workflow completed with output:");
                Console.WriteLine($"{outputEvent}");
            }
            else if (evt is AgentRunUpdateEvent executorComplete)
            {
                Console.WriteLine($"{executorComplete.ExecutorId}: {executorComplete.Data}");
            }
        }

        // Cleanup the agents created for the sample.
        // await persistentAgentsClient.Administration.DeleteAgentAsync(frenchAgent.Id);
        // await persistentAgentsClient.Administration.DeleteAgentAsync(spanishAgent.Id);
        // await persistentAgentsClient.Administration.DeleteAgentAsync(englishAgent.Id);
    }

    /// <summary>
    /// Creates a translation agent for the specified target language.
    /// </summary>
    /// <param name="targetLanguage">The target language for translation</param>
    /// <param name="persistentAgentsClient">The PersistentAgentsClient to create the agent</param>
    /// <param name="model">The model to use for the agent</param>
    /// <returns>A ChatClientAgent configured for the specified language</returns>
    private static AIAgent GetMealPlannerAgent(
        IChatClient chatClient)
    {
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
        );

    }

    private static ChatClientAgent GetBudgetAgent(IChatClient chatClient)
    {
        var systemMessage = @"You are an assistant whose ONLY allowed output is a single valid JSON object matching this schema: 
        { ""items"": [ string ], ""totalCost"": number, ""note"": string }. 
        Do not output any text, explanation, markup, or extra keys. Always use double quotes.";

        return new(chatClient, new ChatClientAgentOptions(instructions: systemMessage)
        {
            ChatOptions = new()
            {
                ResponseFormat = ChatResponseFormat.ForJsonSchema<BudgetResponse>()
            }
        });
}

    private static ChatClientAgent GetInventoryAgent(IChatClient chatClient) =>
        new(chatClient, new ChatClientAgentOptions(instructions: "You are a inventory checking agent.")
        {
            ChatOptions = new()
            {
                ResponseFormat = ChatResponseFormat.ForJsonSchema<InventoryResponse>()
            }
        });
}

internal sealed class MealPlanExecutor : Executor<ChatMessage, MealPlanResponse>
{
    private readonly AIAgent _mealPlannerAgent;

    public MealPlanExecutor(AIAgent mealPlannerAgent) : base("MealPlanExecutor")
    {
        _mealPlannerAgent = mealPlannerAgent;
    }
    public override async ValueTask<MealPlanResponse> HandleAsync(ChatMessage input, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting MealPlanExecutor...");

        var agentResponse = await _mealPlannerAgent.RunAsync(input.Text);

        Console.WriteLine($"LLM Response: {agentResponse.Text}");

        var meals = agentResponse.Deserialize<MealPlanResponse>(JsonSerializerOptions.Web);
        
        Console.WriteLine($"Parsed {meals.Meals.Count} meals from LLM response.");

        return new MealPlanResponse(meals.Meals);
        
    }
}

internal sealed class InventoryCheckExecutor : Executor<MealPlanResponse, InventoryResponse>
{
    private readonly AIAgent _inventoryCheckAgent;

    public InventoryCheckExecutor(AIAgent inventoryCheckAgent) : base("InventoryCheckExecutor")
    {
        _inventoryCheckAgent = inventoryCheckAgent;
    }
    public override async ValueTask<InventoryResponse> HandleAsync(MealPlanResponse input, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting InventoryCheckExecutor...");

        var allIngredients = input.Meals.SelectMany(m => m.Ingredients).Distinct().ToArray();

        var prompt = @$"Given the following list of ingredients, 
        return a JSON object with two arrays: 'available' and 'missing'.\n
        Ingredients: {JsonSerializer.Serialize(allIngredients)}";

        var agentResponse = await _inventoryCheckAgent.RunAsync(prompt);

        var inventoryResponse = agentResponse.Deserialize<InventoryResponse>(JsonSerializerOptions.Web);

        Console.WriteLine($"Parsed inventory response: {inventoryResponse.Available.Length} available, {inventoryResponse.Missing.Length} missing.");

        return inventoryResponse;
    }
}

internal sealed class BudgetExecutor : Executor<InventoryResponse, BudgetResponse>
{
    private readonly AIAgent _budgetAgent;

    public BudgetExecutor(AIAgent budgetAgent) : base("BudgetExecutor")
    {
        _budgetAgent = budgetAgent;
    }
    public override async ValueTask<BudgetResponse> HandleAsync(InventoryResponse input, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting BudgetExecutor...");

        var priceFile = Path.Combine(AppContext.BaseDirectory, "prices.json");
var prices = BudgetService.LoadPricesFromFile(priceFile);

Console.WriteLine($"Loaded {prices.Count} price entries from {priceFile}");

        var budgetService = new BudgetService(prices);
        Console.WriteLine($"BudgetService Prices count: {budgetService.Prices.Count}");

        Console.WriteLine($"Received budget request: Items=[{string.Join(", ", input.Available)}], Budget=100.0");

    // Calculate total cost
    float total = budgetService.CalculateTotal(input.Available);

    Console.WriteLine($"Calculated total cost: {total}");

    if (total <= 100.0)
    {
        new BudgetResponse(input.Available, total, "Within budget - no changes needed");
    }

        var basePrompt = $@"Original items: [{string.Join(", ", input.Available)}]
Budget: 100.0
CurrentTotal: {total}
Prices: {JsonSerializer.Serialize(budgetService.Prices)}

Task: Suggest an adjusted shopping list that meets or is closest to the Budget.
Requirements:
- Return ONLY the JSON object: {{ ""items"": [ string ], ""totalCost"": number, ""note"": string }}.
- items: array of product names.
- totalCost: numeric sum computed using the provided Prices (use default price 2.0 for unknown items).
- note: 1-2 sentence explanation.
- Prefer substitutions over removals. Make minimal changes needed to meet budget.";

        var agentResponse = await _budgetAgent.RunAsync(basePrompt);

        Console.WriteLine($"LLM Response: {agentResponse.Text}");

        var budgetResponse = agentResponse.Deserialize<BudgetResponse>(JsonSerializerOptions.Web);

        Console.WriteLine($"Parsed budget response: {budgetResponse.TotalCost} for {budgetResponse.Items.Length} items.");

        return new BudgetResponse(budgetResponse.Items, budgetResponse.TotalCost, budgetResponse.Note); 

    }
}
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

public record InventoryResponse(string[] Available, string[] Missing);

public record BudgetResponse(string[] Items, float TotalCost, string Note);
