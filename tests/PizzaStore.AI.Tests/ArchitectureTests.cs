using System.Reflection;
using Shouldly;

namespace PizzaStore.AI.Tests;

/// <summary>
/// The assistant reaches data only through its tools and Application services
/// (CLAUDE.md "Things to avoid"), so PizzaStore.AI must never reference EF Core,
/// Infrastructure, or the Api host.
/// </summary>
public sealed class ArchitectureTests
{
    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Microsoft.Data.SqlClient")]
    [InlineData("PizzaStore.Infrastructure")]
    [InlineData("PizzaStore.Api")]
    public void AI_layer_does_not_reference(string forbiddenPrefix)
    {
        IEnumerable<string> references = Assembly.Load("PizzaStore.AI")
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty);

        references.ShouldNotContain(r => r.StartsWith(forbiddenPrefix, StringComparison.Ordinal));
    }
}
