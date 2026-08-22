using System.Reflection;
using NetArchTest.Rules;
using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.UnitTests;

/// <summary>
/// Makes the dependency rule in DECISIONS.md #011 machine-checked instead of just a stated intention -
/// this project is a single assembly, so nothing here is compiler-enforced the way it would be with a
/// physical multi-project split.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly ApiAssembly = typeof(Project).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOn_EntityFrameworkCore()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace("TaskFlow.Api.Domain")
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Domain_ShouldNotDependOn_AspNetCore()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace("TaskFlow.Api.Domain")
            .ShouldNot().HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Npgsql()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace("TaskFlow.Api.Domain")
            .ShouldNot().HaveDependencyOn("Npgsql")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    private static string FailureMessage(TestResult result) =>
        result.FailingTypes is null
            ? "Rule failed with no failing types reported."
            : "Violating types: " + string.Join(", ", result.FailingTypes.Select(t => t.FullName));
}
