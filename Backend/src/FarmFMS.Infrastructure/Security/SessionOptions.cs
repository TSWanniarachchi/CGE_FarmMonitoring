using FarmFMS.Application.Module01.Authentication;
using Microsoft.Extensions.Options;

/*
 * Description  : Session settings ("Session" section) and their startup validation (Step 0 spec section 9; D-08, D-09).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Security;

/// <summary>
/// Technical session values. The 30-minute idle timeout is a business constant (<see cref="SessionPolicy"/>), not an option.
/// </summary>
public sealed class SessionOptions : ISessionSettings
{
    public const string SectionName = "Session";

    public const int MinAbsoluteLifetimeHours = 1;
    public const int MaxAbsoluteLifetimeHours = 24;
    public const int MaxActivityTouchIntervalSeconds = 300;

    /// <summary>Absolute session lifetime (D-09: 12 hours). Range 1-24.</summary>
    public int AbsoluteLifetimeHours { get; set; } = 12;

    /// <summary>Minimum seconds between two <c>LastActDate</c> updates (D-08: 60). Range 0-300.</summary>
    public int ActivityTouchIntervalSeconds { get; set; } = 60;

    int ISessionSettings.AbsoluteLifetimeMinutes => AbsoluteLifetimeHours * 60;
}

/// <summary>Validates <see cref="SessionOptions"/> at startup.</summary>
public sealed class SessionOptionsValidator : IValidateOptions<SessionOptions>
{
    public ValidateOptionsResult Validate(string? name, SessionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.AbsoluteLifetimeHours is < SessionOptions.MinAbsoluteLifetimeHours or > SessionOptions.MaxAbsoluteLifetimeHours)
            failures.Add($"Session:AbsoluteLifetimeHours must be between {SessionOptions.MinAbsoluteLifetimeHours} and {SessionOptions.MaxAbsoluteLifetimeHours}.");

        if (options.ActivityTouchIntervalSeconds is < 0 or > SessionOptions.MaxActivityTouchIntervalSeconds)
            failures.Add($"Session:ActivityTouchIntervalSeconds must be between 0 and {SessionOptions.MaxActivityTouchIntervalSeconds}.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
