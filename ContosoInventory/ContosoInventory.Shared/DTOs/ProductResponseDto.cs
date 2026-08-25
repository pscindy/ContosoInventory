namespace ContosoInventory.Shared.DTOs;

/// <summary>
/// Represents a product returned by the inventory API.
/// </summary>
public class ProductResponseDto
{
    /// <summary>Gets or sets the product identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the product name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the stock keeping unit.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the product description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the unit price.</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the current stock quantity.</summary>
    public int StockQuantity { get; set; }

    /// <summary>Gets or sets the category identifier.</summary>
    public int CategoryId { get; set; }

    /// <summary>Gets or sets the category name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Gets or sets the creation timestamp in UTC.</summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>Gets or sets the last update timestamp in UTC.</summary>
    public DateTime LastUpdatedDate { get; set; }
}
