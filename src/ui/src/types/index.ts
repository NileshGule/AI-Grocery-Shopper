export interface GroceryInput {
  description: string;
  numberOfMeals: number;
  dietaryPreferences: string[];
  budget: number;
}
export interface Meal {
  name: string;
  ingredients: string[];
  notes: string;
}
export interface MealPlanResponse {
  meals: Meal[];
}
export interface InventoryResponse {
  available: string[];
  missing: string[];
}
export interface BudgetResponse {
  totalCost: number;
  items: string[];
}
export interface ShopperResponse {
  categorizedItems: { [key: string]: string };
}
export interface GroceryResult {
  steps: string[];
  mealPlanResponse: MealPlanResponse | null;
  inventoryResponse: InventoryResponse | null;
  budgetResponse: BudgetResponse | null;
  shopperResponse: ShopperResponse | null;
  errors: string[];
}
