using Microsoft.Data.Sqlite;
using PrintInvoicer.Core.Data;
using PrintInvoicer.Core.Models;
using PrintInvoicer.Core.Pricing;

namespace PrintInvoicer.Tests;

/// <summary>
/// Opens a database shaped like one from before price snapshots (Fixtures/pre-snapshot.sql) and checks the upgrade
/// freezes each finalized invoice once, at the total it was sent with.
/// </summary>
public sealed class LegacyDatabaseTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"pi-legacy-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_path);
    }

    /// <summary>
    /// Builds the old database from the fixture script, and checks it really has the old shape, so the test cannot
    /// pass against a current database.
    /// </summary>
    private void CreateFixtureDatabase()
    {
        using var c = new SqliteConnection($"Data Source={_path};Pooling=False");
        c.Open();
        using var script = c.CreateCommand();
        script.CommandText = File.ReadAllText(TestFiles.Fixture("pre-snapshot.sql"));
        script.ExecuteNonQuery();

        Assert.Equal(0L, Count(c, "SELECT COUNT(*) FROM pragma_table_info('jobs') WHERE name IN ('price_snapshot', 'finalized_total')"));
        Assert.Equal(3L, Count(c, "SELECT COUNT(*) FROM jobs"));
    }

    /// <summary>Runs a single-number query on an open connection.</summary>
    private static long Count(SqliteConnection c, string sql)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        return (long)cmd.ExecuteScalar()!;
    }

    /// <summary>
    /// Prices a job the way the app does (frozen prices for a finalized job), and checks the quote has no warnings:
    /// a "was finalized at" warning would mean the stored total and the snapshot disagree.
    /// </summary>
    private static decimal Total(Database db, Job job)
    {
        var quote = PricingCalculator.Calculate(job, PriceSnapshot.For(job, db.LoadPriceBook()));
        Assert.Empty(quote.Warnings);
        return quote.Total;
    }

    [Fact]
    public void Pre_snapshot_database_freezes_each_finalized_invoice_at_its_old_total()
    {
        CreateFixtureDatabase();
        var db = new Database(_path);
        var jobs = db.GetJobs();
        var brackets = jobs.Single(j => j.Id == 1);
        var clips = jobs.Single(j => j.Id == 2);
        var draft = jobs.Single(j => j.Id == 3);

        // Both finalized invoices share one filament and one printer, yet each gets its own snapshot and total.
        Assert.True(brackets.Finalized);
        Assert.NotNull(brackets.PriceSnapshot);
        // 3.00 material + 4.00 machine + 0.20 power + 10 slicing + 50 modeling + 20 post-processing
        Assert.Equal(87.20m, brackets.FinalizedTotal);
        Assert.Equal(87.20m, Total(db, brackets));

        Assert.True(clips.Finalized);
        Assert.NotNull(clips.PriceSnapshot);
        // 1.20 material + 4.00 machine + 0.20 power + 10 slicing + 25 modeling
        Assert.Equal(40.40m, clips.FinalizedTotal);
        Assert.Equal(40.40m, Total(db, clips));

        // The draft is not frozen; it follows current prices.
        Assert.Null(draft.PriceSnapshot);
        Assert.Null(draft.FinalizedTotal);
        // 0.60 material + 2.00 machine + 0.10 power + 10 slicing
        Assert.Equal(12.70m, Total(db, draft));

        // The upgrade seeds no filaments and adds no printer.
        Assert.Single(db.GetFilaments());
        Assert.Single(db.GetPrinters());

        using var c = new SqliteConnection($"Data Source={_path};Pooling=False");
        c.Open();
        Assert.Equal(2L, Count(c, "SELECT COUNT(*) FROM pragma_table_info('jobs') WHERE name IN ('price_snapshot', 'finalized_total')"));
    }
}
