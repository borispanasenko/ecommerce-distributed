using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using Catalog.Application.Products.Queries;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddCatalogInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    await app.Services.SeedCatalogDatabaseAsync();
}

// app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new
{
    service = "Catalog",
    status = "OK",
    timestamp = DateTimeOffset.UtcNow
}))
.WithName("HealthCheck")
.WithOpenApi();

app.MapGet("/products", async (
    IProductQueries productQueries,
    CancellationToken cancellationToken) =>
{
    var products = await productQueries.GetProductsAsync(cancellationToken);

    return Results.Ok(products);
})
.WithName("GetProducts")
.WithOpenApi();

app.Run();