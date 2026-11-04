using System.Data.Common;

/*
 * Description  : Exposes the active unit-of-work transaction to the Stored Procedure executor.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.Data;

/// <summary>
/// Gives the Stored Procedure executor access to the transaction opened by <see cref="UnitOfWork"/>
/// in the same request scope. Internal: Application code uses <c>IUnitOfWork</c> instead.
/// </summary>
internal interface ITransactionAccessor
{
    /// <summary>
    /// The active transaction, or null when no transaction is open.
    /// </summary>
    DbTransaction? CurrentTransaction { get; }
}
