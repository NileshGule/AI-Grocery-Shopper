using System.Collections.Generic;
using Xunit;
using BudgetAgent;

namespace BudgetAgent.Tests
{
    public class BudgetServiceTests
    {
        [Fact]
        public void CalculateTotal_KnownAndUnknownItems_ReturnsExpectedTotal()
        {
            var prices = new Dictionary<string, float> { { "egg", 0.2f }, { "milk", 1.5f } };
            var svc = new BudgetService(prices);
            var items = new[] { "egg", "milk", "banana" };
            var total = svc.CalculateTotal(items, defaultPrice: 2.0f);
            Assert.Equal(3.7f, total, 3);
        }

        [Fact]
        public void ParseAdjustedFromLLM_ParsesValidJson()
        {
            var svc = new BudgetService(new Dictionary<string, float>());
            var resp = "Here is a suggestion:\n{ \"items\": [\"egg\", \"rice\"], \"note\": \"Removed cheese\" }";
            var (items, note) = svc.ParseAdjustedFromLLM(resp);
            Assert.Equal(2, items.Count);
            Assert.Equal("egg", items[0]);
            Assert.Equal("rice", items[1]);
            Assert.Equal("Removed cheese", note);
        }

        [Fact]
        public void ParseAdjustedFromLLM_BadJson_ReturnsFailureNote()
        {
            var svc = new BudgetService(new Dictionary<string, float>());
            var resp = "No json here";
            var (items, note) = svc.ParseAdjustedFromLLM(resp);
            Assert.Empty(items);
            Assert.StartsWith("Failed to parse LLM response", note);
        }
    }
}
