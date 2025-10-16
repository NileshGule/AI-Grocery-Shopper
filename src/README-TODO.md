Project TODOs and bootstrap instructions for AI Grocery Shopper

Root goals:
- Create a modular C# (.NET) solution with independent agents + orchestrator + UI.
- Abstract model client behind an interface to allow swapping local LLMs (Azure AI Foundry, Docker Model Runner) with hosted models later.
- Provide docker-compose for local developer experience and sample JSON data for testing.

Project structure (suggested):

src/
  Common/
    ModelClient/  # abstraction and implementations for local vs hosted models
    DTOs/         # shared request/response models
  Agents/
    MealPlannerAgent/
    InventoryAgent/
    BudgetAgent/
    ShopperAgent/
  Orchestrator/
  UI/
  Tests/

Bootstrap files created:
- .gitignore (existing in repo; ensure it contains .env, bin/ obj/)

Next steps to bootstrap each project below with TODO lists and sample JSON data.

Common/ModelClient TODO
- Create an interface IModelClient with methods: GenerateTextAsync(prompt), EmbeddingsAsync(input), etc.
- Add two implementations: LocalModelClient (Docker Model Runner), AzureFoundryModelClient (wraps Azure AI Foundry SDK or HTTP)
- Ensure implementations read endpoints and keys from environment variables.
- Add unit tests that mock IModelClient to keep agents testable.
- Create sample prompts and expected responses in test fixtures.

Common/DTOs TODO
- Define request/response DTOs: MealPlanRequest, MealPlanResponse, InventorySnapshot, BudgetReport, ShoppingList
- Add JSON schema files in /test-fixtures/json/

Agents TODO (common across agents)
- Create a .NET minimal web API project for each agent (HTTP endpoints: /health, /generate, /process)
- Wire IModelClient via DI and expose endpoints for orchestrator & UI
- Add logging, metrics, and simple retry logic for model calls
- Add dockerfile for each agent and make them compose services

MealPlannerAgent TODO
- Endpoint: POST /plan (MealPlanRequest) -> MealPlanResponse
- Use IModelClient.GenerateTextAsync to get menu ideas
- Parse LLM response into structured MealPlanResponse
- Add tests with fixture: test-fixtures/json/mealplan-request.json and mealplan-response-sample.json

InventoryAgent TODO
- Endpoint: POST /inventory/check -> InventorySnapshot
- Accept JSON representing pantry items; return suggested recipes
- Add fixture: test-fixtures/json/inventory-sample.json

BudgetAgent TODO
- Endpoint: POST /budget/estimate -> BudgetReport
- Use price estimation table (JSON) for items during local testing
- Add fixture: test-fixtures/json/budget-request.json

ShopperAgent TODO
- Endpoint: POST /shopper/list -> ShoppingList
- Optimize list order (simple sort by aisle for local testing)
- Add fixture: test-fixtures/json/shopping-request.json

Orchestrator TODO
- Create a .NET web API that coordinates agent endpoints
- Workflow examples: generate meal plan -> get shopping list -> check inventory -> adjust plan -> produce final shopping list
- Provide endpoint: POST /orchestrate/weekly
- Implement feature flags to switch between local model client implementations

UI TODO
- Simple SPA (Blazor WebAssembly or ASP.NET Core Razor Pages) that calls Orchestrator endpoints
- Pages: Dashboard, Meal Planner, Inventory, Budget, Shopping
- Use sample JSON fixtures to populate UI in dev mode

Docker & Dev DX TODO
- Create docker-compose.yml that defines services for each agent, orchestrator, model-runner, and a simple UI
- Add docker-compose.override.yml for developer overrides
- Include sample commands for starting/stopping and seeding test data

Testing & CI TODO
- Unit tests per project with mocked IModelClient
- Integration tests using Docker Compose to spin up model runner and services; use test fixtures
- Add GitHub Actions workflow skeleton to run unit tests and build images

Sample test-fixtures/json files to add for local testing:
- mealplan-request.json
- inventory-sample.json
- budget-request.json
- shopping-request.json
- model-responses/mealplan-llm-response.json

Example dev commands
- Start everything:
  docker compose up --build
- Run individual agent:
  dotnet run --project src/Agents/MealPlannerAgent

If you want, I can create the actual .NET project files, Dockerfiles, docker-compose.yml, and sample JSON fixtures now. Which parts should I scaffold first? (Suggested: create IModelClient abstraction, one agent (MealPlannerAgent) minimal API, docker-compose, and test fixtures.)
