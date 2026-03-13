using BudgetManager.Api.Middlewares;
using BudgetManager.Api.Services;
using BudgetManager.Application.Configuration;
using BudgetManager.Application.Services;
using BudgetManager.Infrastructure.Configuration;
using BudgetManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer((operation, context, ct) =>
    {
        var hasAuthorize = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();

        if (hasAuthorize && operation.Responses is not null)
        {
            operation.Responses["401"] = new OpenApiResponse { Description = "Unauthorized" };
        }

        return Task.CompletedTask;
    });
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new()
        {
            Version = "v1",
            Title = "Budget API",
            Description = File.Exists("./Assets/api-description.html") ? File.ReadAllText("./Assets/api-description.html") : "",
            Contact = new OpenApiContact
            {
                Email = builder.Configuration["Contact"]
            }
        };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT"
            }
        };
        return Task.CompletedTask;
    });
});

builder.Services.AddAuthorization();
builder.Services.UseBudgetManagerAuth(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpContextUserService>();

builder.Services.UseMediator();
builder.Services.UsePostgreSQL(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsEnvironment("Test"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseCors();

app.UseStaticFiles(
  new StaticFileOptions
  {
      FileProvider = new PhysicalFileProvider(
      Path.Combine(builder.Environment.ContentRootPath, "Assets")
      ),
      RequestPath = "/Assets"
  }
);

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.AddPreferredSecuritySchemes("Bearer").EnablePersistentAuthentication());
}

app.UseHttpsRedirection();

app.MapControllers().RequireAuthorization();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapGet("/", (HttpContext context) => context.Response.Redirect("/scalar", true)).ExcludeFromDescription();

app.Run();

public partial class Program { } // for testing purposes
