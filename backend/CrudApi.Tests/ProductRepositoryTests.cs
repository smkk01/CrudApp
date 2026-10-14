using CrudApi.Models;
using CrudApi.Services;
using Amazon.DynamoDBv2.Model;
using System.Globalization;

namespace CrudApi.Tests;

public class ProductRepositoryTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistProduct()
    {
        var repository = new InMemoryProductRepository();

        var created = await repository.CreateAsync(new ProductCreateRequest(
            "Smart Thermostat",
            "Connected climate control for the office.",
            249.99m,
            "Home Automation"));

        Assert.Equal("Smart Thermostat", created.Name);
        Assert.NotNull(created.Id);
        Assert.True(created.CreatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingProduct()
    {
        var repository = new InMemoryProductRepository();
        var created = await repository.CreateAsync(new ProductCreateRequest(
            "Desk Lamp",
            "Basic lighting solution.",
            79.00m,
            "Office"));

        var updated = await repository.UpdateAsync(created.Id, new ProductUpdateRequest(
            "Desk Lamp Pro",
            "Dimmable smart lighting configuration.",
            119.00m,
            "Office",
            true));

        Assert.NotNull(updated);
        Assert.Equal("Desk Lamp Pro", updated!.Name);
        Assert.Equal(119.00m, updated.Price);
    }

    [Fact]
    public void DynamoDbMapping_ShouldPreservePricesAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var product = new Product
            {
                Id = "product-1",
                Name = "Precision Scale",
                Description = "Test item",
                Price = 1234.56m,
                Category = "Tools",
                IsActive = true,
                CreatedAt = new DateTime(2026, 10, 14, 12, 30, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 10, 14, 13, 45, 0, DateTimeKind.Utc)
            };

            var attributes = DynamoDbProductRepository.ToAttributes(product);
            var restored = DynamoDbProductRepository.Map(attributes);

            Assert.Equal("1234.56", attributes["price"].N);
            Assert.Equal(product.Price, restored.Price);
            Assert.Equal(product.CreatedAt, restored.CreatedAt);
            Assert.Equal(product.UpdatedAt, restored.UpdatedAt);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
