// using OpenAI.Chat;
// using Azure;
// using Azure.AI.OpenAI;

// var endpoint = new Uri("https://ai-foundry-ai-hub.cognitiveservices.azure.com/");
// var deploymentName = "gpt-4.1-mini";
// var apiKey = "ECPNp5Z1PN7rh9cK4y28mxVNovrXQrFN0SdmOQoPXdqhGZUBEHmEJQQJ99BHACYeBjFXJ3w3AAAAACOG5iIT";

// AzureOpenAIClient azureClient = new(
//     endpoint,
//     new AzureKeyCredential(apiKey));
// ChatClient chatClient = azureClient.GetChatClient(deploymentName);

// // var requestOptions = new ChatCompletionOptions()
// // {
// //     MaxCompletionTokens = 13107,
// //     Temperature = 1.0f,
// //     TopP = 1.0f,
// //     FrequencyPenalty = 0.0f,
// //     PresencePenalty = 0.0f,

// // };

// List<ChatMessage> messages = new List<ChatMessage>()
// {
//     new SystemChatMessage("You are a helpful assistant."),
//     new UserChatMessage("I am going to Paris, what should I see?"),
// };

// var response = chatClient.CompleteChat(messages);
// System.Console.WriteLine(response.Value.Content[0].Text);

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