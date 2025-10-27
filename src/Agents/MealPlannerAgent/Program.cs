using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common.ModelClient;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

// Declare the WebApplication builder before using it to fix undefined 'builder' errors.
var builder = WebApplication.CreateBuilder(args);

// Ensure the app listens on all network interfaces inside the container
builder.WebHost.UseUrls("http://0.0.0.0:80");

builder.Services.AddSingleton<IModelClient, Common.ModelClient.LocalModelClient>();
// builder.Services.AddSingleton<IModelClient, Common.ModelClient.AzureFoundryModelClient>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("MealPlannerAgent OK"));

app.MapPost("/plan", async (MealPlanRequest req, IModelClient client) =>
{
    Console.WriteLine($"Received meal plan request: Preferences={req.Preferences}, Constraints={req.Constraints}");

    var systemMessage = "You are a helpful meal planning assistant. Respond ONLY with a single JSON object (no surrounding text) that matches the schema described below.";
    var schemaInstruction = @"Return a JSON object with this shape:
{
  ""meals"": [
    {
      ""name"": string,
      ""ingredients"": [ string ],
      ""notes"": string (optional)
    }
  ]
}
Only output valid JSON and no other text.";

    var prompt = $"Generate a meal plan for: {req.Preferences}\nConstraints: {req.Constraints}\n\n{schemaInstruction}";

    var llmResponse = await client.GenerateTextAsync(systemMessage, prompt);
    Console.WriteLine($"LLM Response: {llmResponse}");

    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    List<MealDto> meals = new List<MealDto>();

    // Try to parse flexibly using JsonDocument so we can handle ingredients as strings or objects
    JsonDocument? doc = null;
    string jsonSource = llmResponse;
    try
    {
        doc = JsonDocument.Parse(llmResponse);
    }
    catch (JsonException)
    {
        // Try extract JSON substring
        var start = llmResponse.IndexOf('{');
        var end = llmResponse.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            var sub = llmResponse[start..(end + 1)];
            try
            {
                doc = JsonDocument.Parse(sub);
                jsonSource = sub;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parse failed: {ex.Message}");
            }
        }
    }

    if (doc != null && doc.RootElement.ValueKind == JsonValueKind.Object)
    {
        var root = doc.RootElement;
        if (root.TryGetProperty("meals", out var mealsProp) && mealsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var mElem in mealsProp.EnumerateArray())
            {
                var name = mElem.TryGetProperty("name", out var nprop) && nprop.ValueKind == JsonValueKind.String ? nprop.GetString() ?? string.Empty : string.Empty;
                var notes = mElem.TryGetProperty("notes", out var notesProp) && notesProp.ValueKind == JsonValueKind.String ? notesProp.GetString() ?? string.Empty : string.Empty;

                var ingredients = new List<string>();
                if (mElem.TryGetProperty("ingredients", out var ingrProp))
                {
                    if (ingrProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var ie in ingrProp.EnumerateArray())
                        {
                            if (ie.ValueKind == JsonValueKind.String)
                            {
                                var s = ie.GetString();
                                if (!string.IsNullOrWhiteSpace(s)) ingredients.Add(s.Trim());
                            }
                            else if (ie.ValueKind == JsonValueKind.Object)
                            {
                                if (ie.TryGetProperty("name", out var iname) && iname.ValueKind == JsonValueKind.String)
                                {
                                    ingredients.Add(iname.GetString()?.Trim() ?? string.Empty);
                                }
                                else
                                {
                                    // fallback: convert entire object to string and try to extract tokens
                                    ingredients.Add(ie.ToString());
                                }
                            }
                            else if (ie.ValueKind == JsonValueKind.Number)
                            {
                                ingredients.Add(ie.ToString());
                            }
                        }
                    }
                    else if (ingrProp.ValueKind == JsonValueKind.String)
                    {
                        var s = ingrProp.GetString() ?? string.Empty;
                        ingredients.AddRange(s.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0));
                    }
                }

                // If ingredients still empty, try to parse from notes
                if (!ingredients.Any() && !string.IsNullOrWhiteSpace(notes))
                {
                    ingredients.AddRange(MealPlannerHelpers.ExtractIngredientsFromText(notes));
                }

                // Still empty? try to find meal-scoped ingredients from the raw JSON/text around the meal name
                if (!ingredients.Any() && !string.IsNullOrWhiteSpace(name))
                {
                    var near = MealPlannerHelpers.ExtractIngredientsForMeal(jsonSource, name);
                    if (near.Any()) ingredients.AddRange(near);
                }

                // Normalize and merge tokens that should be combined into multi-word ingredients
                ingredients = MealPlannerHelpers.MergeIngredientsWithContext(ingredients, name, jsonSource);

                 meals.Add(new MealDto { Name = name, Ingredients = ingredients.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), Notes = notes });
             }
         }
     }

    if (meals.Any())
    {
        return Results.Ok(new MealPlanResponse(meals));
    }

    // Fallback: attempt to extract ingredient-like tokens from raw text
    var fallbackIngredients = MealPlannerHelpers.ExtractIngredientsFromText(llmResponse);
    meals.Add(new MealDto { Name = "Planned Meals", Ingredients = fallbackIngredients, Notes = "Fallback parsed from raw LLM output" });

    return Results.Ok(new MealPlanResponse(meals));
});

app.Run();

// DTOs and helper types
public record MealPlanRequest(string Preferences, string Constraints);
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

// Internal types to parse the LLM JSON
public class StructuredPlan
{
    [JsonPropertyName("meals")]
    public List<StructuredMeal>? Meals { get; set; }
}
public class StructuredMeal
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("ingredients")]
    public List<string>? Ingredients { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

internal static class MealPlannerHelpers
{
    // Very small heuristic to extract ingredient names from free text if JSON parsing fails
    public static List<string> ExtractIngredientsFromText(string text)
    {
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var candidates = new List<string>();
        foreach (var line in lines)
        {
            var l = line.Trim();
            if (string.IsNullOrEmpty(l)) continue;
            // common list prefixes
            if (l.StartsWith("- ") || l.StartsWith("* ") || (l.Length>0 && char.IsDigit(l[0])))
            {
                var cleaned = l.TrimStart('-', '*', ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', ')').Trim();
                // if comma-separated, take first token
                var first = cleaned.Split(',').Select(t => t.Trim()).FirstOrDefault() ?? cleaned;
                if (!string.IsNullOrEmpty(first)) candidates.Add(first);
                continue;
            }

            // If line contains multiple commas, treat as list
            if (l.Count(c => c == ',') >= 1 && l.Length < 200)
            {
                foreach (var part in l.Split(','))
                {
                    var p = part.Trim();
                    if (p.Length > 0 && p.Length < 100) candidates.Add(p);
                }
                continue;
            }

            // short lines with a single or two words might be ingredients
            if (l.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 4 && l.Length < 60)
            {
                candidates.Add(l);
            }
        }

        // Deduplicate and return
        return candidates.Select(c => c.Trim().TrimEnd('.')).Where(s => !string.IsNullOrEmpty(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    // Attempt to extract ingredients for a specific meal name by searching nearby text
    public static List<string> ExtractIngredientsForMeal(string rawText, string mealName)
    {
        if (string.IsNullOrWhiteSpace(mealName) || string.IsNullOrWhiteSpace(rawText)) return new List<string>();

        var idx = rawText.IndexOf(mealName, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return new List<string>();

        // capture a window of text after the meal name
        var start = idx + mealName.Length;
        var length = Math.Min(1000, rawText.Length - start);
        var window = rawText.Substring(start, length);

        // Stop at the next meal heading pattern (another "\n<Word>:") or double newline
        var stop = Regex.Match(window, "\\n\\s*\\w+\\s*[:\\-]\\s*\\n");
        string candidate;
        if (stop.Success)
        {
            candidate = window.Substring(0, stop.Index);
        }
        else
        {
            // stop at double newline or limit
            var dn = window.IndexOf("\n\n");
            candidate = dn >= 0 ? window.Substring(0, dn) : window;
        }

        return ExtractIngredientsFromText(candidate);
    }

    // Merge adjacent single-word tokens into multi-word ingredients when the combined phrase appears in context
    public static List<string> MergeIngredientsWithContext(List<string> ingredients, string mealName, string context)
    {
        // ensure non-null
        ingredients = ingredients ?? new List<string>();

        // Clean tokens (strip quantities, units, parentheses, punctuation)
        var tokens = ingredients
            .Select(t => CleanIngredientToken(t))
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        if (tokens.Count <= 1) return tokens.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var merged = new List<string>();
        int i = 0;
        // also prepare a joined token string to help heuristics
        var joinedTokens = string.Join(" ", tokens);

        while (i < tokens.Count)
        {
            var current = tokens[i];

            // try trigram first (prefer longer matches)
            if (i + 2 < tokens.Count)
            {
                var trigram = string.Join(' ', tokens.Skip(i).Take(3));
                if (ContainsPhrase(mealName, trigram) || ContainsPhrase(context, trigram) || ContainsPhrase(joinedTokens, trigram))
                {
                    merged.Add(ProperCase(trigram));
                    i += 3;
                    continue;
                }
            }

            // try bigram
            if (i + 1 < tokens.Count)
            {
                var bigram = string.Join(' ', tokens.Skip(i).Take(2));
                if (ContainsPhrase(mealName, bigram) || ContainsPhrase(context, bigram) || ContainsPhrase(joinedTokens, bigram))
                {
                    merged.Add(ProperCase(bigram));
                    i += 2;
                    continue;
                }

                // fallback heuristic: if both tokens are alphabetic short words and combined length reasonable, merge
                if (Regex.IsMatch(tokens[i], "^[\\p{L}]+$") && Regex.IsMatch(tokens[i + 1], "^[\\p{L}]+$") && (tokens[i].Length + tokens[i + 1].Length) <= 30)
                {
                    // only merge if combination appears in meal name or context, otherwise keep separate
                    var candidate = tokens[i] + " " + tokens[i + 1];
                    if (ContainsPhrase(mealName, candidate) || ContainsPhrase(context, candidate) || ContainsPhrase(joinedTokens, candidate))
                    {
                        merged.Add(ProperCase(candidate));
                        i += 2;
                        continue;
                    }
                }
            }

            merged.Add(ProperCase(current));
            i++;
        }

        // remove duplicates preserving original order
        return merged.Select(m => m.Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string CleanIngredientToken(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        // remove common list markers and surrounding punctuation
        var t = s.Trim().TrimStart('-', '*').Trim();

        // remove parenthetical content like (400 g)
        t = Regex.Replace(t, "\\(.*?\\)", " ");

        // remove leading quantities and units (e.g. "400 g", "2 cups", "1/2 tsp")
        t = Regex.Replace(t, "^\\s*[0-9]+(?:[\\/.][0-9]+)?\\s*(?:g|kg|mg|ml|l|cup|cups|tbsp|tbs|tsp|pcs|pieces|oz|lb|lbs|pack|pk|packages)?\\b", "", RegexOptions.IgnoreCase);

        // remove trailing punctuation
        t = t.Trim().TrimEnd('.', ',', ';', ':');

        // collapse whitespace
        t = Regex.Replace(t, "\\s+", " ").Trim();

        return t;
    }

    private static bool ContainsPhrase(string? text, string phrase)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(phrase)) return false;
        return text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string ProperCase(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        // simple normalization: collapse whitespace and lower-case, then capitalize first letters
        var parts = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i].ToLowerInvariant();
            parts[i] = char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p.Substring(1) : string.Empty);
        }
        return string.Join(' ', parts);
    }
}
