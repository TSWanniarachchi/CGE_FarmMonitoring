using System.Reflection;
using FarmFMS.Application.Common.Dtos;
using FarmFMS.Domain.Common;

namespace FarmFMS.UnitTests.Application;

/// <summary>
/// The audit base types follow the approved Module 01 specification (Read Me: "Primary keys", "Audit columns").
/// </summary>
public class AuditContractTests
{
    private static readonly (string Name, Type Type)[] AuditColumns =
    [
        ("CreatedBy", typeof(int)),         // INT NOT NULL
        ("CreatedDate", typeof(DateTime)),  // DATETIME2 NOT NULL (UTC)
        ("ModifiedBy", typeof(int?)),       // INT NULL
        ("ModifiedDate", typeof(DateTime?)) // DATETIME2 NULL (UTC)
    ];

    private static Dictionary<string, Type> PublicProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(p => p.Name, p => p.PropertyType);

    [Fact]
    public void AuditableEntity_has_the_specification_key_and_audit_columns()
    {
        var expected = AuditColumns.Prepend((Name: "Id", Type: typeof(int))).ToDictionary(c => c.Name, c => c.Type);

        Assert.True(typeof(AuditableEntity).IsAbstract);
        Assert.Equal(expected, PublicProperties(typeof(AuditableEntity)));
    }

    [Fact]
    public void AuditableDto_exposes_exactly_the_audit_columns()
    {
        Assert.True(typeof(AuditableDto).IsAbstract);
        Assert.Equal(AuditColumns.ToDictionary(c => c.Name, c => c.Type), PublicProperties(typeof(AuditableDto)));
    }

    [Fact]
    public void AuditableDto_is_read_only_after_construction()
    {
        // init-only setters: responses are built once by the service mapping.
        Assert.All(typeof(AuditableDto).GetProperties(), p =>
            Assert.Contains(p.SetMethod!.ReturnParameter.GetRequiredCustomModifiers(),
                m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit"));
    }
}
