# Autonomous Agents - Modular Architecture

This document describes the refactored modular architecture of the Autonomous Agents grocery shopping workflow application.

## 📁 Project Structure

```
AutonomousAgents/
├── Program.cs                          # Application entry point
├── BudgetService.cs                    # Legacy file (moved to Services/)
├── prices.json                         # Price data
├── Agents/
│   └── AgentFactory.cs                # Factory for creating AI agents
├── Configuration/
│   └── AzureClientConfiguration.cs    # Azure OpenAI client setup
├── Executors/
│   ├── MealPlanExecutor.cs           # Meal planning workflow step
│   ├── InventoryCheckExecutor.cs     # Inventory checking workflow step
│   ├── BudgetExecutor.cs             # Budget optimization workflow step
│   └── ShoppingExecutor.cs           # Shopping list generation workflow step
├── Models/
│   ├── MealPlanModels.cs             # Meal plan DTOs
│   ├── InventoryModels.cs            # Inventory DTOs
│   ├── BudgetModels.cs               # Budget DTOs
│   └── ShoppingModels.cs             # Shopping DTOs
├── Services/
│   └── (BudgetService.cs)            # Budget calculation service
└── Workflows/
    └── GroceryWorkflowBuilder.cs     # Workflow composition and configuration
```

## 🏗️ Architecture Overview

The application follows a modular, separation-of-concerns design with the following logical groupings:

### 1. **Models** (`Models/`)
Contains all data transfer objects (DTOs) and response models used throughout the application.

- `MealPlanModels.cs` - Meal plan response and meal DTO
- `InventoryModels.cs` - Inventory availability response
- `BudgetModels.cs` - Budget optimization response
- `ShoppingModels.cs` - Shopping list with descriptions

### 2. **Agents** (`Agents/`)
Responsible for creating and configuring AI agents with specific instructions and response formats.

- `AgentFactory.cs` - Factory pattern for creating specialized agents:
  - `CreateMealPlannerAgent()` - Generates structured meal plans
  - `CreateInventoryAgent()` - Checks ingredient availability
  - `CreateBudgetAgent()` - Optimizes shopping lists for budget constraints
  - `CreateShoppingAgent()` - Generates friendly product descriptions

### 3. **Executors** (`Executors/`)
Implements workflow steps that process data and orchestrate agent interactions.

- `MealPlanExecutor.cs` - Transforms user input into structured meal plans
- `InventoryCheckExecutor.cs` - Analyzes ingredient availability
- `BudgetExecutor.cs` - Applies budget constraints and optimization
- `ShoppingExecutor.cs` - Enriches shopping lists with descriptions

### 4. **Workflows** (`Workflows/`)
Orchestrates the complete workflow by connecting executors.

- `GroceryWorkflowBuilder.cs` - Builds the complete grocery shopping workflow:
  1. Meal Plan → 2. Inventory Check → 3. Budget Optimization → 4. Shopping List

### 5. **Services** (`Services/`)
Business logic and utility services.

- `BudgetService.cs` - Handles price loading, cost calculation, and budget analysis

### 6. **Configuration** (`Configuration/`)
Application configuration and setup.

- `AzureClientConfiguration.cs` - Azure OpenAI client initialization

## 🔄 Workflow Flow

```
User Input (Meal Requirements)
        ↓
[MealPlanExecutor]
  → Generates meal plan
        ↓
[InventoryCheckExecutor]
  → Identifies available/missing ingredients
        ↓
[BudgetExecutor]
  → Optimizes list to meet budget
        ↓
[ShoppingExecutor]
  → Adds friendly descriptions
        ↓
Final Shopping List
```

## 🎯 Design Principles

1. **Separation of Concerns**: Each module has a single, well-defined responsibility
2. **Single Responsibility**: Classes and files focus on one aspect of the system
3. **Factory Pattern**: AgentFactory centralizes agent creation logic
4. **Builder Pattern**: WorkflowBuilder composes the execution pipeline
5. **Dependency Injection**: Executors receive agent dependencies via constructor
6. **Modularity**: Easy to add new executors, agents, or workflow steps
7. **Documentation**: XML comments on all public APIs

## 🚀 Usage

### Running the Application

```bash
cd src/Agents/AutonomousAgents
dotnet run
```

### Extending the Workflow

#### Adding a New Executor

1. Create a new file in `Executors/` (e.g., `NutritionExecutor.cs`)
2. Implement `Executor<TInput, TOutput>`
3. Add the executor to the workflow in `GroceryWorkflowBuilder.cs`

```csharp
// Example
.AddEdge<NutritionResponse>(shoppingExecutor, nutritionExecutor)
.WithOutputFrom(shoppingExecutor)
```

#### Adding a New Agent

1. Add a factory method in `AgentFactory.cs`
2. Define the agent's instructions and response format
3. Use the agent in the appropriate executor

## 📦 Dependencies

- Microsoft.Agents.AI
- Microsoft.Extensions.AI
- Azure.AI.OpenAI
- Azure.Identity

## 🔧 Configuration

Azure OpenAI configuration is centralized in `AzureClientConfiguration.cs`:

- **Endpoint**: `https://ai-foundry-ai-hub.openai.azure.com/`
- **Model**: `gpt-4.1-mini`
- **Authentication**: Azure CLI Credential

To customize:
```csharp
var chatClient = AzureClientConfiguration.CreateChatClient(
    endpoint: "your-endpoint",
    model: "your-model"
);
```

## 🧪 Testing

The modular structure makes unit testing straightforward:

- **Mock agents** in executor tests
- **Mock executors** in workflow tests
- **Test services** independently with sample data

## 📝 Future Improvements

1. Move `BudgetService.cs` fully into `Services/` folder
2. Add dependency injection container (e.g., Microsoft.Extensions.DependencyInjection)
3. Extract configuration to `appsettings.json`
4. Add logging infrastructure (e.g., ILogger)
5. Implement retry policies for agent calls
6. Add validation middleware for executor inputs/outputs
7. Create integration tests for the complete workflow

## 🤝 Contributing

When adding new features:

1. Follow the existing namespace structure
2. Add XML documentation to public APIs
3. Keep classes focused on single responsibilities
4. Update this README with new modules
5. Ensure code compiles without warnings
