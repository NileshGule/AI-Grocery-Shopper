# Quick Reference Guide - Autonomous Agents

## 📚 Module Quick Reference

### Adding a New Model
**Location**: `Models/`

```csharp
namespace AutonomousAgents.Models;

public record YourResponse(/* properties */);
```

### Adding a New Agent
**Location**: `Agents/AgentFactory.cs`

```csharp
public static ChatClientAgent CreateYourAgent(IChatClient chatClient)
{
    var systemMessage = "Your agent instructions...";
    
    return new ChatClientAgent(chatClient, 
        new ChatClientAgentOptions(instructions: systemMessage)
    {
        ChatOptions = new()
        {
            ResponseFormat = ChatResponseFormat.ForJsonSchema<YourResponse>()
        }
    });
}
```

### Adding a New Executor
**Location**: `Executors/`

```csharp
namespace AutonomousAgents.Executors;

internal sealed class YourExecutor : Executor<InputType, OutputType>
{
    private readonly AIAgent _agent;

    public YourExecutor(AIAgent agent) : base("YourExecutor")
    {
        _agent = agent;
    }

    public override async ValueTask<OutputType> HandleAsync(
        InputType input, 
        IWorkflowContext context, 
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting YourExecutor...");
        
        var prompt = $"Process: {input}";
        var response = await _agent.RunAsync(prompt);
        
        return response.Deserialize<OutputType>(JsonSerializerOptions.Web);
    }
}
```

### Modifying the Workflow
**Location**: `Workflows/GroceryWorkflowBuilder.cs`

```csharp
// Add your executor to the workflow
var yourExecutor = new YourExecutor(yourAgent);

var workflow = new WorkflowBuilder(mealPlanExecutor)
    // ... existing edges ...
    .AddEdge<YourResponse>(previousExecutor, yourExecutor)
    .WithOutputFrom(previousExecutor)
    .Build();
```

## 🎯 Common Tasks

### Change Azure OpenAI Configuration
**File**: `Configuration/AzureClientConfiguration.cs`

Modify constants:
```csharp
private const string DefaultEndpoint = "your-endpoint";
private const string DefaultModel = "your-model";
```

Or pass at runtime:
```csharp
var chatClient = AzureClientConfiguration.CreateChatClient(
    endpoint: "custom-endpoint",
    model: "custom-model"
);
```

### Update Budget Constraints
**File**: `Executors/BudgetExecutor.cs`

Change the constant:
```csharp
private const float DefaultBudget = 100.0f; // Change this
```

### Modify Meal Plan Prompt
**File**: `Program.cs`

Edit the prompt variable:
```csharp
var prompt = $@"Generate a meal plan for: 
        Your Event
        Constraints: Your Constraints

{schemaInstruction}";
```

## 🔍 Understanding the Code

### Executor Pattern
Executors are workflow steps that:
1. Receive typed input
2. Process via AI agent
3. Return typed output

### Agent Pattern
Agents are specialized LLMs with:
1. Specific system instructions
2. JSON schema response format
3. Single responsibility

### Workflow Pattern
Workflows connect executors:
1. Define input/output types
2. Chain executors with edges
3. Stream execution events

## 🐛 Troubleshooting

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

### Missing Dependencies
```bash
# Restore packages
dotnet restore
```

### Runtime Errors

**Authentication Issues**:
```bash
# Ensure Azure CLI is logged in
az login
```

**File Not Found (prices.json)**:
- Ensure `prices.json` is in the project root
- Check the file is copied to output directory

## 📊 File Organization at a Glance

```
Component          | Location                  | Purpose
-------------------|---------------------------|---------------------------
Entry Point        | Program.cs                | Main application logic
Agent Creation     | Agents/AgentFactory.cs    | Creates AI agents
Workflow Build     | Workflows/GroceryWorkflow | Orchestrates executors
Execution Steps    | Executors/*.cs            | Individual workflow steps
Data Models        | Models/*.cs               | DTOs and responses
Configuration      | Configuration/*.cs        | Setup and config
Business Logic     | Services/BudgetService.cs | Utility services
```

## 🚦 Workflow Execution Order

1. **MealPlanExecutor** - Generates meal plan from user input
2. **InventoryCheckExecutor** - Identifies available ingredients
3. **BudgetExecutor** - Optimizes for budget constraints
4. **ShoppingExecutor** - Adds friendly descriptions

## 💡 Best Practices

1. **One executor per file** - Keep executors focused
2. **Namespace consistency** - Use `AutonomousAgents.*`
3. **XML documentation** - Document public APIs
4. **Error handling** - Log errors in executors
5. **Const for config** - Use constants for magic numbers
6. **Async/await** - Always use async patterns
7. **Dispose resources** - Use `await using` for streams

## 📝 Code Style

```csharp
// Prefer record types for DTOs
public record ResponseType(Property1, Property2);

// Use sealed for executors
internal sealed class MyExecutor : Executor<In, Out>

// Use static for factories
public static class MyFactory

// Use descriptive names
CreateMealPlannerAgent() // Good
CreateAgent1()          // Bad
```
