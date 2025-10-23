using CSProject.Services;
using CSProject.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using AspNetCoreRateLimit;
using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using CSProject.Services.HealthChecks;
using System.Text.Json;
using System.Text;
using System.Reflection;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

builder.Services.AddAuthentication(options => 
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
    .AddCookie(options =>
    {
        options.LoginPath = "/api/auth/login";
        options.LogoutPath = "/api/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    })
    .AddGoogle("Google", googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new InvalidOperationException("Google ClientId não configurado");
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret não configurado");
        googleOptions.CallbackPath = builder.Configuration["Authentication:Google:CallbackPath"] ?? "/signin-google";
        
        googleOptions.Events = new OAuthEvents
        {
            OnCreatingTicket = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                
                var email = context.Identity?.FindFirst(ClaimTypes.Email)?.Value;
                var name = context.Identity?.FindFirst(ClaimTypes.Name)?.Value;
                var nameIdentifier = context.Identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(email))
                {
                    context.Identity?.AddClaim(new Claim(ClaimTypes.Email, email));
                    context.Identity?.AddClaim(new Claim(ClaimTypes.Name, name ?? ""));
                    context.Identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, nameIdentifier ?? ""));

                    logger.LogInformation("Google authentication ticket created for user: {Email}", email);
                }
                else
                {
                    logger.LogWarning("Email claim not found in Google authentication response");
                }
                
                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("Erro no login do Google: {Error}", context.Failure?.Message);
                context.Response.Redirect("/Home/Error");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1", Description = "Api used for medicinal prediction, featuring prediction for diseases" });

});

builder.Services.AddDbContext<CSProjectContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddControllersWithViews();
builder.Services.AddTransient<StrokePredictionService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<CSProjectContext>("database", tags: new[] { "ready", "db" })
    .AddCheck<MLModelHealthCheck>("ml_model", tags: new[] { "ready", "ml" })
    .AddDiskStorageHealthCheck(setup => 
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var directoryName = Path.GetDirectoryName(assemblyLocation);
        if (directoryName != null)
        {
            var drivePath = Path.GetPathRoot(directoryName);
            if (drivePath != null)
            {
                setup.AddDrive(drivePath, 1024); 
            }
            else
            {
                setup.AddDrive("/", 1024); 
            }
        }
    }, name: "disk_space", tags: new[] { "ready" });

builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = "swagger";
});

if (args.Contains("--train"))
{
    var dataPath = Path.Combine(Directory.GetCurrentDirectory(), 
        "MachineLearning", 
        "Data", 
        "stroke_risk_dataset_v2.csv");
    var logger = app.Services.GetRequiredService<ILogger<StrokePredictionService>>();
    var predictionService = new StrokePredictionService(logger);
    predictionService.Train(dataPath);
    predictionService.SaveModel(Path.Combine(Directory.GetCurrentDirectory(),
        "MachineLearning",
        "Models",
        "Stroke",
        "stroke_risk_model.zip"));
    Console.WriteLine("Model trained and saved.");
}

app.UseHttpsRedirection();

app.UseSecurityHeaders(options =>
{
    options.AddDefaultSecurityHeaders();

    options.AddCustomHeader("Content-Security-Policy", 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "connect-src 'self'; " +
        "font-src 'self';");

    options.AddPermissionsPolicy(builder =>
    {
        builder.AddCamera().None();
        builder.AddMicrophone().None();
        builder.AddGeolocation().None();
    });

    options.AddReferrerPolicyStrictOriginWhenCrossOrigin();
});

app.UseRouting();

app.UseCookiePolicy(new CookiePolicyOptions
{
    HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always,
    Secure = CookieSecurePolicy.Always,
    MinimumSameSitePolicy = SameSiteMode.Lax
});
app.UseAuthentication();
app.UseAuthorization();

app.UseIpRateLimiting();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthCheckResponse
});

app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
    ResponseWriter = WriteHealthCheckResponse
});

app.MapHealthChecks("/health/ml", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ml"),
    ResponseWriter = WriteHealthCheckResponse
});
app.Run();

Log.CloseAndFlush();

static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    
    var response = new
    {
        status = report.Status.ToString(),
        duration = report.TotalDuration,
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            duration = entry.Value.Duration,
            description = entry.Value.Description,
            error = entry.Value.Exception?.Message,
            data = entry.Value.Data
        })
    };
    
    return context.Response.WriteAsync(
        JsonSerializer.Serialize(response, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }),
        Encoding.UTF8);
}
