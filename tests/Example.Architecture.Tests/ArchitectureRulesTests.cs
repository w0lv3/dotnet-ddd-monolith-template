using System.Reflection;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Example.Api.Controllers;
using Example.Application.Interfaces.Services;
using Example.Domain.Entities;
using Example.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchArchitecture = ArchUnitNET.Domain.Architecture;
using ReflectionAssembly = System.Reflection.Assembly;
using ReflectionType = System.Type;

namespace Example.Architecture.Tests;

public sealed class ArchitectureRulesTests
{
    private static readonly ReflectionAssembly ApiAssembly = typeof(ExamplesController).Assembly;
    private static readonly ReflectionAssembly ApplicationAssembly = typeof(IExampleService).Assembly;
    private static readonly ReflectionAssembly DomainAssembly = typeof(ExampleEntity).Assembly;
    private static readonly ReflectionAssembly InfrastructureAssembly = typeof(ApplicationDbContext).Assembly;

    private static readonly ArchArchitecture Architecture = new ArchLoader()
        .LoadAssemblies(ApiAssembly, ApplicationAssembly, DomainAssembly, InfrastructureAssembly)
        .Build();

    private static readonly IObjectProvider<IType> ApiLayer = AssemblyTypes(ApiAssembly, "API layer");
    private static readonly IObjectProvider<IType> ApplicationLayer = AssemblyTypes(ApplicationAssembly, "Application layer");
    private static readonly IObjectProvider<IType> DomainLayer = AssemblyTypes(DomainAssembly, "Domain layer");
    private static readonly IObjectProvider<IType> InfrastructureLayer = AssemblyTypes(InfrastructureAssembly, "Infrastructure layer");

    [Fact]
    public void Domain_HasNoForbiddenAssemblyDependencies()
    {
        AssertNoDependency(DomainLayer, ApplicationLayer, "Domain must not depend on Application");
        AssertNoDependency(DomainLayer, InfrastructureLayer, "Domain must not depend on Infrastructure");
        AssertNoDependency(DomainLayer, ApiLayer, "Domain must not depend on API");
    }

    [Fact]
    public void Application_HasNoForbiddenAssemblyDependencies()
    {
        AssertNoDependency(ApplicationLayer, InfrastructureLayer, "Application must not depend on Infrastructure");
        AssertNoDependency(ApplicationLayer, ApiLayer, "Application must not depend on API");
    }

    [Fact]
    public void Infrastructure_HasNoForbiddenAssemblyDependencies()
    {
        AssertNoDependency(InfrastructureLayer, ApiLayer, "Infrastructure must not depend on API");
    }

    [Fact]
    public void Domain_HasNoFrameworkLeakage()
    {
        AssertNoNamespaceDependency(DomainLayer, "Microsoft.EntityFrameworkCore", "Domain must remain independent of EF Core");
        AssertNoNamespaceDependency(DomainLayer, "Microsoft.AspNetCore", "Domain must remain independent of ASP.NET Core");
        AssertNoNamespaceDependency(DomainLayer, "AutoMapper", "Domain must remain independent of AutoMapper");
        AssertNoNamespaceDependency(DomainLayer, "FluentValidation", "Domain must remain independent of FluentValidation");
    }

    [Fact]
    public void Application_HasNoFrameworkLeakage()
    {
        AssertNoNamespaceDependency(ApplicationLayer, "Microsoft.EntityFrameworkCore", "Application must remain independent of EF Core");
        AssertNoNamespaceDependency(ApplicationLayer, "Microsoft.AspNetCore", "Application must remain independent of ASP.NET Core");
    }

    [Fact]
    public void Controllers_BelongToApi()
    {
        Classes().That().AreAssignableTo(typeof(ControllerBase)).Should().ResideInAssembly(ApiAssembly)
            .Because("controllers belong to the API layer")
            .Check(Architecture);
    }

    [Fact]
    public void Validators_BelongToApplication()
    {
        Classes().That().AreAssignableTo(typeof(IValidator)).Should().ResideInAssembly(ApplicationAssembly)
            .Because("validators belong to the Application layer")
            .Check(Architecture);
    }

    [Fact]
    public void RepositoryInterfaces_BelongToApplication()
    {
        Interfaces().That().HaveNameEndingWith("Repository").Should().ResideInAssembly(ApplicationAssembly)
            .Because("repository abstractions belong to the Application layer")
            .Check(Architecture);
    }

    [Fact]
    public void RepositoryImplementations_BelongToInfrastructure()
    {
        var repositoryInterfaces = Interfaces().That().HaveNameEndingWith("Repository").As("repository interfaces");

        Classes().That().AreAssignableTo(repositoryInterfaces).Should().ResideInAssembly(InfrastructureAssembly)
            .Because("repository implementations belong to the Infrastructure layer")
            .Check(Architecture);
    }

    [Fact]
    public void DbContexts_BelongToInfrastructure()
    {
        Classes().That().AreAssignableTo(typeof(DbContext)).Should().ResideInAssembly(InfrastructureAssembly)
            .Because("DbContext types belong to the Infrastructure layer")
            .Check(Architecture);
    }

    [Fact]
    public void EntityConfigurations_BelongToInfrastructure()
    {
        Classes().That().ImplementInterface(typeof(IEntityTypeConfiguration<>)).Should().ResideInAssembly(InfrastructureAssembly)
            .Because("EF Core entity configurations belong to the Infrastructure layer")
            .Check(Architecture);
    }

    [Fact]
    public void Controllers_DoNotDependOnPersistenceImplementations()
    {
        var controllers = Classes().That().AreAssignableTo(typeof(ControllerBase)).As("controllers");
        var repositoryInterfaces = Interfaces().That().HaveNameEndingWith("Repository").As("repository interfaces");
        var persistenceImplementations = Classes().That()
            .AreAssignableTo(typeof(DbContext)).Or()
            .AreAssignableTo(repositoryInterfaces)
            .As("DbContext and repository implementations");

        Classes().That().Are(controllers).Should().NotDependOnAny(persistenceImplementations)
            .Because("controllers must call Application services instead of persistence implementations")
            .Check(Architecture);
    }

    [Fact]
    public void ControllerActionResults_DoNotExposeDomainEntities()
    {
        var violations = ApiAssembly.GetTypes()
            .Where(type => !type.IsAbstract && InheritsFrom(type, "Microsoft.AspNetCore.Mvc.ControllerBase"))
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(method => ContainsDomainEntity(method.ReturnType))
            .Select(method => $"{method.DeclaringType!.FullName}.{method.Name} returns {method.ReturnType}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Controller action results must not expose Domain entities:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static IObjectProvider<IType> AssemblyTypes(ReflectionAssembly assembly, string description)
    {
        return Types().That().ResideInAssembly(assembly).As(description);
    }

    private static void AssertNoDependency(
        IObjectProvider<IType> source,
        IObjectProvider<IType> target,
        string reason)
    {
        Types().That().Are(source).Should().NotDependOnAny(target)
            .Because(reason)
            .Check(Architecture);
    }

    private static void AssertNoNamespaceDependency(
        IObjectProvider<IType> source,
        string namespaceName,
        string reason)
    {
        var namespacePattern = $"^{Regex.Escape(namespaceName)}(\\..*)?$";
        var frameworkTypes = Types().That().ResideInNamespaceMatching(namespacePattern).As(namespaceName);

        Types().That().Are(source).Should().NotDependOnAny(frameworkTypes)
            .Because(reason)
            .Check(Architecture);
    }

    private static bool ContainsDomainEntity(ReflectionType type)
    {
        if (type.Assembly == DomainAssembly
            && type.Namespace?.StartsWith(typeof(ExampleEntity).Namespace!, StringComparison.Ordinal) == true)
        {
            return true;
        }

        if (type.HasElementType && type.GetElementType() is { } elementType)
        {
            return ContainsDomainEntity(elementType);
        }

        return type.IsGenericType && type.GetGenericArguments().Any(ContainsDomainEntity);
    }

    private static bool InheritsFrom(ReflectionType type, string baseTypeFullName)
    {
        return type.BaseType is { } baseType
            && (baseType.FullName == baseTypeFullName || InheritsFrom(baseType, baseTypeFullName));
    }
}
