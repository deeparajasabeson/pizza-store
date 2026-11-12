using PizzaStore.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("PizzaStore"));

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // Served at /openapi/v1.json; web/ generates its API client from it (npm run gen:api).
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

// Feature endpoint groups (/api/menu, /api/cart, /api/orders, /api/chat, /api/admin) are added here.

app.Run();
