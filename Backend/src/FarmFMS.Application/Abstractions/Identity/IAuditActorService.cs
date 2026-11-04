using FarmFMS.Application.Common.Results;

/*
 * Description  : Defines how services resolve the user ID recorded in CreatedBy / ModifiedBy.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Application.Abstractions.Identity;

/// <summary>
/// Resolves the actor for audit columns (CreatedBy / ModifiedBy):
/// <list type="number">
/// <item>Inside a system operation (<see cref="BeginSystemOperation"/>): the dedicated System user's ID.</item>
/// <item>Otherwise, an authenticated request: the authenticated user's ID.</item>
/// <item>Otherwise: a failed result. Writes without a defined identity are never allowed.</item>
/// </list>
/// Every write service calls <see cref="GetActorIdAsync"/> and stops on failure.
/// </summary>
public interface IAuditActorService
{
    /// <summary>
    /// The user ID to record, or a failed result (Unauthorized when there is no authenticated user,
    /// Failure when a system operation runs but the System user is not configured).
    /// </summary>
    Task<Result<int>> GetActorIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the current scope as a background / system operation until the returned handle is disposed.
    /// Only background jobs, seeders and similar non-request code may call this.
    /// </summary>
    IDisposable BeginSystemOperation(string operationName);
}
