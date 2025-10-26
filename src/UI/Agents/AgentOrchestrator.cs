using System.Net.Http.Json;
using UI.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace UI.Agents
{
    public class AgentOrchestrator : IAgentOrchestrator
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;

        public AgentOrchestrator(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory = httpFactory;
            _config = config;
        }

        public async Task<OrchestrationResult> RunAsync(UserInput input)
        {
            var result = new OrchestrationResult();

            // 1. Meal Planner -> /plan
            var mealPlannerUrl = _config["AgentEndpoints:MealPlanner"];
            var mealClient = _httpFactory.CreateClient();
            mealClient.Timeout = TimeSpan.FromSeconds(10);
            try
            {
                Console.WriteLine("Calling MealPlanner...");
                var mpReq = new { Preferences = input.Description ?? "", Constraints = $"meals={input.NumberOfMeals}; preferences={string.Join(',', input.DietaryPreferences ?? new List<string>())}" };

                Console.WriteLine($"MealPlanner Request: {System.Text.Json.JsonSerializer.Serialize(mpReq)}");

                // Use retry helper to cope with compose startup ordering / transient DNS errors
                var mpResp = await PostWithRetriesAsync(mealClient, $"{mealPlannerUrl}/plan", mpReq, retries: 6, initialDelayMs: 1000);

                Console.WriteLine($"MealPlanner Response Status: {mpResp.StatusCode}");

                mpResp.EnsureSuccessStatusCode();
                var mealPlan = await mpResp.Content.ReadFromJsonAsync<MealPlanResponse>();
                result.MealPlanResponse = mealPlan;
                result.Steps.Add("MealPlanner returned meal plan");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"MealPlanner error: {ex.Message}");
                return result;
            }

            // 2. Budget Agent -> /budget
            var budgetUrl = _config["AgentEndpoints:BudgetAgent"];
            try
            {
                var budgetClient = _httpFactory.CreateClient();
                var budgetReq = new { Budget = input.Budget, Meals = result.MealPlanResponse?.Meals ?? new List<MealDto>() };
                var budgetResp = await budgetClient.PostAsJsonAsync($"{budgetUrl}/budget", budgetReq);
                budgetResp.EnsureSuccessStatusCode();
                var budget = await budgetResp.Content.ReadFromJsonAsync<BudgetResponse>();
                result.BudgetResponse = budget;
                result.Steps.Add("BudgetAgent returned cost estimate");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"BudgetAgent error: {ex.Message}");
                return result;
            }

            // 3. Inventory Agent -> /check
            var invUrl = _config["AgentEndpoints:InventoryAgent"];
            try
            {
                var invClient = _httpFactory.CreateClient();
                var invReq = new { Meals = result.MealPlanResponse?.Meals ?? new List<MealDto>() };
                var invResp = await invClient.PostAsJsonAsync($"{invUrl}/check", invReq);
                invResp.EnsureSuccessStatusCode();
                var inv = await invResp.Content.ReadFromJsonAsync<InventoryResponse>();
                result.InventoryResponse = inv;
                result.Steps.Add("InventoryAgent returned availability info");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"InventoryAgent error: {ex.Message}");
                return result;
            }

            // 4. Shopper Agent -> /prepare-shopping-list
            var shopUrl = _config["AgentEndpoints:ShopperAgent"];
            try
            {
                var shopClient = _httpFactory.CreateClient();
                var shopReq = new { Items = result.BudgetResponse?.Items?.Select(i => i.Name).ToArray() ?? new string[0], Budget = result.BudgetResponse?.TotalCost ?? 0m };
                var shopResp = await shopClient.PostAsJsonAsync($"{shopUrl}/prepare-shopping-list", shopReq);
                shopResp.EnsureSuccessStatusCode();
                var shop = await shopResp.Content.ReadFromJsonAsync<ShopperResponse>();
                result.ShopperResponse = shop;
                result.Steps.Add("ShopperAgent returned shopping list and summary");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"ShopperAgent error: {ex.Message}");
                return result;
            }

            return result;
        }

        // Simple retry helper for HTTP POST JSON calls. Retries on HttpRequestException or non-success status codes.
        private static async Task<HttpResponseMessage> PostWithRetriesAsync(HttpClient client, string url, object payload, int retries = 4, int initialDelayMs = 500)
        {
            if (retries < 1) retries = 1;
            Exception lastEx = null!;
            for (int attempt = 1; attempt <= retries; attempt++)
            {
                try
                {
                    var resp = await client.PostAsJsonAsync(url, payload);
                    // if success or client error (4xx) then return so caller can decide
                    if (resp.IsSuccessStatusCode) return resp;
                    // for server errors (5xx) we may retry
                    if ((int)resp.StatusCode >= 500 && attempt < retries)
                    {
                        await Task.Delay(initialDelayMs * attempt);
                        continue;
                    }
                    return resp;
                }
                catch (HttpRequestException ex)
                {
                    lastEx = ex;
                    if (attempt == retries) throw;
                    await Task.Delay(initialDelayMs * attempt);
                }
                catch (TaskCanceledException ex)
                {
                    // timeout
                    lastEx = ex;
                    if (attempt == retries) throw;
                    await Task.Delay(initialDelayMs * attempt);
                }
            }

            throw lastEx ?? new InvalidOperationException("PostWithRetriesAsync failed");
        }
    }
}
