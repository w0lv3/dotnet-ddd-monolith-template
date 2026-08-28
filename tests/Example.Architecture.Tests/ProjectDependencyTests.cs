using System.Xml.Linq;

namespace Example.Architecture.Tests;

public sealed class ProjectDependencyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Domain_HasNoProjectDependencies()
    {
        AssertProjectReferences(
            "src/Example.Domain/Example.Domain.csproj");
    }

    [Fact]
    public void Application_DependsOnlyOnDomain()
    {
        AssertProjectReferences(
            "src/Example.Application/Example.Application.csproj",
            "src/Example.Domain/Example.Domain.csproj");
    }

    [Fact]
    public void Infrastructure_DependsOnlyOnApplicationAndDomain()
    {
        AssertProjectReferences(
            "src/Example.Infrastructure/Example.Infrastructure.csproj",
            "src/Example.Application/Example.Application.csproj",
            "src/Example.Domain/Example.Domain.csproj");
    }

    [Fact]
    public void Api_DependsOnlyOnApplicationAndInfrastructure()
    {
        AssertProjectReferences(
            "src/Example.Api/Example.Api.csproj",
            "src/Example.Application/Example.Application.csproj",
            "src/Example.Infrastructure/Example.Infrastructure.csproj");
    }

    private static void AssertProjectReferences(string projectPath, params string[] expectedReferences)
    {
        var absoluteProjectPath = Path.Combine(RepositoryRoot, projectPath);
        var projectDirectory = Path.GetDirectoryName(absoluteProjectPath)!;
        var document = XDocument.Load(absoluteProjectPath);

        var actualReferences = document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => include is not null)
            .Select(include => include!
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar))
            .Select(include => Path.GetFullPath(include, projectDirectory))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var absoluteExpectedReferences = expectedReferences
            .Select(reference => Path.GetFullPath(reference, RepositoryRoot))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(absoluteExpectedReferences, actualReferences);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (directory.EnumerateFiles("*.slnx").Any()
                && Directory.Exists(Path.Combine(directory.FullName, "src"))
                && Directory.Exists(Path.Combine(directory.FullName, "tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }
}
