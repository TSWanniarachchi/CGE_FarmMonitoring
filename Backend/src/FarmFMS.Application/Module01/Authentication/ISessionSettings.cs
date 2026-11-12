/*
 * Description  : Technical session settings the services need (Application-level abstraction over Infrastructure options).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>
/// Technical session values (spec section 9, <c>Session</c> options). Implemented by Infrastructure so the Application
/// layer stays free of configuration packages. The idle timeout is a business constant: <see cref="SessionPolicy"/>.
/// </summary>
public interface ISessionSettings
{
    /// <summary>Absolute session lifetime in minutes (D-09: 12 hours by default).</summary>
    int AbsoluteLifetimeMinutes { get; }

    /// <summary>Minimum seconds between two <c>LastActDate</c> updates by request validation (D-08).</summary>
    int ActivityTouchIntervalSeconds { get; }
}
