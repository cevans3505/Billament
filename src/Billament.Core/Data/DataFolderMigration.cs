using Microsoft.Data.Sqlite;

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

/// <summary>Placeholder until the move is written.</summary>
public static class DataFolderMigration
{
    /// <summary>The label of the safety copy of the old database.</summary>
    public const string BackupLabel = "pre-rename";

    /// <summary>Placeholder until the move is written.</summary>
    public static DataMoveResult Run(DataFolders folders, DateTime now, FileOps? ops = null) =>
        new(FolderMove.NothingToMove, FolderMove.NothingToMove, null, null, null, null);
}
