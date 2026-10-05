/*
 * File        : MobileApiExceptionAttribute.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Turns business-rule exceptions into the {status, error, message} JSON the Android app expects.
 */
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SmartSolarMicrogrid.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class MobileApiExceptionAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var (status, message) = context.Exception switch
        {
            KeyNotFoundException e => (StatusCodes.Status404NotFound, e.Message),
            InvalidOperationException e => (StatusCodes.Status400BadRequest, e.Message),
            _ => (0, "")
        };

        if (status == 0) return; // anything else goes to your normal error handling

        context.Result = new ObjectResult(new { status, error = message, message }) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}