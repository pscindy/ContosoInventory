namespace ContosoInventory.Shared.DTOs;

/// <summary>
/// Represents the data required to create a product.
/// </summary>
public class CreateProductDto
{
    /// <summary>Gets or sets the product name.</summary>
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the stock keeping unit.</summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the product description.</summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>Gets or sets the unit price.</summary>
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Price { get; set; }

    /// <summary>Gets or sets the initial stock quantity.</summary>
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    /// <summary>Gets or sets the category identifier.</summary>
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}
