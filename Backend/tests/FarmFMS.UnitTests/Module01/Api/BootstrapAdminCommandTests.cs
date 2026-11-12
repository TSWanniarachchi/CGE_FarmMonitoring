using FarmFMS.API.Commands;
using FarmFMS.Application.Common.Results;
using FarmFMS.Application.Module01.Users;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace FarmFMS.UnitTests.Module01.Api;

public class BootstrapAdminCommandTests
{
    private const string Password = "Adm1n-Pass!";

    private sealed class FakeConsole(IEnumerable<string?> lines, IEnumerable<string?> secrets, bool interactive = true) : IBootstrapConsole
    {
        private readonly Queue<string?> _lines = new(lines);
        private readonly Queue<string?> _secrets = new(secrets);

        public bool IsInteractive { get; } = interactive;
        public List<string> Output { get; } = [];
        public List<string> Prompts { get; } = [];

        public void WriteLine(string text = "") => Output.Add(text);

        public string? ReadLine(string prompt)
        {
            Prompts.Add(prompt);
            return _lines.Count > 0 ? _lines.Dequeue() : null;
        }

        public string? ReadSecret(string prompt)
        {
            Prompts.Add(prompt);
            return _secrets.Count > 0 ? _secrets.Dequeue() : null;
        }

        public string AllOutput => string.Join('\n', Output.Concat(Prompts));
    }

    private sealed class FakeService : IAdministratorBootstrapService
    {
        public Result<AdministratorBootstrapResult> Result { get; set; } = new AdministratorBootstrapResult(101, 100, true);
        public List<AdministratorBootstrapRequest> Requests { get; } = [];

        public Task<Result<AdministratorBootstrapResult>> BootstrapAsync(AdministratorBootstrapRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    // username, first, last, email, processId, confirmation
    private static string?[] Lines(string processId = "5", string confirm = "y") =>
        ["admin", "First", "Last", "admin@example.test", processId, confirm];

    private static (BootstrapAdminCommand Command, FakeConsole Console, FakeService Service) Create(
        IEnumerable<string?> lines,
        IEnumerable<string?>? secrets = null,
        string environment = "Development",
        bool interactive = true)
    {
        var console = new FakeConsole(lines, secrets ?? [Password, Password], interactive);
        var service = new FakeService();
        return (new BootstrapAdminCommand(console, service, new FakeEnvironment(environment)), console, service);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Test")]
    public async Task Refuses_outside_Development_before_any_prompt(string environment)
    {
        var (command, console, service) = Create(Lines(), environment: environment);

        var exit = await command.RunAsync();

        Assert.Equal(BootstrapAdminCommand.Failed, exit);
        Assert.Empty(console.Prompts);
        Assert.Empty(service.Requests);
        Assert.Contains("Development", console.AllOutput);
    }

    [Fact]
    public async Task Refuses_without_an_interactive_terminal()
    {
        var (command, console, service) = Create(Lines(), interactive: false);

        var exit = await command.RunAsync();

        Assert.Equal(BootstrapAdminCommand.Failed, exit);
        Assert.Empty(console.Prompts);
        Assert.Empty(service.Requests);
    }

    [Fact]
    public async Task Happy_path_calls_the_service_with_the_typed_values_and_prints_the_ids()
    {
        var (command, console, service) = Create(Lines(processId: "17"));

        var exit = await command.RunAsync();

        Assert.Equal(BootstrapAdminCommand.Succeeded, exit);
        var request = Assert.Single(service.Requests);
        Assert.Equal(("admin", "First", "Last", "admin@example.test", 17, Password),
            (request.Username, request.FirstName, request.LastName, request.Email, request.ProcessId, request.Password));
        Assert.Contains("Administrator Id : 101", console.AllOutput);
        Assert.Contains("SYSTEM user Id   : 100", console.AllOutput);
        Assert.Contains("no roles", console.AllOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DATABASE CHANGE", console.AllOutput);
        Assert.Contains("DBG-002", console.AllOutput); // the ProcessId note
    }

    [Fact]
    public async Task The_password_is_never_printed_or_prompted_in_clear()
    {
        var (command, console, service) = Create(Lines());

        var exit = await command.RunAsync();

        // The flow really ran to the end (both secret prompts were issued, the summary was printed); otherwise the
        // DoesNotContain below would pass vacuously on an empty output.
        Assert.Equal(BootstrapAdminCommand.Succeeded, exit);
        Assert.Equal(Password, Assert.Single(service.Requests).Password);
        Assert.Contains("Administrator Id : 101", console.AllOutput);
        Assert.True(console.Prompts.Count >= 2);
        Assert.DoesNotContain(Password, console.AllOutput);
    }

    [Fact]
    public async Task A_password_mismatch_stops_without_calling_the_service_and_without_echoing()
    {
        var (command, console, service) = Create(Lines(), secrets: [Password, "Different-Pass1!"]);

        var exit = await command.RunAsync();

        Assert.Equal(BootstrapAdminCommand.Failed, exit);
        Assert.Empty(service.Requests);
        Assert.Contains("do not match", console.AllOutput);
        Assert.DoesNotContain(Password, console.AllOutput);
        Assert.DoesNotContain("Different-Pass1!", console.AllOutput);
    }

    [Fact]
    public async Task A_password_that_violates_the_policy_is_rejected_before_the_summary()
    {
        var (command, console, service) = Create(Lines(), secrets: ["weak", "weak"]);

        var exit = await command.RunAsync();

        Assert.Equal(BootstrapAdminCommand.Failed, exit);
        Assert.Empty(service.Requests);
        Assert.Contains(console.Output, line => line.Contains("Password must")); // the rejection was reported
        Assert.DoesNotContain(console.Output, line => line.StartsWith("Summary"));
        Assert.DoesNotContain("weak", string.Join('\n', console.Output.Where(l => !l.Contains("Password must"))));
    }

    [Fact]
    public async Task ProcessId_must_be_a_positive_whole_number_and_is_asked_again()
    {
        var (command, _, service) = Create(["admin", "First", "", "", "abc", "0", "5", "y"]);

        var exit = await command.RunAsync();

        Assert.Equal(BootstrapAdminCommand.Succeeded, exit);
        Assert.Equal(5, Assert.Single(service.Requests).ProcessId);
    }

    [Fact]
    public async Task Three_invalid_ProcessIds_stop_the_command_without_a_default()
    {
        var (command, console, service) = Create(["admin", "First", "", "", "x", "0", ""]);

        var exit = await command.RunAsync();

        Assert.Equal(BootstrapAdminCommand.Failed, exit);
        Assert.Empty(service.Requests);
        Assert.Contains("No valid ProcessId", console.AllOutput);
    }

    [Theory]
    [InlineData("n")]
    [InlineData("")]
    [InlineData("maybe")]
    public async Task Anything_but_yes_cancels_without_a_database_change(string answer)
    {
        var (command, console, service) = Create(Lines(confirm: answer));

        var exit = await command.RunAsync();

        Assert.Equal(BootstrapAdminCommand.Cancelled, exit);
        Assert.Empty(service.Requests);
        Assert.Contains("Nothing was changed", console.AllOutput);
    }

    [Fact]
    public async Task A_confirmation_of_YES_is_accepted_case_insensitively()
    {
        var (command, _, service) = Create(Lines(confirm: " YES "));

        Assert.Equal(BootstrapAdminCommand.Succeeded, await command.RunAsync());
        Assert.Single(service.Requests);
    }

    [Fact]
    public async Task Ended_input_cancels()
    {
        var (command, _, service) = Create(["admin"]);

        var exit = await command.RunAsync();

        Assert.Equal(BootstrapAdminCommand.Cancelled, exit);
        Assert.Empty(service.Requests);
    }

    [Fact]
    public async Task A_service_failure_prints_the_safe_message_and_exits_with_failure()
    {
        var (command, console, service) = Create(Lines());
        service.Result = Error.Conflict("User.UsernameTaken", "The username is already in use.");

        var exit = await command.RunAsync();

        Assert.Equal(BootstrapAdminCommand.Failed, exit);
        Assert.Contains("The username is already in use.", console.AllOutput);
        Assert.Single(service.Requests); // the service was called, so the output below is the real failure output
        Assert.DoesNotContain(Password, console.AllOutput);
    }

    [Fact]
    public void The_command_is_only_requested_by_its_name_as_the_first_argument()
    {
        Assert.True(BootstrapAdminCommand.IsRequested(["bootstrap-admin"]));
        Assert.True(BootstrapAdminCommand.IsRequested(["Bootstrap-Admin"]));
        Assert.False(BootstrapAdminCommand.IsRequested([]));
        Assert.False(BootstrapAdminCommand.IsRequested(["--urls", "http://localhost:1"]));
        Assert.False(BootstrapAdminCommand.IsRequested(["--environment", "bootstrap-admin"]));
    }
}
