using Microsoft.EntityFrameworkCore;

namespace PizzaStore.Infrastructure.Persistence;

public sealed class PizzaStoreDbContext(DbContextOptions<PizzaStoreDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // One IEntityTypeConfiguration<T> per entity in Persistence/Configurations/.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PizzaStoreDbContext).Assembly);
    }
}
