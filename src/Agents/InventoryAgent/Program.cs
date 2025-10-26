using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:80");
var app = builder.Build();

app.MapGet("/health", () => Results.Ok("InventoryAgent OK")).DisableAntiforgery();

app.MapPost("/inventory-check", async (InventoryRequest req) =>
{
    // Load local inventory JSON file
    var inventoryPath = Path.Combine(AppContext.BaseDirectory, "inventory.json");
    if (!File.Exists(inventoryPath))
    {
        return Results.Problem($"Inventory file not found: {inventoryPath}");
    }

    var invJson = await File.ReadAllTextAsync(inventoryPath);

    // Use case-insensitive property matching so JSON keys like "items" map to C# "Items"
    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    InventoryFile? inventory;
    try
    {
        inventory = JsonSerializer.Deserialize<InventoryFile>(invJson, options);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to parse inventory file: {ex.Message}");
    }

    if (inventory is null)
    {
        return Results.Problem("Failed to parse inventory file (null result)");
    }

    var missing = new List<string>();
    foreach (var item in req.Items)
    {
        if (!inventory.Items.Any(i => string.Equals(i.Name, item, StringComparison.OrdinalIgnoreCase)))
        {
            missing.Add(item);
        }
    }

    return Results.Ok(new InventoryCheckResponse (missing.ToArray() ));
}).DisableAntiforgery();

app.Run();

// DTOs and file models
public record InventoryRequest(string[] Items);
public record InventoryCheckResponse(string[] MissingItems);

public class InventoryFile
{
    public InventoryItem[] Items { get; set; } = Array.Empty<InventoryItem>();
}

public class InventoryItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Expiry { get; set; }
}
