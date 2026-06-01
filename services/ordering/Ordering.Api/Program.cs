using Ordering.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOrderingInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    await app.Services.MigrateOrderingDatabaseAsync();
}

// app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new
{
    service = "Ordering",
    status = "OK",
    timestamp = DateTimeOffset.UtcNow
}))
.WithName("HealthCheck")
.WithOpenApi();

app.Run();