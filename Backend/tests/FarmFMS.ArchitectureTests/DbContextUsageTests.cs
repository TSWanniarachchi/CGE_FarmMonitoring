using System.Reflection;
using FarmFMS.ArchitectureTests.Support;
using Microsoft.EntityFrameworkCore;

/*
 * Description  : Verifies that the EF Core DbContext is used for schema and migrations only.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.ArchitectureTests;

/// <summary>
/// No class outside <c>FarmFMS.Infrastructure.Persistence</c> may receive or hold a DbContext,
/// so repositories and services cannot use EF Core for application data.
/// </summary>
public class DbContextUsageTests
{
    private const string PersistenceNamespace = "FarmFMS.Infrastructure.Persistence";

    private const BindingFlags AllMembers =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static IEnumerable<Type> ApplicationTypes() =>
        SolutionFiles.LayerProjects
            .Select(SolutionFiles.LoadLayer)
            .SelectMany(a => a.GetTypes())
            .Where(t => !IsInPersistence(t));

    [Fact]
    public void No_constructor_outside_persistence_takes_a_DbContext()
    {
        var offenders = ApplicationTypes()
            .SelectMany(t => t.GetConstructors(AllMembers)
                .SelectMany(c => c.GetParameters())
                .Where(p => IsDbContext(p.ParameterType))
                .Select(p => $"{t.FullName}({p.ParameterType.Name} {p.Name})"))
            .ToList();

        Assert.True(offenders.Count == 0,
            $"DbContext must not be injected outside {PersistenceNamespace}: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void No_field_or_property_outside_persistence_holds_a_DbContext()
    {
        var offenders = ApplicationTypes()
            .SelectMany(t =>
                t.GetFields(AllMembers).Where(f => IsDbContext(f.FieldType)).Select(f => $"{t.FullName}.{f.Name}")
                .Concat(t.GetProperties(AllMembers).Where(p => IsDbContext(p.PropertyType)).Select(p => $"{t.FullName}.{p.Name}")))
            .ToList();

        Assert.True(offenders.Count == 0,
            $"DbContext must not be held outside {PersistenceNamespace}: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void DbContext_types_live_only_in_persistence()
    {
        var offenders = SolutionFiles.LayerProjects
            .Select(SolutionFiles.LoadLayer)
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(DbContext).IsAssignableFrom(t) && !IsInPersistence(t))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"DbContext classes must live in {PersistenceNamespace}: {string.Join(", ", offenders)}");
    }

    private static bool IsDbContext(Type type) => typeof(DbContext).IsAssignableFrom(type);

    private static bool IsInPersistence(Type type) =>
        type.Namespace?.StartsWith(PersistenceNamespace, StringComparison.Ordinal) == true;
}
