/*
 * File        : ErrorResponsesOperationFilter.cs
 * Description : Adds the error responses every endpoint can return (400/404 with the JSON error body,
 *               401/403 on endpoints that require a token) so the Swagger contract matches real behaviour.
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using SmartSolarMicrogrid.Api.DTOs;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SmartSolarMicrogrid.Api.Swagger;

public sealed class ErrorResponsesOperationFilter : IOperationFilter
{
    // Declares the standard error responses on one operation.
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var schema = context.SchemaGenerator.GenerateSchema(typeof(ErrorResponse), context.SchemaRepository);
        OpenApiResponse Json(string description) => new()
        {
            Description = description,
            Content = { ["application/json"] = new OpenApiMediaType { Schema = schema } }
        };

        operation.Responses["400"] = Json("Validation or business-rule error");

        if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor action)
        {
            var anonymous = action.MethodInfo.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any();
            var secured = !anonymous && (action.MethodInfo.GetCustomAttributes(true).OfType<IAuthorizeData>().Any()
                                         || action.ControllerTypeInfo.GetCustomAttributes(true).OfType<IAuthorizeData>().Any());
            if (secured)
            {
                operation.Responses["401"] = new OpenApiResponse { Description = "Missing or invalid token" };
                operation.Responses["403"] = new OpenApiResponse { Description = "Role not allowed" };
            }

            if (action.ControllerTypeInfo.Name != "AuthController")
            {
                operation.Responses["404"] = Json("Resource not found");
            }
        }
    }
}
