/*
 * Description  : Links a user to a process (account) with web / mobile access flags (table UserAccount).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Domain.Module01.Users;

/// <summary>Links a user to a process (account) with web / mobile access flags (table UserAccount).</summary>
public class UserAccount : FarmFMS.Domain.Common.AuditableEntity
{
    public int UserId { get; set; }

    public int ProcessId { get; set; }

    public bool CanAccessWeb { get; set; }

    public bool CanAccessMobile { get; set; }

    public bool IsActive { get; set; }
}
