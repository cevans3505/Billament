using System.Diagnostics;

namespace PrintInvoicer.Core.Files;

/// <summary>
/// How PrintInvoicer asks Explorer to show a folder or a file. Each path goes in as its own argument (ArgumentList),
/// never pasted into a command line, so a quote or space in a path cannot change what Explorer is told. These only
/// build the request; the App starts it.
/// </summary>
public static class ShellCommands
{
    /// <summary>The request that opens Explorer at a folder.</summary>
    public static ProcessStartInfo ShowFolder(string path)
    {
        var info = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
        info.ArgumentList.Add(path);
        return info;
    }

    /// <summary>The request that opens Explorer at the folder holding a file, with the file highlighted.</summary>
    public static ProcessStartInfo ShowFile(string path)
    {
        var info = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
        info.ArgumentList.Add("/select,");
        info.ArgumentList.Add(path);
        return info;
    }
}
