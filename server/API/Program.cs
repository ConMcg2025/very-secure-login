using System.Threading.RateLimiting;
using API;
using Infra;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Service;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

// ---- Data access (Linq2DB + SQLite) -------------------------------------------------
builder.Services.AddSingleton(Database.CreateOptions(connectionString));
builder.Services.AddScoped<AppDb>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// ---- Application services -----------------------------------------------------------
builder.Services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SeedService>();

// ---- Authentication: HttpOnly session cookie ----------------------------------------
// The cookie is HttpOnly (not readable by JavaScript, so XSS cannot steal it) and
// SameSite=Strict (not sent on cross-site requests, which blocks CSRF).
// Set Auth:SecureCookie=true when the site is served over HTTPS.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "vsl.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Configuration.GetValue<bool>("Auth:SecureCookie")
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        // An API should answer 401/403, not redirect to a login page.
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };

        // Checked on every request: if the account was deleted, renamed, given another
        // role or got a new password, the old session is rejected immediately.
        options.Events.OnValidatePrincipal = async ctx =>
        {
            var claimed = ctx.Principal.TryGetActor();
            var repository = ctx.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
            var user = claimed is null ? null : await repository.FindByIdAsync(claimed.Id);

            if (claimed is null || user is null || Actor.From(user) != claimed)
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();

// ---- Brute-force protection on the login endpoint -----------------------------------
// The client's Bun server forwards the real client address in X-Forwarded-For. The API
// is only reachable through it (it is not published by docker-compose).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
    {
        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();
        var key = !string.IsNullOrEmpty(forwarded)
            ? forwarded
            : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

// ---- Web API ------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// ---- Database: create schema and seed the initial accounts ---------------------------
Database.EnsureCreated(connectionString);
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<SeedService>().SeedAsync();
}

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Cache-Control"] = "no-store";
    await next();
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.Run();
