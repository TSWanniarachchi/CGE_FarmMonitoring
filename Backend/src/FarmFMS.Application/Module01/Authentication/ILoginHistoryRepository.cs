using FarmFMS.Domain.Module01.Authentication;

/*
 * Description  : Login history repository contract (ScrLoginHist).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>
/// Append-only login history (spec: no update or delete procedure exists).
/// </summary>
public interface ILoginHistoryRepository
{
    /// <summary>
    /// <c>dbo.ScrLoginHist</c>: returns the new row Id. <paramref name="userId"/> is null for unknown usernames.
    /// Raises 50003 (invalid event type) or 50004 (user not found).
    /// </summary>
    Task<int> CreateAsync(
        int? userId,
        string username,
        LoginEventType eventType,
        string? ipAddress,
        string? deviceInfo,
        string? failureReason,
        int createdBy,
        CancellationToken cancellationToken = default);
}
