using System.Data.Common;

/*
 * Description  : Defines the factory that creates open database connections.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Application.Abstractions.Data;

/// <summary>
/// Creates open database connections. This is the ONLY way application code obtains a connection:
/// the Stored Procedure executor and the unit of work both use it, and nothing borrows the EF Core connection.
/// Implemented in the Infrastructure layer.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates and opens a new connection. The caller owns it and must dispose it.
    /// </summary>
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
