import React from "react";
import { MealPlanResponse } from "../types";
interface MealPlanDisplayProps {
  mealPlan: MealPlanResponse | null;
}
export const MealPlanDisplay: React.FC<MealPlanDisplayProps> = ({
  mealPlan,
}) => {
  if (!mealPlan || !mealPlan.meals.length) return null;
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
    </div>
  );
};
