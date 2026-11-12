using System.ComponentModel.DataAnnotations;
using FarmFMS.Application.Module01.Users;
using FarmFMS.Domain.Module01;

/*
 * Description  : Defines the validated System user settings bound from the "SystemUser" configuration section.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.Identity;

/// <summary>
/// System user settings. The dedicated System user is an <c>AppUser</c> row (inactive, never able to sign in) whose
/// <b>Id is resolved from the database</b> by <see cref="Username"/> through <c>dbo.SreAppUsrSystemId</c>
/// (AD-14, Step 0 spec D-19). The Id is never hard-coded and never configured (the former <c>SystemUser:UserId</c>
/// bridge was replaced). Until the SYSTEM record exists (bootstrap), system operations fail closed; request writes by
/// authenticated users are unaffected.
/// </summary>
public sealed class SystemUserOptions : ISystemUserSettings
{
    public const string SectionName = "SystemUser";

    /// <summary>Username of the System user record (default <c>SYSTEM</c>).</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "SystemUser:Username is required.")]
    [StringLength(Module01FieldLengths.Username, ErrorMessage = "SystemUser:Username must not exceed {1} characters.")]
    public string Username { get; set; } = "SYSTEM";
}
