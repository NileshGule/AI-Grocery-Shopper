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
        AIAgent mealPlannerAgent = MealPlannerAgentAsync(chatClient);



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
        var workflow = new WorkflowBuilder(mealPlannerAgent)
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
    private static AIAgent MealPlannerAgentAsync(
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


        // return new AzureOpenAIClient(
        // new Uri(endpoint),
        // new AzureCliCredential())
        //     .GetChatClient(model)
        //     .CreateAIAgent(
        //         new ChatClientAgentOptions()
        //         {
        //             Name = "MealPlannerAgent",
        //             Instructions = systemMessage,
        //             ChatOptions = chatOptions
        //         }
        // );

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

        var prompt = $"Given the following list of ingredients, return a JSON object with two arrays: 'available' and 'missing'.\nIngredients: {JsonSerializer.Serialize(allIngredients)}";

        var agentResponse = await _inventoryCheckAgent.RunAsync(prompt);

        // context.Logger.LogInfo($"Inventory Check LLM Response: {agentResponse.Text}");

        var inventoryResponse = agentResponse.Deserialize<InventoryResponse>(JsonSerializerOptions.Web);

        // context.Logger.LogInfo($"Parsed inventory response: {inventoryResponse.Available.Length} available, {inventoryResponse.Missing.Length} missing.");

        return inventoryResponse;
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
