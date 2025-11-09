import { GroceryInput, GroceryResult } from '../types';

const API_BASE_URL = 'http://localhost:5003';

export const groceryApi = {
  async generateMealPlan(input: GroceryInput): Promise<GroceryResult> {
    // Convert GroceryInput to MealPlanRequest format
    const preferences = `${input.description}. Number of meals: ${input.numberOfMeals}. Budget: $${input.budget}`;
    const constraints = input.dietaryPreferences.join(', ');

    const requestBody = {
      Preferences: preferences,
      Constraints: constraints
    };

    const response = await fetch(`${API_BASE_URL}/plan`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',        
      },
      body: JSON.stringify(requestBody),
    });

    if (!response.ok) {
      throw new Error(`Failed to generate meal plan: ${response.statusText}`);
    }

    const apiResponse = await response.json();
    console.log('Raw API Response:', apiResponse);

    // Check if response has meals property or is direct array
    const meals = Array.isArray(apiResponse) ? apiResponse : apiResponse.meals || [];

    const result: GroceryResult = {
      steps: ['Generated meal plan successfully'],
      mealPlanResponse: {
        meals: meals
      },
      inventoryResponse: null,
      budgetResponse: null,
      shopperResponse: null,
      errors: []
    };

    return result;
  },
};