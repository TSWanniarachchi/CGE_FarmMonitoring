using System.ComponentModel.DataAnnotations;

/*
 * Description  : Defines the validated database settings bound from the "Database" configuration section.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.Data;

/// <summary>
/// Database settings. The connection string itself stays in <c>ConnectionStrings:FarmFMS</c>
/// (user-secrets / environment variables for real credentials).
/// Validated at startup; the API does not start with invalid values.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Command timeout in seconds for every Stored Procedure call.
    /// </summary>
    [Range(1, 600, ErrorMessage = "Database:CommandTimeoutSeconds must be between 1 and 600.")]
    public int CommandTimeoutSeconds { get; set; } = 30;
}
