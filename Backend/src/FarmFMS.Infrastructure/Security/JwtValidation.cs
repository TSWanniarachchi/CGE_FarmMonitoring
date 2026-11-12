using Microsoft.IdentityModel.Tokens;

/*
 * Description  : Shared token validation parameters (API JwtBearer and tests validate identically) (Step 0 spec 8.2).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Security;

/// <summary>
/// Single source of the access-token validation rules: HS256 only, signature, issuer, audience and lifetime
/// validated, skew from <see cref="JwtOptions"/>, <c>name</c> is the name claim. The API must also set
/// <c>JwtBearerOptions.MapInboundClaims = false</c> so that claim names are not remapped.
/// </summary>
public static class JwtValidation
{
    /// <summary>Claim name of the user Id (<c>AppUser.Id</c>).</summary>
    public const string SubjectClaim = "sub";

    /// <summary>Claim name of the session Id (<c>UserSession.Id</c>).</summary>
    public const string SessionIdClaim = "sid";

    /// <summary>Claim name of the username.</summary>
    public const string NameClaim = "name";

    /// <summary>
    /// Builds the <see cref="TokenValidationParameters"/> for the API (<c>JwtBearerOptions.TokenValidationParameters</c>)
    /// and for tests. Throws <see cref="InvalidOperationException"/> when the options are invalid.
    /// </summary>
    public static TokenValidationParameters CreateParameters(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(options.GetSigningKeyBytes()),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds),
            NameClaimType = NameClaim
        };
    }
}
