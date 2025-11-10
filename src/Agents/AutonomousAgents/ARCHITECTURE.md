# Architecture Diagram

## Component Organization

```
┌─────────────────────────────────────────────────────────────────┐
│                         Program.cs                               │
│                    (Application Entry Point)                     │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│                   Configuration Layer                            │
├─────────────────────────────────────────────────────────────────┤
│  AzureClientConfiguration                                        │
│  • CreateChatClient()                                            │
│  • Manages Azure OpenAI connection                               │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│                     Workflow Layer                               │
├─────────────────────────────────────────────────────────────────┤
│  GroceryWorkflowBuilder                                          │
│  • BuildGroceryWorkflow()                                        │
│  • Orchestrates executor pipeline                                │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┴────────────────┐
        ↓                                  ↓
┌──────────────────┐             ┌──────────────────┐
│   Agent Layer    │             │  Executor Layer  │
├──────────────────┤             ├──────────────────┤
│  AgentFactory    │────────────→│  Executors       │
│                  │   provides  │                  │
│ • MealPlanner    │   agents    │ • MealPlan       │
│ • Inventory      │      to     │ • Inventory      │
│ • Budget         │             │ • Budget         │
│ • Shopping       │             │ • Shopping       │
└──────────────────┘             └────────┬─────────┘
                                          │
                                          ↓
                                 ┌──────────────────┐
                                 │   Models Layer   │
                                 ├──────────────────┤
                                 │ • MealPlan       │
                                 │ • Inventory      │
                                 │ • Budget         │
                                 │ • Shopping       │
                                 └────────┬─────────┘
                                          │
                                          ↓
                                 ┌──────────────────┐
                                 │  Services Layer  │
                                 ├──────────────────┤
                                 │  BudgetService   │
                                 │ • LoadPrices()   │
                                 │ • CalculateTotal │
                                 └──────────────────┘
```

## Workflow Execution Flow

```
┌──────────────┐
│  User Input  │
│  "Diwali     │
│   Gluten-    │
│   Free"      │
└──────┬───────┘
       │
       ↓
┌────────────────────────────────────────┐
│     MealPlanExecutor                    │
│  ┌──────────────────────────────────┐  │
│  │ MealPlannerAgent                 │  │
│  │ • Generates structured meals     │  │
│  │ • Returns: MealPlanResponse      │  │
│  └──────────────────────────────────┘  │
└──────┬─────────────────────────────────┘
       │ MealPlanResponse
       │ {meals: [...]}
       ↓
┌────────────────────────────────────────┐
│   InventoryCheckExecutor                │
│  ┌──────────────────────────────────┐  │
│  │ InventoryAgent                   │  │
│  │ • Checks ingredient availability │  │
│  │ • Returns: InventoryResponse     │  │
│  └──────────────────────────────────┘  │
└──────┬─────────────────────────────────┘
       │ InventoryResponse
       │ {available: [...], missing: [...]}
       ↓
┌────────────────────────────────────────┐
│      BudgetExecutor                     │
│  ┌──────────────────────────────────┐  │
│  │ BudgetAgent + BudgetService      │  │
│  │ • Calculates costs               │  │
│  │ • Optimizes for budget           │  │
│  │ • Returns: BudgetResponse        │  │
│  └──────────────────────────────────┘  │
└──────┬─────────────────────────────────┘
       │ BudgetResponse
       │ {items: [...], totalCost: 98.50}
       ↓
┌────────────────────────────────────────┐
│     ShoppingExecutor                    │
│  ┌──────────────────────────────────┐  │
│  │ ShoppingAgent                    │  │
│  │ • Adds friendly descriptions     │  │
│  │ • Returns: ShoppingResponse      │  │
│  └──────────────────────────────────┘  │
└──────┬─────────────────────────────────┘
       │ ShoppingResponse
       │ {categorizedItems: {...}}
       ↓
┌──────────────┐
│ Final Output │
│ Shopping List│
│ with Friendly│
│ Descriptions │
└──────────────┘
```

## Data Flow

```
ChatMessage (User Input)
        ↓
MealPlanResponse (List<MealDto>)
        ↓
InventoryResponse (Available[], Missing[])
        ↓
BudgetResponse (Items[], TotalCost, Note)
        ↓
ShoppingResponse (Dictionary<Item, Description>)
```

## Module Dependencies

```
Program.cs
  ↓
  ├─→ Configuration.AzureClientConfiguration
  └─→ Workflows.GroceryWorkflowBuilder
        ↓
        ├─→ Agents.AgentFactory
        │     └─→ Models.*
        │
        └─→ Executors.*
              ├─→ Models.*
              └─→ Services.BudgetService
```
