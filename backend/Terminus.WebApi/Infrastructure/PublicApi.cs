using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace Terminus.WebApi.Infrastructure;

/// <summary>
/// Zaštita javnih endpointa (/api/public/*): ograničenje zahtjeva po IP adresi i
/// kratko čuvanje odgovora, jer ih zasloni na stanicama i mobiteli stalno osvježavaju.
/// </summary>
public static class PublicApi
{
    public const string SectionName = "PublicApi";

    /// <summary>Polasci, postavke i logo.</summary>
    public const string RateLimitPolicy = "public";

    /// <summary>GTFS feed; izvoz je skup, pa je granica niža.</summary>
    public const string FeedRateLimitPolicy = "public-feed";

    public const string DeparturesCachePolicy = "public-departures";
    public const string SettingsCachePolicy = "public-settings";
    public const string FeedCachePolicy = "public-feed";
    public const string MapCachePolicy = "public-map";

    /// <summary>Oznaka odgovora koji ovise o voznom redu; briše se nakon uvoza GTFS-a.</summary>
    public const string TimetableTag = "timetable";

    /// <summary>Oznaka odgovora koji ovise o postavkama; briše se nakon njihove promjene.</summary>
    public const string SettingsTag = "settings";

    public static IServiceCollection AddPublicApi(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(SectionName).Get<PublicApiOptions>() ?? new PublicApiOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(RateLimitPolicy, context =>
                FixedWindowPerIp(context, RateLimitPolicy, options.PermitLimit));
            limiter.AddPolicy(FeedRateLimitPolicy, context =>
                FixedWindowPerIp(context, FeedRateLimitPolicy, options.FeedPermitLimit));

            limiter.OnRejected = WriteTooManyRequestsAsync;
        });

        services.AddOutputCache(cache =>
        {
            // GTFS kodovi stanica razlikuju velika i mala slova.
            cache.UseCaseSensitivePaths = true;

            // Zasloni sami računaju minute do polaska, pa malo stariji popis ne smeta.
            cache.AddPolicy(DeparturesCachePolicy, policy => policy
                .Expire(TimeSpan.FromSeconds(options.DeparturesCacheSeconds))
                .SetVaryByQuery("limit", "past")
                .Tag(TimetableTag));

            // Naziv bez postavki dolazi od prijevoznika, pa ovisi i o voznom redu.
            cache.AddPolicy(SettingsCachePolicy, policy => policy
                .Expire(TimeSpan.FromMinutes(5))
                .Tag(SettingsTag, TimetableTag));

            // Karta: vozni red dana i stanice se rijetko mijenjaju; uključenost je u postavkama.
            cache.AddPolicy(MapCachePolicy, policy => policy
                .Expire(TimeSpan.FromMinutes(5))
                .SetVaryByQuery("*")
                .Tag(SettingsTag, TimetableTag));

            // Uključenost feeda je u postavkama.
            cache.AddPolicy(FeedCachePolicy, policy => policy
                .Expire(TimeSpan.FromMinutes(10))
                .Tag(SettingsTag, TimetableTag));
        });

        return services;
    }

    /// <summary>
    /// Iza reverse proxyja RemoteIpAddress je adresa proxyja dok se ne uključe
    /// forwarded headers.
    /// </summary>
    private static RateLimitPartition<string> FixedWindowPerIp(HttpContext context, string policy, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"{policy}:{context.Connection.RemoteIpAddress}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });

    private static async ValueTask WriteTooManyRequestsAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Previše zahtjeva.",
                Detail = "Pokušaj ponovno za minutu."
            }
        });
    }
}

/// <summary>Granice za javne endpointe (konfiguracija PublicApi).</summary>
public sealed class PublicApiOptions
{
    /// <summary>Zahtjeva u minuti po IP adresi. Više mobitela iza istog NAT-a dijeli granicu.</summary>
    public int PermitLimit { get; set; } = 120;

    /// <summary>Preuzimanja GTFS feeda u minuti po IP adresi.</summary>
    public int FeedPermitLimit { get; set; } = 5;

    /// <summary>Koliko se dugo čuva popis polazaka jedne stanice.</summary>
    public int DeparturesCacheSeconds { get; set; } = 20;
}
