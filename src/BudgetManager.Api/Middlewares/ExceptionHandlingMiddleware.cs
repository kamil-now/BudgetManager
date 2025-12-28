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
        catch (NotFoundException ex)
        {
            await Set(context, ex.Message, HttpStatusCode.NotFound);
        }
        catch (ValidationException ex)
        {
            await Set(context, ex.Message, HttpStatusCode.UnprocessableEntity);
        }
        catch (AuthenticationException ex)
        {
            await Set(context, ex.Message, HttpStatusCode.Unauthorized);
        }
        catch (AuthorizationException ex)
        {
            await Set(context, ex.Message, HttpStatusCode.Forbidden);
        }
    }

    private static async Task Set(HttpContext context, string message, HttpStatusCode status)
    {
        context.Response.ContentType = "application/text";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsync(message);
    }
}
