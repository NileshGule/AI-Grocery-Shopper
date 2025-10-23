using System.Collections.Generic;

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
        public List<ItemCost> Items { get; set; } = new();
    }

    public class ItemCost
    {
        public string Name { get; set; }
        public decimal Cost { get; set; }
    }

    public class InventoryResponse
    {
        public List<string> Available { get; set; } = new();
        public List<string> Missing { get; set; } = new();
    }

    public class ShopperResponse
    {
        public List<string> ShoppingList { get; set; } = new();
        public string Summary { get; set; }
    }
}
