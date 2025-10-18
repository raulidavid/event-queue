using Jiban.Domain;
using Jiban.AspNetCore;
using Jiban.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Optional: show some config values
Console.WriteLine($"Entorno actual: {builder.Environment.EnvironmentName}");

// Ensure IHttpContextAccessor is available (GetDataKeyFromUserNormal needs it)
builder.Services.AddHttpContextAccessor();

// expose webHostBuilder for APIs expecting it
var webHostBuilder = builder.WebHost;

var setup = builder.Services.MultiTenant<DefaultPermissions>(webHostBuilder, builder.Configuration);
builder.Services.AddJibanJwtServices(setup.Options);
builder.Services.AddJibanRedis(builder.Configuration);
builder.Services.AddJibanAwsServices(builder.Configuration);

builder.Services.AddJibanHostedServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// pipeline...
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Jiban Event v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();