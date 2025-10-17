using System.Collections.Generic;
using Xunit;
using Moq;
using BudgetAgent;
using Common.ModelClient;
using System.Threading.Tasks;
using System;

namespace BudgetAgent.Tests
{
    public class LLMRetryTests
    {
        [Fact]
        public async Task RetryLoop_ParsesOnSecondAttempt()
        {
            var prices = new Dictionary<string, float> { { "egg", 0.2f }, { "rice", 1.0f } };
            var svc = new BudgetService(prices);

            // Mock model client to return invalid payload first, then valid JSON
            var mock = new Mock<IModelClient>();
            mock.SetupSequence(m => m.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Some text without json")
                .ReturnsAsync("{ \"items\": [\"egg\", \"rice\"], \"totalCost\": 1.2, \"note\": \"Removed cheese\" }");

            var client = mock.Object;

            var reqItems = new[] { "egg", "milk", "cheese" };
            var budget = 2.0f;

            // Emulate the part of Program.cs that runs the retry/parse logic
            string lastRaw = string.Empty;
            List<string> adjustedItems = new List<string>();
            string adjustedNote = string.Empty;
            float adjustedTotal = 0f;
            bool parsed = false;

            var allowedKeys = new HashSet<string>(new[] { "items", "totalCost", "note" }, StringComparer.OrdinalIgnoreCase);
            int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var attemptPrompt = "prompt";
                if (attempt > 1)
                    attemptPrompt += " retry";

                lastRaw = await client.GenerateTextAsync("sys", attemptPrompt);

                var start = lastRaw.IndexOf('{');
                var end = lastRaw.LastIndexOf('}');
                var adjustedJson = lastRaw;
                if (start >= 0 && end > start)
                    adjustedJson = lastRaw[start..(end + 1)];

                try
                {
                    using var doc = JsonDocument.Parse(adjustedJson);
                    var root = doc.RootElement;
                    if (root.ValueKind != JsonValueKind.Object)
                        throw new Exception("root is not an object");

                    var keySet = new HashSet<string>(root.EnumerateObject().Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
                    if (!allowedKeys.SetEquals(keySet))
                        throw new Exception("unexpected or missing top-level keys");

                    var itemsElem = root.GetProperty("items");
                    if (itemsElem.ValueKind != JsonValueKind.Array)
                        throw new Exception("items is not an array");

                    var newItems = new List<string>();
                    foreach (var el in itemsElem.EnumerateArray())
                    {
                        if (el.ValueKind != JsonValueKind.String)
                            throw new Exception("items must be strings");
                        newItems.Add(el.GetString() ?? string.Empty);
                    }

                    var totalElem = root.GetProperty("totalCost");
                    if (totalElem.ValueKind != JsonValueKind.Number)
                        throw new Exception("totalCost is not a number");

                    var note = root.GetProperty("note").GetString() ?? string.Empty;

                    var recalculated = svc.CalculateTotal(newItems);

                    adjustedItems = newItems;
                    adjustedNote = note;
                    adjustedTotal = recalculated;
                    parsed = true;
                    break;
                }
                catch
                {
                    // continue
                }
            }

            Assert.True(parsed);
            Assert.Equal(2, adjustedItems.Count);
            Assert.Equal("egg", adjustedItems[0]);
            Assert.Equal("rice", adjustedItems[1]);
            Assert.Equal("Removed cheese", adjustedNote);
            Assert.Equal(1.2f, adjustedTotal, 3);
        }

        [Fact]
        public async Task RetryLoop_FailsAfterMaxAttempts()
        {
            var prices = new Dictionary<string, float> { { "egg", 0.2f } };
            var svc = new BudgetService(prices);

            var mock = new Mock<IModelClient>();
            mock.Setup(m => m.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("no json here");

            var client = mock.Object;

            string lastRaw = string.Empty;
            List<string> adjustedItems = new List<string>();
            string adjustedNote = string.Empty;
            float adjustedTotal = 0f;
            bool parsed = false;

            var allowedKeys = new HashSet<string>(new[] { "items", "totalCost", "note" }, StringComparer.OrdinalIgnoreCase);
            int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                lastRaw = await client.GenerateTextAsync("sys", "prompt");
                var start = lastRaw.IndexOf('{');
                var end = lastRaw.LastIndexOf('}');
                var adjustedJson = lastRaw;
                if (start >= 0 && end > start)
                    adjustedJson = lastRaw[start..(end + 1)];

                try
                {
                    using var doc = JsonDocument.Parse(adjustedJson);
                    var root = doc.RootElement;
                    var keySet = new HashSet<string>(root.EnumerateObject().Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
                    if (!allowedKeys.SetEquals(keySet))
                        throw new Exception("unexpected or missing top-level keys");
                    parsed = true; break;
                }
                catch
                {
                }
            }

            Assert.False(parsed);
        }
    }
}
