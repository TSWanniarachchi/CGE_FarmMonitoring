using System.Security.Cryptography;
using System.Text;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace FarmFMS.UnitTests.Module01;

/// <summary>AES-256-GCM protector and key ring (spec 7.1, 10.1). Keys are random throwaway values created at runtime.</summary>
public class PasswordProtectorTests
{
    private const string Password = "Sup3r-Secret!";
    private const int UserId = 42;

    private static string NewKey(int bytes = 32) => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes));

    private static PasswordEncryptionOptions Options(string active = "test-1", params string[] versions)
    {
        var options = new PasswordEncryptionOptions { ActiveKeyVersion = active };
        foreach (var version in versions.Length == 0 ? [active] : versions)
            options.Keys[version] = NewKey();
        return options;
    }

    private static AesGcmPasswordProtector Protector(PasswordEncryptionOptions? options = null) =>
        new(new PasswordKeyRing(options ?? Options()));

    #region Round trip and binding

    [Fact]
    public void Round_trip_matches()
    {
        var protector = Protector();

        var payload = protector.Protect(Password, UserId);

        Assert.Equal(PasswordVerificationResult.Match, protector.Verify(Password, payload, UserId));
        Assert.Equal("test-1", payload.KeyVersion);
        Assert.Equal(12, payload.Nonce.Length);
        Assert.Equal(16, payload.Tag.Length);
        Assert.Equal(Encoding.UTF8.GetByteCount(Password), payload.Ciphertext.Length);
    }

    [Fact]
    public void Wrong_password_is_a_mismatch()
    {
        var protector = Protector();
        var payload = protector.Protect(Password, UserId);

        Assert.Equal(PasswordVerificationResult.Mismatch, protector.Verify("Sup3r-Secret?", payload, UserId));
        Assert.Equal(PasswordVerificationResult.Mismatch, protector.Verify(Password + "x", payload, UserId));
        Assert.Equal(PasswordVerificationResult.Mismatch, protector.Verify("", payload, UserId));
    }

    [Fact]
    public void Equal_passwords_get_distinct_nonces_and_ciphertexts()
    {
        var protector = Protector();

        var first = protector.Protect(Password, UserId);
        var second = protector.Protect(Password, UserId);

        Assert.NotEqual(first.Nonce, second.Nonce);
        Assert.NotEqual(first.Ciphertext, second.Ciphertext);
    }

    [Fact]
    public void The_payload_is_bound_to_the_user_id()
    {
        var protector = Protector();
        var payload = protector.Protect(Password, UserId);

        Assert.Equal(PasswordVerificationResult.IntegrityFailure, protector.Verify(Password, payload, UserId + 1));
    }

    [Fact]
    public void The_payload_is_bound_to_the_key_version()
    {
        // Same key material under two labels: the label is part of the AAD, so relabelling is detected.
        var options = new PasswordEncryptionOptions { ActiveKeyVersion = "v1" };
        var key = NewKey();
        options.Keys["v1"] = key;
        options.Keys["v2"] = key;
        var protector = Protector(options);
        var payload = protector.Protect(Password, UserId);

        var relabelled = payload with { KeyVersion = "v2" };

        Assert.Equal(PasswordVerificationResult.IntegrityFailure, protector.Verify(Password, relabelled, UserId));
    }

    [Fact]
    public void Aad_layout_is_label_zero_version_zero_big_endian_user_id()
    {
        var aad = AesGcmPasswordProtector.BuildAad("dev-1", 0x01020304);

        var expected = Encoding.UTF8.GetBytes("FarmFMS.Password.v1")
            .Concat([(byte)0])
            .Concat(Encoding.UTF8.GetBytes("dev-1"))
            .Concat([(byte)0])
            .Concat([(byte)1, (byte)2, (byte)3, (byte)4])
            .ToArray();

        Assert.Equal(expected, aad);
    }

    #endregion

    #region Tampering and structure

    [Fact]
    public void Tampered_tag_ciphertext_or_nonce_is_an_integrity_failure()
    {
        var protector = Protector();
        var payload = protector.Protect(Password, UserId);

        foreach (var tampered in new[]
        {
            payload with { Tag = Flip(payload.Tag) },
            payload with { Ciphertext = Flip(payload.Ciphertext) },
            payload with { Nonce = Flip(payload.Nonce) }
        })
        {
            Assert.Equal(PasswordVerificationResult.IntegrityFailure, protector.Verify(Password, tampered, UserId));
        }
    }

    [Fact]
    public void Wrong_lengths_are_integrity_failures_before_any_decryption()
    {
        var protector = Protector();
        var payload = protector.Protect(Password, UserId);

        Assert.Equal(PasswordVerificationResult.IntegrityFailure, protector.Verify(Password, payload with { Nonce = new byte[11] }, UserId));
        Assert.Equal(PasswordVerificationResult.IntegrityFailure, protector.Verify(Password, payload with { Nonce = new byte[13] }, UserId));
        Assert.Equal(PasswordVerificationResult.IntegrityFailure, protector.Verify(Password, payload with { Tag = new byte[15] }, UserId));
        Assert.Equal(PasswordVerificationResult.IntegrityFailure, protector.Verify(Password, payload with { Tag = new byte[17] }, UserId));
        Assert.Equal(PasswordVerificationResult.IntegrityFailure, protector.Verify(Password, payload with { Ciphertext = [] }, UserId));
        Assert.Equal(PasswordVerificationResult.IntegrityFailure, protector.Verify(Password, payload with { Ciphertext = new byte[513] }, UserId));
        Assert.Equal(PasswordVerificationResult.IntegrityFailure, protector.Verify(Password, payload with { Nonce = null! }, UserId));
    }

    [Fact]
    public void Unknown_key_version_is_reported_and_never_falls_back_to_another_key()
    {
        var protector = Protector(Options("test-1", "test-1", "test-2"));
        var payload = protector.Protect(Password, UserId); // protected with test-1

        // The ring holds another key (test-2), but 'old-9' must not be resolved with it or the active one.
        var result = protector.Verify(Password, payload with { KeyVersion = "old-9" }, UserId);

        Assert.Equal(PasswordVerificationResult.UnknownKeyVersion, result);
        Assert.Equal(PasswordVerificationResult.UnknownKeyVersion, protector.Verify(Password, payload with { KeyVersion = "TEST-1" }, UserId)); // exact, ordinal lookup
        Assert.Equal(PasswordVerificationResult.UnknownKeyVersion, protector.Verify(Password, payload with { KeyVersion = "" }, UserId));
        Assert.Equal(PasswordVerificationResult.UnknownKeyVersion, protector.Verify(Password, payload with { KeyVersion = null! }, UserId));
    }

    [Fact]
    public void A_payload_of_another_key_is_an_integrity_failure_not_a_match()
    {
        var first = Protector(Options("v1"));
        var second = Protector(Options("v1")); // same label, different key material
        var payload = first.Protect(Password, UserId);

        Assert.Equal(PasswordVerificationResult.IntegrityFailure, second.Verify(Password, payload, UserId));
    }

    #endregion

    #region Unicode and limits

    [Fact]
    public void Composed_and_decomposed_passwords_verify_equally_NFC()
    {
        var protector = Protector();
        var composed = "Café-1Xy!";
        var decomposed = "Café-1Xy!";

        var fromComposed = protector.Protect(composed, UserId);
        var fromDecomposed = protector.Protect(decomposed, UserId);

        Assert.Equal(PasswordVerificationResult.Match, protector.Verify(decomposed, fromComposed, UserId));
        Assert.Equal(PasswordVerificationResult.Match, protector.Verify(composed, fromDecomposed, UserId));
        Assert.Equal(fromComposed.Ciphertext.Length, fromDecomposed.Ciphertext.Length);
    }

    [Fact]
    public void Protect_accepts_512_UTF8_bytes_and_rejects_more_or_nothing()
    {
        var protector = Protector();

        var atLimit = protector.Protect(new string('a', 512), UserId);
        Assert.Equal(512, atLimit.Ciphertext.Length);
        Assert.Equal(PasswordVerificationResult.Match, protector.Verify(new string('a', 512), atLimit, UserId));

        Assert.Throws<ArgumentException>(() => protector.Protect(new string('a', 513), UserId));
        Assert.Throws<ArgumentException>(() => protector.Protect("", UserId));
        Assert.Throws<ArgumentException>(() => protector.Protect("\ud800", UserId)); // lone surrogate
    }

    [Fact]
    public void Exception_messages_never_contain_the_password()
    {
        var protector = Protector();
        var secret = new string('s', 600);

        var exception = Assert.Throws<ArgumentException>(() => protector.Protect(secret, UserId));

        Assert.False(string.IsNullOrWhiteSpace(exception.Message)); // there is a message to inspect
        Assert.DoesNotContain(secret, exception.Message);
    }

    [Fact]
    public void Invalid_unicode_in_a_login_attempt_is_a_mismatch_not_an_exception()
    {
        var protector = Protector();
        var payload = protector.Protect(Password, UserId);

        Assert.Equal(PasswordVerificationResult.Mismatch, protector.Verify("\ud800abc", payload, UserId));
    }

    #endregion

    #region Placeholder

    [Fact]
    public void Placeholder_has_the_correct_sizes_active_version_and_never_verifies()
    {
        var protector = Protector();

        var placeholder = protector.CreatePlaceholder();
        var other = protector.CreatePlaceholder();

        Assert.Equal(12, placeholder.Nonce.Length);
        Assert.Equal(16, placeholder.Tag.Length);
        Assert.InRange(placeholder.Ciphertext.Length, 1, 512);
        Assert.Equal("test-1", placeholder.KeyVersion);
        Assert.NotEqual(placeholder.Ciphertext, other.Ciphertext);
        Assert.Equal(PasswordVerificationResult.IntegrityFailure, protector.Verify(Password, placeholder, UserId));
    }

    #endregion

    #region Key ring and options validation

    [Fact]
    public void Key_ring_zeroes_keys_on_dispose()
    {
        var ring = new PasswordKeyRing(Options());
        Assert.True(ring.TryGetKey("test-1", out var key));
        Assert.Contains(key!, b => b != 0);

        ring.Dispose();

        Assert.All(key!, b => Assert.Equal(0, b));
        Assert.Throws<ObjectDisposedException>(() => ring.TryGetKey("test-1", out _));
    }

    private static string[] Failures(PasswordEncryptionOptions options) =>
        [.. new PasswordEncryptionOptionsValidator().Validate(null, options).Failures ?? []];

    [Fact]
    public void A_valid_configuration_passes()
    {
        Assert.True(new PasswordEncryptionOptionsValidator().Validate(null, Options("dev-20261105", "dev-20261105", "old-1")).Succeeded);
    }

    [Fact]
    public void Missing_active_version_fails()
    {
        var options = Options();
        options.ActiveKeyVersion = "";

        Assert.Contains(Failures(options), f => f.Contains("ActiveKeyVersion is required"));
    }

    [Fact]
    public void Active_version_without_a_key_fails()
    {
        var options = Options("test-1");
        options.ActiveKeyVersion = "test-2";

        Assert.Contains(Failures(options), f => f.Contains("does not exist"));
    }

    [Fact]
    public void No_keys_fails()
    {
        var options = new PasswordEncryptionOptions { ActiveKeyVersion = "test-1" };

        Assert.NotEmpty(Failures(options));
    }

    [Theory]
    [InlineData(16)]
    [InlineData(31)]
    [InlineData(33)]
    [InlineData(64)]
    public void Keys_must_be_exactly_32_bytes(int length)
    {
        var options = Options();
        options.Keys["test-1"] = NewKey(length);

        Assert.Contains(Failures(options), f => f.Contains("exactly 32 bytes"));
    }

    [Theory]
    [InlineData("not base64 at all!!")]
    [InlineData("")]
    [InlineData("   ")]
    public void Keys_must_be_valid_base64(string value)
    {
        var options = Options();
        options.Keys["test-1"] = value;

        Assert.NotEmpty(Failures(options));
    }

    [Theory]
    [InlineData("bad label")]
    [InlineData("bad_label")]
    [InlineData("bad.label")]
    [InlineData("")]
    [InlineData("0123456789012345678901234567890123456789012345678901")] // 51 characters
    [InlineData("v1\n")]
    public void Version_labels_must_match_the_D22_format(string label)
    {
        var options = new PasswordEncryptionOptions { ActiveKeyVersion = "good-1" };
        options.Keys["good-1"] = NewKey();
        options.Keys[label] = NewKey();

        Assert.NotEmpty(Failures(options));
    }

    [Fact]
    public void A_50_character_label_is_valid()
    {
        var label = new string('a', 50);
        var options = new PasswordEncryptionOptions { ActiveKeyVersion = label };
        options.Keys[label] = NewKey();

        Assert.Empty(Failures(options));
    }

    [Fact]
    public void Duplicate_labels_differing_only_by_case_fail()
    {
        var options = Options("dev-1", "dev-1");
        options.Keys["DEV-1"] = NewKey();

        Assert.Contains(Failures(options), f => f.Contains("more than once"));
    }

    [Fact]
    public void Validation_messages_never_contain_key_material()
    {
        var secret = NewKey(16);
        var options = Options();
        options.Keys["test-1"] = secret;

        var failures = Failures(options);
        Assert.NotEmpty(failures); // a 16-byte key is invalid, so there are messages to inspect
        Assert.DoesNotContain(failures, f => f.Contains(secret));
    }

    [Fact]
    public void The_key_ring_constructor_rejects_invalid_options()
    {
        var options = Options();
        options.Keys["test-1"] = NewKey(16);

        Assert.Throws<OptionsValidationException>(() => new PasswordKeyRing(options));
    }

    #endregion

    private static byte[] Flip(byte[] bytes)
    {
        var copy = (byte[])bytes.Clone();
        copy[0] ^= 0x01;
        return copy;
    }
}
