using BudgetManager.Api.Middlewares;
using BudgetManager.Api.Services;
using BudgetManager.Application.Configuration;
using BudgetManager.Application.Services;
using BudgetManager.Infrastructure.Configuration;
using BudgetManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Budget API",
        Description = File.ReadAllText("./Assets/api-description.html"),
        Contact = new OpenApiContact
        {
            Email = builder.Configuration["Contact"]
        },
    });
    options.EnableAnnotations();
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.DocumentTitle = "Budget API";
        c.OAuthScopes($"{builder.Configuration["AzureAd:Audience"]}/full");
        c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{builder.Environment.ApplicationName} v1");
        c.InjectStylesheet("/Assets/swagger-dark.css");
        c.OAuthClientId(builder.Configuration["AzureAd:ClientId"]);
    });
}

app.UseHttpsRedirection();

app.MapControllers().RequireAuthorization();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapGet("/", (HttpContext context) => context.Response.Redirect("/swagger", true)).ExcludeFromDescription();

app.Run();

public partial class Program { } // for testing purposes
