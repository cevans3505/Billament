using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Billament.Core.Data;
using Billament.Core.Models;
using Billament.Core.Pricing;

namespace Billament.Tests;

/// <summary>The one-time move of the data folders from the old name, on temporary folders.</summary>
public sealed class DataFolderMigrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"bm-move-{Guid.NewGuid():N}");
    private static readonly DateTime Now = new(2026, 11, 2, 9, 0, 0);

    public DataFolderMigrationTests()
    {
        Directory.CreateDirectory(Docs);
        Directory.CreateDirectory(Local);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_root, recursive: true);
    }

    // The stand-ins for Documents and %LOCALAPPDATA%.
    private string Docs => Path.Combine(_root, "docs");
    private string Local => Path.Combine(_root, "local");
    private DataFolders Folders => DataFolders.Under(Docs, Local);
    private string OldDb => Path.Combine(Folders.OldDocuments, DataFolders.OldDatabaseName);
    private string NewDb => Path.Combine(Folders.NewDocuments, DataFolders.NewDatabaseName);

    /// <summary>
    /// Builds what a 3.7.0 PC has: the old Documents folder with a database holding two finalized invoices and a draft,
    /// a PDF and an old daily backup, and the old local folder with its two small JSON files.
    /// </summary>
    private void CreateOldData(bool withDatabase = true)
    {
        Directory.CreateDirectory(Path.Combine(Folders.OldDocuments, "Invoices"));
        Directory.CreateDirectory(Path.Combine(Folders.OldDocuments, "Backups"));
        if (withDatabase)
        {
            using (var c = new SqliteConnection($"Data Source={OldDb};Pooling=False"))
            {
                c.Open();
                using var script = c.CreateCommand();
                script.CommandText = File.ReadAllText(TestFiles.Fixture("pre-snapshot.sql"));
                script.ExecuteNonQuery();
            }
            // Opening it once freezes the two finalized invoices, as the last old version did on their PC.
            _ = new Database(OldDb);
            SqliteConnection.ClearAllPools();
        }
        File.WriteAllBytes(Path.Combine(Folders.OldDocuments, "Invoices", "2025-014 Dive light brackets.pdf"), [1, 2, 3, 4]);
        File.WriteAllBytes(Path.Combine(Folders.OldDocuments, "Backups", OldDailyBackupName), [5, 6, 7, 8]);

        Directory.CreateDirectory(Folders.OldLocal);
        File.WriteAllText(Path.Combine(Folders.OldLocal, "window-layout.json"), "{}");
        File.WriteAllText(Path.Combine(Folders.OldLocal, "update-check.json"), "{}");
    }

    // A daily backup made by 3.7.0, named with the old prefix.
    private static string OldDailyBackupName => $"{DataFolders.OldName.ToLowerInvariant()}-2026-11-01.db";

    /// <summary>The id, frozen total and frozen prices of every finalized job, read straight from the file.</summary>
    private static List<string> Frozen(string dbPath)
    {
        using var c = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False");
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT id, finalized_total, price_snapshot FROM jobs WHERE finalized = 1 ORDER BY id";
        using var r = cmd.ExecuteReader();
        var rows = new List<string>();
        while (r.Read())
            rows.Add(string.Join("|", Enumerable.Range(0, 3).Select(i =>
                r.IsDBNull(i) ? "<null>" : Convert.ToString(r.GetValue(i), CultureInfo.InvariantCulture))));
        return rows;
    }

    /// <summary>Every file under a folder as a relative path and a SHA-256, sorted; empty for a missing folder.</summary>
    private static string[] Tree(string folder) =>
        !Directory.Exists(folder)
            ? []
            : Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                .Select(f => $"{Path.GetRelativePath(folder, f)} {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f)))}")
                .Order(StringComparer.Ordinal)
                .ToArray();

    /// <summary>Prices a job the way the app does, and checks the quote has no warnings.</summary>
    private static decimal Total(Database db, Job job)
    {
        var quote = PricingCalculator.Calculate(job, PriceSnapshot.For(job, db.LoadPriceBook()));
        Assert.Empty(quote.Warnings);
        return quote.Total;
    }

    [Fact]
    public void Moves_both_folders_and_renames_the_database()
    {
        CreateOldData();

        var result = DataFolderMigration.Run(Folders, Now);

        Assert.Equal(FolderMove.Moved, result.Documents);
        Assert.Equal(FolderMove.Moved, result.Local);
        Assert.Null(result.Problem);
        Assert.Null(result.Notice);
        Assert.Null(result.Error);
        Assert.False(Directory.Exists(Folders.OldDocuments));
        Assert.False(Directory.Exists(Folders.OldLocal));
        Assert.True(File.Exists(NewDb));
        Assert.False(File.Exists(Path.Combine(Folders.NewDocuments, DataFolders.OldDatabaseName)));
        Assert.True(File.Exists(Path.Combine(Folders.NewDocuments, "Invoices", "2025-014 Dive light brackets.pdf")));
        Assert.True(File.Exists(Path.Combine(Folders.NewDocuments, "Backups", OldDailyBackupName)));
        Assert.True(File.Exists(Path.Combine(Folders.NewLocal, "window-layout.json")));
        Assert.True(File.Exists(Path.Combine(Folders.NewLocal, "update-check.json")));

        // The safety copy moved with the folder and is a Billament database that pruning never touches.
        string backups = Path.Combine(Folders.NewDocuments, "Backups");
        Assert.Equal(Path.Combine(backups, "billament-2026-11-02-090000-pre-rename.db"), result.BackupPath);
        Assert.True(File.Exists(result.BackupPath));
        Assert.True(DatabaseBackup.LooksLikeBillament(result.BackupPath!));
        DatabaseBackup.Prune(backups, keep: 0);
        Assert.True(File.Exists(result.BackupPath));
    }

    [Fact]
    public void Finalized_invoices_are_byte_for_byte_the_same_after_the_move()
    {
        CreateOldData();
        var before = Frozen(OldDb);
        var bytes = File.ReadAllBytes(OldDb);
        Assert.Equal(2, before.Count);

        DataFolderMigration.Run(Folders, Now);

        Assert.Equal(before, Frozen(NewDb));
        Assert.Equal(bytes, File.ReadAllBytes(NewDb));

        // Opened the way the app opens it, the two invoices still price to the totals they were sent with.
        var db = new Database(NewDb);
        var jobs = db.GetJobs();
        Assert.Equal(87.20m, Total(db, jobs.Single(j => j.Id == 1)));
        Assert.Equal(40.40m, Total(db, jobs.Single(j => j.Id == 2)));
    }

    [Fact]
    public void The_new_database_is_where_Database_opens_it()
    {
        Assert.Equal(DataFolders.NewName, Path.GetFileName(Path.GetDirectoryName(Database.DefaultPath)));
        Assert.Equal(DataFolders.NewDatabaseName, Path.GetFileName(Database.DefaultPath));
    }
}
