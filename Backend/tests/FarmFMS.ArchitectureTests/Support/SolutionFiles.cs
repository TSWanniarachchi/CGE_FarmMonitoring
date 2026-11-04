using System.Reflection;
using System.Xml.Linq;

/*
 * Description  : Locates the Backend solution and reads project files and C# sources for architecture tests.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.ArchitectureTests.Support;

/// <summary>
/// File-system helpers for the architecture tests (project references, package references, source files).
/// </summary>
internal static class SolutionFiles
{
    private const string SolutionFileName = "FarmFMS.sln";

    public static readonly string[] LayerProjects =
        ["FarmFMS.Domain", "FarmFMS.Application", "FarmFMS.Infrastructure", "FarmFMS.API"];

    private static readonly Lazy<string> Root = new(FindRoot);

    /// <summary>
    /// The Backend folder (contains FarmFMS.sln).
    /// </summary>
    public static string BackendRoot => Root.Value;

    public static string SourceRoot => Path.Combine(BackendRoot, "src");

    /// <summary>
    /// The repository Database folder (version-controlled SQL scripts), next to Backend.
    /// </summary>
    public static string DatabaseRoot => Path.GetFullPath(Path.Combine(BackendRoot, "..", "Database"));

    /// <summary>
    /// SQL scripts under Database/ (relative path with '/' separators, text).
    /// </summary>
    public static IReadOnlyList<SourceFile> SqlScripts =>
        Directory.Exists(DatabaseRoot)
            ? Directory.EnumerateFiles(DatabaseRoot, "*.sql", SearchOption.AllDirectories)
                .Select(path => new SourceFile(Path.GetRelativePath(DatabaseRoot, path).Replace('\\', '/'), File.ReadAllText(path)))
                .ToList()
            : [];

    public static Assembly LoadLayer(string projectName) => Assembly.Load(projectName);

    #region Project files

    public static string ProjectFile(string projectName) =>
        Path.Combine(SourceRoot, projectName, $"{projectName}.csproj");

    /// <summary>
    /// Names of the projects referenced by &lt;ProjectReference&gt; (file name without extension).
    /// </summary>
    public static IReadOnlyList<string> ProjectReferences(string projectName) =>
        ReadIncludes(projectName, "ProjectReference")
            .Select(path => Path.GetFileNameWithoutExtension(path.Replace('\\', '/')))
            .ToList();

    /// <summary>
    /// Package IDs referenced by &lt;PackageReference&gt;.
    /// </summary>
    public static IReadOnlyList<string> PackageReferences(string projectName) =>
        ReadIncludes(projectName, "PackageReference").ToList();

    private static IEnumerable<string> ReadIncludes(string projectName, string elementName)
    {
        var document = XDocument.Load(ProjectFile(projectName));

        return document
            .Descendants()
            .Where(e => e.Name.LocalName == elementName)
            .Select(e => (string?)e.Attribute("Include"))
            .OfType<string>();
    }

    #endregion

    #region Source files

    /// <summary>
    /// A C# source file under src/ (bin and obj excluded).
    /// RelativePath uses '/' separators, e.g. "FarmFMS.Infrastructure/Data/UnitOfWork.cs".
    /// </summary>
    public sealed record SourceFile(string RelativePath, string Text)
    {
        public bool IsUnder(string relativeFolder) =>
            RelativePath.StartsWith(relativeFolder.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);

        public bool IsMigration => RelativePath.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly Lazy<IReadOnlyList<SourceFile>> Sources = new(LoadSources);

    public static IReadOnlyList<SourceFile> SourceFiles => Sources.Value;

    private static IReadOnlyList<SourceFile> LoadSources()
    {
        var separator = Path.DirectorySeparatorChar;

        return Directory
            .EnumerateFiles(SourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{separator}bin{separator}") && !path.Contains($"{separator}obj{separator}"))
            .Select(path => new SourceFile(
                Path.GetRelativePath(SourceRoot, path).Replace('\\', '/'),
                File.ReadAllText(path)))
            .ToList();
    }

    #endregion

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new InvalidOperationException($"Could not find {SolutionFileName} above {AppContext.BaseDirectory}.");
    }
}
