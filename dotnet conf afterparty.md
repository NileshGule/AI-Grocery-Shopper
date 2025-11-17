## Install Dotnet 10 runtime & SDK

```winget

winget install dotnet-runtime-10

winget install dotnet-sdk-10

```

## Server hosted agents

```code

dotnet new install Microsoft.Agents.AI.ProjectTemplates

dotnet new aiagent-webapi -o MyFirstAIAgentWebApi

dotnet user-secrets set "AI_FOUNDRY_TOKEN" "your-aifoundry-models-token-here"

```
