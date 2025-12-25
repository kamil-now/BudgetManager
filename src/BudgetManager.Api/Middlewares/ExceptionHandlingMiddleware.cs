using System.Net;
using BudgetManager.Application.Validators;

namespace BudgetManager.Api.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.ContentType = "application/text";
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync(ex.Message);
        }
        catch (AuthenticationException ex)
        {
            context.Response.ContentType = "application/text";
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync(ex.Message);
        }
        catch (AuthorizationException ex)
        {
            context.Response.ContentType = "application/text";
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsync(ex.Message);
        }
    }
}
