using EnterpriseFlow.Domain.Entities;
using FluentAssertions;
using NetArchTest.Rules;
using ApplicationAssemblyMarker = EnterpriseFlow.Application.DependencyInjection;
using InfrastructureAssemblyMarker = EnterpriseFlow.Infrastructure.DependencyInjection;

namespace EnterpriseFlow.Architecture.Tests;

/// <summary>
/// Enforces the Clean Architecture dependency rule from ADR-0002 as an executable test,
/// not just a diagram — a regression here means the layering has actually been violated.
/// </summary>
public class DependencyRuleTests
{
    private const string ApplicationNamespace = "EnterpriseFlow.Application";
    private const string InfrastructureNamespace = "EnterpriseFlow.Infrastructure";
    private const string ApiNamespace = "EnterpriseFlow.Api";

    [Fact]
    public void Domain_Should_Not_Depend_On_Application()
    {
        var result = Types.InAssembly(typeof(Company).Assembly)
            .Should()
            .NotHaveDependencyOn(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(typeof(Company).Assembly)
            .Should()
            .NotHaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Frameworks()
    {
        var result = Types.InAssembly(typeof(Company).Assembly)
            .Should()
            .NotHaveDependencyOnAny("MediatR", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(typeof(ApplicationAssemblyMarker).Assembly)
            .Should()
            .NotHaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(typeof(InfrastructureAssemblyMarker).Assembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Handlers_In_Features_Namespace_Should_Be_Sealed()
    {
        var result = Types.InAssembly(typeof(ApplicationAssemblyMarker).Assembly)
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.Features")
            .And()
            .HaveNameEndingWith("Handler")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    private static string FailureMessage(TestResult result) =>
        result.FailingTypes is null
            ? "Unknown failure"
            : string.Join(", ", result.FailingTypes.Select(t => t.FullName));
}
