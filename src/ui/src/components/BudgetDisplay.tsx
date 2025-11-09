import React from "react";
import { BudgetResponse } from "../types";
interface BudgetDisplayProps {
  budget: BudgetResponse | null;
  requestedBudget: number;
}
export const BudgetDisplay: React.FC<BudgetDisplayProps> = ({
  budget,
  requestedBudget,
}) => {
  if (!budget) return null;
  const isOverBudget = budget.totalCost > requestedBudget;
  const percentageUsed = (budget.totalCost / requestedBudget) * 100;
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
    </div>
  );
};
