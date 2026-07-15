using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace OrderManagementTest;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                {
                    "ConnectionStrings:DefaultConnection",
                    @"Data Source=C:\Users\syahr\Downloads\repository\AdayaSolusi\OrderManagement\OrderManagement.db"
                }
            };

            config.AddInMemoryCollection(settings);
        });
    }
}
public class OrderTest : IClassFixture<WebApplicationFactory<Program>>
{
    private HttpClient _client;

    public OrderTest(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Concurrent_CreateOrder_WithSameIdempotencyKey_Should_CreateOnlyOneOrder()
    {

        var factory = new CustomWebApplicationFactory();
        _client = factory.CreateClient();
        // Arrange
        var key = Guid.NewGuid().ToString();

        var json = """
        {
            "customerId":1,
            "shippingAddress":"Jl. Contoh Kemanakah ku kan melangkah",
            "shippingStatusId":1,
            "orderDate":"2026-07-14T11:29:00Z",

            "productOrders":[
                {
                    "productId":1,
                    "quantity":3
                }
            ]
        }
        """;

        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/Order");
        request1.Headers.Add("Idempotency-Key", key);
        request1.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/Order");
        request2.Headers.Add("Idempotency-Key", key);
        request2.Content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var task1 = _client.SendAsync(request1);
        var task2 = _client.SendAsync(request2);

        await Task.WhenAll(task1, task2);

        // Assert
        var responses = new[]
        {
            task1.Result.StatusCode,
            task2.Result.StatusCode
        };

        Assert.Contains(HttpStatusCode.Created, responses);
        Assert.Contains(HttpStatusCode.Conflict, responses);
    }

    [Fact]
    public async Task UpdateStatusInvalidStatus_Should_ReturnBadRequest()
    {
        var factory = new CustomWebApplicationFactory();
        _client = factory.CreateClient();
        var key = Guid.NewGuid().ToString();
        // Arrange
        var orderId = 15; // Replace with a valid order ID
        var changeStatusId = 5;
        var rowVersion = 2;

        var request1 = new HttpRequestMessage(HttpMethod.Put, $"/api/Order?id={orderId}&statusId={changeStatusId}&rowVersion={rowVersion}");
        request1.Headers.Add("Idempotency-Key", key);
        request1.Content = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.SendAsync(request1);
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);


    }
}
