using Amazon.Lambda.AspNetCoreServer.Hosting;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using CrudApi.Models;
using CrudApi.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
        policy.WithOrigins(
            "http://localhost:5173",
            "http://127.0.0.1:5173",
            "https://localhost:5173");
    });
});

builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

var useDynamoDb = builder.Configuration.GetValue<bool>("DynamoDb:Enabled")
    || !string.IsNullOrWhiteSpace(builder.Configuration["PRODUCT_TABLE"]);
builder.Services.AddSingleton<IProductRepository>(useDynamoDb
    ? new DynamoDbProductRepository(builder.Configuration)
    : new InMemoryProductRepository());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok", environment = app.Environment.EnvironmentName }));

app.MapGet("/api/products", async (IProductRepository repository, string? category, string? search, int page = 1, int pageSize = 10) =>
{
    var products = await repository.ListAsync(category, search, page, pageSize);
    return Results.Ok(products);
});

app.MapGet("/api/products/{id}", async (string id, IProductRepository repository) =>
{
    var product = await repository.GetByIdAsync(id);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.MapPost("/api/products", async (ProductCreateRequest request, IProductRepository repository) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "Product name is required." });
    }

    if (request.Price <= 0)
    {
        return Results.BadRequest(new { message = "Price must be greater than zero." });
    }

    var product = await repository.CreateAsync(request);
    return Results.Created($"/api/products/{product.Id}", product);
});

app.MapPut("/api/products/{id}", async (string id, ProductUpdateRequest request, IProductRepository repository) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "Product name is required." });
    }

    if (request.Price <= 0)
    {
        return Results.BadRequest(new { message = "Price must be greater than zero." });
    }

    var product = await repository.UpdateAsync(id, request);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.MapDelete("/api/products/{id}", async (string id, IProductRepository repository) =>
{
    var deleted = await repository.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();

namespace CrudApi
{
    using System.Text.Json;
    using Amazon.Lambda.APIGatewayEvents;
    using Amazon.Lambda.Core;

    public class ApiFunction
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private static readonly IProductRepository Repository = CreateRepository();

        [LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var path = request.Path ?? "/";
            var method = request.HttpMethod ?? "GET";
            var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            try
            {
                if (path.Equals("/health", StringComparison.OrdinalIgnoreCase) && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonResponse(200, new { status = "ok", environment = "aws" });
                }

                if (segments.Length >= 2 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase) && segments[1].Equals("products", StringComparison.OrdinalIgnoreCase))
                {
                    if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && segments.Length == 2)
                    {
                        var category = request.MultiValueQueryStringParameters != null && request.MultiValueQueryStringParameters.TryGetValue("category", out var categories)
                            ? categories.FirstOrDefault()
                            : null;
                        var search = request.MultiValueQueryStringParameters != null && request.MultiValueQueryStringParameters.TryGetValue("search", out var searches)
                            ? searches.FirstOrDefault()
                            : null;

                        var products = Repository.ListAsync(category, search).GetAwaiter().GetResult();
                        return JsonResponse(200, products);
                    }

                    if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && segments.Length == 3)
                    {
                        var product = Repository.GetByIdAsync(segments[2]).GetAwaiter().GetResult();
                        return product is null ? JsonResponse(404, new { message = "Product not found." }) : JsonResponse(200, product);
                    }

                    if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && segments.Length == 2)
                    {
                        var payload = string.IsNullOrWhiteSpace(request.Body)
                            ? new ProductCreateRequest(string.Empty, string.Empty, 0m, string.Empty)
                            : JsonSerializer.Deserialize<ProductCreateRequest>(request.Body, JsonOptions) ?? new ProductCreateRequest(string.Empty, string.Empty, 0m, string.Empty);

                        if (string.IsNullOrWhiteSpace(payload.Name))
                        {
                            return JsonResponse(400, new { message = "Product name is required." });
                        }

                        if (payload.Price <= 0)
                        {
                            return JsonResponse(400, new { message = "Price must be greater than zero." });
                        }

                        var created = Repository.CreateAsync(payload).GetAwaiter().GetResult();
                        return JsonResponse(201, created);
                    }

                    if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase) && segments.Length == 3)
                    {
                        var payload = string.IsNullOrWhiteSpace(request.Body)
                            ? new ProductUpdateRequest(string.Empty, string.Empty, 0m, string.Empty)
                            : JsonSerializer.Deserialize<ProductUpdateRequest>(request.Body, JsonOptions) ?? new ProductUpdateRequest(string.Empty, string.Empty, 0m, string.Empty);

                        if (string.IsNullOrWhiteSpace(payload.Name))
                        {
                            return JsonResponse(400, new { message = "Product name is required." });
                        }

                        if (payload.Price <= 0)
                        {
                            return JsonResponse(400, new { message = "Price must be greater than zero." });
                        }

                        var updated = Repository.UpdateAsync(segments[2], payload).GetAwaiter().GetResult();
                        return updated is null ? JsonResponse(404, new { message = "Product not found." }) : JsonResponse(200, updated);
                    }

                    if (method.Equals("DELETE", StringComparison.OrdinalIgnoreCase) && segments.Length == 3)
                    {
                        var deleted = Repository.DeleteAsync(segments[2]).GetAwaiter().GetResult();
                        return deleted ? JsonResponse(204, new { }) : JsonResponse(404, new { message = "Product not found." });
                    }
                }

                return JsonResponse(404, new { message = "Not found." });
            }
            catch (Exception ex)
            {
                return JsonResponse(500, new { message = ex.Message });
            }
        }

        private static APIGatewayProxyResponse JsonResponse(int statusCode, object payload)
        {
            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            };
            var allowedOrigin = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN");
            if (!string.IsNullOrWhiteSpace(allowedOrigin))
            {
                headers["Access-Control-Allow-Origin"] = allowedOrigin;
                headers["Vary"] = "Origin";
            }

            return new APIGatewayProxyResponse
            {
                StatusCode = statusCode,
                Headers = headers,
                Body = JsonSerializer.Serialize(payload, JsonOptions)
            };
        }

        private static IProductRepository CreateRepository()
        {
            var tableName = Environment.GetEnvironmentVariable("PRODUCT_TABLE");
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                return new DynamoDbProductRepository(tableName);
            }

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")))
            {
                throw new InvalidOperationException("PRODUCT_TABLE must be configured for the Lambda runtime.");
            }

            return new InMemoryProductRepository();
        }
    }
}
