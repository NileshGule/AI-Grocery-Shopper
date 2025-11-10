namespace AutonomousAgents.Models;

public record InventoryResponse(string[] Available, string[] Missing);

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