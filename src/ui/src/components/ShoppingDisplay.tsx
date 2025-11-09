import React from "react";
import { ShopperResponse } from "../types";
interface ShoppingDisplayProps {
  shopping: ShopperResponse | null;
}
export const ShoppingDisplay: React.FC<ShoppingDisplayProps> = ({
  shopping,
}) => {
  if (!shopping || !shopping.categorizedItems) return null;
  const items = Object.entries(shopping.categorizedItems);
  
  if (items.length === 0) return null;

  return (
    <div className="mb-4">
      {" "}
      <h2 className="h5 mb-3 text-primary">
        {" "}
        <i className="bi bi-cart me-2"></i> Shopping List{" "}
      </h2>{" "}
      <div className="card shadow-sm">
        {" "}
        <div className="card-body">
          {" "}
          <ul className="list-group list-group-flush">
            {" "}
            {items.map(([ingredient, description], index) => (
              <li key={index} className="list-group-item d-flex justify-content-between align-items-center">
                {" "}
                <div>
                  {" "}
                  <i className="bi bi-cart-check text-success me-2"></i>
                  <strong className="text-capitalize">{ingredient}:</strong>{" "}
                  <span className="text-muted">{description}</span>
                </div>{" "}
              </li>
            ))}{" "}
          </ul>{" "}
        </div>{" "}
      </div>{" "}
    </div>
  );
};
