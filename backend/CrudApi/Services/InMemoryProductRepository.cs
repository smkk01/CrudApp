using System.Collections.Concurrent;
using CrudApi.Models;

namespace CrudApi.Services;

public class InMemoryProductRepository : IProductRepository
{
    private static readonly ConcurrentDictionary<string, Product> Products = new();

    static InMemoryProductRepository()
    {
        Seed();
    }

    public Task<IReadOnlyList<Product>> ListAsync(string? category = null, string? search = null, int page = 1, int pageSize = 10)
    {
        var items = Products.Values
            .Where(product => string.IsNullOrWhiteSpace(category) || product.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .Where(product => string.IsNullOrWhiteSpace(search) ||
                product.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                product.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                product.Category.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(product => product.UpdatedAt)
            .ToList();

        var skip = Math.Max(page - 1, 0) * pageSize;
        var paged = items.Skip(skip).Take(pageSize).ToList();

        return Task.FromResult<IReadOnlyList<Product>>(paged);
    }

    public Task<Product?> GetByIdAsync(string id)
    {
        Products.TryGetValue(id, out var product);
        return Task.FromResult(product);
    }

    public Task<Product> CreateAsync(ProductCreateRequest request)
    {
        var product = new Product
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            Category = request.Category.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Products[product.Id] = product;
        return Task.FromResult(product);
    }

    public Task<Product?> UpdateAsync(string id, ProductUpdateRequest request)
    {
        if (!Products.TryGetValue(id, out var existing))
        {
            return Task.FromResult<Product?>(null);
        }

        var updated = new Product
        {
            Id = existing.Id,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            Category = request.Category.Trim(),
            IsActive = request.IsActive,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        Products[id] = updated;
        return Task.FromResult<Product?>(updated);
    }

    public Task<bool> DeleteAsync(string id)
    {
        return Task.FromResult(Products.TryRemove(id, out _));
    }

    private static void Seed()
    {
        var now = DateTime.UtcNow;
        var sampleProducts = new[]
        {
            new Product { Id = "p-1001", Name = "Laptop Pro", Description = "Premium productivity laptop", Price = 1499.99m, Category = "Electronics", IsActive = true, CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-2) },
            new Product { Id = "p-1002", Name = "Ergo Chair", Description = "Adjustable office chair", Price = 349.50m, Category = "Office", IsActive = true, CreatedAt = now.AddDays(-8), UpdatedAt = now.AddDays(-1) },
            new Product { Id = "p-1003", Name = "Noise Cancelling Headphones", Description = "High fidelity audio device", Price = 299.00m, Category = "Electronics", IsActive = true, CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-2) }
        };

        foreach (var product in sampleProducts)
        {
            Products[product.Id] = product;
        }
    }
}
