using FarmFMS.Application.Common.Results;
using FarmFMS.Application.Module01.Users;
using FarmFMS.UnitTests.Module01.Fakes;

namespace FarmFMS.UnitTests.Module01;

/// <summary>Bootstrap seed (spec 7.4, D-19, D-20): order of calls, actor ids, ProcessId, policy, unit of work.</summary>
public class AdministratorBootstrapServiceTests
{
    private const string Password = "Adm1n-Pass!";

    private sealed class Harness
    {
        public CallLog Log { get; } = new();
        public FakeBootstrapRepository Repository { get; }
        public FakeAppUserReadRepository Users { get; } = new() { User = new AppUserProfileReadModel { Id = 9, IsActive = false } };
        public FakePasswordProtector Protector { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; }
        public ListLogger<AdministratorBootstrapService> Logger { get; } = new();
        public AdministratorBootstrapService Service { get; }

        public Harness(string systemUsername = "SYSTEM")
        {
            Repository = new FakeBootstrapRepository(Log);
            UnitOfWork = new FakeUnitOfWork(Log);
            Service = new AdministratorBootstrapService(
                Repository, Users, Protector, new FakeSystemUserSettings(systemUsername), UnitOfWork, Logger);
        }
    }

    private static AdministratorBootstrapRequest Request(
        string username = "admin",
        string password = Password,
        int processId = 3) => new()
    {
        Username = username,
        FirstName = "First",
        LastName = "Last",
        Email = "admin@example.test",
        Password = password,
        ProcessId = processId
    };

    [Fact]
    public async Task Creates_the_System_user_then_the_administrator_in_one_unit_of_work()
    {
        var h = new Harness();

        var result = await h.Service.BootstrapAsync(Request());

        Assert.True(result.IsSuccess);
        Assert.True(h.Log.HappenedInOrder("BEGIN", "N8", "ScrAppUser:system", "ScrAppUser:admin", "N7", "ScrUserAccount", "COMMIT"));
        Assert.Equal(1, h.UnitOfWork.Commits);
        Assert.Equal(0, h.UnitOfWork.Rollbacks);
        Assert.Equal("SYSTEM", h.Repository.LookedUpUsername);

        // IDs: SYSTEM = 100, administrator = 101.
        Assert.Equal(new AdministratorBootstrapResult(101, 100, true), result.Value);

        var system = h.Repository.Users[0];
        Assert.Equal("SYSTEM", system.Username);
        Assert.False(system.IsActive);
        Assert.Equal(0, system.CreatedBy); // documented sentinel
        Assert.Equal([9, 9, 9], system.Payload.Ciphertext); // random placeholder, never a real password

        var admin = h.Repository.Users[1];
        Assert.Equal("admin", admin.Username);
        Assert.True(admin.IsActive);
        Assert.Equal(100, admin.CreatedBy); // SYSTEM.Id
        Assert.Equal([9, 9, 9], admin.Payload.Ciphertext);
        Assert.False(admin.PasswordChangeRequired);
    }

    [Fact]
    public async Task The_real_password_is_protected_with_the_new_administrator_id_and_stored_by_SupAppUsrPwd()
    {
        var h = new Harness();

        await h.Service.BootstrapAsync(Request());

        Assert.Equal((Password, 101), Assert.Single(h.Protector.Protected));
        var set = Assert.Single(h.Repository.Passwords);
        Assert.Equal(101, set.UserId);
        Assert.Equal([1, 2, 3], set.Payload.Ciphertext);
        Assert.Equal(100, set.ModifiedBy);
        Assert.False(set.PwdChangeReq);
    }

    [Fact]
    public async Task The_operator_supplied_ProcessId_is_passed_through_with_web_access_only()
    {
        var h = new Harness();

        await h.Service.BootstrapAsync(Request(processId: 17));

        var account = Assert.Single(h.Repository.Accounts);
        Assert.Equal((101, 17, true, false, 100), account);
    }

    [Fact]
    public async Task An_existing_System_user_is_reused()
    {
        var h = new Harness();
        h.Repository.ExistingSystemUserId = 9;

        var result = await h.Service.BootstrapAsync(Request());

        Assert.Equal(new AdministratorBootstrapResult(100, 9, false), result.Value);
        Assert.DoesNotContain("ScrAppUser:system", h.Log);
        Assert.Equal(9, h.Repository.Users.Single().CreatedBy);
        Assert.Equal(9, h.Repository.Accounts.Single().CreatedBy);
        Assert.Equal(9, h.Repository.Passwords.Single().ModifiedBy);
    }

    [Fact]
    public async Task An_existing_ACTIVE_user_with_the_System_username_is_refused()
    {
        var h = new Harness();
        h.Repository.ExistingSystemUserId = 9;
        h.Users.User = new AppUserProfileReadModel { Id = 9, IsActive = true };

        var result = await h.Service.BootstrapAsync(Request());

        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Bootstrap.SystemUserActive", result.Error.Code);
        Assert.Equal(1, h.UnitOfWork.Rollbacks);
        Assert.Empty(h.Repository.Users);
        Assert.DoesNotContain("ScrAppUser:admin", h.Log);
    }

    [Fact]
    public async Task PasswordChangeRequired_is_passed_to_the_user_and_the_password_procedure()
    {
        var h = new Harness();

        await h.Service.BootstrapAsync(new AdministratorBootstrapRequest
        {
            Username = "admin", FirstName = "First", Password = Password, ProcessId = 1, PasswordChangeRequired = true
        });

        Assert.True(h.Repository.Users[1].PasswordChangeRequired);
        Assert.True(h.Repository.Passwords.Single().PwdChangeReq);
    }

    [Theory]
    [InlineData("Sh0rt!x")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoDigitsHere!")]
    [InlineData("NoSpecial123")]
    public async Task Password_policy_violations_return_a_validation_error_without_touching_the_database(string password)
    {
        var h = new Harness();

        var result = await h.Service.BootstrapAsync(Request(password: password));

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.True(result.Error.ValidationErrors.ContainsKey("Password"));
        Assert.DoesNotContain(password, string.Join(' ', result.Error.ValidationErrors["Password"]));
        Assert.DoesNotContain(password, result.Error.Message);
        Assert.Empty(h.Log);
        Assert.Empty(h.Protector.Protected);
    }

    [Fact]
    public async Task Password_longer_than_128_characters_is_rejected()
    {
        var h = new Harness();

        var result = await h.Service.BootstrapAsync(Request(password: "Aa1!" + new string('x', 125)));

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains("128", string.Join(' ', result.Error.ValidationErrors["Password"]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task ProcessId_must_be_positive(int processId)
    {
        var h = new Harness();

        var result = await h.Service.BootstrapAsync(Request(processId: processId));

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.True(result.Error.ValidationErrors.ContainsKey("ProcessId"));
        Assert.Empty(h.Log);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("system")] // the reserved System username, case-insensitive
    public async Task Missing_or_reserved_usernames_are_rejected(string username)
    {
        var h = new Harness();

        var result = await h.Service.BootstrapAsync(Request(username: username));

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.True(result.Error.ValidationErrors.ContainsKey("Username"));
        Assert.Empty(h.Log);
    }

    [Fact]
    public async Task Other_invalid_fields_are_reported_together()
    {
        var h = new Harness();

        var result = await h.Service.BootstrapAsync(new AdministratorBootstrapRequest
        {
            Username = "admin",
            FirstName = " ",
            LastName = new string('l', 101),
            Email = "not-an-email",
            Password = Password,
            ProcessId = 1
        });

        Assert.Equal(["Email", "FirstName", "LastName"], result.Error.ValidationErrors.Keys.Order());
    }

    [Fact]
    public async Task A_taken_username_returns_the_conflict_and_rolls_back_everything()
    {
        var h = new Harness();
        h.Repository.CreateUserExceptionForAdministrator =
            Errors.Database(50005, Error.Conflict("User.UsernameTaken", "The username is already in use."));

        var result = await h.Service.BootstrapAsync(Request());

        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("User.UsernameTaken", result.Error.Code);
        Assert.Equal(1, h.UnitOfWork.Rollbacks);
        Assert.Equal(0, h.UnitOfWork.Commits);
        Assert.DoesNotContain("N7", h.Log);
    }

    [Fact]
    public async Task When_the_password_procedure_updates_no_row_the_unit_of_work_rolls_back()
    {
        var h = new Harness();
        h.Repository.PasswordRows = 0;

        var result = await h.Service.BootstrapAsync(Request());

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal(1, h.UnitOfWork.Rollbacks);
        Assert.DoesNotContain("ScrUserAccount", h.Log);
    }

    [Fact]
    public async Task The_username_is_normalized_and_the_password_never_logged()
    {
        var h = new Harness();

        await h.Service.BootstrapAsync(Request(username: "  Admín  "));

        Assert.Equal("Admín", h.Repository.Users[1].Username);
        // NOTE (QA review): AdministratorBootstrapService currently writes no log entries at all, so the check below is a
        // guard for future logging, not proof of redaction today. It is deliberately not "NotEmpty"-asserted.
        Assert.DoesNotContain(Password, h.Logger.All);
    }
}
