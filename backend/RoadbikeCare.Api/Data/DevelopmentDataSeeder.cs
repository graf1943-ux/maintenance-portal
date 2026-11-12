using Microsoft.EntityFrameworkCore;
using RoadbikeCare.Api.Models;

namespace RoadbikeCare.Api.Data;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(RoadbikeCareDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (await db.Customers.AnyAsync(cancellationToken))
        {
            return;
        }

        var source = new MockDataStore();
        await AddSeedRowsAsync(db, db.Customers, source.Customers, "Customers", cancellationToken);
        await AddSeedRowsAsync(db, db.Shops, source.Shops, "Shops", cancellationToken);
        await AddSeedRowsAsync(db, db.Plans, source.Plans, "Plans", cancellationToken);

        var bikes = source.Bikes.Select(x => x with { ActiveContractId = null }).ToList();
        await AddSeedRowsAsync(db, db.Bikes, bikes, "Bikes", cancellationToken);
        await AddSeedRowsAsync(db, db.Staff, source.Staff, "Staff", cancellationToken);

        await AddSeedRowsAsync(db, db.Contracts, source.Contracts, "Contracts", cancellationToken);

        await AddSeedRowsAsync(db, db.WorkItems, source.WorkItems, "WorkItems", cancellationToken);
        await AddSeedRowsAsync(db, db.Events, source.Events, "Events", cancellationToken);
        await AddSeedRowsAsync(db, db.History, source.History, "History", cancellationToken);

        foreach (Bike sourceBike in source.Bikes)
        {
            Bike bike = await db.Bikes.SingleAsync(x => x.Id == sourceBike.Id, cancellationToken);
            db.Entry(bike).Property(x => x.ActiveContractId).CurrentValue = sourceBike.ActiveContractId;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task AddSeedRowsAsync<TEntity>(
        RoadbikeCareDbContext db,
        DbSet<TEntity> set,
        IEnumerable<TEntity> rows,
        string tableName,
        CancellationToken cancellationToken) where TEntity : class
    {
        if (!db.Database.IsSqlServer())
        {
            set.AddRange(rows);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        (string On, string Off) commands = tableName switch
        {
            "Customers" => (On: "SET IDENTITY_INSERT [Customers] ON", Off: "SET IDENTITY_INSERT [Customers] OFF"),
            "Shops" => (On: "SET IDENTITY_INSERT [Shops] ON", Off: "SET IDENTITY_INSERT [Shops] OFF"),
            "Plans" => (On: "SET IDENTITY_INSERT [Plans] ON", Off: "SET IDENTITY_INSERT [Plans] OFF"),
            "Bikes" => (On: "SET IDENTITY_INSERT [Bikes] ON", Off: "SET IDENTITY_INSERT [Bikes] OFF"),
            "Staff" => (On: "SET IDENTITY_INSERT [Staff] ON", Off: "SET IDENTITY_INSERT [Staff] OFF"),
            "Contracts" => (On: "SET IDENTITY_INSERT [Contracts] ON", Off: "SET IDENTITY_INSERT [Contracts] OFF"),
            "WorkItems" => (On: "SET IDENTITY_INSERT [WorkItems] ON", Off: "SET IDENTITY_INSERT [WorkItems] OFF"),
            "Events" => (On: "SET IDENTITY_INSERT [Events] ON", Off: "SET IDENTITY_INSERT [Events] OFF"),
            "History" => (On: "SET IDENTITY_INSERT [History] ON", Off: "SET IDENTITY_INSERT [History] OFF"),
            _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, "Unknown seed table.")
        };

        await db.Database.OpenConnectionAsync(cancellationToken);
        var identityInsertEnabled = false;
        try
        {
            await db.Database.ExecuteSqlRawAsync(commands.On, cancellationToken);
            identityInsertEnabled = true;
            set.AddRange(rows);
            await db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            try
            {
                if (identityInsertEnabled)
                {
                    await db.Database.ExecuteSqlRawAsync(commands.Off, cancellationToken);
                }
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }
}
