using System.Globalization;
using Microsoft.Data.Sqlite;
using Billament.Core.Models;

namespace Billament.Core.Data;

/// <summary>
/// The two pairs of folders of the one-time move in 4.0 (the app was called PrintInvoicer before). Passed in, so tests
/// use temporary folders and the app passes the real Windows folders.
/// </summary>
public sealed record DataFolders(string OldDocuments, string NewDocuments, string OldLocal, string NewLocal)
{
    /// <summary>The folder name before 4.0, inside Documents and inside the local application data folder.</summary>
    public const string OldName = "PrintInvoicer";

    /// <summary>The folder name from 4.0 on; the same folder name <see cref="Database.DefaultPath"/> uses.</summary>
    public const string NewName = "Billament";

    /// <summary>The database file name before 4.0.</summary>
    public const string OldDatabaseName = "printinvoicer.db";

    /// <summary>The database file name from 4.0 on; the same file name <see cref="Database.DefaultPath"/> uses.</summary>
    public const string NewDatabaseName = "billament.db";

    /// <summary>The four folders under a Documents folder and a local application data folder. Reads nothing.</summary>
    public static DataFolders Under(string documents, string localAppData) => new(
        Path.Combine(documents, OldName), Path.Combine(documents, NewName),
        Path.Combine(localAppData, OldName), Path.Combine(localAppData, NewName));
}

/// <summary>What happened to one pair of folders in <see cref="DataFolderMigration.Run"/>.</summary>
public enum FolderMove
{
    /// <summary>There is no old folder.</summary>
    NothingToMove,

    /// <summary>Something already has the new name, so the old folder was left alone.</summary>
    NewFolderExists,

    /// <summary>The old folder now has the new name.</summary>
    Moved,

    /// <summary>The move did not work; the data is still where it was, or in the one place the message names.</summary>
    Failed,

    /// <summary>The run stopped before this pair because the safety copy failed.</summary>
    NotTried,
}

/// <summary>
/// The outcome of the one-time move. <paramref name="Problem"/> is the plain message for the user when the Documents
/// pair failed (start-up must stop); <paramref name="Notice"/> is a plain message worth showing once when nothing
/// failed; <paramref name="Error"/> holds the technical details for the error file (the Documents failure if there is
/// one, otherwise the local failure); <paramref name="BackupPath"/> is where the safety copy is now (under the new
/// Documents folder once that has moved).
/// </summary>
public sealed record DataMoveResult(
    FolderMove Documents, FolderMove Local, string? BackupPath, string? Problem, string? Notice, Exception? Error)
{
    /// <summary>True when the Documents folder could not be moved and start-up must stop.</summary>
    public bool DocumentsFailed => Documents == FolderMove.Failed;
}

/// <summary>Test seam: the two operations that can fail part way. Null means the real Directory and File methods.</summary>
public sealed record FileOps(Action<string, string>? MoveDirectory = null, Action<string, string>? MoveFile = null);

/// <summary>
/// The once-only move of the data folders from the name used before 4.0. It first copies the old database to a safety
/// backup, then renames the old folders to the new names and the database file to its new name. It never deletes,
/// copies, merges or overwrites anything, and it never touches an old folder whose new name is already taken. Every
/// folder and the time are passed in, so the logic runs in tests on temporary folders.
/// </summary>
public static class DataFolderMigration
{
    /// <summary>The label of the safety copy of the old database (see <see cref="DatabaseBackup.Manual"/>).</summary>
    public const string BackupLabel = "pre-rename";

    /// <summary>
    /// Does the move, in this order: the safety copy, the local folder, the Documents folder, the database file. A
    /// pair whose new name is taken is skipped on its own. A failed Documents move leaves the data where it was. Right
    /// after the database file is renamed, a backup or invoice folder set inside the old Documents folder is pointed at
    /// the same place inside the new one; <paramref name="pathComparison"/> decides whether letter case counts in that
    /// comparison (default: it does not on Windows, it does elsewhere).
    /// </summary>
    public static DataMoveResult Run(
        DataFolders folders, DateTime now, FileOps? ops = null, StringComparison? pathComparison = null)
    {
        string oldDb = Path.Combine(folders.OldDocuments, DataFolders.OldDatabaseName);
        string movedOldDb = Path.Combine(folders.NewDocuments, DataFolders.OldDatabaseName);
        string movedNewDb = Path.Combine(folders.NewDocuments, DataFolders.NewDatabaseName);
        var comparison = pathComparison ?? (OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        bool renamed = false;

        // An earlier start moved the folder but could not rename the database inside it: finish that, touching only
        // the new folder. This stands in for a marker file.
        bool repair = FolderExists(folders.NewDocuments) && FileExists(movedOldDb) && !FileExists(movedNewDb);
        FolderMove? documents = repair ? FolderMove.Moved : Decide(folders.OldDocuments, folders.NewDocuments);
        FolderMove? local = Decide(folders.OldLocal, folders.NewLocal);

        // A new folder that Billament did not make blocks the move. Say where the old data is, once, and touch nothing.
        string? notice = null;
        if (documents == FolderMove.NewFolderExists && FolderExists(folders.NewDocuments) && FileExists(oldDb)
            && !FileExists(movedOldDb) && !FileExists(movedNewDb))
        {
            notice = $"Billament found your data from {DataFolders.OldName} in {folders.OldDocuments}, but a folder named " +
                $"{folders.NewDocuments} already exists, so nothing was moved and Billament is starting with the data in " +
                $"{folders.NewDocuments}. To use your old data instead, close Billament, rename or move " +
                $"{folders.NewDocuments}, and start Billament again.";
        }

        // The safety copy comes first, so a move that goes wrong later still leaves a good copy of the data.
        string? backup = null;
        if (documents is null && FileExists(oldDb))
        {
            try
            {
                // AUDIT: file write. The one-time safety copy of the old database, in the old data folder's Backups folder, before anything moves.
                backup = DatabaseBackup.Manual(oldDb, Path.Combine(folders.OldDocuments, "Backups"), now, BackupLabel);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
            {
                string message = $"Billament could not make a safety copy of your data in {folders.OldDocuments}, so it did not " +
                    $"move anything and has closed. Nothing was lost: your data is still in {folders.OldDocuments}. Close " +
                    $"{DataFolders.OldName} and any other program that may be using that folder, such as a sync app, and start " +
                    "Billament again.";
                return new DataMoveResult(FolderMove.Failed, local ?? FolderMove.NotTried, null, message, null, ex);
            }
        }

        // No pooled connection may keep a database file open while its folder is renamed.
        SqliteConnection.ClearAllPools();

        // The local folder goes first: if the Documents move then fails, the error file is written into the moved folder.
        Exception? localError = null;
        if (local is null)
        {
            try
            {
                // AUDIT: folder move. Renames %LOCALAPPDATA%\PrintInvoicer to %LOCALAPPDATA%\Billament, only when nothing has the new name.
                (ops?.MoveDirectory ?? Directory.Move)(folders.OldLocal, folders.NewLocal);
                local = FolderMove.Moved;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Only the window layout and the update answer live here, so this is not worth stopping for.
                local = FolderMove.Failed;
                localError = ex;
            }
        }

        if (repair)
        {
            try
            {
                RenameDatabase(folders.NewDocuments, ops);
                renamed = true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                string message = $"Billament could not finish setting up your data in {folders.NewDocuments}, so it has closed. " +
                    $"Nothing was lost: your data is in that folder, in the file {DataFolders.OldDatabaseName}. Close any " +
                    "other program that may be using it and start Billament again.";
                return new DataMoveResult(FolderMove.Failed, local.Value, null, message, null, ex);
            }
        }

        if (documents is null)
        {
            try
            {
                // AUDIT: folder move. Renames Documents\PrintInvoicer to Documents\Billament, only when nothing has the new name.
                (ops?.MoveDirectory ?? Directory.Move)(folders.OldDocuments, folders.NewDocuments);
                documents = FolderMove.Moved;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                string message = $"Billament could not move your data from {folders.OldDocuments} to {folders.NewDocuments}, so it " +
                    $"has closed. Nothing was lost: your data is still in {folders.OldDocuments}. Close {DataFolders.OldName} and " +
                    "any other program that may be using that folder, such as a sync app, and start Billament again.";
                return new DataMoveResult(FolderMove.Failed, local.Value, backup, message, null, ex);
            }

            string? backupNow = backup is null ? null : Path.Combine(folders.NewDocuments, "Backups", Path.GetFileName(backup));
            if (FileExists(movedOldDb) && !FileExists(movedNewDb))
            {
                try
                {
                    RenameDatabase(folders.NewDocuments, ops);
                    renamed = true;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    return UndoMove(folders, ops, local.Value, backup, backupNow, ex);
                }
            }
            else if (FileExists(movedOldDb))
            {
                // Both files were in the old folder. Never overwrite one with the other; keep both.
                notice = $"Billament moved your data to {folders.NewDocuments}. That folder already held a file named " +
                    $"{DataFolders.NewDatabaseName}, so Billament uses that file and left {DataFolders.OldDatabaseName} next to " +
                    "it unchanged.";
            }
            backup = backupNow;
        }

        // The moved database now has its new name: point a custom backup or invoice folder at the new place. The data
        // is safe whatever happens here, so a failure is only a notice.
        Exception? error = localError;
        if (renamed)
        {
            try
            {
                RewriteFolderSettings(movedNewDb, folders, comparison);
            }
            catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
            {
                string settingsNotice = $"Billament moved your data to {folders.NewDocuments}, but could not update the backup or " +
                    "invoice folder set in Settings. Check both on the Settings tab.";
                notice = notice is null ? settingsNotice : notice + "\n\n" + settingsNotice;
                error ??= ex;
            }
        }

        return new DataMoveResult(documents.Value, local.Value, backup, null, notice, error);
    }

    /// <summary>
    /// The same place under the new data folder when <paramref name="value"/> is the old data folder or a folder inside
    /// it; otherwise <paramref name="value"/> unchanged. Treats the setting as untrusted text and never touches the disk.
    /// </summary>
    public static string? RemapFolder(string? value, string oldFolder, string newFolder, StringComparison comparison)
    {
        // A blank or relative value would resolve against the current folder and could match by accident.
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value)) return value;

        string path, old, target;
        try
        {
            path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(value));
            old = Path.TrimEndingDirectorySeparator(Path.GetFullPath(oldFolder));
            target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(newFolder));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return value;
        }

        if (string.Equals(path, old, comparison)) return target;

        // The character after the old folder must be a separator, so a sibling such as "<old>Old" never matches. The
        // rest comes from the normalized path, so it cannot climb out with "..".
        if (path.Length > old.Length && path.StartsWith(old, comparison)
            && (path[old.Length] == Path.DirectorySeparatorChar || path[old.Length] == Path.AltDirectorySeparatorChar))
            return target + path[old.Length..];
        return value;
    }

    // Points BackupFolder and InvoiceFolder at the new place when they are inside the old data folder. Raw SQL on
    // those two keys only (the Database class's whole-settings load and save would read and write every setting, and
    // a bad value elsewhere would stop the load), and the file is opened for writing only when a value really changes.
    // Returns true when a value changed.
    private static bool RewriteFolderSettings(string databasePath, DataFolders folders, StringComparison comparison)
    {
        var changes = new List<(string Key, string Value)>();
        using (var read = OpenDatabase(databasePath, readOnly: true))
        {
            using (var exists = read.CreateCommand())
            {
                exists.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'settings'";
                if (Convert.ToInt64(exists.ExecuteScalar(), CultureInfo.InvariantCulture) == 0) return false;
            }

            foreach (string key in new[] { nameof(AppSettings.BackupFolder), nameof(AppSettings.InvoiceFolder) })
            {
                using var cmd = read.CreateCommand();
                cmd.CommandText = "SELECT value FROM settings WHERE key = $k";
                cmd.Parameters.AddWithValue("$k", key);
                // A missing or empty row means "use the default folder", which already points at the new place.
                if (cmd.ExecuteScalar() is not string { Length: > 0 } value) continue;
                string? mapped = RemapFolder(value, folders.OldDocuments, folders.NewDocuments, comparison);
                if (mapped is not null && !string.Equals(mapped, value, StringComparison.Ordinal)) changes.Add((key, mapped));
            }
        }
        if (changes.Count == 0) return false;

        using var write = OpenDatabase(databasePath, readOnly: false);
        using var tx = write.BeginTransaction();
        foreach (var (key, value) in changes)
        {
            using var cmd = write.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE settings SET value = $v WHERE key = $k";
            cmd.Parameters.AddWithValue("$k", key);
            cmd.Parameters.AddWithValue("$v", value);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
        return true;
    }

    // An unpooled connection that never creates a file: read-only, or read-write on a file that already exists.
    private static SqliteConnection OpenDatabase(string path, bool readOnly)
    {
        // AUDIT: file read/write. Opens the moved billament.db to read the two folder settings, and to change them only when one points inside the old data folder; never creates a file.
        var c = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
            Pooling = false,
        }.ToString());
        c.Open();
        return c;
    }

    // The state of a pair when no move is needed, or null when the old folder is there and the new name is free.
    private static FolderMove? Decide(string oldFolder, string newFolder)
    {
        if (!FolderExists(oldFolder)) return FolderMove.NothingToMove;
        return Occupied(newFolder) ? FolderMove.NewFolderExists : null;
    }

    // The database file could not be renamed, so the Documents folder goes back to its old name. If even that fails,
    // the message names the new folder as the place of the data, and the next start finishes the rename there.
    private static DataMoveResult UndoMove(
        DataFolders folders, FileOps? ops, FolderMove local, string? backupBefore, string? backupNow, Exception renameError)
    {
        try
        {
            // AUDIT: folder move. Puts Documents\Billament back to Documents\PrintInvoicer when the database rename failed.
            (ops?.MoveDirectory ?? Directory.Move)(folders.NewDocuments, folders.OldDocuments);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            string stuck = $"Billament moved your data to {folders.NewDocuments} but could not finish setting it up, so it has " +
                $"closed. Nothing was lost: your data is in {folders.NewDocuments}. Start Billament again to finish; if this " +
                "message comes back, close any other program that may be using that folder first.";
            return new DataMoveResult(FolderMove.Failed, local, backupNow, stuck, null, renameError);
        }

        string back = $"Billament could not finish moving your data, so it moved it back and has closed. Nothing was lost: " +
            $"your data is still in {folders.OldDocuments}. Close any other program that may be using it and start " +
            "Billament again.";
        return new DataMoveResult(FolderMove.Failed, local, backupBefore, back, null, renameError);
    }

    // Renames the old database file to the new name inside a folder; the two-argument move never overwrites a file.
    private static void RenameDatabase(string folder, FileOps? ops)
    {
        // AUDIT: file move. Renames printinvoicer.db to billament.db inside Documents\Billament; never over an existing file.
        (ops?.MoveFile ?? File.Move)(Path.Combine(folder, DataFolders.OldDatabaseName), Path.Combine(folder, DataFolders.NewDatabaseName));
    }

    // True when anything at all has this name: a folder, a file, or a link (even one that points nowhere).
    private static bool Occupied(string path)
    {
        // AUDIT: folder check. Whether a folder, file or link already has this name; reads no contents.
        return Path.Exists(path) || new FileInfo(path).LinkTarget is not null;
    }

    // Whether an old data folder exists.
    private static bool FolderExists(string path)
    {
        // AUDIT: folder check. Whether an old data folder exists.
        return Directory.Exists(path);
    }

    // Whether a database file with the old or new name exists.
    private static bool FileExists(string path)
    {
        // AUDIT: file check. Whether a database file with the old or new name exists.
        return File.Exists(path);
    }
}
