using FarmFMS.Application.Common.Results;

/*
 * Description  : Defines the approved per-module Stored Procedure business-error number ranges (AD-09).
 * Author       : T.S.Wanniarachchi
 * Created Date : 05-11-2026
 */
namespace FarmFMS.Infrastructure.Data;

/// <summary>
/// A block of business-error numbers allocated to one module (AD-09). Every catalogue entry is registered
/// under a range, and its number must fall inside that range.
/// </summary>
public sealed record StoredProcedureErrorRange(string Name, int First, int Last)
{
    #region Approved allocation (AD-09)

    /// <summary>
    /// 50001–50099: Module 01 – Security &amp; Access Management.
    /// </summary>
    public static readonly StoredProcedureErrorRange Module01 = new("Module 01", 50001, 50099);

    /// <summary>
    /// 50100–50199: Module 02.
    /// </summary>
    public static readonly StoredProcedureErrorRange Module02 = new("Module 02", 50100, 50199);

    /// <summary>
    /// 50200–50299: Module 03.
    /// </summary>
    public static readonly StoredProcedureErrorRange Module03 = new("Module 03", 50200, 50299);

    /// <summary>
    /// 50300–50999: shared errors and future modules (reserved).
    /// </summary>
    public static readonly StoredProcedureErrorRange SharedAndFuture = new("Shared errors and future modules", 50300, 50999);

    /// <summary>
    /// All approved ranges, in number order. Together they cover 50001–50999 without overlap.
    /// </summary>
    public static IReadOnlyList<StoredProcedureErrorRange> All { get; } = [Module01, Module02, Module03, SharedAndFuture];

    #endregion

    public bool Contains(int errorNumber) => errorNumber >= First && errorNumber <= Last;

    public override string ToString() => $"{Name} ({First}-{Last})";
}

/// <summary>
/// One business error in the catalogue: the module range it belongs to, its SQL Server error number
/// (the number used in <c>THROW</c>) and the safe application error.
/// </summary>
public sealed record StoredProcedureErrorEntry(StoredProcedureErrorRange Range, int Number, Error Error);
