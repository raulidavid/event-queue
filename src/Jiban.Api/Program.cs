using Jiban.Infrastructure.Configuration;
using Jiban.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// VERIFICACIÓN TEMPORAL
Console.WriteLine($"Entorno actual: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Redis Hostname: {builder.Configuration["Redis:Hostname"]}");
Console.WriteLine($"Redis Password: {builder.Configuration["Redis:Password"]}");

builder.Services.AddJibanRedis(builder.Configuration);
builder.Services.AddJibanHostedServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddControllers();

// Usar OpenAPI nativo de .NET 10 en lugar de Swashbuckle
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Swagger UI disponible en Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Jiban API v1");
    });
    
    // Mostrar la URL de Swagger en la consola
    Console.WriteLine("?? Swagger UI disponible en: https://localhost:56982/swagger");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
