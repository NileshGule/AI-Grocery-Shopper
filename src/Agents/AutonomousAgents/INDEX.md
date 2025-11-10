# AutonomousAgents - Documentation Index

Welcome to the refactored AutonomousAgents codebase! This index will help you navigate the documentation and understand the project structure.

## 📖 Documentation Guide

### For New Developers
Start here to understand the project:

1. **[README.md](./README.md)** - Project overview, structure, and design principles
2. **[ARCHITECTURE.md](./ARCHITECTURE.md)** - Visual diagrams and component relationships
3. **[QUICK_REFERENCE.md](./QUICK_REFERENCE.md)** - Common tasks and code snippets

### For Existing Developers
If you're familiar with the old codebase:

1. **[REFACTORING_SUMMARY.md](./REFACTORING_SUMMARY.md)** - Before/after comparison and migration guide

### For All Developers
Keep these handy:

- **[QUICK_REFERENCE.md](./QUICK_REFERENCE.md)** - Quick lookup for common tasks
- **[ARCHITECTURE.md](./ARCHITECTURE.md)** - Understanding data flow

## 🗂️ Code Structure

### Core Application
- **[Program.cs](./Program.cs)** - Application entry point (52 lines)

### Domain Organization

#### Models (Data Transfer Objects)
- **[Models/MealPlanModels.cs](./Models/MealPlanModels.cs)** - Meal plan response and DTOs
- **[Models/InventoryModels.cs](./Models/InventoryModels.cs)** - Inventory response
- **[Models/BudgetModels.cs](./Models/BudgetModels.cs)** - Budget response
- **[Models/ShoppingModels.cs](./Models/ShoppingModels.cs)** - Shopping response

#### Agents (AI Configuration)
- **[Agents/AgentFactory.cs](./Agents/AgentFactory.cs)** - Creates and configures AI agents
  - `CreateMealPlannerAgent()` - Meal planning
  - `CreateInventoryAgent()` - Inventory checking
  - `CreateBudgetAgent()` - Budget optimization
  - `CreateShoppingAgent()` - Shopping descriptions

#### Executors (Workflow Steps)
- **[Executors/MealPlanExecutor.cs](./Executors/MealPlanExecutor.cs)** - Generates meal plans
- **[Executors/InventoryCheckExecutor.cs](./Executors/InventoryCheckExecutor.cs)** - Checks inventory
- **[Executors/BudgetExecutor.cs](./Executors/BudgetExecutor.cs)** - Applies budget constraints
- **[Executors/ShoppingExecutor.cs](./Executors/ShoppingExecutor.cs)** - Adds descriptions

#### Workflows (Orchestration)
- **[Workflows/GroceryWorkflowBuilder.cs](./Workflows/GroceryWorkflowBuilder.cs)** - Builds complete workflow

#### Configuration
- **[Configuration/AzureClientConfiguration.cs](./Configuration/AzureClientConfiguration.cs)** - Azure OpenAI setup

#### Services (Business Logic)
- **[BudgetService.cs](./BudgetService.cs)** - Price management and calculations

## 🎯 Common Use Cases

### I want to...

#### Understand the overall architecture
→ Read **[README.md](./README.md)** sections 1-3
→ View **[ARCHITECTURE.md](./ARCHITECTURE.md)** diagrams

#### Add a new workflow step
→ See **[QUICK_REFERENCE.md](./QUICK_REFERENCE.md)** - "Adding a New Executor"
→ Example: [Executors/MealPlanExecutor.cs](./Executors/MealPlanExecutor.cs)

#### Modify an existing agent
→ Open **[Agents/AgentFactory.cs](./Agents/AgentFactory.cs)**
→ Modify the relevant `Create*Agent()` method

#### Change the workflow order
→ Edit **[Workflows/GroceryWorkflowBuilder.cs](./Workflows/GroceryWorkflowBuilder.cs)**
→ Reorder or add `.AddEdge()` calls

#### Add a new data model
→ See **[QUICK_REFERENCE.md](./QUICK_REFERENCE.md)** - "Adding a New Model"
→ Example: [Models/BudgetModels.cs](./Models/BudgetModels.cs)

#### Change Azure OpenAI settings
→ Edit **[Configuration/AzureClientConfiguration.cs](./Configuration/AzureClientConfiguration.cs)**
→ Or pass parameters to `CreateChatClient()`

#### Update budget logic
→ Edit **[BudgetService.cs](./BudgetService.cs)**
→ Or modify **[Executors/BudgetExecutor.cs](./Executors/BudgetExecutor.cs)**

#### Understand what changed from the old code
→ Read **[REFACTORING_SUMMARY.md](./REFACTORING_SUMMARY.md)**

## 📊 Quick Stats

```
Total Files Created: 14
Total Lines of Code: ~511
Documentation Pages: 4
Namespaces: 6
Executors: 4
Agents: 4
Models: 4
```

## 🚀 Getting Started

### Build and Run
```bash
cd src/Agents/AutonomousAgents
dotnet build
dotnet run
```

### Project Dependencies
- Microsoft.Agents.AI
- Microsoft.Extensions.AI
- Azure.AI.OpenAI
- Azure.Identity

## 📚 Learning Path

### Beginner (30 minutes)
1. Read [README.md](./README.md) - Architecture Overview
2. Look at [ARCHITECTURE.md](./ARCHITECTURE.md) - Workflow Flow diagram
3. Browse [Program.cs](./Program.cs) - Entry point
4. Read [Workflows/GroceryWorkflowBuilder.cs](./Workflows/GroceryWorkflowBuilder.cs)

### Intermediate (1 hour)
5. Study one complete flow:
   - [Models/MealPlanModels.cs](./Models/MealPlanModels.cs)
   - [Agents/AgentFactory.cs](./Agents/AgentFactory.cs) - `CreateMealPlannerAgent()`
   - [Executors/MealPlanExecutor.cs](./Executors/MealPlanExecutor.cs)
6. Read [QUICK_REFERENCE.md](./QUICK_REFERENCE.md)

### Advanced (2 hours)
7. Study all executors in [Executors/](./Executors/)
8. Understand [BudgetService.cs](./BudgetService.cs)
9. Try adding a new feature using [QUICK_REFERENCE.md](./QUICK_REFERENCE.md)
10. Read [REFACTORING_SUMMARY.md](./REFACTORING_SUMMARY.md) for design decisions

## 🤝 Contributing

When contributing:
1. Follow the namespace structure (`AutonomousAgents.*`)
2. Add XML documentation to public APIs
3. Keep files focused (one class per file)
4. Update this index if you add new files
5. Follow patterns in [QUICK_REFERENCE.md](./QUICK_REFERENCE.md)

## 📞 Need Help?

- **Quickstart**: [README.md](./README.md)
- **Diagrams**: [ARCHITECTURE.md](./ARCHITECTURE.md)
- **How-to**: [QUICK_REFERENCE.md](./QUICK_REFERENCE.md)
- **Migration**: [REFACTORING_SUMMARY.md](./REFACTORING_SUMMARY.md)

## 🎉 Success Criteria

You'll know you understand the codebase when you can:
- ✅ Explain the workflow flow without looking at code
- ✅ Add a new executor in under 15 minutes
- ✅ Navigate to any component in under 10 seconds
- ✅ Modify an agent without breaking others
- ✅ Understand what each file does from its name

---

**Last Updated**: November 10, 2025
**Version**: 2.0 (Refactored)
**Maintainability Score**: 🟢 Excellent
