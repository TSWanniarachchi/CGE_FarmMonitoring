/*
 * Description  : Names of the standard write-result output parameters of Stored Procedures (AD-09).
 * Author       : T.S.Wanniarachchi
 * Created Date : 05-11-2026
 */
namespace FarmFMS.Infrastructure.Data;

/// <summary>
/// Standard write-result contract (AD-09): write metadata is returned through output parameters, never through
/// ad-hoc <c>SELECT</c> rows. The names are reserved: callers must not pass parameters with these names.
/// Names are given without <c>@</c> (Dapper parameter convention); in the procedure they are <c>@NewId</c> and
/// <c>@RowsAffected</c>.
/// </summary>
public static class StoredProcedureOutputParameters
{
    /// <summary>
    /// <c>@NewId INT OUTPUT</c>: set by insert procedures to the new row's <c>Id</c>.
    /// </summary>
    public const string NewId = "NewId";

    /// <summary>
    /// <c>@RowsAffected INT OUTPUT</c>: set by update, deactivate and delete procedures to the number of rows changed.
    /// </summary>
    public const string RowsAffected = "RowsAffected";
}
