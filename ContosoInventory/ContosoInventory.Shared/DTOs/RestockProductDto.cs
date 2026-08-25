namespace ContosoInventory.Shared.DTOs;

/// <summary>
/// Represents a stock increase for a product.
/// </summary>
public class RestockProductDto
{
    /// <summary>Gets or sets the quantity to add to stock.</summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
