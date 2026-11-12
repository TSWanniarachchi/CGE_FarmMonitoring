using FarmFMS.Application.Module01.Authentication;
using Microsoft.Net.Http.Headers;

/*
 * Description  : Builds the client context (IP address, device) recorded with sessions and login history (D-26).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.API.Common.Security;

internal static class ClientContextFactory
{
    /// <summary>
    /// The IP is <c>Connection.RemoteIpAddress</c> (already replaced by the forwarded header when it came from a
    /// trusted proxy); the device is the User-Agent. <see cref="ClientContext"/> truncates to the column lengths.
    /// </summary>
    public static ClientContext From(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ip = context.Connection.RemoteIpAddress;
        if (ip is { IsIPv4MappedToIPv6: true })
            ip = ip.MapToIPv4();

        return new ClientContext(ip?.ToString(), context.Request.Headers[HeaderNames.UserAgent].ToString());
    }
}
