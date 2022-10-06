using System.Net;
using WebApi.Models;
using FluentAssertions;

namespace WebApi.Tests;

public class ItemsControllerTests
{
    private readonly HttpClient _client;

    public ItemsControllerTests()
    {
        _client = new WebApiApplication().CreateClient();
    }

    [Fact]
    public void api_should_return_ok()
    {
        // Arrange
        var items = SeedData.Items();

        // Act
        var response = _client.GetAsync("/api/items").Result;
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task api_should_return_items()
    {
        // Arrange
        var items = SeedData.Items();

        // Act
        var response = await _client.GetFromJsonAsync<Item[]>("/api/items");

        // Assert
        response.Should().BeEquivalentTo(items);
    }
}

            