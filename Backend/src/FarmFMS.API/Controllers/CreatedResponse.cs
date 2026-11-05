/*
 * Description  : Response body of a successful create (POST) request.
 * Author       : T.S.Wanniarachchi
 * Created Date : 05-11-2026
 */
namespace FarmFMS.API.Controllers;

/// <summary>
/// Body of a 201 Created response: <c>{ "id": n }</c> (AD-18). Primary keys are <c>INT IDENTITY(1,1)</c>.
/// </summary>
public sealed record CreatedResponse(int Id);
