using System.Reflection;
using Shouldly;

namespace PizzaStore.UnitTests;

/// <summary>Enforces the dependency rule in CLAUDE.md: Domain ← Application ← Infrastructure / AI ← Api.</summary>
public sealed class ArchitectureTests
{
    private static readonly string[] ForbiddenForCoreLayers =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Azure",
        "PizzaStore.Infrastructure",
        "PizzaStore.AI",
        "PizzaStore.Api",
    ];

    [Theory]
    [InlineData("PizzaStore.Domain")]
    [InlineData("PizzaStore.Application")]
    public void Core_layer_does_not_reference_infrastructure_web_or_azure(string assemblyName)
    {
        IEnumerable<string> references = ReferencedAssemblyNames(assemblyName);

        references.ShouldNotContain(
            r => ForbiddenForCoreLayers.Any(f => r.StartsWith(f, StringComparison.Ordinal)),
            $"{assemblyName} must stay free of EF Core, ASP.NET Core, Azure SDKs and outer layers.");
    }

    [Fact]
    public void Domain_does_not_reference_application()
    {
        ReferencedAssemblyNames("PizzaStore.Domain").ShouldNotContain("PizzaStore.Application");
    }

    internal static IEnumerable<string> ReferencedAssemblyNames(string assemblyName) =>
        Assembly.Load(assemblyName).GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);
}
