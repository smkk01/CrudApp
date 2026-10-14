using CrudApi.Models;

namespace CrudApi.Services;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> ListAsync(string? category = null, string? search = null, int page = 1, int pageSize = 10);
    Task<Product?> GetByIdAsync(string id);
    Task<Product> CreateAsync(ProductCreateRequest request);
    Task<Product?> UpdateAsync(string id, ProductUpdateRequest request);
    Task<bool> DeleteAsync(string id);
}
