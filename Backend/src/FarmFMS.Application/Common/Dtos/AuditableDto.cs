/*
 * Description  : Base class for response DTOs that expose the standard audit information.
 * Author       : T.S.Wanniarachchi
 * Created Date : 05-11-2026
 */
namespace FarmFMS.Application.Common.Dtos;

/// <summary>
/// Audit information returned with a record (drives the frontend audit panel). The fields mirror the approved
/// audit columns (<c>FarmFMS.Domain.Common.AuditableEntity</c>): user IDs and UTC timestamps. The frontend converts
/// the timestamps for display. Response DTOs only: requests never carry audit fields (AD-14).
/// </summary>
public abstract class AuditableDto
{
    public int CreatedBy { get; init; }

    /// <summary>
    /// Creation time, UTC.
    /// </summary>
    public DateTime CreatedDate { get; init; }

    public int? ModifiedBy { get; init; }

    /// <summary>
    /// Last modification time, UTC.
    /// </summary>
    public DateTime? ModifiedDate { get; init; }
}
