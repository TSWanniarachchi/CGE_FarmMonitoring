using System.IO.Compression;
using System.Xml.Linq;

/*
 * Description  : Reads values from the approved Module 01 database specification workbook (.xlsx) for tests.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.ArchitectureTests.Support;

/// <summary>
/// Minimal read-only .xlsx reader (Open XML in a ZIP): returns the first-column values of a sheet,
/// so tests can compare code against the specification instead of a copied list.
/// </summary>
internal static class SpecificationWorkbook
{
    public const string FileName = "Farm_FMS_Module_01_Database_Specification_v0.2.xlsx";

    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static string FilePath =>
        Path.GetFullPath(Path.Combine(SolutionFiles.BackendRoot, "..", "Documentation", "06_Database", FileName));

    /// <summary>
    /// Values of column A of <paramref name="sheetName"/>, excluding the header row and empty cells.
    /// </summary>
    public static IReadOnlyList<string> FirstColumn(string sheetName)
    {
        using var archive = ZipFile.OpenRead(FilePath);

        var sharedStrings = Load(archive, "xl/sharedStrings.xml")?
            .Root!.Elements(Main + "si")
            .Select(si => string.Concat(si.Descendants(Main + "t").Select(t => t.Value)))
            .ToList() ?? [];

        var workbook = Load(archive, "xl/workbook.xml")!;
        var relationshipId = workbook.Descendants(Main + "sheet")
            .Single(s => (string?)s.Attribute("name") == sheetName)
            .Attribute(Relationships + "id")!.Value;

        var target = Load(archive, "xl/_rels/workbook.xml.rels")!
            .Root!.Elements(PackageRelationships + "Relationship")
            .Single(r => (string?)r.Attribute("Id") == relationshipId)
            .Attribute("Target")!.Value.TrimStart('/');

        var sheet = Load(archive, target.StartsWith("xl/", StringComparison.Ordinal) ? target : $"xl/{target}")!;

        return sheet.Descendants(Main + "row")
            .Skip(1)
            .Select(row => row.Elements(Main + "c").FirstOrDefault(c => ((string?)c.Attribute("r"))?.StartsWith('A') == true))
            .Where(cell => cell is not null)
            .Select(cell => CellValue(cell!, sharedStrings))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    /// <summary>
    /// All rows of <paramref name="sheetName"/> except the header row; each row has one entry per column
    /// (A, B, C, ...) up to the last used column, empty string for an empty cell.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<string>> Rows(string sheetName)
    {
        using var archive = ZipFile.OpenRead(FilePath);

        var sharedStrings = Load(archive, "xl/sharedStrings.xml")?
            .Root!.Elements(Main + "si")
            .Select(si => string.Concat(si.Descendants(Main + "t").Select(t => t.Value)))
            .ToList() ?? [];

        var workbook = Load(archive, "xl/workbook.xml")!;
        var relationshipId = workbook.Descendants(Main + "sheet")
            .Single(s => (string?)s.Attribute("name") == sheetName)
            .Attribute(Relationships + "id")!.Value;

        var target = Load(archive, "xl/_rels/workbook.xml.rels")!
            .Root!.Elements(PackageRelationships + "Relationship")
            .Single(r => (string?)r.Attribute("Id") == relationshipId)
            .Attribute("Target")!.Value.TrimStart('/');

        var sheet = Load(archive, target.StartsWith("xl/", StringComparison.Ordinal) ? target : $"xl/{target}")!;

        var rows = sheet.Descendants(Main + "row").Skip(1)
            .Select(row => row.Elements(Main + "c")
                .Select(cell => (Column: ColumnIndex((string?)cell.Attribute("r")), Value: CellValue(cell, sharedStrings)))
                .ToList())
            .Where(cells => cells.Count > 0)
            .ToList();

        var width = rows.Count == 0 ? 0 : rows.Max(cells => cells.Max(c => c.Column)) + 1;

        return rows
            .Select(cells =>
            {
                var values = new string[width];
                Array.Fill(values, string.Empty);
                foreach (var (column, value) in cells) values[column] = value;
                return (IReadOnlyList<string>)values;
            })
            .Where(values => values.Any(v => !string.IsNullOrWhiteSpace(v)))
            .ToList();
    }

    private static int ColumnIndex(string? reference)
    {
        var index = 0;
        foreach (var ch in reference ?? "A")
        {
            if (!char.IsLetter(ch)) break;
            index = index * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
        }

        return index - 1;
    }

    private static string CellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var type = (string?)cell.Attribute("t");
        var value = cell.Element(Main + "v")?.Value;

        return type switch
        {
            "s" when int.TryParse(value, out var index) => sharedStrings[index],
            "inlineStr" => string.Concat(cell.Descendants(Main + "t").Select(t => t.Value)),
            _ => value ?? string.Empty
        };
    }

    private static XDocument? Load(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        if (entry is null) return null;

        using var stream = entry.Open();
        return XDocument.Load(stream);
    }
}
