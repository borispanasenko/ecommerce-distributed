using Ordering.Infrastructure;
using Ordering.Api.Endpoints;
using Ordering.Api.BackgroundServices;


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

builder.Services.AddOrderingInfrastructure(builder.Configuration);
builder.Services.Configure<OrderExpirationOptions>(
    builder.Configuration.GetSection(OrderExpirationOptions.SectionName));
builder.Services.AddHostedService<OrderExpirationBackgroundService>();

var app = builder.Build();
app.UseCors("Frontend");

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

app.MapOrderingEndpoints();

app.Run();
