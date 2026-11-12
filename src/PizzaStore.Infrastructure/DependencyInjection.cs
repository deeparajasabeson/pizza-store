using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PizzaStore.Infrastructure.Persistence;

namespace PizzaStore.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence services. The connection string comes from User Secrets locally
    /// (ConnectionStrings:PizzaStore) and Azure Key Vault when deployed; it may be null at
    /// design time, so SQL Server is configured without it until a real query runs.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString)
    {
        services.AddDbContext<PizzaStoreDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
