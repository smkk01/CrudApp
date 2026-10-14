using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CrudApi.Models;
using System.Globalization;

namespace CrudApi.Services;

public class DynamoDbProductRepository : IProductRepository
{
    private readonly IAmazonDynamoDB _client;
    private readonly string _tableName;

    public DynamoDbProductRepository(IConfiguration configuration)
        : this(configuration["PRODUCT_TABLE"] ?? configuration["DynamoDb:TableName"]
            ?? throw new InvalidOperationException("Configure PRODUCT_TABLE or DynamoDb:TableName before using DynamoDB."))
    {
    }

    public DynamoDbProductRepository(string tableName)
    {
        _client = new AmazonDynamoDBClient();
        _tableName = string.IsNullOrWhiteSpace(tableName)
            ? throw new ArgumentException("A DynamoDB table name is required.", nameof(tableName))
            : tableName;
    }

    public async Task<IReadOnlyList<Product>> ListAsync(string? category = null, string? search = null, int page = 1, int pageSize = 10)
    {
        var request = new ScanRequest
        {
            TableName = _tableName
        };

        var items = new List<Dictionary<string, AttributeValue>>();
        ScanResponse response;
        do
        {
            response = await _client.ScanAsync(request);
            items.AddRange(response.Items);
            if (response.LastEvaluatedKey is not { Count: > 0 })
            {
                break;
            }

            request.ExclusiveStartKey = response.LastEvaluatedKey;
        }
        while (true);

        var products = items
            .Select(Map)
            .Where(product => string.IsNullOrWhiteSpace(category) || product.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .Where(product => string.IsNullOrWhiteSpace(search) ||
                product.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                product.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                product.Category.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(product => product.UpdatedAt)
            .ToList();

        var skip = Math.Max(page - 1, 0) * pageSize;
        return products.Skip(skip).Take(pageSize).ToList();
    }

    public async Task<Product?> GetByIdAsync(string id)
    {
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = id }
            }
        };

        var response = await _client.GetItemAsync(request);
        return response.Item.Count == 0 ? null : Map(response.Item);
    }

    public async Task<Product> CreateAsync(ProductCreateRequest request)
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

        await _client.PutItemAsync(_tableName, ToAttributes(product));
        return product;
    }

    public async Task<Product?> UpdateAsync(string id, ProductUpdateRequest request)
    {
        var existing = await GetByIdAsync(id);
        if (existing is null)
        {
            return null;
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

        await _client.PutItemAsync(_tableName, ToAttributes(updated));
        return updated;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var request = new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = id }
            }
        };

        request.ReturnValues = ReturnValue.ALL_OLD;
        var response = await _client.DeleteItemAsync(request);
        return response.Attributes.Count > 0;
    }

    internal static Product Map(Dictionary<string, AttributeValue> items)
    {
        return new Product
        {
            Id = GetString(items, "id"),
            Name = GetString(items, "name"),
            Description = GetString(items, "description"),
            Price = decimal.Parse(GetString(items, "price"), CultureInfo.InvariantCulture),
            Category = GetString(items, "category"),
            IsActive = bool.Parse(GetString(items, "isActive")),
            CreatedAt = DateTime.Parse(GetString(items, "createdAt"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            UpdatedAt = DateTime.Parse(GetString(items, "updatedAt"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }

    internal static Dictionary<string, AttributeValue> ToAttributes(Product product)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = product.Id },
            ["name"] = new AttributeValue { S = product.Name },
            ["description"] = new AttributeValue { S = product.Description },
            ["price"] = new AttributeValue { N = product.Price.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            ["category"] = new AttributeValue { S = product.Category },
            ["isActive"] = new AttributeValue { BOOL = product.IsActive },
            ["createdAt"] = new AttributeValue { S = product.CreatedAt.ToString("O") },
            ["updatedAt"] = new AttributeValue { S = product.UpdatedAt.ToString("O") }
        };
    }

    private static string GetString(Dictionary<string, AttributeValue> items, string key)
    {
        if (!items.TryGetValue(key, out var value))
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(value.S))
        {
            return value.S;
        }

        if (!string.IsNullOrEmpty(value.N))
        {
            return value.N;
        }

        if (value.BOOL.HasValue)
        {
            return value.BOOL.Value.ToString();
        }

        return string.Empty;
    }
}
