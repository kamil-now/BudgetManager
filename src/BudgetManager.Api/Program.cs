using BudgetManager.Infrastructure.Configuration;
using BudgetManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Budget API",
        Description = File.ReadAllText("./assets/api-description.html"),
        Contact = new OpenApiContact
        {
            Email = builder.Configuration["Contact"]
        },
    });
    options.EnableAnnotations();
});

// TODO
// builder.Services.UseBudgetManagerAuth(builder.Configuration);

// services.AddAuthentication("Bearer")
//     .AddJwtBearer("Bearer", options =>
//     {
//       options.TokenValidationParameters = JwtTokenGenerator.GetTokenValidationParameters(configuration);
//     });

// TODO
builder.Services.AddCors(
    options => options.AddDefaultPolicy(
    build => build.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.UsePostgreSQL(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
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
    RequestPath = "/assets"
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
        c.InjectStylesheet("/assets/swagger-dark.css");
        c.OAuthClientId(builder.Configuration["AzureAd:ClientId"]);
    });
}

app.UseHttpsRedirection();
app.MapGet("/", (HttpContext context) => context.Response.Redirect("/swagger", true)).ExcludeFromDescription();

app.Run();
