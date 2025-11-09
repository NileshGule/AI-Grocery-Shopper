import React, { useState } from 'react';
import { GroceryForm } from './components/GroceryForm';
import { MealPlanDisplay } from './components/MealPlanDisplay';
import { InventoryDisplay } from './components/InventoryDisplay';
import { BudgetDisplay } from './components/BudgetDisplay';
import { ShoppingDisplay } from './components/ShoppingDisplay';
import { groceryApi } from './services/api';
import { GroceryInput, GroceryResult } from './types';
import './App.css';

function App() {
  const [result, setResult] = useState<GroceryResult | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [requestedBudget, setRequestedBudget] = useState(0);

  const handleSubmit = async (input: GroceryInput) => {
    setIsLoading(true);
    setRequestedBudget(input.budget);
    try {
      const data = await groceryApi.generateMealPlan(input);
      setResult(data);
    } catch (error) {
      console.error('Error generating meal plan:', error);
      setResult({
        steps: [],
        mealPlanResponse: null,
        inventoryResponse: null,
        budgetResponse: null,
        shopperResponse: null,
        errors: ['Failed to generate meal plan. Please try again.'],
      });
    } finally {
      setIsLoading(false);
    }
  };

  const addStep = (step: string) => {
    if (result) {
      setResult({
        ...result,
        steps: [...result.steps, step],
      });
    }
  };

  return (
    <div className="container py-4">
      <div className="d-flex align-items-center justify-content-between mb-4">
        <h1 className="h3 mb-0">
          <i className="bi bi-cart-check text-success me-2"></i>
          AI Grocery Shopper
        </h1>
      </div>

      <GroceryForm onSubmit={handleSubmit} isLoading={isLoading} />

      {result && (
        <div className="result-section">
          {result.steps.length > 0 && (
            <div className="card p-3 mb-4 shadow-sm">
              <h2 className="h5 mb-3 text-primary">
                <i className="bi bi-list-check me-2"></i>
                Processing Steps
              </h2>
              <ol className="mb-0">
                {result.steps.map((step, idx) => (
                  <li key={idx} className="mb-2">
                    {step}
                  </li>
                ))}
              </ol>
            </div>
          )}

          <MealPlanDisplay mealPlan={result.mealPlanResponse} budget={requestedBudget} onStepComplete={addStep} />
          <InventoryDisplay inventory={result.inventoryResponse} budget={requestedBudget} />
          <BudgetDisplay budget={result.budgetResponse} requestedBudget={requestedBudget} />
          <ShoppingDisplay shopping={result.shopperResponse} />

          {result.errors.length > 0 && (
            <div className="alert alert-danger shadow-sm">
              <h5 className="alert-heading">
                <i className="bi bi-exclamation-triangle-fill me-2"></i>
                Errors
              </h5>
              <ul className="mb-0">
                {result.errors.map((error, idx) => (
                  <li key={idx}>{error}</li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

export default App;
