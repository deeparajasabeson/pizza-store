IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// The API uses the local SQL Server instance via its own User Secrets connection string;
// no database container is started here (see CLAUDE.md "Database").
builder.AddProject<Projects.PizzaStore_Api>("api");

builder.Build().Run();
