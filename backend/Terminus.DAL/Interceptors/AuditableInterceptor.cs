using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Interceptors;

/// <summary>
/// Postavlja UpdatedAt na izmijenjenim IAuditable entitetima.
/// Kod novih entiteta CreatedAt i UpdatedAt postavlja baza (DEFAULT now()).
/// </summary>
public sealed class AuditableInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SetUpdatedAt(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetUpdatedAt(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SetUpdatedAt(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        // Entries() poziva DetectChanges, pa su promjene na praćenim entitetima već vidljive.
        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
