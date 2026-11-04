using System.Threading.RateLimiting;
using BudgetManager.Api.Configuration;
using BudgetManager.Api.Middlewares;
using BudgetManager.Api.Services;
using BudgetManager.Application.Configuration;
using BudgetManager.Application.Services;
using BudgetManager.Infrastructure.Configuration;
using BudgetManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, config) => config.ReadFrom.Configuration(ctx.Configuration));

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

    // the UI's nginx proxies /api, so the client address arrives in X-Forwarded-For;
    // only trust it from private networks so a client reaching the API directly cannot spoof it
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.16.0.0/12"));
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("192.168.0.0/16"));
    });

    builder.Services.Configure<AuthRateLimit>(builder.Configuration.GetSection(AuthRateLimit.SectionName));
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(AuthRateLimit.PolicyName, context =>
        {
            var limit = context.RequestServices.GetRequiredService<IOptions<AuthRateLimit>>().Value;
            return RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit.PermitLimit,
                    Window = TimeSpan.FromSeconds(limit.WindowSeconds),
                    QueueLimit = 0,
                });
        });
    });

    var app = builder.Build();

    app.UseForwardedHeaders();

    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseRateLimiter();

    if (builder.Configuration.GetValue<bool>("RunDatabaseMigrationsOnStartup"))
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

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    await Log.CloseAndFlushAsync();
}
public partial class Program { } // for testing purposes
