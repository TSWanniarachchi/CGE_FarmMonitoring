using FarmFMS.Application.Common.Results;
using FarmFMS.Infrastructure.Data;

namespace FarmFMS.UnitTests.Infrastructure;

public class SqlErrorTranslatorTests
{
    // Sample catalogue for tests only; the real catalogue has no entries until procedures are approved.
    private static readonly IReadOnlyDictionary<int, Error> SampleCatalog = StoredProcedureErrors.Build(
    [
        new(50101, Error.Conflict("Sample.Duplicate", "The sample already exists.")),
        new(50102, Error.NotFound("Sample.NotFound", "The sample was not found."))
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
            StoredProcedureErrors.Build([new(number, Error.Conflict("X.Y", "Message."))]));
    }

    [Fact]
    public void Catalogue_rejects_generic_error_types()
    {
        Assert.Throws<InvalidOperationException>(() =>
            StoredProcedureErrors.Build([new(50001, Error.Failure("X.Y", "Message."))]));
    }

    [Fact]
    public void Catalogue_rejects_duplicate_numbers_and_codes()
    {
        Assert.Throws<InvalidOperationException>(() => StoredProcedureErrors.Build(
        [
            new(50001, Error.Conflict("X.A", "A.")),
            new(50001, Error.Conflict("X.B", "B."))
        ]));

        Assert.Throws<InvalidOperationException>(() => StoredProcedureErrors.Build(
        [
            new(50001, Error.Conflict("X.A", "A.")),
            new(50002, Error.NotFound("X.A", "B."))
        ]));
    }

    [Fact]
    public void Catalogue_rejects_missing_messages()
    {
        Assert.Throws<InvalidOperationException>(() =>
            StoredProcedureErrors.Build([new(50001, Error.Conflict("X.Y", " "))]));
    }

    #endregion
}
