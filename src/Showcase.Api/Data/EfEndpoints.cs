// EF Core 11 queries. Generated SQL is logged to the console (see appsettings.json).

using Microsoft.EntityFrameworkCore;

namespace Showcase.Api.Data;

public static class EfEndpoints
{
    public static void MapEfEndpoints(this WebApplication app)
    {
        app.MapGet("/ef/report", async (ShopContext db, CancellationToken ct) =>
        {
            await db.Database.MigrateAsync(ct); // applies migrations created with `dotnet ef database update <name> --add`
            await SeedAsync(db, ct);

            // FullJoin (new LINQ operator in .NET 11) -> FULL JOIN in SQL.
            // Customers without orders AND orders without customers both appear.
            var fullJoin = await db.Customers
                .FullJoin(
                    db.Orders,
                    c => (int?)c.Id,
                    o => o.CustomerId,
                    (c, o) => new { Customer = c == null ? null : c.Name, OrderId = o == null ? (int?)null : o.Id })
                .ToListAsync(ct);

            // MaxBy / MinBy translated to ORDER BY ... LIMIT 1.
            var biggest = await db.Orders.MaxByAsync(o => o.Total, ct);
            var mostOrders = await db.Customers.MaxByAsync(c => c.Orders.Count(), ct);
            var fewestOrders = await db.Customers.MinByAsync(c => c.Orders.Count(), ct);

            return new
            {
                fullJoin,
                biggestOrder = new { biggest?.Id, biggest?.Total },
                customerWithMostOrders = mostOrders?.Name,
                customerWithFewestOrders = fewestOrders?.Name,
            };
        });
    }

    private static async Task SeedAsync(ShopContext db, CancellationToken ct)
    {
        if (await db.Customers.AnyAsync(ct))
        {
            return;
        }
        var ada = new Customer { Name = "Ada" };
        var grace = new Customer { Name = "Grace" };
        var linus = new Customer { Name = "Linus" }; // no orders
        db.Customers.AddRange(ada, grace, linus);
        db.Orders.AddRange(
            new Order { Customer = ada, Total = 10m },
            new Order { Customer = ada, Total = 99.5m },
            new Order { Customer = grace, Total = 42m },
            new Order { Total = 7m }); // guest order, no customer
        await db.SaveChangesAsync(ct);
    }
}
