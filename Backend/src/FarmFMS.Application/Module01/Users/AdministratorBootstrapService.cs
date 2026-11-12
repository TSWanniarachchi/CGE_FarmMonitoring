using System.Net.Mail;
using FarmFMS.Application.Abstractions.Data;
using FarmFMS.Application.Common.Exceptions;
using FarmFMS.Application.Common.Extensions;
using FarmFMS.Application.Common.Results;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Domain.Module01;
using Microsoft.Extensions.Logging;

/*
 * Description  : Development-only first Administrator bootstrap (Step 0 spec section 7.4, D-19, D-20, C-09).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Users;

/// <summary>
/// One unit of work: resolve (or create) the SYSTEM user, create the Administrator with a random placeholder payload,
/// encrypt the real password with the new Id as AAD (P-03 Option A), store it with <c>SupAppUsrPwd</c> and create the
/// web-enabled <c>UserAccount</c>.
/// <para>
/// Safety: an existing record under the System username must be inactive, otherwise the bootstrap refuses (D-19).
/// Audit actors (D-20): the SYSTEM row has <c>CreatedBy = 0</c> (documented sentinel: no user exists yet); the
/// Administrator and every later write use <c>CreatedBy / ModifiedBy = SYSTEM.Id</c>. The <c>ProcessId</c> is supplied by
/// the operator and never hard-coded. No roles are seeded (DBG-014). The password is never logged.
/// </para>
/// </summary>
internal sealed class AdministratorBootstrapService : IAdministratorBootstrapService
{
    /// <summary>The documented <c>CreatedBy</c> sentinel of the very first row (no user exists yet), D-20.</summary>
    internal const int SystemUserCreatedBySentinel = 0;

    private const string SystemFirstName = "System";
    private const string ValidationCode = "Bootstrap.Invalid";

    private static readonly Error SystemUserActiveError = Error.Conflict(
        "Bootstrap.SystemUserActive",
        "A user with the System username already exists and is active. The System user must be inactive; nothing was changed.");

    private readonly IAdministratorBootstrapRepository _repository;
    private readonly IAppUserReadRepository _users;
    private readonly IPasswordProtector _passwordProtector;
    private readonly ISystemUserSettings _systemUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdministratorBootstrapService> _logger;

    public AdministratorBootstrapService(
        IAdministratorBootstrapRepository repository,
        IAppUserReadRepository users,
        IPasswordProtector passwordProtector,
        ISystemUserSettings systemUser,
        IUnitOfWork unitOfWork,
        ILogger<AdministratorBootstrapService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _passwordProtector = passwordProtector ?? throw new ArgumentNullException(nameof(passwordProtector));
        _systemUser = systemUser ?? throw new ArgumentNullException(nameof(systemUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AdministratorBootstrapResult>> BootstrapAsync(
        AdministratorBootstrapRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = Validate(request, out var username, out var firstName, out var lastName, out var email);
        if (validation is not null)
            return validation;

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync<Result<AdministratorBootstrapResult>>(
                async token =>
                {
                    // SYSTEM user: resolve, or create (inactive, never able to log in, random unknown payload).
                    var systemId = await _repository.GetSystemUserIdAsync(_systemUser.Username, token);
                    var systemCreated = false;
                    if (systemId is not null)
                    {
                        // Safety invariant (D-19): the System user can never sign in, so an ACTIVE record under the
                        // System username is not the System user. Refuse instead of recording its Id as the actor.
                        var existing = await _users.GetByIdAsync(systemId.Value, token);
                        if (existing is null || existing.IsActive)
                            return SystemUserActiveError;
                    }
                    else
                    {
                        systemId = await _repository.CreateUserAsync(
                            new BootstrapUserData(
                                _systemUser.Username,
                                SystemFirstName,
                                null,
                                null,
                                _passwordProtector.CreatePlaceholder(),
                                PasswordChangeRequired: false,
                                IsActive: false,
                                CreatedBy: SystemUserCreatedBySentinel),
                            token);
                        systemCreated = true;
                    }

                    // Administrator with a placeholder payload: the Id is the AAD, so it must exist before encryption.
                    var adminId = await _repository.CreateUserAsync(
                        new BootstrapUserData(
                            username,
                            firstName,
                            lastName,
                            email,
                            _passwordProtector.CreatePlaceholder(),
                            request.PasswordChangeRequired,
                            IsActive: true,
                            CreatedBy: systemId.Value),
                        token);

                    var payload = _passwordProtector.Protect(request.Password, adminId);
                    var rows = await _repository.SetPasswordAsync(
                        adminId, payload, request.PasswordChangeRequired, systemId.Value, token);
                    if (rows == 0)
                        return Error.NotFound("User.NotFound", "The user was not found.");

                    await _repository.CreateUserAccountAsync(
                        adminId, request.ProcessId, canAccessWeb: true, canAccessMobile: false, systemId.Value, token);

                    return new AdministratorBootstrapResult(adminId, systemId.Value, systemCreated);
                },
                cancellationToken: cancellationToken);
        }
        catch (DataAccessException ex) when (!ex.IsTransient && IsBusinessError(ex))
        {
            // Business errors (e.g. 50005 username taken, 50006 account exists): the unit of work was rolled back.
            _logger.LogWarning("Administrator bootstrap rejected: {ErrorCode}", ex.Error.Code);
            return ex.Error;
        }
    }

    // A translated catalogue error (not the generic Failure / Unavailable ones): match on the error type, never on SQL numbers.
    private static bool IsBusinessError(DataAccessException ex) =>
        ex.Error.Type is ErrorType.Conflict or ErrorType.NotFound or ErrorType.Validation
            or ErrorType.Unauthorized or ErrorType.Forbidden;

    private Result<AdministratorBootstrapResult>? Validate(
        AdministratorBootstrapRequest request,
        out string username,
        out string firstName,
        out string? lastName,
        out string? email)
    {
        var errors = new Dictionary<string, List<string>>();

        void Add(string field, string message)
        {
            if (!errors.TryGetValue(field, out var list))
                errors[field] = list = [];
            list.Add(message);
        }

        username = UsernameNormalizer.Normalize(request.Username);
        if (username.Length == 0)
            Add(nameof(request.Username), "Username is required.");
        else if (username.Length > Module01FieldLengths.Username)
            Add(nameof(request.Username), $"Username must not exceed {Module01FieldLengths.Username} characters.");
        else if (string.Equals(username, _systemUser.Username, StringComparison.OrdinalIgnoreCase))
            Add(nameof(request.Username), "This username is reserved.");

        firstName = request.FirstName.TrimOrEmpty();
        if (firstName.Length == 0)
            Add(nameof(request.FirstName), "FirstName is required.");
        else if (firstName.Length > Module01FieldLengths.FirstName)
            Add(nameof(request.FirstName), $"FirstName must not exceed {Module01FieldLengths.FirstName} characters.");

        lastName = request.LastName.TrimToNull();
        if (lastName is { Length: > Module01FieldLengths.LastName })
            Add(nameof(request.LastName), $"LastName must not exceed {Module01FieldLengths.LastName} characters.");

        email = request.Email.TrimToNull();
        if (email is not null && !IsValidEmail(email))
            Add(nameof(request.Email), "Email is not valid.");

        foreach (var problem in PasswordPolicy.Validate(request.Password))
            Add(nameof(request.Password), problem);

        if (request.ProcessId <= 0)
            Add(nameof(request.ProcessId), "ProcessId must be greater than 0 and is supplied by the operator.");

        if (errors.Count == 0)
            return null;

        return Error.Validation(
            ValidationCode,
            "The administrator details are not valid.",
            errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()));
    }

    private static bool IsValidEmail(string email)
    {
        if (email.Length > Module01FieldLengths.Email)
            return false;

        try
        {
            return new MailAddress(email).Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
