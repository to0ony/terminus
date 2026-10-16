using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Terminus.DAL;
using Terminus.DAL.Entities;
using Terminus.DAL.Interceptors;
using Terminus.Repository;
using Terminus.Repository.Common;
using Terminus.Service;
using Terminus.Service.Common;
using Terminus.Service.Gtfs;
using Terminus.Service.Gtfs.Import;
using Terminus.WebApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TerminusExceptionHandler>();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' nije postavljen.");

builder.Services.AddDbContext<TerminusDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention()
           .AddInterceptors(new AuditableInterceptor()));

// Prijava administratora: Identity bez uloga, kolačić umjesto tokena (frontend je na istoj domeni).
builder.Services
    .AddIdentityCore<AdminUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        // Duljina je važnija od pravila o vrstama znakova.
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<TerminusDbContext>()
    .AddSignInManager()
    .AddErrorDescriber<CroatianIdentityErrorDescriber>();

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "terminus_auth";
    options.Cookie.HttpOnly = true;
    // Strict: preglednik ne šalje kolačić uz zahtjeve s drugih stranica (zaštita od CSRF-a).
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;

    // API ne preusmjerava na stranicu za prijavu, nego vraća 401/403.
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// Promjena lozinke odjavljuje ostale preglednike najkasnije nakon ovog intervala.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(5));

// Sve traži prijavu osim onoga što je označeno s [AllowAnonymous] (/api/public/*, prijava, /health).
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Ključevi za kolačiće u bazi: prijava preživi restart i radi s više instanci aplikacije.
builder.Services.AddDataProtection()
    .SetApplicationName("Terminus")
    .PersistKeysToDbContext<TerminusDbContext>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TerminusDbContext>("database");

// Repozitoriji
builder.Services.AddScoped<IAgencyRepository, AgencyRepository>();
builder.Services.AddScoped<IStopRepository, StopRepository>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<IServiceCalendarRepository, ServiceCalendarRepository>();
builder.Services.AddScoped<ICalendarDateRepository, CalendarDateRepository>();
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<IStopTimeRepository, StopTimeRepository>();
builder.Services.AddScoped<IGtfsExportRepository, GtfsExportRepository>();
builder.Services.AddScoped<IDepartureRepository, DepartureRepository>();
builder.Services.AddScoped<IShapeRepository, ShapeRepository>();
builder.Services.AddScoped<IFareRepository, FareRepository>();
builder.Services.AddScoped<IFeedInfoRepository, FeedInfoRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<ITimetableRepository, TimetableRepository>();
builder.Services.AddScoped<ISetupRepository, SetupRepository>();
builder.Services.AddScoped<IGtfsImportRepository, GtfsImportRepository>();

// Servisi
builder.Services.AddScoped<IAgencyService, AgencyService>();
builder.Services.AddScoped<IStopService, StopService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<IServiceCalendarService, ServiceCalendarService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IGtfsExportService, GtfsExportService>();
builder.Services.AddScoped<IDepartureService, DepartureService>();
builder.Services.AddScoped<IShapeService, ShapeService>();
builder.Services.AddScoped<IFareService, FareService>();
builder.Services.AddScoped<IFeedInfoService, FeedInfoService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<ITimetableService, TimetableService>();
builder.Services.AddScoped<ISetupService, SetupService>();
builder.Services.AddScoped<IGtfsImportService, GtfsImportService>();

builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

await app.SeedAdminAsync();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();
