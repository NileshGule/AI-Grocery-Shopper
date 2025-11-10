using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using AutonomousAgents.Models;

namespace AutonomousAgents.Executors;

/// <summary>
/// Executor responsible for generating meal plans based on user requirements
/// </summary>
internal sealed class MealPlanExecutor : Executor<ChatMessage, MealPlanResponse>
{
    private readonly AIAgent _mealPlannerAgent;

    public MealPlanExecutor(AIAgent mealPlannerAgent) : base("MealPlanExecutor")
    {
        _mealPlannerAgent = mealPlannerAgent;
    }

    public override async ValueTask<MealPlanResponse> HandleAsync(
        ChatMessage input, 
        IWorkflowContext context, 
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting MealPlanExecutor...");

        var agentResponse = await _mealPlannerAgent.RunAsync(input.Text);

        Console.WriteLine($"LLM Response: {agentResponse.Text}");

        var meals = agentResponse.Deserialize<MealPlanResponse>(JsonSerializerOptions.Web);

        Console.WriteLine($"Parsed {meals.Meals.Count} meals from LLM response.");

        return new MealPlanResponse(meals.Meals);
    }
}
