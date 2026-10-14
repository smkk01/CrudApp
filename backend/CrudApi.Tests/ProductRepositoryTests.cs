using CrudApi.Models;
using CrudApi.Services;

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
}
