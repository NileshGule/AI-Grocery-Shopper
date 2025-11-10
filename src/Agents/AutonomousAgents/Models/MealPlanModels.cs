using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AutonomousAgents.Models;

public record MealPlanResponse(List<MealDto> Meals);

public class MealDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("ingredients")]
    public List<string> Ingredients { get; set; } = new();

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}
