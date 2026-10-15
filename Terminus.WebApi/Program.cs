using Microsoft.EntityFrameworkCore;
using Terminus.DAL;
using Terminus.DAL.Interceptors;
using Terminus.Repository;
using Terminus.Repository.Common;
using Terminus.Service;
using Terminus.Service.Common;
using Terminus.Service.Gtfs;
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

// Servisi
builder.Services.AddScoped<IAgencyService, AgencyService>();
builder.Services.AddScoped<IStopService, StopService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<IServiceCalendarService, ServiceCalendarService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IGtfsExportService, GtfsExportService>();
builder.Services.AddScoped<IDepartureService, DepartureService>();

builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

await app.SeedAgencyAsync();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
