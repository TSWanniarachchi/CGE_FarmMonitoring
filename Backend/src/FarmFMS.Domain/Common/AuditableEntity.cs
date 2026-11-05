/*
 * Description  : Base class for entities with the standard primary key and audit columns.
 * Author       : T.S.Wanniarachchi
 * Created Date : 05-11-2026
 */
namespace FarmFMS.Domain.Common;

/// <summary>
/// Primary key and audit columns shared by every table, as defined by the approved Module 01 database
/// specification (v0.2, Read Me: "Primary keys" and "Audit columns"):
/// <list type="table">
/// <item><term>Id</term><description><c>INT IDENTITY(1,1)</c>, primary key.</description></item>
/// <item><term>CreatedBy</term><description><c>INT NOT NULL</c>, the AppUser ID of the creator.</description></item>
/// <item><term>CreatedDate</term><description><c>DATETIME2 NOT NULL</c>, UTC.</description></item>
/// <item><term>ModifiedBy</term><description><c>INT NULL</c>, the AppUser ID of the last modifier.</description></item>
/// <item><term>ModifiedDate</term><description><c>DATETIME2 NULL</c>, UTC.</description></item>
/// </list>
/// Active status is not part of this base: master tables carry <c>IsActive</c>, while transactional and history
/// tables use their own event or status fields (specification, "Master status").
/// Audit values are set on the server from the audit actor and <c>TimeProvider</c> (AD-14), never from requests.
/// </summary>
public abstract class AuditableEntity
{
    public int Id { get; set; }

    public int CreatedBy { get; set; }

    /// <summary>
    /// Creation time, UTC.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    /// <summary>
    /// Last modification time, UTC.
    /// </summary>
    public DateTime? ModifiedDate { get; set; }
}
