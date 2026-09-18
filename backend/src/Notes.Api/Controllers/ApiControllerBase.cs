using Microsoft.AspNetCore.Mvc;
using Notes.Api.Extensions;
using Notes.Domain.Common;

namespace Notes.Api.Controllers;

/// <summary>
/// Translates domain <see cref="Result"/> values into HTTP responses, so the mapping
/// from error kind to status code is written once instead of in every action.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>The caller's id, taken from the token - never from the request body.</summary>
    protected Guid CurrentUserId => User.GetUserId();

    protected ActionResult Failure(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Type switch
            {
                ErrorType.Validation => "Validation failed",
                ErrorType.Unauthorized => "Authentication failed",
                ErrorType.NotFound => "Not found",
                ErrorType.Conflict => "Conflict",
                _ => "Unexpected error",
            },
            Detail = error.Description,
            Instance = Request.Path,
        };

        // Machine-readable code alongside the human-readable detail.
        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" },
        };
    }
}
