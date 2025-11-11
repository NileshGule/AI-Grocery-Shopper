using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BudgetAgent
{
    public class BudgetService
    {
        public Dictionary<string, float> Prices { get; }

        public BudgetService(Dictionary<string, float> prices)
        {
            Prices = prices ?? new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        }

        public static Dictionary<string, float> LoadPricesFromFile(string path)
        {
            if (!File.Exists(path))
            {
                return new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            }

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, float>>(json, options);
                return dict ?? new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public float CalculateTotal(IEnumerable<string> items, float defaultPrice = 5.0f)
        {
            float total = 0f;
            foreach (var it in items)
            {
                if (Prices.TryGetValue(it, out var p))
                    total += p;
                else
                    total += defaultPrice;
            }
            return total;
        }

        public (List<string> Items, string Note) ParseAdjustedFromLLM(string llmResp)
        {
            Console.WriteLine($"LLM response: {llmResp}");

            if (string.IsNullOrWhiteSpace(llmResp))
                return (new List<string>(), "Empty response");

            var start = llmResp.IndexOf('{');
            var end = llmResp.LastIndexOf('}');
            var adjustedJson = llmResp;
            if (start >= 0 && end > start)
                adjustedJson = llmResp[start..(end + 1)];

            try
            {
                using var doc = JsonDocument.Parse(adjustedJson);
                var root = doc.RootElement;
                var newItems = new List<string>();
                if (root.TryGetProperty("items", out var itemsElem))
                {
                    foreach (var el in itemsElem.EnumerateArray())
                    {
                        newItems.Add(el.GetString() ?? string.Empty);
                    }
                }

                var note = root.TryGetProperty("note", out var n) ? n.GetString() ?? "Adjusted by LLM" : "Adjusted by LLM";
                return (newItems, note);
            }
            catch (Exception ex)
            {
                return (new List<string>(), "Failed to parse LLM response: " + ex.Message);
            }
        }
    }
}
