using System.ComponentModel.DataAnnotations;

/*
 * Description  : Defines the validated System user settings bound from the "SystemUser" configuration section.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.Identity;

/// <summary>
/// System user settings. <see cref="UserId"/> is the <c>AppUser.Id</c> (INT IDENTITY per the Module 01
/// specification) of the dedicated System user record created by the controlled seed / deployment process.
/// Because the ID is assigned by IDENTITY at seed time, it is set per environment (environment variable
/// <c>SystemUser__UserId</c> or user-secrets) and is never hard-coded or committed.
/// When it is not set, system operations fail closed; request writes by authenticated users are unaffected.
/// </summary>
public sealed class SystemUserOptions
{
    public const string SectionName = "SystemUser";

    [Range(1, int.MaxValue, ErrorMessage = "SystemUser:UserId must be a positive AppUser ID.")]
    public int? UserId { get; set; }
}
