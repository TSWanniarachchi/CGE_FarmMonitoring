using FarmFMS.Application.Common.Results;
using FarmFMS.Infrastructure.Data;

namespace FarmFMS.UnitTests.Infrastructure;

public class SqlErrorTranslatorTests
{
    // Sample catalogue for tests only; the real catalogue has no entries until procedures are approved.
    private static readonly IReadOnlyDictionary<int, Error> SampleCatalog = StoredProcedureErrors.Build(
    [
        new(StoredProcedureErrorRange.Module02, 50101, Error.Conflict("Sample.Duplicate", "The sample already exists.")),
        new(StoredProcedureErrorRange.Module02, 50102, Error.NotFound("Sample.NotFound", "The sample was not found."))
    ]);

    [Theory]
    [InlineData(2601, ErrorType.Conflict, "Database.Duplicate", false)]
    [InlineData(2627, ErrorType.Conflict, "Database.Duplicate", false)]
    [InlineData(547, ErrorType.Conflict, "Database.ConstraintViolation", false)]
    [InlineData(1205, ErrorType.Unavailable, "Database.Deadlock", true)]
    [InlineData(-2, ErrorType.Unavailable, "Database.Timeout", true)]
    [InlineData(4060, ErrorType.Unavailable, "Database.Unavailable", false)]
    [InlineData(18456, ErrorType.Unavailable, "Database.Unavailable", false)]
    [InlineData(10054, ErrorType.Unavailable, "Database.Unavailable", true)]
    public void Known_SQL_Server_errors_are_translated(int number, ErrorType type, string code, bool transient)
    {
        var translated = SqlErrorTranslator.Translate(number, innerException: null, SampleCatalog);

        Assert.Equal(type, translated.Error.Type);
        Assert.Equal(code, translated.Error.Code);
        Assert.Equal(transient, translated.IsTransient);
        Assert.Equal(number, translated.DatabaseErrorNumber);
    }

    [Theory]
    [InlineData(50000)]   // generic procedure error
    [InlineData(50001)]   // business range, not registered
    [InlineData(50999)]   // business range, not registered
    [InlineData(51000)]   // above the business range
    [InlineData(208)]     // unknown SQL Server error (invalid object name)
    public void Generic_and_unknown_errors_return_the_generic_500_error(int number)
    {
        var translated = SqlErrorTranslator.Translate(number, innerException: null, SampleCatalog);

        Assert.Same(SqlErrorTranslator.GenericError, translated.Error);
        Assert.Equal(ErrorType.Failure, translated.Error.Type);
        Assert.False(translated.IsTransient);
    }

    [Theory]
    [InlineData(50101, ErrorType.Conflict, "Sample.Duplicate")]
    [InlineData(50102, ErrorType.NotFound, "Sample.NotFound")]
    public void Registered_business_errors_use_the_catalogue_error(int number, ErrorType type, string code)
    {
        var translated = SqlErrorTranslator.Translate(number, innerException: null, SampleCatalog);

        Assert.Equal(type, translated.Error.Type);
        Assert.Equal(code, translated.Error.Code);
        Assert.Same(SampleCatalog[number], translated.Error);
    }

    [Fact]
    public void Server_and_THROW_messages_are_not_exposed()
    {
        var inner = new InvalidOperationException("THROW 50101: Username 'secret-user' already exists in dbo.AppUser.");

        foreach (var number in new[] { 50000, 50101, 50500, 2601, 208 })
        {
            var translated = SqlErrorTranslator.Translate(number, inner, SampleCatalog);

            Assert.DoesNotContain("secret", translated.Message);
            Assert.DoesNotContain("secret", translated.Error.Message);
            Assert.DoesNotContain("AppUser", translated.Error.Message);
            Assert.Same(inner, translated.InnerException);
        }
    }

    #region Catalogue

    [Fact]
    public void Real_catalogue_is_valid_and_within_the_business_range()
    {
        Assert.All(StoredProcedureErrors.Catalog, entry =>
        {
            Assert.True(StoredProcedureErrors.IsBusinessErrorNumber(entry.Key));
            Assert.Contains(entry.Value.Type, StoredProcedureErrors.AllowedTypes);
        });
    }

    [Theory]
    [InlineData(50000)]
    [InlineData(51000)]
    [InlineData(1205)]
    public void Catalogue_rejects_numbers_outside_the_business_range(int number)
    {
        Assert.Throws<InvalidOperationException>(() =>
            StoredProcedureErrors.Build([new(StoredProcedureErrorRange.Module01, number, Error.Conflict("X.Y", "Message."))]));
    }

    [Fact]
    public void Catalogue_rejects_generic_error_types()
    {
        Assert.Throws<InvalidOperationException>(() =>
            StoredProcedureErrors.Build([new(StoredProcedureErrorRange.Module01, 50001, Error.Failure("X.Y", "Message."))]));
    }

    [Fact]
    public void Catalogue_rejects_duplicate_numbers_and_codes()
    {
        Assert.Throws<InvalidOperationException>(() => StoredProcedureErrors.Build(
        [
            new(StoredProcedureErrorRange.Module01, 50001, Error.Conflict("X.A", "A.")),
            new(StoredProcedureErrorRange.Module01, 50001, Error.Conflict("X.B", "B."))
        ]));

        Assert.Throws<InvalidOperationException>(() => StoredProcedureErrors.Build(
        [
            new(StoredProcedureErrorRange.Module01, 50001, Error.Conflict("X.A", "A.")),
            new(StoredProcedureErrorRange.Module01, 50002, Error.NotFound("X.A", "B."))
        ]));
    }

    [Fact]
    public void Catalogue_rejects_missing_messages()
    {
        Assert.Throws<InvalidOperationException>(() =>
            StoredProcedureErrors.Build([new(StoredProcedureErrorRange.Module01, 50001, Error.Conflict("X.Y", " "))]));
    }

    [Fact]
    public void Ranges_match_the_approved_AD09_allocation()
    {
        Assert.Collection(StoredProcedureErrorRange.All,
            r => Assert.Equal((50001, 50099), (r.First, r.Last)),
            r => Assert.Equal((50100, 50199), (r.First, r.Last)),
            r => Assert.Equal((50200, 50299), (r.First, r.Last)),
            r => Assert.Equal((50300, 50999), (r.First, r.Last)));
    }

    [Fact]
    public void Ranges_cover_the_business_range_contiguously_without_overlap()
    {
        var ranges = StoredProcedureErrorRange.All;

        Assert.Equal(StoredProcedureErrors.FirstBusinessError, ranges[0].First);
        Assert.Equal(StoredProcedureErrors.LastBusinessError, ranges[^1].Last);
        for (var i = 1; i < ranges.Count; i++)
            Assert.Equal(ranges[i - 1].Last + 1, ranges[i].First);
    }

    [Fact]
    public void Catalogue_accepts_the_first_and_last_number_of_every_range()
    {
        var entries = StoredProcedureErrorRange.All
            .SelectMany(range => new[]
            {
                new StoredProcedureErrorEntry(range, range.First, Error.Conflict($"R{range.First}.First", "First.")),
                new StoredProcedureErrorEntry(range, range.Last, Error.Conflict($"R{range.First}.Last", "Last."))
            });

        var catalog = StoredProcedureErrors.Build(entries);

        Assert.Equal(StoredProcedureErrorRange.All.Count * 2, catalog.Count);
    }

    [Theory]
    [InlineData(50100)] // Module 02
    [InlineData(50200)] // Module 03
    [InlineData(50300)] // shared / future
    public void Catalogue_rejects_a_number_outside_its_module_range(int number)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            StoredProcedureErrors.Build([new(StoredProcedureErrorRange.Module01, number, Error.Conflict("X.Y", "Message."))]));

        Assert.Contains("Module 01", exception.Message);
    }

    [Fact]
    public void Catalogue_rejects_ranges_that_are_not_approved()
    {
        var unapproved = new StoredProcedureErrorRange("Module 01", 50001, 50999);

        Assert.Throws<InvalidOperationException>(() =>
            StoredProcedureErrors.Build([new(unapproved, 50500, Error.Conflict("X.Y", "Message."))]));
    }

    #endregion
}
