using System.Text;

namespace PrintInvoicer.Core.Files;

/// <summary>
/// File names built from things the user typed (an invoice number, a job title), made safe for Windows and
/// length-limited. Never throws for any text, so a strange title can never stop an invoice being saved.
/// </summary>
public static class SafeFileName
{
    /// <summary>The most UTF-16 chars a job title adds to a file name.</summary>
    public const int MaxTitleLength = 60;

    /// <summary>The most UTF-16 chars an invoice number adds to a file name.</summary>
    public const int MaxNumberLength = 30;

    /// <summary>The most UTF-16 chars a whole composed file name may have, extension included.</summary>
    public const int MaxNameLength = 120;

    /// <summary>The name part used when the text has nothing usable in it.</summary>
    public const string Fallback = "untitled";

    // The characters Windows forbids in a name. A fixed list, not the operating system's, so the same title gives the
    // same name in the app and in tests on any system.
    private static readonly char[] Forbidden = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    // Windows device names. Written with escapes so the source stays ASCII: u00B9, u00B2 and u00B3 are the superscript
    // digits one, two and three, which Windows also treats as COM and LPT ports.
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$",
        "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        "COM\u00B9", "COM\u00B2", "COM\u00B3", "LPT\u00B9", "LPT\u00B2", "LPT\u00B3",
    };

    /// <summary>
    /// The text as one safe name part: forbidden and control characters become a dash, runs of dots become one dot,
    /// spaces and dots are trimmed from both ends, a reserved device name gets a leading underscore, and the result is
    /// at most maxLength chars without splitting a character. Empty results become Fallback.
    /// </summary>
    public static string Clean(string? text, int maxLength)
    {
        if (maxLength < Fallback.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "The limit is shorter than the fallback name.");
        }

        string replaced = ReplaceUnsafe(text ?? "");
        string trimmed = Cut(CollapseDots(replaced).Trim(' ', '.'), maxLength);
        if (trimmed.Length == 0)
        {
            return Fallback;
        }

        // The device name is the part before the first dot, without trailing spaces: Windows ignores both.
        int dot = trimmed.IndexOf('.');
        string stem = (dot < 0 ? trimmed : trimmed[..dot]).TrimEnd(' ');
        if (Reserved.Contains(stem))
        {
            trimmed = Cut("_" + trimmed, maxLength);
        }
        return trimmed;
    }

    /// <summary>
    /// An invoice file name: prefix, the cleaned number, the cleaned title, then the suffix, such as
    /// "Invoice 2026-001 Tube caps (detail).pdf". The prefix and suffix are fixed text from code, not user data.
    /// </summary>
    public static string Compose(string prefix, string? number, string? title, string suffix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("The prefix cannot be empty.", nameof(prefix));
        }
        if (prefix.Length + suffix.Length + MaxNumberLength + MaxTitleLength + 2 > MaxNameLength)
        {
            throw new ArgumentException("The prefix and suffix leave too little room for the number and title.");
        }
        return $"{prefix} {Clean(number, MaxNumberLength)} {Clean(title, MaxTitleLength)}{suffix}";
    }

    /// <summary>
    /// True when path names a file directly in folder (not in a subfolder and not outside it); used as a last check
    /// before a PDF is written.
    /// </summary>
    public static bool IsDirectlyInside(string folder, string path)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is null)
        {
            return false;
        }
        return string.Equals(directory, Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)), StringComparison.Ordinal);
    }

    // Forbidden characters, control characters (U+0000 to U+001F and U+007F to U+009F) and unpaired surrogates become
    // a dash. A proper surrogate pair (an emoji, for example) is kept whole.
    private static string ReplaceUnsafe(string text)
    {
        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                sb.Append(c).Append(text[i + 1]);
                i++;
            }
            else if (char.IsSurrogate(c) || char.IsControl(c) || Array.IndexOf(Forbidden, c) >= 0)
            {
                sb.Append('-');
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    // Every run of two or more dots becomes one dot, so ".." can never climb out of a folder.
    private static string CollapseDots(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (c == '.' && sb.Length > 0 && sb[^1] == '.')
            {
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    // Cuts to maxLength chars, never leaving half of a surrogate pair, then trims trailing spaces and dots again.
    private static string Cut(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }
        string cut = text[..maxLength];
        if (char.IsHighSurrogate(cut[^1]))
        {
            cut = cut[..^1];
        }
        return cut.TrimEnd(' ', '.');
    }
}
