using System;
using System.Threading.Tasks;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using AutonomousAgents.Configuration;
using AutonomousAgents.Workflows;

/// <summary>
/// Entry point for the Autonomous Agents grocery shopping workflow application
/// </summary>
public static class Program
{
    private static async Task Main()
    {
        // Set up the Azure OpenAI client
        var chatClient = AzureClientConfiguration.CreateChatClient();

        // Build the grocery shopping workflow
        var workflow = GroceryWorkflowBuilder.BuildGroceryWorkflow(chatClient);

        // Define the schema instruction for the meal planner
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

        var prompt = $@"Generate a meal plan for: 
        Christmas dinner with family
        Constraints: Gluten free, nut allergy

        {schemaInstruction}";

        Console.WriteLine("Starting workflow execution...");

        // Execute the workflow
        await using StreamingRun run = await InProcessExecution.StreamAsync(
            workflow, 
            new ChatMessage(ChatRole.User, prompt));

        Console.WriteLine("Workflow execution started.");

        // Send the turn token to trigger the agents
        // The agents are wrapped as executors. When they receive messages,
        // they will cache the messages and only start processing when they receive a TurnToken.
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        // Watch for workflow events
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
    }
}
