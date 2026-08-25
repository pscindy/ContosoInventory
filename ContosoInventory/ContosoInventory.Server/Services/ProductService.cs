using ContosoInventory.Server.Data;
using ContosoInventory.Server.Models;
using ContosoInventory.Shared.DTOs;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContosoInventory.Server.Services;

/// <summary>
/// Provides operations for managing inventory products.
/// </summary>
public class ProductService : IProductService
{
    private readonly InventoryContext _context;
    private readonly ILogger<ProductService> _logger;

    /// <summary>Initializes a new instance of the <see cref="ProductService"/> class.</summary>
    public ProductService(InventoryContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ProductResponseDto>> GetAllProductsAsync(int? categoryId)
    {
        if (categoryId.HasValue && categoryId.Value <= 0)
        {
            throw new ArgumentException("Category ID must be greater than zero.", nameof(categoryId));
        }

        try
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(product => product.CategoryId == categoryId.Value);
            }

            var products = await query
                .OrderBy(product => product.Name)
                .ToListAsync();

            return products.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products for category {CategoryId}.", categoryId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Product ID must be greater than zero.", nameof(id));
        }

        try
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(item => item.Category)
                .FirstOrDefaultAsync(item => item.Id == id);

            return product == null ? null : MapToDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with ID {ProductId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateProductValues(dto.Name, dto.Sku, dto.Description, dto.Price, dto.StockQuantity, dto.CategoryId);

        try
        {
            await EnsureCategoryExistsAsync(dto.CategoryId);
            await EnsureSkuIsUniqueAsync(dto.Sku, null);

            var now = DateTime.UtcNow;
            var product = new Product
            {
                Name = dto.Name.Trim(),
                Sku = NormalizeSku(dto.Sku),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.CategoryId,
                CreatedDate = now,
                LastUpdatedDate = now
            };

            _context.Products.Add(product);
            await SaveChangesHandlingDuplicateSkuAsync();

            _logger.LogInformation("Product created: {ProductSku} (ID: {ProductId}).", product.Sku, product.Id);
            return await GetProductByIdAsync(product.Id) ?? throw new InvalidOperationException("The created product could not be retrieved.");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product with SKU {ProductSku}.", dto.Sku);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto dto)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Product ID must be greater than zero.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(dto);
        ValidateProductValues(dto.Name, dto.Sku, dto.Description, dto.Price, dto.StockQuantity, dto.CategoryId);

        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return null;
            }

            await EnsureCategoryExistsAsync(dto.CategoryId);
            await EnsureSkuIsUniqueAsync(dto.Sku, id);

            product.Name = dto.Name.Trim();
            product.Sku = NormalizeSku(dto.Sku);
            product.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryId = dto.CategoryId;
            product.LastUpdatedDate = DateTime.UtcNow;

            await SaveChangesHandlingDuplicateSkuAsync();

            _logger.LogInformation("Product updated: {ProductSku} (ID: {ProductId}).", product.Sku, product.Id);
            return await GetProductByIdAsync(product.Id);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID {ProductId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProductAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Product ID must be greater than zero.", nameof(id));
        }

        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return false;
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product deleted: {ProductSku} (ID: {ProductId}).", product.Sku, product.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID {ProductId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> RestockProductAsync(int id, RestockProductDto dto)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Product ID must be greater than zero.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Quantity <= 0)
        {
            throw new ArgumentException("Restock quantity must be greater than zero.", nameof(dto.Quantity));
        }

        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return null;
            }

            if ((long)product.StockQuantity + dto.Quantity > int.MaxValue)
            {
                throw new InvalidOperationException("The restock quantity exceeds the maximum stock level.");
            }

            var updatedRows = await _context.Products
                .Where(item => item.Id == id && item.StockQuantity <= int.MaxValue - dto.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.StockQuantity, item => item.StockQuantity + dto.Quantity)
                    .SetProperty(item => item.LastUpdatedDate, DateTime.UtcNow));

            if (updatedRows == 0)
            {
                throw new InvalidOperationException("The restock quantity exceeds the maximum stock level.");
            }

            _logger.LogInformation("Product restocked: {ProductSku} (ID: {ProductId}) by {Quantity}.",
                product.Sku, product.Id, dto.Quantity);
            return await GetProductByIdAsync(product.Id);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restocking product with ID {ProductId}.", id);
            throw;
        }
    }

    private async Task EnsureCategoryExistsAsync(int categoryId)
    {
        if (!await _context.Categories.AnyAsync(category => category.Id == categoryId))
        {
            throw new InvalidOperationException($"Category with ID {categoryId} was not found.");
        }
    }

    private async Task EnsureSkuIsUniqueAsync(string sku, int? productId)
    {
        var normalizedSku = NormalizeSku(sku);
        var exists = await _context.Products
            .AnyAsync(product => product.Sku == normalizedSku && product.Id != productId);

        if (exists)
        {
            throw new InvalidOperationException($"A product with the SKU '{sku.Trim()}' already exists.");
        }
    }

    private async Task SaveChangesHandlingDuplicateSkuAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsDuplicateSkuException(ex))
        {
            throw new InvalidOperationException("A product with the same SKU already exists.", ex);
        }
    }

    private static bool IsDuplicateSkuException(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException sqliteException
            && sqliteException.SqliteErrorCode == 19
            && sqliteException.Message.Contains("Products.Sku", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSku(string sku)
    {
        return sku.Trim().ToUpperInvariant();
    }

    private static void ValidateProductValues(
        string name,
        string sku,
        string? description,
        decimal price,
        int stockQuantity,
        int categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("Product SKU is required.", nameof(sku));
        }

        if (description?.Length > 1000)
        {
            throw new ArgumentException("Product description cannot exceed 1000 characters.", nameof(description));
        }

        if (price < 0)
        {
            throw new ArgumentException("Product price cannot be negative.", nameof(price));
        }

        if (stockQuantity < 0)
        {
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(stockQuantity));
        }

        if (categoryId <= 0)
        {
            throw new ArgumentException("Category ID must be greater than zero.", nameof(categoryId));
        }
    }

    private static ProductResponseDto MapToDto(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            CreatedDate = product.CreatedDate,
            LastUpdatedDate = product.LastUpdatedDate
        };
    }
}
