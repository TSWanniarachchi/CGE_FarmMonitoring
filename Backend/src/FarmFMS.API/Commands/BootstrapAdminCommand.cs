using System.Globalization;
using System.Text;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Application.Module01.Users;

/*
 * Description  : Development-only console command that creates the first Administrator (Step 0 spec 7.4, D-19, D-20, D-31).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.API.Commands;

/// <summary>Console input / output of interactive commands (replaceable in tests).</summary>
public interface IBootstrapConsole
{
    /// <summary>False when input or output is redirected (no interactive terminal).</summary>
    bool IsInteractive { get; }

    void WriteLine(string text = "");

    /// <summary>Prompts and reads a line; <c>null</c> when the input ended.</summary>
    string? ReadLine(string prompt);

    /// <summary>Prompts and reads a secret without echo; <c>null</c> when the input ended.</summary>
    string? ReadSecret(string prompt);
}

/// <summary>The real terminal. The secret is read key by key with echo suppressed.</summary>
internal sealed class SystemBootstrapConsole : IBootstrapConsole
{
    public bool IsInteractive => !Console.IsInputRedirected && !Console.IsOutputRedirected;

    public void WriteLine(string text = "") => Console.WriteLine(text);

    public string? ReadLine(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine();
    }

    public string? ReadSecret(string prompt)
    {
        Console.Write(prompt);

        var secret = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
                break;

            if (key.Key == ConsoleKey.Backspace)
            {
                if (secret.Length > 0)
                    secret.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                secret.Append(key.KeyChar);
            }
        }

        Console.WriteLine();
        return secret.ToString();
    }
}

/// <summary>
/// <c>dotnet run --project Backend/src/FarmFMS.API -- bootstrap-admin</c>. Runs instead of the web host, never at
/// normal startup, and only in the Development environment (D-31). The password is typed masked and confirmed twice; it
/// is never read from arguments, configuration or environment variables, and never printed or logged.
/// Exit codes: 0 done, 1 refused or failed, 2 cancelled.
/// </summary>
public sealed class BootstrapAdminCommand
{
    public const string CommandName = "bootstrap-admin";
    public const int Succeeded = 0;
    public const int Failed = 1;
    public const int Cancelled = 2;

    private const int MaxAttempts = 3;

    private readonly IBootstrapConsole _console;
    private readonly IAdministratorBootstrapService _service;
    private readonly IHostEnvironment _environment;

    public BootstrapAdminCommand(
        IBootstrapConsole console,
        IAdministratorBootstrapService service,
        IHostEnvironment environment)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>True when the command line asks for this command (the first argument).</summary>
    public static bool IsRequested(string[] args) =>
        args is { Length: > 0 } && string.Equals(args[0], CommandName, StringComparison.OrdinalIgnoreCase);

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
        {
            _console.WriteLine(
                "Refused: bootstrap-admin runs only in the Development environment (Step 0 spec D-31). Nothing was changed.");
            return Failed;
        }

        if (!_console.IsInteractive)
        {
            _console.WriteLine("Refused: bootstrap-admin needs an interactive terminal (the password is typed, never piped). Nothing was changed.");
            return Failed;
        }

        _console.WriteLine("Farm FMS - create the first System Administrator (Development only).");
        _console.WriteLine("This is a DATABASE CHANGE: it creates the SYSTEM user (if missing), the Administrator and a web-enabled user account.");
        _console.WriteLine("The Administrator has NO roles yet (no role / permission seed exists, DBG-014): it can sign in but has no permissions.");
        _console.WriteLine();

        var username = PromptRequired("Username: ");
        if (username is null) return Cancelled;

        var firstName = PromptRequired("First name: ");
        if (firstName is null) return Cancelled;

        var lastName = _console.ReadLine("Last name (optional): ");
        var email = _console.ReadLine("Email (optional): ");

        _console.WriteLine();
        _console.WriteLine("ProcessId: the Id of the Farm Monitoring Process master record, agreed with the process owner (DBG-002).");
        _console.WriteLine("There is no default and it cannot be checked here.");
        var processId = PromptProcessId();
        if (processId is null) return Failed;

        var password = _console.ReadSecret("Password: ");
        if (password is null) return Cancelled;

        var confirmation = _console.ReadSecret("Confirm password: ");
        if (confirmation is null) return Cancelled;

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            _console.WriteLine("The passwords do not match. Nothing was changed.");
            return Failed;
        }

        var problems = PasswordPolicy.Validate(password);
        if (problems.Count > 0)
        {
            foreach (var problem in problems)
                _console.WriteLine(problem);
            _console.WriteLine("Nothing was changed.");
            return Failed;
        }

        _console.WriteLine();
        _console.WriteLine("Summary (the password is not shown):");
        _console.WriteLine($"  Username  : {username}");
        _console.WriteLine($"  Name      : {firstName} {lastName}".TrimEnd());
        _console.WriteLine($"  Email     : {(string.IsNullOrWhiteSpace(email) ? "(none)" : email)}");
        _console.WriteLine($"  ProcessId : {processId.Value.ToString(CultureInfo.InvariantCulture)}");
        _console.WriteLine("  Access    : web");

        var answer = _console.ReadLine("Apply this change to the database? [y/N]: ");
        if (answer is null || !(answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) ||
                                answer.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase)))
        {
            _console.WriteLine("Cancelled. Nothing was changed.");
            return Cancelled;
        }

        var result = await _service.BootstrapAsync(
            new AdministratorBootstrapRequest
            {
                Username = username,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Password = password,
                ProcessId = processId.Value
            },
            cancellationToken);

        if (result.IsFailure)
        {
            _console.WriteLine($"Failed: {result.Error.Message}");
            foreach (var (field, messages) in result.Error.ValidationErrors)
            {
                foreach (var message in messages)
                    _console.WriteLine($"  {field}: {message}");
            }

            _console.WriteLine("Nothing was changed.");
            return Failed;
        }

        var created = result.Value;
        _console.WriteLine("Done.");
        _console.WriteLine($"  Administrator Id : {created.AdministratorId.ToString(CultureInfo.InvariantCulture)}");
        _console.WriteLine($"  SYSTEM user Id   : {created.SystemUserId.ToString(CultureInfo.InvariantCulture)}" +
                           (created.SystemUserCreated ? " (created)" : " (existing)"));
        _console.WriteLine("The Administrator has no roles yet (DBG-014).");
        return Succeeded;
    }

    private string? PromptRequired(string prompt)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var value = _console.ReadLine(prompt);
            if (value is null) return null;
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();

            _console.WriteLine("A value is required.");
        }

        return null;
    }

    private int? PromptProcessId()
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var value = _console.ReadLine("ProcessId: ");
            if (value is null) return null;

            if (int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0)
                return id;

            _console.WriteLine("ProcessId must be a positive whole number.");
        }

        _console.WriteLine("No valid ProcessId was entered. Nothing was changed.");
        return null;
    }
}
