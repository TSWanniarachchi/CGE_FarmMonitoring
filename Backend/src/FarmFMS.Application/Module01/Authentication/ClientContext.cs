using FarmFMS.Domain.Module01;

/*
 * Description  : Client context (IP address and device) recorded with sessions and login history (D-26).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>
/// Caller details recorded in <c>UserSession</c> and <c>LoginHistory</c>. Values are trimmed and truncated to the
/// column lengths, and blank values become <c>null</c>, so they can never make a write fail.
/// </summary>
public sealed class ClientContext
{
    public static readonly ClientContext Empty = new(null, null);

    public ClientContext(string? ipAddress, string? deviceInfo)
    {
        IPAddress = Normalize(ipAddress, Module01FieldLengths.IPAddress);
        DeviceInfo = Normalize(deviceInfo, Module01FieldLengths.DeviceInfo);
    }

    public string? IPAddress { get; }

    public string? DeviceInfo { get; }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
