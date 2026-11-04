/*
 * Description  : Defines access to the dedicated System user record used for background / system operations.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Application.Abstractions.Identity;

/// <summary>
/// Resolves the ID of the dedicated System user record (an <c>AppUser</c> row, <c>Id INT</c> per the Module 01
/// specification) that background and system operations record as CreatedBy / ModifiedBy.
/// The ID is never hard-coded; it comes from the environment where that record was seeded.
/// Implemented in the Infrastructure layer.
/// </summary>
public interface ISystemUserProvider
{
    /// <summary>
    /// The System user's ID, or null when it is not configured for this environment.
    /// </summary>
    ValueTask<int?> GetSystemUserIdAsync(CancellationToken cancellationToken = default);
}
