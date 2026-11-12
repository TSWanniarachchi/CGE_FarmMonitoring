/*
 * Description  : Maximum lengths of the Module 01 NVARCHAR / VARBINARY columns, shared by EF configurations and request validation (B-10).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Domain.Module01;

/// <summary>
/// Column lengths from the approved Module 01 database specification (v0.2).
/// </summary>
public static class Module01FieldLengths
{
    public const int Username = 100;
    public const int EncPassword = 512;
    public const int PasswordNonce = 12;
    public const int PasswordTag = 16;
    public const int KeyVersion = 50;
    public const int FirstName = 100;
    public const int LastName = 100;
    public const int Email = 255;
    public const int MobileNo = 20;
    public const int RoleName = 100;
    public const int RoleDescription = 250;
    public const int PermissionName = 50;
    public const int PermissionCode = 50;
    public const int PermissionDescription = 250;
    public const int MenuName = 100;
    public const int MenuCode = 100;
    public const int MenuIcon = 100;
    public const int MenuRoute = 250;
    public const int TokenHash = 32;
    public const int IPAddress = 45;
    public const int DeviceInfo = 500;
    public const int FailureReason = 250;
    public const int RevokedReason = 250;
}
