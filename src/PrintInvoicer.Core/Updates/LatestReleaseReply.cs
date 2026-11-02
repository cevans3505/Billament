using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrintInvoicer.Core.Updates;

/// <summary>
/// The two fields the update check reads from GitHub's latest-release reply, and a reader that never takes more than
/// a set number of bytes from it. GitHub's reply is untrusted input.
/// </summary>
public sealed record LatestReleaseReply
{
    /// <summary>The release's tag, for example v3.7.0.</summary>
    [JsonPropertyName("tag_name")]
    public string? TagName { get; init; }

    /// <summary>The release's page on GitHub.</summary>
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; init; }

    /// <summary>The largest reply read (256 KB). A normal reply is a few KB; a longer one is treated as no update information.</summary>
    public const int MaxBytes = 256 * 1024;

    /// <summary>
    /// Reads the reply and returns its two fields; null when the reply is longer than maxBytes (nothing past the cap is
    /// kept) or is the JSON value null. A damaged reply throws JsonException; a cancelled read throws
    /// OperationCanceledException.
    /// </summary>
    public static async Task<LatestReleaseReply?> ReadAsync(Stream body, CancellationToken ct, int maxBytes = MaxBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBytes, 1);
        var buffer = new MemoryStream();
        var chunk = new byte[8192];
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            int n = await body.ReadAsync(chunk, ct);
            if (n == 0) break;
            // Over the cap: stop reading and report no update information.
            if (buffer.Length + n > maxBytes) return null;
            buffer.Write(chunk, 0, n);
        }
        buffer.Position = 0;
        return JsonSerializer.Deserialize<LatestReleaseReply>(buffer);
    }
}
