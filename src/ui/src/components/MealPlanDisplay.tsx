import React, { useState } from "react";
import { MealPlanResponse, InventoryResponse } from "../types";
import { InventoryDisplay } from "./InventoryDisplay";

interface MealPlanDisplayProps {
  mealPlan: MealPlanResponse | null;
  budget: number;
  onStepComplete?: (step: string) => void;
}
export const MealPlanDisplay: React.FC<MealPlanDisplayProps> = ({
  mealPlan,
  budget,
  onStepComplete,
}) => {
  const [inventoryResponse, setInventoryResponse] = useState<InventoryResponse | null>(null);
  const [isCheckingInventory, setIsCheckingInventory] = useState(false);
  const [inventoryError, setInventoryError] = useState<string | null>(null);
  const [inventoryCheckedSuccess, setInventoryCheckedSuccess] = useState(false);

  if (!mealPlan || !mealPlan.meals.length) return null;

  // Extract unique ingredients from all meals
  const getUniqueIngredients = (): string[] => {
    const allIngredients = mealPlan.meals.flatMap(meal => meal.ingredients);
    return Array.from(new Set(allIngredients));
  };

  const handleCheckInventory = async () => {
    setIsCheckingInventory(true);
    setInventoryError(null);
    setInventoryCheckedSuccess(false);
    
    try {
      const uniqueIngredients = getUniqueIngredients();
      const response = await fetch('http://localhost:5002/inventory-check', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ Items: uniqueIngredients }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data: InventoryResponse = await response.json();
      setInventoryResponse(data);
      setInventoryCheckedSuccess(true);
      
      // Add step to Processing Steps
      if (onStepComplete) {
        const availableCount = data.available.length;
        const missingCount = data.missing.length;
        onStepComplete(`Inventory checked: ${availableCount} item(s) available, ${missingCount} item(s) missing`);
      }
    } catch (error) {
      console.error('Error checking inventory:', error);
      setInventoryError(error instanceof Error ? error.message : 'Failed to check inventory');
    } finally {
      setIsCheckingInventory(false);
    }
  };

  return (
    <div className="mb-4">
      {" "}
      <h2 className="h5 mb-3 text-primary">
        {" "}
        <i className="bi bi-calendar-check me-2"></i> Meal Plan{" "}
      </h2>{" "}
      <div className="row g-3">
        {" "}
        {mealPlan.meals.map((meal, index) => (
          <div key={index} className="col-12 col-md-6 col-lg-4">
            {" "}
            <div className="card h-100 shadow-sm">
              {" "}
              <div className="card-body">
                {" "}
                <h5 className="card-title text-success">{meal.name}</h5>{" "}
                <h6 className="card-subtitle mb-2 text-muted small">
                  Ingredients:
                </h6>{" "}
                <ul className="list-unstyled small mb-3">
                  {" "}
                  {meal.ingredients.map((ingredient, idx) => (
                    <li key={idx} className="mb-1">
                      {" "}
                      <i className="bi bi-check-circle text-success me-2"></i>{" "}
                      {ingredient}{" "}
                    </li>
                  ))}{" "}
                </ul>{" "}
                {meal.notes && (
                  <div className="alert alert-light small mb-0">
                    {" "}
                    <strong>Notes:</strong> {meal.notes}{" "}
                  </div>
                )}{" "}
              </div>{" "}
            </div>{" "}
          </div>
        ))}{" "}
      </div>{" "}
      
      <div className="mt-4 text-center">
        <button 
          className="btn btn-primary"
          onClick={handleCheckInventory}
          disabled={isCheckingInventory}
        >
          {isCheckingInventory ? (
            <>
              <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
              Checking Inventory...
            </>
          ) : (
            <>
              <i className="bi bi-box-seam me-2"></i>
              Check Inventory
            </>
          )}
        </button>
      </div>

      {inventoryCheckedSuccess && (
        <div className="alert alert-success mt-3" role="alert">
          <i className="bi bi-check-circle-fill me-2"></i>
          <strong>Success!</strong> Inventory has been checked successfully. 
          {inventoryResponse && (
            <span> Found {inventoryResponse.available.length} available item(s) and {inventoryResponse.missing.length} missing item(s).</span>
          )}
        </div>
      )}

      {inventoryError && (
        <div className="alert alert-danger mt-3" role="alert">
          <i className="bi bi-exclamation-triangle me-2"></i>
          Error: {inventoryError}
        </div>
      )}

      {inventoryResponse && (
        <div className="mt-4">
          <InventoryDisplay inventory={inventoryResponse} budget={budget} />
        </div>
      )}
    </div>
  );
};
