# AI Grocery Shopper

AI Grocery Shopper is a modular C# application that helps users plan meals, manage pantry inventory, track grocery budgets, and automate shopping tasks using local LLMs. The system is composed of multiple independent agents coordinated by an orchestrator and exposed through a user interface. Local model hosting can be done either via Azure AI Foundry or a Docker-based Model Runner to keep development fast and private.

## Key Features

- Meal Planner Agent: Generate weekly meal plans and shopping lists based on preferences and dietary constraints.
- Inventory Checker Agent: Track pantry items, expiration dates, and suggest recipes using available ingredients.
- Budget Agent: Monitor and optimize grocery budgets, provide cost estimates and alerts.
- Shopper Agent: Produce optimized shopping routes/lists and integrate with online stores or local stores (simulated for local development).
- Orchestrator: Coordinates agent workflows and resolves multi-agent decisions.
- UI: Simple web or desktop front-end (configurable) for user interaction.

## Architecture Overview

- Language: C# (.NET)
- Agents: Independent services / components implementing single responsibilities. Communicate with the orchestrator via lightweight messaging or HTTP.
- Models: Local LLMs hosted with either:
  - Azure AI Foundry (local/private deployment), or
  - Docker Model Runner (containerized models)
- Developer DX: Docker Compose to bring up agent services, model runner, and a local UI quickly.

## Tech Stack

- .NET (C#)
- Docker & Docker Compose
- Local LLM hosting: Azure AI Foundry or Docker Model Runner
- (Optional) SQLite / other lightweight DB for local persistence
- REST / gRPC for inter-service communication

## Getting Started (Developer)

Prerequisites
- .NET 8+ SDK installed
- Docker & Docker Compose installed
- (Optional) Azure AI Foundry access/credentials if using that hosting option

Local development with Docker Compose

1. Configure environment variables in `.env` or `docker-compose.override.yml`:

   - AZURE_FOUNDRY_ENDPOINT - API / local endpoint if using Azure AI Foundry
   - AZURE_API_KEY - credentials for Azure AI Foundry (if required)
   - MODEL_RUNNER_URL - URL for Docker Model Runner (if used)

2. Start all services:

   ```bash
   docker compose up --build
   ```

3. Open the UI at http://localhost:3000 (or configured port) and interact with the agents.

Run without Docker

- Start model runner or provide a running model endpoint, then start each C# service with:

  ```bash
  dotnet run --project src/Orchestrator
  dotnet run --project src/MealPlannerAgent
  # ... other services
  ```

## Configuration

- Services read endpoints / keys from environment variables. Provide either AZURE_* variables or MODEL_RUNNER_* variables depending on the hosting option.
- Use configuration files per environment (development, staging, production) to toggle which model host to call.

## Development Notes

- Keep agents independent and side-effect-free where possible to make testing easier.
- Orchestrator should implement retry/backoff and request/response correlation when calling agents.
- When switching between Azure AI Foundry and Docker Model Runner, abstract model client behind an interface to keep the agents unchanged.

## Testing

- Unit tests for each agent (mock the model client and external dependencies).
- Integration tests with a local model runner container and a test DB.

## Contributing

- Follow the repository's branching and PR policies.
- Add tests for new features and update docs.

## License

This project is distributed under the terms of the repository LICENSE file.

## Bootstrapping the project (scaffolded)

I created a starter scaffold under `src/` and test fixtures to help you iterate iteratively. The key additions are:

- `src/Common/ModelClient/IModelClient.cs` (abstraction for model hosts)
- `src/Common/ModelClient/LocalModelClient.cs` (dummy local implementation)
- `src/Agents/MealPlannerAgent/Program.cs` (minimal API that uses IModelClient)
- `docker-compose.yml` (development compose for agents, orchestrator, model-runner and UI)
- `src/test-fixtures/json/*` (sample JSON files for local testing)

## Project structure (current scaffold)

```
src/
  Common/
    ModelClient/
      IModelClient.cs
      LocalModelClient.cs
  Agents/
    MealPlannerAgent/
      Program.cs
    InventoryAgent/
    BudgetAgent/
    ShopperAgent/
  Orchestrator/
  UI/
  test-fixtures/
    json/
```

## Per-project TODO checklists

**Common/ModelClient**
- [ ] Finalize `IModelClient` methods for your usage (chat, single-turn generation, embeddings).
- [ ] Implement `AzureFoundryModelClient` wrapping the Foundry API or HTTP endpoints.
- [ ] Add configuration reading (endpoints, apiKey) from environment or config provider.
- [ ] Add unit tests that mock `IModelClient`.

**Common/DTOs**
- [ ] Add shared DTOs and serializers for requests/responses between agents and orchestrator.
- [ ] Add JSON schema files for validation during development.

**Agents (each)**
- [ ] Convert agent folder into a .NET Web API project (dotnet new webapi)
- [ ] Add endpoints: `/health`, `/process` or domain-specific endpoints.
- [ ] Use `IModelClient` via DI.
- [ ] Add Dockerfile and update `docker-compose.yml`.
- [ ] Add unit tests and integration tests using `test-fixtures`.

**MealPlannerAgent specific**
- [ ] Parse `mealplan-request.json` and return a structured `MealPlanResponse`.
- [ ] Add logic to validate constraints (allergies, diets).
- [ ] Add sample parsing tests using `src/test-fixtures/json/mealplan-request.json`.

**InventoryAgent specific**
- [ ] Implement ingestion endpoint for inventory snapshots.
- [ ] Suggest recipes from inventory using LLM and local rules.
- [ ] Add tests using `src/test-fixtures/json/inventory-sample.json`.

**BudgetAgent specific**
- [ ] Implement price estimation using a local price table (JSON) for dev.
- [ ] Add endpoint to report budget impact and suggestions.
- [ ] Add tests using `src/test-fixtures/json/budget-request.json`.

**ShopperAgent specific**
- [ ] Implement shopping list formatter and simple optimizer (group by aisle).
- [ ] Add endpoint and tests using `src/test-fixtures/json/shopping-request.json`.

**Orchestrator**
- [ ] Implement orchestration flows that call agents and aggregate results.
- [ ] Add feature flags or configuration to switch `IModelClient` implementations.
- [ ] Add integration tests that spin up agents (or use mocks) and exercise full workflows.

**UI**
- [ ] Decide UI technology (Blazor, React, Razor Pages). Blazor (wasm) works well with .NET backend.
- [ ] Create pages for Meal Planner, Inventory, Budget, Shopper.
- [ ] In dev mode, load `test-fixtures` JSON to simulate responses.

**Docker & Dev DX**
- [ ] Finalize Dockerfiles for each service.
- [ ] Use `docker-compose.override.yml` for local volumes, ports, and mounting source for fast iteration.
- [ ] Provide `seed` commands or endpoints to load test fixtures into services.

**Testing & CI**
- [ ] Unit tests: mock `IModelClient` and external dependencies.
- [ ] Integration tests: use Docker Compose to create a test environment and run workflows with fixtures.
- [ ] GitHub Actions: build, run unit tests, and optionally run integration tests using a self-hosted runner with Docker.

## How to run the scaffold locally (dev)

1. Build and run using Docker Compose:

   ```bash
   docker compose up --build
   ```

2. Call the Meal Planner endpoint with the fixture:

   ```bash
   curl -X POST http://localhost:5001/plan \
     -H "Content-Type: application/json" \
     -d @src/test-fixtures/json/mealplan-request.json
   ```

   You should get a simple echoed response from `LocalModelClient`.

3. Run a single agent locally without Docker (for faster code iteration):

   ```bash
   dotnet run --project src/Agents/MealPlannerAgent
   ```

4. Replace `LocalModelClient` with `AzureFoundryModelClient` in DI when you're ready for real models.

## Next steps I can scaffold for you

- Create .NET solution and project files for each agent + orchestrator + common library.
- Add Dockerfiles per service and improve `docker-compose.yml` with build contexts and healthchecks.
- Implement `AzureFoundryModelClient` stub and a config loader.
- Scaffold UI (Blazor WASM) with a basic Dashboard page.

Which of the above would you like me to scaffold next? (Recommended: create solution + MealPlannerAgent project, Common library, Dockerfile for MealPlannerAgent, and the orchestrator project.)
