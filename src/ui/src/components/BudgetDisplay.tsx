import React, { useState } from "react";
import { BudgetResponse, ShopperResponse } from "../types";
import { ShoppingDisplay } from "./ShoppingDisplay";

interface BudgetDisplayProps {
  budget: BudgetResponse | null;
  requestedBudget: number;
  onStepComplete?: (step: string) => void;
}
export const BudgetDisplay: React.FC<BudgetDisplayProps> = ({
  budget,
  requestedBudget,
  onStepComplete,
}) => {
  const [shoppingResponse, setShoppingResponse] = useState<ShopperResponse | null>(null);
  const [isPreparingList, setIsPreparingList] = useState(false);
  const [shoppingError, setShoppingError] = useState<string | null>(null);
  const [shoppingListCompleted, setShoppingListCompleted] = useState(false);

  if (!budget) return null;

  const isOverBudget = budget.totalCost > requestedBudget;
  const percentageUsed = (budget.totalCost / requestedBudget) * 100;

  const handlePrepareShoppingList = async () => {
    if (budget.items.length === 0) {
      setShoppingError("No items to prepare shopping list for.");
      return;
    }

    setIsPreparingList(true);
    setShoppingError(null);
    setShoppingListCompleted(false);
    
    try {
      const response = await fetch('http://localhost:5004/prepare-shopping-list', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ items: budget.items }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data: ShopperResponse = await response.json();
      setShoppingResponse(data);
      setShoppingListCompleted(true);
      
      // Update progress steps
      if (onStepComplete) {
        onStepComplete("Shopping list prepared successfully");
      }
    } catch (error) {
      console.error('Error preparing shopping list:', error);
      setShoppingError(error instanceof Error ? error.message : 'Failed to prepare shopping list');
    } finally {
      setIsPreparingList(false);
    }
  };

  return (
    <div className="mb-4">
      {" "}
      <h2 className="h5 mb-3 text-primary">
        {" "}
        <i className="bi bi-cash-stack me-2"></i> Budget Analysis{" "}
      </h2>{" "}
      <div className="card shadow-sm">
        {" "}
        <div className="card-body">
          {" "}
          <div className="row align-items-center mb-3">
            {" "}
            <div className="col-md-6">
              {" "}
              <div className="d-flex justify-content-between align-items-center mb-2">
                {" "}
                <span className="text-muted">Requested Budget:</span>{" "}
                <span className="fw-bold">${requestedBudget.toFixed(2)}</span>{" "}
              </div>{" "}
              <div className="d-flex justify-content-between align-items-center mb-2">
                {" "}
                <span className="text-muted">Estimated Total:</span>{" "}
                <span
                  className={`fw-bold fs-4 ${
                    isOverBudget ? "text-danger" : "text-success"
                  }`}
                >
                  {" "}
                  ${budget.totalCost.toFixed(2)}{" "}
                </span>{" "}
              </div>{" "}
              <div className="progress" style={{ height: "25px" }}>
                {" "}
                <div
                  className={`progress-bar ${
                    isOverBudget ? "bg-danger" : "bg-success"
                  }`}
                  role="progressbar"
                  style={{ width: `${Math.min(percentageUsed, 100)}%` }}
                  aria-valuenow={percentageUsed}
                  aria-valuemin={0}
                  aria-valuemax={100}
                >
                  {" "}
                  {percentageUsed.toFixed(0)}%{" "}
                </div>{" "}
              </div>{" "}
              {isOverBudget && (
                <div className="alert alert-danger mt-3 mb-0 small">
                  {" "}
                  <i className="bi bi-exclamation-triangle-fill me-2"></i> Over
                  budget by ${(budget.totalCost - requestedBudget).toFixed(2)}{" "}
                </div>
              )}{" "}
            </div>{" "}
            <div className="col-md-6">
              {" "}
              <h6 className="text-muted mb-2">Item Breakdown:</h6>{" "}
              <ul className="list-group list-group-flush small">
                {" "}
                {budget.items.map((item, idx) => (
                  <li key={idx} className="list-group-item px-0">
                    {" "}
                    <i className="bi bi-dot me-1"></i> {item}{" "}
                  </li>
                ))}{" "}
              </ul>{" "}
            </div>{" "}
          </div>{" "}
        </div>{" "}
      </div>{" "}

      <div className="mt-4 text-center">
        <button 
          className="btn btn-info text-white"
          onClick={handlePrepareShoppingList}
          disabled={isPreparingList || budget.items.length === 0}
        >
          {isPreparingList ? (
            <>
              <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
              Preparing Shopping List...
            </>
          ) : (
            <>
              <i className="bi bi-cart-plus me-2"></i>
              Prepare Shopping List
            </>
          )}
        </button>
      </div>

      {shoppingListCompleted && !shoppingError && (
        <div className="alert alert-success mt-3 shadow-sm" role="alert">
          <i className="bi bi-check-circle-fill me-2"></i>
          <strong>Success!</strong> Shopping list has been prepared successfully.
        </div>
      )}

      {shoppingError && (
        <div className="alert alert-danger mt-3" role="alert">
          <i className="bi bi-exclamation-triangle me-2"></i>
          Error: {shoppingError}
        </div>
      )}

      {shoppingResponse && (
        <div className="mt-4">
          <ShoppingDisplay shopping={shoppingResponse} />
        </div>
      )}
    </div>
  );
};
