import React from "react";
import { ShopperResponse } from "../types";
interface ShoppingDisplayProps {
  shopping: ShopperResponse | null;
}
export const ShoppingDisplay: React.FC<ShoppingDisplayProps> = ({
  shopping,
}) => {
  if (!shopping || !shopping.categorizedItems) return null;
  const categories = Object.entries(shopping.categorizedItems);
  return (
    <div className="mb-4">
      {" "}
      <h2 className="h5 mb-3 text-primary">
        {" "}
        <i className="bi bi-cart me-2"></i> Shopping List{" "}
      </h2>{" "}
      <div className="accordion" id="shoppingAccordion">
        {" "}
        {categories.map(([category, items], index) => (
          <div key={index} className="accordion-item">
            {" "}
            <h2 className="accordion-header">
              {" "}
              <button
                className={`accordion-button ${index !== 0 ? "collapsed" : ""}`}
                type="button"
                data-bs-toggle="collapse"
                data-bs-target={`#collapse${index}`}
                aria-expanded={index === 0}
                aria-controls={`collapse${index}`}
              >
                {" "}
                <i className="bi bi-basket me-2"></i>{" "}
                <strong>{category}</strong>{" "}
              </button>{" "}
            </h2>{" "}
            <div
              id={`collapse${index}`}
              className={`accordion-collapse collapse ${
                index === 0 ? "show" : ""
              }`}
              data-bs-parent="#shoppingAccordion"
            >
              {" "}
              <div className="accordion-body">
                {" "}
                <div className="card bg-light">
                  {" "}
                  <div className="card-body"> {items} </div>{" "}
                </div>{" "}
              </div>{" "}
            </div>{" "}
          </div>
        ))}{" "}
      </div>{" "}
    </div>
  );
};
