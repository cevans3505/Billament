using Billament.Core.Files;

namespace Billament.Tests;

/// <summary>
/// Invoice file names come from what the user typed; they must always be safe, short enough, and the same as before
/// for normal names.
/// </summary>
public sealed class SafeFileNameTests
{
    // Reserved device names as this test knows them, kept apart from SafeFileName's own list.
    private static readonly string[] TestReserved =
    [
        "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$",
        "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        "COM\u00B9", "COM\u00B2", "COM\u00B3", "LPT\u00B9", "LPT\u00B2", "LPT\u00B3",
    ];

    /// <summary>Checks a name against Windows' rules independently of SafeFileName's own code.</summary>
    private static void AssertSafe(string name, int maxLength)
    {
        Assert.False(string.IsNullOrEmpty(name));
        Assert.True(name.Length <= maxLength, $"'{name}' is {name.Length} chars, limit {maxLength}");
        foreach (char c in "<>:\"/\\|?*")
        {
            Assert.DoesNotContain(c, name);
        }
        Assert.DoesNotContain(name, char.IsControl);
        for (int i = 0; i < name.Length; i++)
        {
            if (char.IsHighSurrogate(name[i]))
            {
                Assert.True(i + 1 < name.Length && char.IsLowSurrogate(name[i + 1]), "unpaired high surrogate");
                i++;
            }
            else
            {
                Assert.False(char.IsLowSurrogate(name[i]), "unpaired low surrogate");
            }
        }
        Assert.False(name.StartsWith(' ') || name.StartsWith('.'), $"'{name}' starts with a space or dot");
        Assert.False(name.EndsWith(' ') || name.EndsWith('.'), $"'{name}' ends with a space or dot");
        Assert.NotEqual(".", name);
        Assert.NotEqual("..", name);
        int dot = name.IndexOf('.');
        string stem = (dot < 0 ? name : name[..dot]).Trim();
        Assert.DoesNotContain(TestReserved, r => string.Equals(r, stem, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The text the app made from a title before this phase, in the Windows build: Windows' forbidden characters and
    /// U+0000 to U+001F became a dash, then whitespace was trimmed from both ends. Written out by hand so the test
    /// gives the Windows answer on Linux too.
    /// </summary>
    private static string OldWindowsName(string title)
    {
        const string forbidden = "\"<>|:*?\\/";
        var chars = title.Select(c => forbidden.Contains(c) || c < ' ' ? '-' : c).ToArray();
        return new string(chars).Trim();
    }

    [Theory]
    [InlineData("..")]
    [InlineData(@"..\..\evil")]
    [InlineData(@"C:\x")]
    [InlineData("report:2026")]
    [InlineData("CON")]
    [InlineData("con")]
    [InlineData("CON.txt")]
    public void Requirement_cases_come_out_safe(string input)
    {
        AssertSafe(SafeFileName.Clean(input, 60), 60);
    }

    [Theory]
    [InlineData("report:2026", "report-2026")]
    [InlineData(@"C:\x", "C--x")]
    [InlineData("a/b", "a-b")]
    [InlineData(@"..\..\evil", "-.-evil")]
    [InlineData("con", "_con")]
    [InlineData("CON.txt", "_CON.txt")]
    [InlineData("Tube caps.", "Tube caps")]
    public void Known_names_are_cleaned_as_expected(string input, string expected)
    {
        Assert.Equal(expected, SafeFileName.Clean(input, 60));
    }

    [Theory]
    [InlineData("Invoice", "2026-001", "Wire Brush Inserts", ".pdf", "Invoice 2026-001 Wire Brush Inserts.pdf")]
    [InlineData("Invoice", "2026-001", "Wire Brush Inserts", " (detail).pdf", "Invoice 2026-001 Wire Brush Inserts (detail).pdf")]
    [InlineData("Preview", "draft 2026-10-31", "Tube caps", ".pdf", "Preview draft 2026-10-31 Tube caps.pdf")]
    public void Normal_invoice_names_are_unchanged(string prefix, string number, string title, string suffix, string expected)
    {
        Assert.Equal(expected, SafeFileName.Compose(prefix, number, title, suffix));
    }

    [Theory]
    [InlineData(@"x\..\..\y", @"..\evil:CON")]
    [InlineData("../../etc/y", "..")]
    public void Hostile_invoice_number_and_title_stay_safe(string number, string title)
    {
        string result = SafeFileName.Compose("Invoice", number, title, ".pdf");

        Assert.DoesNotContain('/', result);
        Assert.DoesNotContain('\\', result);
        Assert.EndsWith(".pdf", result);
        Assert.Equal(result, Path.GetFileName(result));
    }

    [Theory]
    [InlineData("Tube caps")]
    [InlineData("  Tube caps  ")]
    [InlineData("v1.2 bracket")]
    [InlineData("Aux cable clips")]
    [InlineData("Tube caps?")]
    [InlineData("Fin\tclips")]
    public void Titles_inside_the_boundary_keep_their_old_name(string title)
    {
        Assert.Equal(OldWindowsName(title), SafeFileName.Clean(title, SafeFileName.MaxTitleLength));
    }

    [Theory]
    [InlineData("Tube caps.", "Tube caps")]
    [InlineData("", "untitled")]
    [InlineData("CON", "_CON")]
    [InlineData("v1..2", "v1.2")]
    [InlineData("\u00A0Tube caps", "\u00A0Tube caps")] // a leading no-break space is kept; the old code trimmed it
    [InlineData("Fin\u0085clips", "Fin-clips")]
    public void Titles_outside_the_boundary_get_a_new_name(string title, string expected)
    {
        string result = SafeFileName.Clean(title, SafeFileName.MaxTitleLength);

        Assert.NotEqual(OldWindowsName(title), result);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void A_title_over_60_chars_gets_a_new_name()
    {
        string title = new('a', 61);

        Assert.Equal(new string('a', 60), SafeFileName.Clean(title, SafeFileName.MaxTitleLength));
        Assert.Equal(title, OldWindowsName(title));
    }

    [Fact]
    public void Only_files_directly_in_the_folder_count_as_inside()
    {
        string folder = Path.Combine(Path.GetTempPath(), "pi-names");

        Assert.True(SafeFileName.IsDirectlyInside(folder, Path.Combine(folder, "a.pdf")));
        Assert.True(SafeFileName.IsDirectlyInside(folder + Path.DirectorySeparatorChar, Path.Combine(folder, "a.pdf")));
        Assert.False(SafeFileName.IsDirectlyInside(folder, Path.Combine(folder, "..", "a.pdf")));
        Assert.False(SafeFileName.IsDirectlyInside(folder, Path.Combine(folder, "sub", "a.pdf")));
    }

    // A folder typed with a trailing space or dot: Windows creates and uses it without them, so the check must agree.
    [Theory]
    [InlineData(" ")]
    [InlineData(".")]
    [InlineData(". ")]
    [InlineData("  ..")]
    public void A_folder_with_trailing_spaces_or_dots_still_counts_as_inside(string ending)
    {
        string folder = Path.Combine(Path.GetTempPath(), "pi-names");
        string typed = folder + ending;
        string normalized = SafeFileName.NormalizeFolder(typed);

        Assert.Equal(folder, normalized);
        Assert.True(SafeFileName.IsDirectlyInside(typed, Path.Combine(normalized, "a.pdf")));
        Assert.True(SafeFileName.IsDirectlyInside(typed, Path.Combine(typed, "a.pdf")));
        Assert.True(SafeFileName.IsDirectlyInside(folder, Path.Combine(typed, "a.pdf")));
        Assert.False(SafeFileName.IsDirectlyInside(typed, Path.Combine(normalized, "sub", "a.pdf")));
        Assert.False(SafeFileName.IsDirectlyInside(typed, Path.Combine(normalized, "..", "a.pdf")));
    }

    [Fact]
    public void Every_folder_segment_loses_its_trailing_spaces_and_dots()
    {
        char sep = Path.DirectorySeparatorChar;
        string typed = string.Join(sep, "root", "Sample Printing Co. ", "Invoices.", "2026 ");
        string expected = string.Join(sep, "root", "Sample Printing Co", "Invoices", "2026");

        Assert.Equal(expected, SafeFileName.NormalizeFolder(typed));
        Assert.Equal(expected + sep, SafeFileName.NormalizeFolder(typed + sep));
    }

    [Fact]
    public void Folder_segments_that_are_relative_or_would_be_empty_are_kept()
    {
        char sep = Path.DirectorySeparatorChar;
        string typed = string.Join(sep, "root", ".", "..", "...", " ", "a");

        Assert.Equal(typed, SafeFileName.NormalizeFolder(typed));
    }

    [Fact]
    public void A_normal_folder_is_unchanged()
    {
        string folder = Path.Combine(Path.GetTempPath(), "Sample Printing Co", "Invoices");

        Assert.Equal(folder, SafeFileName.NormalizeFolder(folder));
    }

    // A long prefix in Settings: the sequence digits are at the end of the number and must survive the cut.
    [Theory]
    [InlineData(28)]
    [InlineData(29)]
    [InlineData(30)]
    [InlineData(35)]
    [InlineData(40)]
    public void Long_invoice_numbers_keep_their_sequence_digits(int prefixLength)
    {
        string prefix = "Harbor Dive Shop Invoice No 2026-0000".PadRight(prefixLength, 'x')[..prefixLength];

        string first = SafeFileName.Compose("Invoice", prefix + "001", "Tube caps", ".pdf");
        string second = SafeFileName.Compose("Invoice", prefix + "002", "Tube caps", ".pdf");

        Assert.NotEqual(first, second);
        Assert.Contains("001 Tube caps", first);
        Assert.Contains("002 Tube caps", second);
        Assert.True(first.Length <= SafeFileName.MaxNameLength);
        AssertSafe(first, SafeFileName.MaxNameLength);
    }

    [Theory]
    [InlineData("2026-001")]
    [InlineData("draft 2026-10-31")]
    [InlineData("Sample Printing Co. 2026-001")]
    [InlineData("  Harbor-2026-001.  ")]
    [InlineData("CON")]
    [InlineData("123456789012345678901234567890")]
    [InlineData("12345678901234567890123456789 ")]
    public void Numbers_that_fit_give_the_same_name_as_before(string number)
    {
        string before = $"Invoice {SafeFileName.Clean(number, SafeFileName.MaxNumberLength)} Tube caps.pdf";

        Assert.Equal(before, SafeFileName.Compose("Invoice", number, "Tube caps", ".pdf"));
    }

    [Fact]
    public void A_long_number_cut_from_the_front_keeps_an_emoji_whole()
    {
        string number = new string('a', 10) + "\U0001F600" + new string('b', 29);

        string part = SafeFileName.Compose("Invoice", number, "t", ".pdf")["Invoice ".Length..^" t.pdf".Length];

        Assert.Equal(new string('b', 29), part);
    }

    [Fact]
    public void A_long_number_cut_from_the_front_drops_leading_spaces_and_dots()
    {
        string number = new string('a', 10) + ". " + new string('b', 28);

        string part = SafeFileName.Compose("Invoice", number, "t", ".pdf")["Invoice ".Length..^" t.pdf".Length];

        Assert.Equal(new string('b', 28), part);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("...")]
    [InlineData(". .")]
    [InlineData("..")]
    public void Empty_text_becomes_untitled(string? text)
    {
        Assert.Equal("untitled", SafeFileName.Clean(text, 60));
    }

    [Theory]
    [InlineData("Console", "Console")]
    [InlineData("COM10", "COM10")]
    [InlineData("v1..2", "v1.2")]
    [InlineData("Com1", "_Com1")]
    [InlineData("lpt9 ", "_lpt9")]
    [InlineData("NUL", "_NUL")]
    [InlineData("aux.pdf", "_aux.pdf")]
    [InlineData("CONIN$", "_CONIN$")]
    [InlineData("CONOUT$", "_CONOUT$")]
    [InlineData("PRN", "_PRN")]
    [InlineData("COM\u00B9", "_COM\u00B9")]
    public void Reserved_names_get_an_underscore_and_others_do_not(string text, string expected)
    {
        Assert.Equal(expected, SafeFileName.Clean(text, 60));
    }

    [Fact]
    public void A_reserved_name_at_the_limit_stays_within_it()
    {
        string result = SafeFileName.Clean("CON." + new string('a', 56), 60);

        Assert.StartsWith("_CON.", result);
        AssertSafe(result, 60);
    }

    [Theory]
    [InlineData("Fin\u0007clips", "Fin-clips")]
    public void Control_characters_become_dashes(string text, string expected)
    {
        Assert.Equal(expected, SafeFileName.Clean(text, 60));
    }

    // Lone surrogates cannot be written in an attribute argument (the compiler stores them as U+FFFD), so they are
    // built from char values here.
    [Fact]
    public void Broken_surrogates_become_dashes()
    {
        char high = (char)0xD800;
        char low = (char)0xDC00;

        Assert.Equal("abc-def", SafeFileName.Clean("abc" + high + "def", 60));
        Assert.Equal("abc-def", SafeFileName.Clean("abc" + low + "def", 60));
        Assert.Equal("abc-", SafeFileName.Clean("abc" + high, 60));
        Assert.Equal("a" + high + low + "b", SafeFileName.Clean("a" + high + low + "b", 60));
        Assert.Equal("-" + high + low + "-", SafeFileName.Clean("" + high + high + low + low, 60));
    }

    [Fact]
    public void Broken_surrogates_come_out_safe()
    {
        char high = (char)0xD800;
        char low = (char)0xDC00;

        AssertSafe(SafeFileName.Clean("abc" + high + low + high, 60), 60);
        AssertSafe(SafeFileName.Clean("" + low + high, 60), 60);
        AssertSafe(SafeFileName.Clean(new string('z', 59) + high, 60), 60);
    }

    [Fact]
    public void Long_text_is_cut_to_the_limit()
    {
        Assert.Equal(60, SafeFileName.Clean(new string('x', 1000), 60).Length);
    }

    [Fact]
    public void An_emoji_at_the_cut_point_is_dropped_whole()
    {
        string text = new string('a', 59) + "\U0001F600";

        Assert.Equal(new string('a', 59), SafeFileName.Clean(text, 60));
    }

    [Fact]
    public void An_emoji_that_fits_is_kept_whole()
    {
        string text = new string('a', 58) + "\U0001F600";

        Assert.Equal(text, SafeFileName.Clean(text, 60));
    }

    [Fact]
    public void Long_number_and_title_keep_the_name_within_the_limit()
    {
        string name = SafeFileName.Compose("Invoice", new string('9', 1000), new string('t', 1000), " (detail).pdf");

        Assert.True(name.Length <= SafeFileName.MaxNameLength);
        Assert.EndsWith(" (detail).pdf", name);
    }

    [Fact]
    public void Programmer_mistakes_are_reported()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SafeFileName.Clean("x", 3));
        Assert.Throws<ArgumentException>(() => SafeFileName.Compose(" ", "1", "t", ".pdf"));
        Assert.Throws<ArgumentException>(() => SafeFileName.Compose("Invoice", "1", "t", new string('s', 100)));
    }

    [Theory]
    [InlineData("..")]
    [InlineData(@"..\..\evil")]
    [InlineData("../../etc/passwd")]
    [InlineData(@"C:\Windows\x")]
    [InlineData("a|b?c*d<e>f\"g")]
    [InlineData("CON")]
    [InlineData("Com1.txt")]
    [InlineData("NUL ")]
    [InlineData("  . .  ")]
    [InlineData("Fin\u0007\u0000clips")]
    [InlineData("\U0001F600\U0001F600\U0001F600")]
    public void Hostile_text_comes_out_safe(string text)
    {
        AssertSafe(SafeFileName.Clean(text, 60), 60);
        AssertSafe(SafeFileName.Clean(text, 30), 30);
        AssertSafe(SafeFileName.Clean(new string('z', 100) + text, 60), 60);
    }
}
