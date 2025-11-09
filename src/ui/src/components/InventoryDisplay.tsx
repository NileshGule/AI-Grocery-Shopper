import React, { useState } from "react";
import { InventoryResponse, BudgetResponse } from "../types";
import { BudgetDisplay } from "./BudgetDisplay";

interface InventoryDisplayProps {
  inventory: InventoryResponse | null;
  budget: number;
  onStepComplete?: (step: string) => void;
}
export const InventoryDisplay: React.FC<InventoryDisplayProps> = ({
  inventory,
  budget,
  onStepComplete,
}) => {
  const [budgetResponse, setBudgetResponse] = useState<BudgetResponse | null>(null);
  const [isCheckingBudget, setIsCheckingBudget] = useState(false);
  const [budgetError, setBudgetError] = useState<string | null>(null);
  const [showSuccessIndicator, setShowSuccessIndicator] = useState(false);

  if (!inventory) return null;

  const handleCheckBudget = async () => {
    if (inventory.missing.length === 0) {
      setBudgetError("No missing items to check budget for.");
      return;
    }

    setIsCheckingBudget(true);
    setBudgetError(null);
    setShowSuccessIndicator(false);
    
    try {
      const response = await fetch('http://localhost:5001/check-budget', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ Items: inventory.missing, Budget: budget }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data: BudgetResponse = await response.json();
      setBudgetResponse(data);
      setShowSuccessIndicator(true);
      
      // Update steps progress to indicate completion
      if (onStepComplete) {
        onStepComplete(`Budget check completed: ${inventory.missing.length} missing items analyzed - Total cost: $${data.totalCost.toFixed(2)} of $${budget} budget`);
      }
      
      // Hide success indicator after 3 seconds
      setTimeout(() => {
        setShowSuccessIndicator(false);
      }, 3000);
    } catch (error) {
      console.error('Error checking budget:', error);
      setBudgetError(error instanceof Error ? error.message : 'Failed to check budget');
    } finally {
      setIsCheckingBudget(false);
    }
  };

  return (
    <div className="mb-4">
      {" "}
      <h2 className="h5 mb-3 text-primary">
        {" "}
        <i className="bi bi-box-seam me-2"></i> Inventory{" "}
      </h2>{" "}
      <div className="row g-3">
        {" "}
        <div className="col-12 col-md-6">
          {" "}
          <div className="card shadow-sm border-success">
            {" "}
            <div className="card-body">
              {" "}
              <h6 className="card-subtitle mb-3 text-success">
                {" "}
                <i className="bi bi-check-circle-fill me-2"></i> Available (
                {inventory.available.length}){" "}
              </h6>{" "}
              {inventory.available.length > 0 ? (
                <div className="d-flex flex-wrap gap-2">
                  {" "}
                  {inventory.available.map((item, idx) => (
                    <span
                      key={idx}
                      className="badge bg-success-subtle text-success border border-success"
                    >
                      {" "}
                      {item}{" "}
                    </span>
                  ))}{" "}
                </div>
              ) : (
                <p className="text-muted small mb-0">No items available</p>
              )}{" "}
            </div>{" "}
          </div>{" "}
        </div>{" "}
        <div className="col-12 col-md-6">
          {" "}
          <div className="card shadow-sm border-warning">
            {" "}
            <div className="card-body">
              {" "}
              <h6 className="card-subtitle mb-3 text-warning">
                {" "}
                <i className="bi bi-exclamation-circle-fill me-2"></i> Missing (
                {inventory.missing.length}){" "}
              </h6>{" "}
              {inventory.missing.length > 0 ? (
                <div className="d-flex flex-wrap gap-2">
                  {" "}
                  {inventory.missing.map((item, idx) => (
                    <span
                      key={idx}
                      className="badge bg-warning-subtle text-warning border border-warning"
                    >
                      {" "}
                      {item}{" "}
                    </span>
                  ))}{" "}
                </div>
              ) : (
                <p className="text-muted small mb-0">All items available!</p>
              )}{" "}
            </div>{" "}
          </div>{" "}
        </div>{" "}
      </div>{" "}

      <div className="mt-4 text-center">
        <button 
          className="btn btn-success"
          onClick={handleCheckBudget}
          disabled={isCheckingBudget || inventory.missing.length === 0}
        >
          {isCheckingBudget ? (
            <>
              <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
              Checking Budget...
            </>
          ) : (
            <>
              <i className="bi bi-cash-stack me-2"></i>
              Check Budget for Missing Items
            </>
          )}
        </button>
        
        {showSuccessIndicator && (
          <div className="alert alert-success mt-3 d-inline-flex align-items-center animate__animated animate__fadeIn" role="alert">
            <i className="bi bi-check-circle-fill me-2"></i>
            Budget check completed successfully!
          </div>
        )}
      </div>

      {budgetError && (
        <div className="alert alert-danger mt-3" role="alert">
          <i className="bi bi-exclamation-triangle me-2"></i>
          Error: {budgetError}
        </div>
      )}

      {budgetResponse && (
        <div className="mt-4">
          <BudgetDisplay budget={budgetResponse} requestedBudget={budget} />
        </div>
      )}
    </div>
  );
};
