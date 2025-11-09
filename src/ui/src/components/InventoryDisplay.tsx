import React from "react";
import { InventoryResponse } from "../types";
interface InventoryDisplayProps {
  inventory: InventoryResponse | null;
}
export const InventoryDisplay: React.FC<InventoryDisplayProps> = ({
  inventory,
}) => {
  if (!inventory) return null;
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
    </div>
  );
};
