using Billament.Core.Files;

namespace Billament.Tests;

/// <summary>Explorer must get each path as one argument, whatever characters it holds.</summary>
public sealed class ShellCommandsTests
{
    [Theory]
    [InlineData(@"C:\Users\Sample\Documents\Billament\Backups")]
    [InlineData(@"C:\Users\Sample\Harbor Dive Shop ""draft""\")]
    [InlineData(@"D:\Invoices\Sample Printing Co\")]
    [InlineData(@"/select,C:\x")]
    public void Folder_path_is_passed_as_one_argument(string path)
    {
        var info = ShellCommands.ShowFolder(path);

        Assert.Equal("explorer.exe", info.FileName);
        Assert.True(info.UseShellExecute);
        Assert.Equal("", info.Arguments);
        Assert.Equal(new[] { path }, info.ArgumentList);
    }

    [Theory]
    [InlineData(@"C:\Users\Sample\Documents\Billament\Backups")]
    [InlineData(@"C:\Users\Sample\Harbor Dive Shop ""draft""\")]
    [InlineData(@"D:\Invoices\Sample Printing Co\")]
    [InlineData(@"/select,C:\x")]
    public void File_to_select_is_passed_after_the_select_switch(string path)
    {
        var info = ShellCommands.ShowFile(path);

        Assert.Equal("explorer.exe", info.FileName);
        Assert.True(info.UseShellExecute);
        Assert.Equal("", info.Arguments);
        Assert.Equal(new[] { "/select,", path }, info.ArgumentList);
    }
}
