using ContosoInventory.Shared.DTOs;

namespace ContosoInventory.Server.Services;

/// <summary>
/// Defines operations for managing inventory products.
/// </summary>
public interface IProductService
{
    /// <summary>Retrieves products, optionally limited to a category.</summary>
    /// <param name="categoryId">An optional category identifier.</param>
    /// <returns>The matching products.</returns>
    Task<List<ProductResponseDto>> GetAllProductsAsync(int? categoryId);

    /// <summary>Retrieves a product by its identifier.</summary>
    /// <param name="id">The product identifier.</param>
    /// <returns>The product, or null if it does not exist.</returns>
    Task<ProductResponseDto?> GetProductByIdAsync(int id);

    /// <summary>Creates a product.</summary>
    /// <param name="dto">The product creation data.</param>
    /// <returns>The created product.</returns>
    Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto);

    /// <summary>Updates a product.</summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="dto">The updated product data.</param>
    /// <returns>The updated product, or null if it does not exist.</returns>
    Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto dto);

    /// <summary>Deletes a product.</summary>
    /// <param name="id">The product identifier.</param>
    /// <returns>True when deleted; otherwise false.</returns>
    Task<bool> DeleteProductAsync(int id);

    /// <summary>Increases the stock quantity for a product.</summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="dto">The restock data.</param>
    /// <returns>The updated product, or null if it does not exist.</returns>
    Task<ProductResponseDto?> RestockProductAsync(int id, RestockProductDto dto);
}
