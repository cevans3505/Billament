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

    // Moves folders for real, except that moving the folder named here fails the way a folder held open by another
    // program does. Root ignores permission bits, so failures come through this seam rather than through chmod.
    private static FileOps FailWhenMoving(string source) => new(MoveDirectory: (from, to) =>
    {
        if (from == source) throw new IOException("The folder is in use.");
        Directory.Move(from, to);
    });

    // A file move that always fails, as when another program holds the database file.
    private static void FileInUse(string from, string to) => throw new IOException("The file is in use.");

    [Fact]
    public void Leaves_old_folders_alone_when_the_new_ones_exist()
    {
        CreateOldData();
        Directory.CreateDirectory(Folders.NewDocuments);
        File.WriteAllBytes(NewDb, [1, 1, 1]);
        Directory.CreateDirectory(Folders.NewLocal);
        File.WriteAllText(Path.Combine(Folders.NewLocal, "window-layout.json"), "{\"new\":1}");
        var oldDocs = Tree(Folders.OldDocuments);
        var oldLocal = Tree(Folders.OldLocal);
        var newDocs = Tree(Folders.NewDocuments);
        var newLocal = Tree(Folders.NewLocal);

        var result = DataFolderMigration.Run(Folders, Now);

        Assert.Equal(FolderMove.NewFolderExists, result.Documents);
        Assert.Equal(FolderMove.NewFolderExists, result.Local);
        Assert.Null(result.Notice);
        Assert.Null(result.Problem);
        Assert.Null(result.BackupPath);
        Assert.Equal(oldDocs, Tree(Folders.OldDocuments));
        Assert.Equal(oldLocal, Tree(Folders.OldLocal));
        Assert.Equal(newDocs, Tree(Folders.NewDocuments));
        Assert.Equal(newLocal, Tree(Folders.NewLocal));
        Assert.Empty(Directory.GetFiles(_root, "*pre-rename*", SearchOption.AllDirectories));
    }

    [Fact]
    public void Tells_you_when_an_unrelated_new_folder_blocks_the_move()
    {
        CreateOldData();
        Directory.CreateDirectory(Folders.NewDocuments);
        var oldDocs = Tree(Folders.OldDocuments);

        var result = DataFolderMigration.Run(Folders, Now);

        Assert.Equal(FolderMove.NewFolderExists, result.Documents);
        Assert.Equal(oldDocs, Tree(Folders.OldDocuments));
        Assert.Empty(Tree(Folders.NewDocuments));
        Assert.NotNull(result.Notice);
        Assert.Contains(Folders.OldDocuments, result.Notice);
        Assert.Contains(Folders.NewDocuments, result.Notice);
    }

    [Fact]
    public void Each_pair_follows_its_own_rule()
    {
        CreateOldData();
        Directory.CreateDirectory(Folders.NewLocal);
        var oldLocal = Tree(Folders.OldLocal);

        var result = DataFolderMigration.Run(Folders, Now);

        Assert.Equal(FolderMove.Moved, result.Documents);
        Assert.Equal(FolderMove.NewFolderExists, result.Local);
        Assert.Equal(oldLocal, Tree(Folders.OldLocal));
        Assert.True(File.Exists(NewDb));
    }

    [Fact]
    public void Nothing_to_move_creates_nothing()
    {
        var result = DataFolderMigration.Run(Folders, Now);

        Assert.Equal(FolderMove.NothingToMove, result.Documents);
        Assert.Equal(FolderMove.NothingToMove, result.Local);
        Assert.Null(result.BackupPath);
        Assert.Empty(Directory.GetFileSystemEntries(Docs));
        Assert.Empty(Directory.GetFileSystemEntries(Local));
    }

    [Fact]
    public void A_file_with_the_new_name_counts_as_taken()
    {
        CreateOldData();
        File.WriteAllText(Folders.NewDocuments, "not a folder");
        var oldDocs = Tree(Folders.OldDocuments);

        var result = DataFolderMigration.Run(Folders, Now);

        Assert.Equal(FolderMove.NewFolderExists, result.Documents);
        Assert.Equal(oldDocs, Tree(Folders.OldDocuments));
        Assert.True(File.Exists(Folders.NewDocuments));
    }

    [Fact]
    public void A_dangling_link_with_the_new_name_counts_as_taken()
    {
        CreateOldData();
        try
        {
            Directory.CreateSymbolicLink(Folders.NewDocuments, Path.Combine(_root, "missing"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Windows without the symbolic link privilege cannot make the link, so there is nothing to check there.
            // Linux and the GitHub runner always run this fact.
            return;
        }
        var oldDocs = Tree(Folders.OldDocuments);

        var result = DataFolderMigration.Run(Folders, Now);

        Assert.Equal(FolderMove.NewFolderExists, result.Documents);
        Assert.Equal(oldDocs, Tree(Folders.OldDocuments));
        Assert.NotNull(new FileInfo(Folders.NewDocuments).LinkTarget);
    }

    [Fact]
    public void Failed_backup_moves_nothing()
    {
        CreateOldData(withDatabase: false);
        File.WriteAllText(OldDb, string.Concat(Enumerable.Repeat("this is plain text, not a database. ", 100)));
        var oldDocs = Tree(Folders.OldDocuments);
        var oldLocal = Tree(Folders.OldLocal);

        var result = DataFolderMigration.Run(Folders, Now);

        Assert.Equal(FolderMove.Failed, result.Documents);
        Assert.Equal(FolderMove.NotTried, result.Local);
        Assert.True(result.DocumentsFailed);
        Assert.Contains(Folders.OldDocuments, result.Problem);
        Assert.Contains("Nothing was lost", result.Problem);
        Assert.IsType<SqliteException>(result.Error);
        Assert.Equal(oldDocs, Tree(Folders.OldDocuments));
        Assert.Equal(oldLocal, Tree(Folders.OldLocal));
        Assert.False(Directory.Exists(Folders.NewDocuments));
        Assert.False(Directory.Exists(Folders.NewLocal));
    }

    [Theory]
    [InlineData("-journal")]
    [InlineData("-wal")]
    [InlineData("-shm")]
    public void A_journal_left_by_a_crash_stops_the_move_with_a_plain_message(string suffix)
    {
        // What a crash in the middle of a save leaves next to the database.
        CreateOldData();
        File.WriteAllBytes(OldDb + suffix, [1, 2, 3]);
        var oldDocs = Tree(Folders.OldDocuments);
        var oldLocal = Tree(Folders.OldLocal);

        var result = DataFolderMigration.Run(Folders, Now);

        Assert.True(result.DocumentsFailed);
        Assert.Equal(FolderMove.NotTried, result.Local);
        Assert.Null(result.BackupPath);
        Assert.Contains(DataFolders.OldDatabaseName + suffix, result.Problem);
        Assert.Contains("did not close normally", result.Problem);
        Assert.Contains("Nothing was lost", result.Problem);
        Assert.Contains(Folders.OldDocuments, result.Problem);
        Assert.DoesNotContain("sync app", result.Problem);
        Assert.Equal(oldDocs, Tree(Folders.OldDocuments));
        Assert.Equal(oldLocal, Tree(Folders.OldLocal));
        Assert.False(Directory.Exists(Folders.NewDocuments));
        Assert.False(Directory.Exists(Folders.NewLocal));

        // Once the old version has closed normally, the journal is gone and the move goes ahead.
        File.Delete(OldDb + suffix);
        var later = DataFolderMigration.Run(Folders, Now.AddMinutes(1));
        Assert.Equal(FolderMove.Moved, later.Documents);
        Assert.True(File.Exists(NewDb));
    }

    [Fact]
    public void A_journal_next_to_a_half_finished_move_stops_the_finishing_start()
    {
        CreateOldData();
        var stuck = FailWhenMoving(Folders.NewDocuments) with { MoveFile = FileInUse };
        Assert.True(DataFolderMigration.Run(Folders, Now, stuck).DocumentsFailed);
        string movedOld = Path.Combine(Folders.NewDocuments, DataFolders.OldDatabaseName);
        File.WriteAllBytes(movedOld + "-journal", [1, 2, 3]);
        var before = Tree(Folders.NewDocuments);

        var result = DataFolderMigration.Run(Folders, Now.AddMinutes(1));

        Assert.True(result.DocumentsFailed);
        Assert.Contains(Folders.NewDocuments, result.Problem);
        Assert.Contains("Nothing was lost", result.Problem);
        Assert.Equal(before, Tree(Folders.NewDocuments));
        Assert.False(File.Exists(NewDb));
    }

    [Fact]
    public void Failed_folder_move_leaves_old_data_and_creates_no_new_folder()
    {
        CreateOldData();
        var bytes = File.ReadAllBytes(OldDb);

        var result = DataFolderMigration.Run(Folders, Now, FailWhenMoving(Folders.OldDocuments));

        Assert.Equal(FolderMove.Failed, result.Documents);
        Assert.Equal(FolderMove.Moved, result.Local);
        Assert.Contains(Folders.OldDocuments, result.Problem);
        Assert.Contains("Nothing was lost", result.Problem);
        Assert.Equal(bytes, File.ReadAllBytes(OldDb));
        Assert.False(Directory.Exists(Folders.NewDocuments));
        Assert.StartsWith(Path.Combine(Folders.OldDocuments, "Backups"), result.BackupPath);
        Assert.True(File.Exists(result.BackupPath));
    }

    [Fact]
    public void A_move_made_by_a_second_copy_at_the_same_moment_names_the_new_folder()
    {
        CreateOldData();
        // The folder is moved by someone else just before this copy's own move, which then fails.
        var raced = new FileOps(MoveDirectory: (from, to) =>
        {
            Directory.Move(from, to);
            if (from == Folders.OldDocuments) throw new DirectoryNotFoundException("The folder is gone.");
        });

        var result = DataFolderMigration.Run(Folders, Now, raced);

        Assert.True(result.DocumentsFailed);
        Assert.Contains($"your data is now in {Folders.NewDocuments}", result.Problem);
        Assert.DoesNotContain("still in", result.Problem);
        Assert.Equal(Path.Combine(Folders.NewDocuments, "Backups", Path.GetFileName(result.BackupPath)), result.BackupPath);
        Assert.True(File.Exists(result.BackupPath));
        Assert.True(File.Exists(Path.Combine(Folders.NewDocuments, DataFolders.OldDatabaseName)));

        // The next start finishes the rename in the new folder.
        var next = DataFolderMigration.Run(Folders, Now.AddMinutes(1));
        Assert.Equal(FolderMove.Moved, next.Documents);
        Assert.True(File.Exists(NewDb));
    }

    [Fact]
    public void A_failed_local_move_is_not_a_failure()
    {
        CreateOldData();

        var result = DataFolderMigration.Run(Folders, Now, FailWhenMoving(Folders.OldLocal));

        Assert.Equal(FolderMove.Moved, result.Documents);
        Assert.Equal(FolderMove.Failed, result.Local);
        Assert.False(result.DocumentsFailed);
        Assert.Null(result.Problem);
        Assert.Null(result.Notice);
        Assert.NotNull(result.Error);
        Assert.True(Directory.Exists(Folders.OldLocal));
        Assert.True(File.Exists(NewDb));
    }

    [Fact]
    public void A_failed_rename_moves_the_folder_back()
    {
        CreateOldData();
        var bytes = File.ReadAllBytes(OldDb);

        var result = DataFolderMigration.Run(Folders, Now, new FileOps(MoveFile: FileInUse));

        Assert.Equal(FolderMove.Failed, result.Documents);
        Assert.True(File.Exists(OldDb));
        Assert.Equal(bytes, File.ReadAllBytes(OldDb));
        Assert.False(Directory.Exists(Folders.NewDocuments));
        Assert.Contains(Folders.OldDocuments, result.Problem);
        Assert.Contains("Nothing was lost", result.Problem);
        Assert.IsType<IOException>(result.Error);
    }

    [Fact]
    public void When_the_move_back_also_fails_the_next_start_finishes_it()
    {
        CreateOldData();
        var before = Frozen(OldDb);
        var stuck = FailWhenMoving(Folders.NewDocuments) with { MoveFile = FileInUse };

        var first = DataFolderMigration.Run(Folders, Now, stuck);

        Assert.Equal(FolderMove.Failed, first.Documents);
        Assert.Contains(Folders.NewDocuments, first.Problem);
        Assert.True(File.Exists(Path.Combine(Folders.NewDocuments, DataFolders.OldDatabaseName)));

        var second = DataFolderMigration.Run(Folders, Now.AddMinutes(1));

        Assert.Equal(FolderMove.Moved, second.Documents);
        Assert.Equal(FolderMove.NothingToMove, second.Local);
        Assert.True(File.Exists(NewDb));
        Assert.False(File.Exists(Path.Combine(Folders.NewDocuments, DataFolders.OldDatabaseName)));
        Assert.Equal(before, Frozen(NewDb));
    }

    [Fact]
    public void Two_database_files_are_both_kept()
    {
        CreateOldData();
        var oldBytes = File.ReadAllBytes(OldDb);
        File.WriteAllBytes(Path.Combine(Folders.OldDocuments, DataFolders.NewDatabaseName), [9, 9, 9]);

        var result = DataFolderMigration.Run(Folders, Now);

        Assert.Equal(FolderMove.Moved, result.Documents);
        Assert.Equal(oldBytes, File.ReadAllBytes(Path.Combine(Folders.NewDocuments, DataFolders.OldDatabaseName)));
        Assert.Equal(new byte[] { 9, 9, 9 }, File.ReadAllBytes(NewDb));
        Assert.NotNull(result.Notice);
        Assert.Contains(DataFolders.OldDatabaseName, result.Notice);
        Assert.Contains(DataFolders.NewDatabaseName, result.Notice);
    }

    [Fact]
    public void An_old_folder_without_a_database_moves_without_a_backup()
    {
        Directory.CreateDirectory(Folders.OldDocuments);
        File.WriteAllText(Path.Combine(Folders.OldDocuments, "note.txt"), "sample");

        var result = DataFolderMigration.Run(Folders, Now);

        Assert.Equal(FolderMove.Moved, result.Documents);
        Assert.Null(result.BackupPath);
        Assert.True(File.Exists(Path.Combine(Folders.NewDocuments, "note.txt")));
        Assert.False(Directory.Exists(Path.Combine(Folders.NewDocuments, "Backups")));
    }

    [Theory]
    [InlineData("", "local")]
    [InlineData("docs", "")]
    [InlineData("  ", "local")]
    [InlineData("docs", "local")]
    public void Empty_or_relative_special_folders_are_refused(string documents, string localAppData)
    {
        // Windows returns an empty path when it cannot find a special folder; relative paths would hit the current folder.
        string full(string p) => p == "local" ? Local : p;
        Assert.Throws<InvalidOperationException>(() => DataFolders.Under(full(documents), full(localAppData)));
    }

    [Fact]
    public void A_relative_folder_stops_the_move_before_anything_is_touched()
    {
        CreateOldData();
        var oldDocs = Tree(Folders.OldDocuments);
        var oldLocal = Tree(Folders.OldLocal);
        var relative = Folders with { OldDocuments = DataFolders.OldName, NewDocuments = DataFolders.NewName };

        Assert.Throws<InvalidOperationException>(() => DataFolderMigration.Run(relative, Now));

        Assert.Equal(oldDocs, Tree(Folders.OldDocuments));
        Assert.Equal(oldLocal, Tree(Folders.OldLocal));
        Assert.False(Directory.Exists(Folders.NewLocal));
    }

    [Fact]
    public void Runs_only_once()
    {
        CreateOldData();
        DataFolderMigration.Run(Folders, Now);
        var newDocs = Tree(Folders.NewDocuments);
        var newLocal = Tree(Folders.NewLocal);

        var again = DataFolderMigration.Run(Folders, Now.AddHours(1));

        Assert.Equal(FolderMove.NothingToMove, again.Documents);
        Assert.Equal(FolderMove.NothingToMove, again.Local);
        Assert.Null(again.BackupPath);
        Assert.Equal(newDocs, Tree(Folders.NewDocuments));
        Assert.Equal(newLocal, Tree(Folders.NewLocal));
    }

    [Fact]
    public void A_later_start_succeeds_after_a_failed_one()
    {
        CreateOldData();

        var failed = DataFolderMigration.Run(Folders, Now, FailWhenMoving(Folders.OldDocuments));
        Assert.True(failed.DocumentsFailed);

        var later = DataFolderMigration.Run(Folders, Now.AddMinutes(5));

        Assert.Equal(FolderMove.Moved, later.Documents);
        Assert.True(File.Exists(NewDb));
        Assert.False(Directory.Exists(Folders.OldDocuments));
    }

    // ---------------- Custom backup and invoice folders ----------------

    /// <summary>Runs SQL against a database file through an unpooled connection, so nothing keeps the file open.</summary>
    private static void Execute(string dbPath, string sql, params (string Name, object Value)[] parameters)
    {
        using (var c = new SqliteConnection($"Data Source={dbPath};Pooling=False"))
        {
            c.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in parameters) cmd.Parameters.AddWithValue(name, value);
            cmd.ExecuteNonQuery();
        }
        SqliteConnection.ClearAllPools();
    }

    /// <summary>Writes setting rows straight into a database file, the way SaveSettings stores them.</summary>
    private static void SetSettings(string dbPath, params (string Key, string Value)[] rows)
    {
        foreach (var (key, value) in rows)
            Execute(dbPath, "INSERT INTO settings(key, value) VALUES ($k, $v) ON CONFLICT(key) DO UPDATE SET value = excluded.value",
                ("$k", key), ("$v", value));
    }

    /// <summary>
    /// Every row of every table (the sqlite_ tables left out) as one line of text: the table, the row id and each value,
    /// in table name then row id order. Two dumps are equal only when no row anywhere changed.
    /// </summary>
    private static List<string> Dump(string dbPath)
    {
        using var c = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False");
        c.Open();
        var tables = new List<string>();
        using (var list = c.CreateCommand())
        {
            list.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite!_%' ESCAPE '!' ORDER BY name";
            using var names = list.ExecuteReader();
            while (names.Read()) tables.Add(names.GetString(0));
        }

        var lines = new List<string>();
        foreach (string table in tables)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = $"SELECT rowid, * FROM \"{table}\" ORDER BY rowid";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                lines.Add(table + "|" + string.Join("|", Enumerable.Range(0, r.FieldCount).Select(i =>
                    r.IsDBNull(i) ? "<null>"
                    : r.GetValue(i) is byte[] blob ? Convert.ToBase64String(blob)
                    : Convert.ToString(r.GetValue(i), CultureInfo.InvariantCulture))));
        }
        return lines;
    }

    // Turns a path written with forward slashes into one with this system's separator.
    private static string Native(string path) => path.Replace('/', Path.DirectorySeparatorChar);

    [Theory]
    [InlineData(DataFolders.OldName, DataFolders.NewName)]
    [InlineData(DataFolders.OldName + "/", DataFolders.NewName)]
    [InlineData(DataFolders.OldName + "/Invoices 2026", DataFolders.NewName + "/Invoices 2026")]
    [InlineData(DataFolders.OldName + "/Sub/../Backups", DataFolders.NewName + "/Backups")]
    [InlineData(DataFolders.OldName + "Old/x", null)]
    [InlineData(DataFolders.OldName + "2026", null)]
    [InlineData(DataFolders.OldName + "/../Other", null)]
    [InlineData("Other/" + DataFolders.OldName + "/x", null)]
    public void Remaps_only_paths_inside_the_old_folder(string relative, string? expectedRelative)
    {
        string value = Path.Combine(Docs, Native(relative));

        string? result = DataFolderMigration.RemapFolder(value, Folders.OldDocuments, Folders.NewDocuments, StringComparison.Ordinal);

        if (expectedRelative is null)
            Assert.Same(value, result);   // not a folder of the old data: the very same text comes back
        else
            Assert.Equal(Path.Combine(Docs, Native(expectedRelative)), result);
    }

    [Fact]
    public void Blank_and_relative_values_are_left_as_they_are()
    {
        foreach (string? value in new string?[] { null, "", "   ", "Invoices" })
            Assert.Same(value, DataFolderMigration.RemapFolder(value, Folders.OldDocuments, Folders.NewDocuments, StringComparison.Ordinal));
    }

    [Fact]
    public void The_letter_case_rule_comes_from_the_caller()
    {
        string value = Path.Combine(Docs, DataFolders.OldName.ToUpperInvariant(), "Invoices");

        Assert.Equal(Path.Combine(Folders.NewDocuments, "Invoices"),
            DataFolderMigration.RemapFolder(value, Folders.OldDocuments, Folders.NewDocuments, StringComparison.OrdinalIgnoreCase));
        Assert.Same(value,
            DataFolderMigration.RemapFolder(value, Folders.OldDocuments, Folders.NewDocuments, StringComparison.Ordinal));

        // The same rule reaches the settings rewrite inside Run.
        CreateOldData();
        SetSettings(OldDb, (nameof(AppSettings.InvoiceFolder), value));
        DataFolderMigration.Run(Folders, Now, pathComparison: StringComparison.OrdinalIgnoreCase);
        Assert.Equal(Path.Combine(Folders.NewDocuments, "Invoices"), new Database(NewDb).LoadSettings().InvoiceFolder);
    }

    [Fact]
    public void Custom_folders_inside_the_old_folder_follow_the_move()
    {
        CreateOldData();
        SetSettings(OldDb,
            (nameof(AppSettings.BackupFolder), Path.Combine(Folders.OldDocuments, "My backups")),
            (nameof(AppSettings.InvoiceFolder), Path.Combine(Folders.OldDocuments, "Invoices 2026")));

        DataFolderMigration.Run(Folders, Now, pathComparison: StringComparison.Ordinal);

        var settings = new Database(NewDb).LoadSettings();
        Assert.Equal(Path.Combine(Folders.NewDocuments, "My backups"), settings.BackupFolder);
        Assert.Equal(Path.Combine(Folders.NewDocuments, "Invoices 2026"), settings.InvoiceFolder);

        // The next backup lands in the moved folder, not in the old name's folder.
        string? made = DatabaseBackup.DailyIfDue(NewDb, settings.BackupFolder, Now);
        Assert.NotNull(made);
        Assert.Equal(settings.BackupFolder, Path.GetDirectoryName(made));
        Assert.True(File.Exists(made));
        Assert.False(Directory.Exists(Folders.OldDocuments));
    }

    [Fact]
    public void Folders_set_elsewhere_are_left_exactly_as_they_are()
    {
        CreateOldData();
        string elsewhere = Path.Combine(_root, "elsewhere", "Backups");
        string sibling = Path.Combine(Docs, DataFolders.OldName + "Old", "x");
        SetSettings(OldDb, (nameof(AppSettings.BackupFolder), elsewhere), (nameof(AppSettings.InvoiceFolder), sibling));
        var bytes = File.ReadAllBytes(OldDb);

        DataFolderMigration.Run(Folders, Now, pathComparison: StringComparison.Ordinal);

        var settings = new Database(NewDb).LoadSettings();
        Assert.Equal(elsewhere, settings.BackupFolder);
        Assert.Equal(sibling, settings.InvoiceFolder);
        SqliteConnection.ClearAllPools();
        Assert.Equal(bytes, File.ReadAllBytes(NewDb));   // never opened for writing
    }

    [Fact]
    public void Only_the_two_folder_settings_change()
    {
        CreateOldData();
        SetSettings(OldDb,
            (nameof(AppSettings.BackupFolder), Path.Combine(Folders.OldDocuments, "My backups")),
            (nameof(AppSettings.InvoiceFolder), Path.Combine(Folders.OldDocuments, "Invoices 2026")));
        var before = Dump(OldDb);
        var frozen = Frozen(OldDb);
        Assert.Equal(2, frozen.Count);

        DataFolderMigration.Run(Folders, Now, pathComparison: StringComparison.Ordinal);

        var after = Dump(NewDb);
        Assert.Equal(before.Count, after.Count);
        var removed = before.Except(after).ToList();
        var added = after.Except(before).ToList();
        Assert.Equal(2, removed.Count);
        Assert.Equal(2, added.Count);
        Assert.All(removed.Concat(added), line => Assert.True(
            line.StartsWith("settings|") && (line.Contains("|BackupFolder|") || line.Contains("|InvoiceFolder|")), line));
        Assert.Equal(frozen, Frozen(NewDb));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Custom_folders_follow_the_move_when_billament_db_was_already_in_the_old_folder(bool withOldDatabase)
    {
        // Both database files in the old folder (the notice case), or only billament.db: the app opens billament.db.
        CreateOldData(withDatabase: withOldDatabase);
        string already = Path.Combine(Folders.OldDocuments, DataFolders.NewDatabaseName);
        _ = new Database(already);
        SqliteConnection.ClearAllPools();
        SetSettings(already,
            (nameof(AppSettings.BackupFolder), Path.Combine(Folders.OldDocuments, "My backups")),
            (nameof(AppSettings.InvoiceFolder), Path.Combine(Folders.OldDocuments, "Invoices 2026")));
        byte[]? oldBytes = withOldDatabase ? File.ReadAllBytes(OldDb) : null;

        var result = DataFolderMigration.Run(Folders, Now, pathComparison: StringComparison.Ordinal);

        Assert.Equal(FolderMove.Moved, result.Documents);
        Assert.Null(result.Error);
        var settings = new Database(NewDb).LoadSettings();
        Assert.Equal(Path.Combine(Folders.NewDocuments, "My backups"), settings.BackupFolder);
        Assert.Equal(Path.Combine(Folders.NewDocuments, "Invoices 2026"), settings.InvoiceFolder);
        if (oldBytes is not null)
        {
            // The old database is kept next to it, unchanged, and the notice says so.
            SqliteConnection.ClearAllPools();
            Assert.Equal(oldBytes, File.ReadAllBytes(Path.Combine(Folders.NewDocuments, DataFolders.OldDatabaseName)));
            Assert.Contains(DataFolders.OldDatabaseName, result.Notice);
        }

        // The next backup lands in the moved folder and never recreates the old one.
        string? made = DatabaseBackup.DailyIfDue(NewDb, settings.BackupFolder, Now);
        Assert.Equal(settings.BackupFolder, Path.GetDirectoryName(made));
        Assert.False(Directory.Exists(Folders.OldDocuments));
    }

    [Fact]
    public void A_settings_update_that_fails_is_a_notice()
    {
        CreateOldData();
        SetSettings(OldDb, (nameof(AppSettings.BackupFolder), Path.Combine(Folders.OldDocuments, "My backups")));
        Execute(OldDb, "CREATE TRIGGER no_update BEFORE UPDATE ON settings BEGIN SELECT RAISE(ABORT, 'read only'); END;");

        var result = DataFolderMigration.Run(Folders, Now, pathComparison: StringComparison.Ordinal);

        Assert.Equal(FolderMove.Moved, result.Documents);
        Assert.False(result.DocumentsFailed);
        Assert.Null(result.Problem);
        Assert.NotNull(result.Notice);
        Assert.Contains("Settings tab", result.Notice);
        Assert.IsType<SqliteException>(result.Error);
        Assert.True(File.Exists(NewDb));
    }

    [Fact]
    public void A_database_without_a_settings_table_is_moved_and_left_alone()
    {
        CreateOldData(withDatabase: false);
        Execute(OldDb, "CREATE TABLE stuff (x INTEGER); INSERT INTO stuff(x) VALUES (1);");
        var bytes = File.ReadAllBytes(OldDb);

        var result = DataFolderMigration.Run(Folders, Now, pathComparison: StringComparison.Ordinal);

        Assert.Equal(FolderMove.Moved, result.Documents);
        Assert.Null(result.Notice);
        Assert.Null(result.Error);
        Assert.Equal(bytes, File.ReadAllBytes(NewDb));
    }

    [Fact]
    public void The_finishing_start_also_updates_the_folder_settings()
    {
        CreateOldData();
        SetSettings(OldDb,
            (nameof(AppSettings.BackupFolder), Path.Combine(Folders.OldDocuments, "My backups")),
            (nameof(AppSettings.InvoiceFolder), Path.Combine(Folders.OldDocuments, "Invoices 2026")));
        var stuck = FailWhenMoving(Folders.NewDocuments) with { MoveFile = FileInUse };

        var first = DataFolderMigration.Run(Folders, Now, stuck, StringComparison.Ordinal);
        Assert.True(first.DocumentsFailed);

        var second = DataFolderMigration.Run(Folders, Now.AddMinutes(1), pathComparison: StringComparison.Ordinal);

        Assert.Equal(FolderMove.Moved, second.Documents);
        var settings = new Database(NewDb).LoadSettings();
        Assert.Equal(Path.Combine(Folders.NewDocuments, "My backups"), settings.BackupFolder);
        Assert.Equal(Path.Combine(Folders.NewDocuments, "Invoices 2026"), settings.InvoiceFolder);
    }

    // ---------------- Restoring a backup from before the rename ----------------

    [Fact]
    public void A_restored_backup_from_before_the_rename_gets_its_folders_updated()
    {
        CreateOldData();
        SetSettings(OldDb,
            (nameof(AppSettings.BackupFolder), Path.Combine(Folders.OldDocuments, "My backups")),
            (nameof(AppSettings.InvoiceFolder), Path.Combine(Folders.OldDocuments, "Invoices 2026")));
        var frozen = Frozen(OldDb);

        // A backup made by 3.7.0, picked from a folder outside the data folder.
        string picked = Path.Combine(_root, "picked");
        string made = DatabaseBackup.Manual(OldDb, picked, new DateTime(2026, 10, 20, 9, 0, 0));
        string backup = Path.Combine(picked, $"{DataFolders.OldName.ToLowerInvariant()}-2026-10-20.db");
        File.Move(made, backup);

        // The live 4.0 database is replaced by it, as the Restore a backup button does.
        var live = new Database(NewDb);
        DatabaseBackup.Restore(backup, NewDb, Path.Combine(Folders.NewDocuments, "Backups"), Now);
        Assert.Equal(Path.Combine(Folders.OldDocuments, "My backups"), new Database(NewDb).LoadSettings().BackupFolder);

        Assert.True(DataFolderMigration.UpdateFolderSettings(NewDb, Folders, StringComparison.Ordinal));

        var settings = new Database(NewDb).LoadSettings();
        Assert.Equal(Path.Combine(Folders.NewDocuments, "My backups"), settings.BackupFolder);
        Assert.Equal(Path.Combine(Folders.NewDocuments, "Invoices 2026"), settings.InvoiceFolder);
        Assert.Equal(frozen, Frozen(NewDb));
    }

    [Fact]
    public void Updating_folder_settings_changes_nothing_when_no_folder_is_inside_the_old_one()
    {
        CreateOldData();
        SetSettings(OldDb,
            (nameof(AppSettings.BackupFolder), Path.Combine(_root, "elsewhere", "Backups")),
            (nameof(AppSettings.InvoiceFolder), Path.Combine(Docs, DataFolders.OldName + "Old", "x")));
        var bytes = File.ReadAllBytes(OldDb);

        Assert.False(DataFolderMigration.UpdateFolderSettings(OldDb, Folders, StringComparison.Ordinal));

        Assert.Equal(bytes, File.ReadAllBytes(OldDb));   // never opened for writing
    }
}
