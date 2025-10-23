using System.Net.Http.Json;
using UI.Models;
using Microsoft.Extensions.Configuration;

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
            try
            {
                var mpReq = new { Preferences = input.Description ?? "", Constraints = $"meals={input.NumberOfMeals}; prefs={string.Join(',', input.DietaryPreferences ?? new List<string>())}" };
                var mpResp = await mealClient.PostAsJsonAsync($"{mealPlannerUrl}/plan", mpReq);
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
    }
}
