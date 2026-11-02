using System.Windows;
using System.Windows.Controls;
using System.IO;
using Billament.Core.Data;
using Billament.Core.Models;

namespace Billament.App;

/// <summary>
/// The program's starting point: sets up the PDF library, styles and error reporting, opens the database, makes
/// today's backup and shows the main window.
/// </summary>
public partial class App : Application
{
    /// <summary>The open database, shared by every window.</summary>
    public static Database Db { get; internal set; } = null!;

    /// <summary>Why the last automatic backup failed, if it did; shown on the Settings tab.</summary>
    public static string? BackupProblem { get; set; }

    /// <summary>The folder backups go to: the one set in Settings, or Backups next to the database.</summary>
    public static string BackupFolder(Database db, AppSettings s) =>
        string.IsNullOrWhiteSpace(s.BackupFolder) ? DatabaseBackup.DefaultFolder(db.FilePath) : s.BackupFolder;

    /// <summary>Today's backup, made in the background so a slow network folder never delays the window.</summary>
    public static Task BackUpTodayAsync(Database db) => Task.Run(() =>
    {
        string folder = BackupFolder(db, db.LoadSettings());
        try
        {
            // AUDIT: file write. Today's backup of the database, in the backup folder (see DatabaseBackup).
            DatabaseBackup.DailyIfDue(db.FilePath, folder, DateTime.Now);
            BackupProblem = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Microsoft.Data.Sqlite.SqliteException)
        {
            BackupProblem = $"The automatic backup to {folder} failed: {ex.Message}";
        }
    });

    // Runs once when the program starts.
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        EventManager.RegisterClassHandler(typeof(TextBox), FrameworkElement.LoadedEvent, new RoutedEventHandler(HideClearButton));

        // Anything unexpected is reported plainly, with details saved for the developer, instead of a bare message.
        DispatcherUnhandledException += (_, args) =>
        {
            ErrorReport.Show("Billament ran into an unexpected error. Your saved data is not affected; you can keep working.", args.Exception);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex) ErrorReport.Show("Billament ran into an error it cannot recover from and will close.", ex);
        };

        try
        {
            if (!MoveOldData()) return;
            Db = new Database(Database.DefaultPath);
            var backup = BackUpTodayAsync(Db);
            var main = new MainWindow();
            main.Show();
            backup.ContinueWith(_ => main.Dispatcher.BeginInvoke(() => main.ViewModel.Settings.RefreshBackupStatus()));
        }
        catch (Exception ex)
        {
            ErrorReport.Show(
                $"Billament could not start, and nothing was changed. Your data is in {Path.GetDirectoryName(Database.DefaultPath)}, " +
                $"or still in the {DataFolders.OldName} folder next to it if Billament has not moved it yet.", ex);
            Shutdown(1);
        }
    }

    /// <summary>
    /// The one-time move of the data folders from the old name (see <see cref="DataFolderMigration"/>). Returns false
    /// when start-up must stop: the Documents move failed, the user has been told where the data is, and nothing new
    /// was created.
    /// </summary>
    private bool MoveOldData()
    {
        var folders = DataFolders.Under(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        var result = DataFolderMigration.Run(folders, DateTime.Now);

        // The details go to the error file; the message the user reads stays a plain sentence.
        string? saved = result.Error is null
            ? null
            : ErrorReport.Save(result.Problem ?? result.Notice ?? "The one-time move to the Billament folders did not fully finish.", result.Error);

        if (result.DocumentsFailed)
        {
            string text = result.Problem ?? "Billament could not move your data, so it has closed. Nothing was lost.";
            if (saved is not null) text += $"\n\nDetails for the developer were saved in {saved}.";
            MessageBox.Show(text, "Billament", MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown(1);
            return false;
        }

        if (result.Notice is not null)
            MessageBox.Show(result.Notice, "Billament", MessageBoxButton.OK, MessageBoxImage.Information);
        return true;
    }

    /// <summary>
    /// The Fluent TextBox shows a clear-text X while focused, which covers the value in narrow number fields.
    /// A local value beats the template's triggers, so collapsing the button here keeps it hidden.
    /// </summary>
    internal static void HideClearButton(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox box) return;
        box.ApplyTemplate();
        if (box.Template?.FindName("DeleteButton", box) is UIElement button)
            button.Visibility = Visibility.Collapsed;
    }
}
