using Fulfillment.Api.Endpoints;
using Fulfillment.Infrastructure;

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

builder.Services.AddFulfillmentInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    await app.Services.MigrateFulfillmentDatabaseAsync();
}

app.MapGet("/health", () => Results.Ok(new
{
    service = "Fulfillment",
    status = "OK",
    timestamp = DateTimeOffset.UtcNow
}))
.WithName("HealthCheck")
.WithOpenApi();

app.MapFulfillmentEndpoints();

app.Run();
