using System.Globalization;
using FarmFMS.Application.Module01.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

/*
 * Description  : Issues HS256 JWT access tokens (Step 0 spec sections 3.1, 7.2; D-09, D-11, D-23).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Security;

/// <summary>
/// Claims: <c>sub</c> (AppUser.Id), <c>sid</c> (UserSession.Id), <c>name</c> (username), <c>jti</c>, <c>iat</c>, <c>nbf</c>,
/// <c>exp</c>, <c>iss</c>, <c>aud</c>. No permission claims (D-11): permissions are resolved per request.
/// Times come from <see cref="TimeProvider"/> (UTC).
/// </summary>
internal sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;
    private readonly TimeProvider _timeProvider;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(_options.GetSigningKeyBytes()),
            SecurityAlgorithms.HmacSha256);
    }

    public AccessToken Issue(int userId, int sessionId, string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        // JWT times have one-second precision; use the truncated values everywhere so the response matches the token.
        var now = TruncateToSeconds(_timeProvider.GetUtcNow().UtcDateTime);
        var expires = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = _credentials,
            Claims = new Dictionary<string, object>
            {
                [JwtValidation.SubjectClaim] = userId.ToString(CultureInfo.InvariantCulture),
                [JwtValidation.SessionIdClaim] = sessionId.ToString(CultureInfo.InvariantCulture),
                [JwtValidation.NameClaim] = username,
                ["jti"] = Guid.NewGuid().ToString("N")
            }
        };

        return new AccessToken(_handler.CreateToken(descriptor), expires);
    }

    private static DateTime TruncateToSeconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
}
