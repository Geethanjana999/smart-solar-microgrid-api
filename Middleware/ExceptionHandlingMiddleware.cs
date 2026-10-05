/*
 * File        : ExceptionHandlingMiddleware.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Translates service exceptions into JSON error responses.
 */
using System.Net;

namespace SmartSolarMicrogrid.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    // Runs the pipeline; business-rule errors become 400, missing data 404, anything else 500.
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (InvalidOperationException ex)
        {
            await WriteAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error");
            await WriteAsync(context, HttpStatusCode.InternalServerError, "An unexpected server error occurred.");
        }
    }

    // Writes the {status, error, message} JSON body.
    private static Task WriteAsync(HttpContext context, HttpStatusCode status, string detail)
    {
        context.Response.StatusCode = (int)status;
        return context.Response.WriteAsJsonAsync(new { status = (int)status, error = detail, message = detail });
    }
}
