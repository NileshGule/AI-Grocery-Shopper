import React, { useState } from "react";
import { GroceryInput } from "../types";
interface GroceryFormProps {
  onSubmit: (input: GroceryInput) => void;
  isLoading: boolean;
}
export const GroceryForm: React.FC<GroceryFormProps> = ({
  onSubmit,
  isLoading,
}) => {
  const [formData, setFormData] = useState<GroceryInput>({
    description: "",
    numberOfMeals: 3,
    dietaryPreferences: [],
    budget: 25,
  });
  const [preferencesRaw, setPreferencesRaw] = useState("");
  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const preferences = preferencesRaw
      .split(",")
      .map((p) => p.trim())
      .filter((p) => p.length > 0);
    onSubmit({ ...formData, dietaryPreferences: preferences });
  };
  const handleReset = () => {
    setFormData({
      description: "",
      numberOfMeals: 3,
      dietaryPreferences: [],
      budget: 25,
    });
    setPreferencesRaw("");
  };
  return (
    <div className="card p-4 mb-4 shadow-sm">
      {" "}
      <form onSubmit={handleSubmit}>
        {" "}
        <div className="table-responsive">
          {" "}
          <table className="table table-borderless align-middle">
            {" "}
            <tbody>
              {" "}
              <tr>
                {" "}
                <th className="text-muted fw-bold" style={{ width: "25%" }}>
                  {" "}
                  Meal description{" "}
                </th>{" "}
                <td>
                  {" "}
                  <textarea
                    className="form-control"
                    rows={3}
                    placeholder="Help me prepare 3 meals over Christmas weekend for family gathering of 10 people"
                    value={formData.description}
                    onChange={(e) =>
                      setFormData({ ...formData, description: e.target.value })
                    }
                    required
                  />{" "}
                </td>{" "}
              </tr>{" "}
              <tr>
                {" "}
                <th className="text-muted fw-bold">Number of meals</th>{" "}
                <td>
                  {" "}
                  <input
                    className="form-control"
                    type="number"
                    min="1"
                    value={formData.numberOfMeals}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        numberOfMeals: parseInt(e.target.value),
                      })
                    }
                    required
                  />{" "}
                </td>{" "}
              </tr>{" "}
              <tr>
                {" "}
                <th className="text-muted fw-bold">Dietary preferences</th>{" "}
                <td>
                  {" "}
                  <input
                    className="form-control"
                    placeholder="e.g. vegetarian, nut-free"
                    value={preferencesRaw}
                    onChange={(e) => setPreferencesRaw(e.target.value)}
                  />{" "}
                  <small className="text-muted">
                    Separate multiple preferences with commas
                  </small>{" "}
                </td>{" "}
              </tr>{" "}
              <tr>
                {" "}
                <th className="text-muted fw-bold">Budget</th>{" "}
                <td>
                  {" "}
                  <div className="input-group">
                    {" "}
                    <span className="input-group-text">$</span>{" "}
                    <input
                      className="form-control"
                      type="number"
                      step="0.01"
                      min="0"
                      value={formData.budget}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          budget: parseFloat(e.target.value),
                        })
                      }
                      required
                    />{" "}
                  </div>{" "}
                </td>{" "}
              </tr>{" "}
            </tbody>{" "}
          </table>{" "}
        </div>{" "}
        <div className="mt-3 d-flex justify-content-center gap-3">
          {" "}
          <button
            className="btn btn-outline-danger px-4"
            type="button"
            onClick={handleReset}
            disabled={isLoading}
          >
            {" "}
            Cancel{" "}
          </button>{" "}
          <button
            className="btn btn-success px-4"
            type="submit"
            disabled={isLoading}
          >
            {" "}
            {isLoading ? (
              <>
                {" "}
                <span
                  className="spinner-border spinner-border-sm me-2"
                  role="status"
                  aria-hidden="true"
                ></span>{" "}
                Generating...{" "}
              </>
            ) : (
              "Generate"
            )}{" "}
          </button>{" "}
        </div>{" "}
      </form>{" "}
    </div>
  );
};
