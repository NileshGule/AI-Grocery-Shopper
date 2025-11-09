using System.Collections.Generic;
    using System.Text.Json.Serialization;

namespace UI.Models
{
    public class OrchestrationResult
    {
        public List<string> Steps { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public MealPlanResponse? MealPlanResponse { get; set; }
        public BudgetResponse? BudgetResponse { get; set; }
        public InventoryResponse? InventoryResponse { get; set; }
        public ShopperResponse? ShopperResponse { get; set; }
    }

    public class MealPlanResponse
    {
        public List<MealDto> Meals { get; set; } = new();
    }

    public class MealDto
    {
        public string Name { get; set; }
        public List<string> Ingredients { get; set; } = new();
        public string Notes { get; set; }
    }

    public class BudgetResponse
    {
        public decimal TotalCost { get; set; }
        public List<string> Items { get; set; } = new();

        public string Note { get; set; } = string.Empty;
    }

    // public class ItemCost
    // {
    //     public string Name { get; set; }
    //     public decimal Cost { get; set; }
    // }

    public class InventoryResponse
    {
        public List<string> Available { get; set; } = new();
        public List<string> Missing { get; set; } = new();
    }

    public class ShopperResponse
    {
        public Dictionary<string, string> CategorizedItems { get; set; } = new();
        // // maps to the ShopperAgent output: { "categories": { "CategoryName": [ { "name": "...", "description": "..." }, ... ] } }
        // [JsonPropertyName("categories")]
        // public Dictionary<string, List<ShopItem>> Categories { get; set; } = new();

        // // Optional summary field if agents include one in future
        // [JsonPropertyName("summary")]
        // public string Summary { get; set; } = string.Empty;
    }

    public class ShopItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
