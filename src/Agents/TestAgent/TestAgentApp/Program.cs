using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

var endpoint = "https://ai-foundry-ai-hub.openai.azure.com/";
var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_OPENAI_APIKEY");
var model = "gpt-4.1-mini";


AIAgent agent = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureCliCredential())
        .GetChatClient(model)
        .CreateAIAgent(instructions: "You are good at telling jokes.", name: "Joker");

Console.WriteLine(await agent.RunAsync("Tell me a joke about Alibaba and the 40 Thieves."));