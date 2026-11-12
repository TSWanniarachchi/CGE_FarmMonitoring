using System.Security.Cryptography;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Infrastructure.Identity;
using FarmFMS.Infrastructure.Security;
using FarmFMS.UnitTests.Module01.Fakes;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FarmFMS.UnitTests.Module01;

/// <summary>JWT issuing / validation, refresh tokens and the startup validation of the token options (spec 7.2, 10.1).</summary>
public class TokenTests
{
    private static string NewKey(int bytes = 32) => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes));

    private static JwtOptions Options(Action<JwtOptions>? configure = null)
    {
        var options = new JwtOptions { SigningKey = NewKey() };
        configure?.Invoke(options);
        return options;
    }

    private static JwtAccessTokenIssuer Issuer(JwtOptions options, DateTimeOffset? now = null) =>
        new(Microsoft.Extensions.Options.Options.Create(options), new TestTimeProvider(now ?? DateTimeOffset.UtcNow));

    private static async Task<TokenValidationResult> ValidateAsync(string token, JwtOptions options) =>
        await new JsonWebTokenHandler().ValidateTokenAsync(token, JwtValidation.CreateParameters(options));

    #region JWT

    [Fact]
    public async Task Issued_token_validates_with_the_shared_parameters_and_carries_the_claims()
    {
        var options = Options();
        var now = DateTimeOffset.UtcNow;
        var token = Issuer(options, now).Issue(42, 501, "jsmith");

        var result = await ValidateAsync(token.Token, options);

        Assert.True(result.IsValid, result.Exception?.Message);
        var jwt = (JsonWebToken)result.SecurityToken;
        Assert.Equal("42", jwt.GetClaim("sub").Value);
        Assert.Equal("501", jwt.GetClaim("sid").Value);
        Assert.Equal("jsmith", jwt.GetClaim("name").Value);
        Assert.Equal("FarmFMS.API", jwt.Issuer);
        Assert.Contains("FarmFMS", jwt.Audiences);
        Assert.False(string.IsNullOrEmpty(jwt.Id)); // jti
        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Alg);
        Assert.Equal("jsmith", result.ClaimsIdentity.Name);

        // iat / nbf / exp from the TimeProvider; 10 minute lifetime; ExpiresUtc equals the exp claim.
        var issued = DateTimeOffset.FromUnixTimeSeconds(now.ToUnixTimeSeconds()).UtcDateTime;
        Assert.Equal(issued, jwt.IssuedAt);
        Assert.Equal(issued, jwt.ValidFrom);
        Assert.Equal(issued.AddMinutes(10), jwt.ValidTo);
        Assert.Equal(jwt.ValidTo, token.ExpiresUtc);
        Assert.Equal(DateTimeKind.Utc, token.ExpiresUtc.Kind);
    }

    [Fact]
    public void The_token_has_no_permission_or_role_claims()
    {
        var options = Options();
        var token = Issuer(options).Issue(42, 501, "jsmith");

        var jwt = new JsonWebToken(token.Token);

        var names = jwt.Claims.Select(c => c.Type).Order().ToArray();
        Assert.Equal(["aud", "exp", "iat", "iss", "jti", "name", "nbf", "sid", "sub"], names);
    }

    [Fact]
    public void Every_token_gets_a_distinct_jti()
    {
        var issuer = Issuer(Options());

        var first = new JsonWebToken(issuer.Issue(1, 1, "a").Token).Id;
        var second = new JsonWebToken(issuer.Issue(1, 1, "a").Token).Id;

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task A_token_expired_beyond_the_skew_is_rejected_within_the_skew_accepted()
    {
        var options = Options();

        // Lifetime 10 min: issued 11 minutes ago -> expired 1 minute ago (skew 30 s) -> rejected.
        var expired = Issuer(options, DateTimeOffset.UtcNow.AddMinutes(-11)).Issue(1, 1, "a");
        Assert.False((await ValidateAsync(expired.Token, options)).IsValid);

        // Expired 10 seconds ago: inside the 30 s skew -> still valid.
        var withinSkew = Issuer(options, DateTimeOffset.UtcNow.AddMinutes(-10).AddSeconds(-10)).Issue(1, 1, "a");
        Assert.True((await ValidateAsync(withinSkew.Token, options)).IsValid);
    }

    [Fact]
    public async Task A_token_that_is_not_yet_valid_beyond_the_skew_is_rejected()
    {
        var options = Options();
        var future = Issuer(options, DateTimeOffset.UtcNow.AddMinutes(5)).Issue(1, 1, "a");

        Assert.False((await ValidateAsync(future.Token, options)).IsValid);
    }

    [Fact]
    public async Task Wrong_issuer_audience_and_signature_are_rejected()
    {
        var options = Options();
        var token = Issuer(options).Issue(1, 1, "a").Token;

        var otherIssuer = Options(o => { o.SigningKey = options.SigningKey; o.Issuer = "Someone.Else"; });
        var otherAudience = Options(o => { o.SigningKey = options.SigningKey; o.Audience = "Other"; });
        var otherKey = Options(); // different random key

        Assert.False((await ValidateAsync(token, otherIssuer)).IsValid);
        Assert.False((await ValidateAsync(token, otherAudience)).IsValid);
        Assert.False((await ValidateAsync(token, otherKey)).IsValid);
    }

    [Fact]
    public async Task A_tampered_payload_is_rejected()
    {
        var options = Options();
        var token = Issuer(options).Issue(1, 1, "a").Token;
        var parts = token.Split('.');
        var payload = parts[1].Length > 4 ? parts[1][..^2] + (parts[1][^2] == 'A' ? "B" : "A") + parts[1][^1] : parts[1];

        Assert.False((await ValidateAsync($"{parts[0]}.{payload}.{parts[2]}", options)).IsValid);
    }

    [Fact]
    public async Task Only_HS256_is_accepted()
    {
        // A 512-bit key, so the HS512 token below is validly signed with the right key: only the algorithm is wrong.
        var options = Options(o => o.SigningKey = NewKey(64));
        var key = new SymmetricSecurityKey(options.GetSigningKeyBytes());
        var hs512 = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512)
        });
        var unsigned = new JsonWebTokenHandler().CreateToken("{\"iss\":\"FarmFMS.API\",\"aud\":\"FarmFMS\",\"exp\":" +
            DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds() + "}");

        Assert.False((await ValidateAsync(hs512, options)).IsValid);
        Assert.False((await ValidateAsync(unsigned, options)).IsValid);
    }

    [Fact]
    public void The_validation_parameters_follow_the_approved_settings()
    {
        var parameters = JwtValidation.CreateParameters(Options(o => o.ClockSkewSeconds = 20));

        Assert.True(parameters.ValidateIssuer && parameters.ValidateAudience && parameters.ValidateLifetime && parameters.ValidateIssuerSigningKey);
        Assert.Equal(TimeSpan.FromSeconds(20), parameters.ClockSkew);
        Assert.Equal("name", parameters.NameClaimType);
        Assert.Equal([SecurityAlgorithms.HmacSha256], parameters.ValidAlgorithms);
    }

    [Fact]
    public void The_configured_lifetime_is_used()
    {
        var options = Options(o => o.AccessTokenLifetimeMinutes = 5);
        var now = DateTimeOffset.UtcNow;

        var token = Issuer(options, now).Issue(1, 1, "a");

        Assert.Equal(5, (token.ExpiresUtc - now.UtcDateTime).TotalMinutes, precision: 0);
    }

    #endregion

    #region JwtOptions validation

    private static string[] Failures(JwtOptions options) =>
        [.. new JwtOptionsValidator().Validate(null, options).Failures ?? []];

    [Fact]
    public void Default_options_with_a_strong_key_are_valid()
    {
        var options = Options();

        Assert.Empty(Failures(options));
        Assert.Equal(("FarmFMS.API", "FarmFMS", 10, 30), (options.Issuer, options.Audience, options.AccessTokenLifetimeMinutes, options.ClockSkewSeconds));
    }

    [Theory]
    [InlineData(16)]
    [InlineData(31)]
    public void A_short_signing_key_fails_startup_validation(int bytes)
    {
        Assert.Contains(Failures(Options(o => o.SigningKey = NewKey(bytes))), f => f.Contains("SigningKey"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("%%% not base64 %%%")]
    public void A_missing_or_malformed_signing_key_fails(string key)
    {
        Assert.Contains(Failures(Options(o => o.SigningKey = key)), f => f.Contains("SigningKey"));
    }

    [Fact]
    public void A_32_byte_key_is_the_minimum_and_longer_keys_are_accepted()
    {
        Assert.Empty(Failures(Options(o => o.SigningKey = NewKey(32))));
        Assert.Empty(Failures(Options(o => o.SigningKey = NewKey(64))));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(-1)]
    public void Access_token_lifetime_must_be_1_to_30_minutes(int minutes)
    {
        Assert.NotEmpty(Failures(Options(o => o.AccessTokenLifetimeMinutes = minutes)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    public void Access_token_lifetime_bounds_are_accepted(int minutes)
    {
        Assert.Empty(Failures(Options(o => o.AccessTokenLifetimeMinutes = minutes)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(61)]
    public void Clock_skew_must_be_0_to_60_seconds(int seconds)
    {
        Assert.NotEmpty(Failures(Options(o => o.ClockSkewSeconds = seconds)));
    }

    [Fact]
    public void Issuer_and_audience_are_required_and_messages_never_contain_the_key()
    {
        var options = Options(o => { o.Issuer = " "; o.Audience = ""; });

        var failures = Failures(options);

        Assert.Equal(2, failures.Length);
        Assert.False(string.IsNullOrEmpty(options.SigningKey)); // an empty key would match every message
        Assert.DoesNotContain(failures, f => f.Contains(options.SigningKey));
    }

    [Fact]
    public void The_issuer_cannot_be_built_from_an_invalid_key()
    {
        Assert.Throws<InvalidOperationException>(() => Issuer(Options(o => o.SigningKey = NewKey(8))));
    }

    #endregion

    #region Session options and System user options

    [Theory]
    [InlineData(0, 60, false)]
    [InlineData(25, 60, false)]
    [InlineData(12, -1, false)]
    [InlineData(12, 301, false)]
    [InlineData(1, 0, true)]
    [InlineData(24, 300, true)]
    [InlineData(12, 60, true)]
    public void Session_options_are_range_checked(int hours, int touchSeconds, bool valid)
    {
        var options = new SessionOptions { AbsoluteLifetimeHours = hours, ActivityTouchIntervalSeconds = touchSeconds };

        Assert.Equal(valid, new SessionOptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Session_defaults_are_12_hours_and_60_seconds_and_exposed_as_minutes()
    {
        var options = new SessionOptions();
        ISessionSettings settings = options;

        Assert.Equal(12, options.AbsoluteLifetimeHours);
        Assert.Equal(720, settings.AbsoluteLifetimeMinutes);
        Assert.Equal(60, settings.ActivityTouchIntervalSeconds);
    }

    [Fact]
    public void System_user_username_defaults_to_SYSTEM_and_must_not_be_blank()
    {
        var options = new SystemUserOptions();
        Assert.Equal("SYSTEM", options.Username);

        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        options.Username = "   ";
        Assert.False(System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            options, new System.ComponentModel.DataAnnotations.ValidationContext(options), results, true));
    }

    #endregion

    #region Refresh tokens

    private static readonly SessionTokenFactory Factory = new();

    [Fact]
    public void A_refresh_token_is_43_base64url_characters_of_32_bytes()
    {
        var token = Factory.Create();

        Assert.Equal(43, token.RawToken.Length);
        Assert.All(token.RawToken, c => Assert.True(char.IsAsciiLetterOrDigit(c) || c is '-' or '_', $"unexpected '{c}'"));
        Assert.Equal(32, System.Buffers.Text.Base64Url.DecodeFromChars(token.RawToken).Length);
        Assert.Equal(32, token.Hash.Length);
    }

    [Fact]
    public void The_hash_is_SHA256_of_the_raw_bytes_and_TryHash_reproduces_it()
    {
        var token = Factory.Create();
        var raw = System.Buffers.Text.Base64Url.DecodeFromChars(token.RawToken);

        Assert.Equal(SHA256.HashData(raw), token.Hash);
        Assert.True(Factory.TryHash(token.RawToken, out var hash));
        Assert.Equal(token.Hash, hash);
    }

    [Fact]
    public void Tokens_are_unique()
    {
        var tokens = Enumerable.Range(0, 200).Select(_ => Factory.Create().RawToken).ToHashSet();

        Assert.Equal(200, tokens.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("short")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")] // 44 chars
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA+")] // '+' is base64, not base64url
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA/")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA ")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\n")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAé")]
    public void Malformed_tokens_are_rejected_without_throwing(string? raw)
    {
        Assert.False(Factory.TryHash(raw, out var hash));
        Assert.Empty(hash);
    }

    [Fact]
    public void Non_canonical_encodings_of_the_last_character_are_rejected()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        var token = Factory.Create().RawToken;
        var lastIndex = alphabet.IndexOf(token[^1]);

        foreach (var variant in new[] { lastIndex ^ 1, lastIndex ^ 2, lastIndex ^ 3 })
        {
            var forged = token[..^1] + alphabet[variant];
            Assert.False(Factory.TryHash(forged, out _), $"accepted non-canonical '{alphabet[variant]}'");
        }
    }

    [Fact]
    public void The_token_validated_against_the_request_DTO_length_matches_the_factory()
    {
        Assert.Equal(SessionPolicy.RefreshTokenLength, Factory.Create().RawToken.Length);
    }

    #endregion
}
