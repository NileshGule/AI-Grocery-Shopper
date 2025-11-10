using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using AutonomousAgents.Agents;
using AutonomousAgents.Executors;
using AutonomousAgents.Models;

namespace AutonomousAgents.Workflows;

/// <summary>
/// Builds and configures the grocery shopping workflow
/// </summary>
public static class GroceryWorkflowBuilder
{
    /// <summary>
    /// Creates a complete grocery shopping workflow that:
    /// 1. Generates a meal plan
    /// 2. Checks inventory for available ingredients
    /// 3. Applies budget constraints
    /// 4. Generates shopping descriptions
    /// </summary>
    public static Workflow BuildGroceryWorkflow(IChatClient chatClient)
    {
        // Create agents
        AIAgent mealPlannerAgent = AgentFactory.CreateMealPlannerAgent(chatClient);
        AIAgent inventoryAgent = AgentFactory.CreateInventoryAgent(chatClient);
        AIAgent budgetAgent = AgentFactory.CreateBudgetAgent(chatClient);
        AIAgent shoppingAgent = AgentFactory.CreateShoppingAgent(chatClient);

        // Create executors
        MealPlanExecutor mealPlanExecutor = new(mealPlannerAgent);
        InventoryCheckExecutor inventoryCheckExecutor = new(inventoryAgent);
        BudgetExecutor budgetExecutor = new(budgetAgent);
        ShoppingExecutor shoppingExecutor = new(shoppingAgent);

        // Build the workflow by connecting executors
        var workflow = new WorkflowBuilder(mealPlanExecutor)
            .AddEdge<InventoryResponse>(mealPlanExecutor, inventoryCheckExecutor)
            .WithOutputFrom(mealPlanExecutor)
            .AddEdge<BudgetResponse>(inventoryCheckExecutor, budgetExecutor)
            .WithOutputFrom(inventoryCheckExecutor)
            .AddEdge<ShoppingResponse>(budgetExecutor, shoppingExecutor)
            .WithOutputFrom(budgetExecutor)
            .Build();

        return workflow;
    }
}
