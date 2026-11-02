using System.Text;
using PrintInvoicer.Core.Updates;

namespace PrintInvoicer.Tests;

/// <summary>
/// GitHub's reply is untrusted: read it only up to the cap, and treat anything longer as no update information.
/// </summary>
public sealed class LatestReleaseReplyTests
{
    private const string Head = "{\"tag_name\":\"v3.7.0\",\"html_url\":\"https://github.com/cevans3505/PrintInvoicer/releases/tag/v3.7.0\",\"body\":\"";
    private const string Tail = "\"}";

    /// <summary>A valid reply padded with a long body text to exactly the given number of bytes.</summary>
    private static byte[] Reply(int bytes)
    {
        int pad = bytes - Head.Length - Tail.Length;
        return Encoding.UTF8.GetBytes(Head + new string('x', pad) + Tail);
    }

    [Fact]
    public async Task Reads_the_tag_and_page_from_a_normal_reply()
    {
        var reply = await LatestReleaseReply.ReadAsync(new MemoryStream(Reply(2000)), CancellationToken.None);
        Assert.NotNull(reply);
        Assert.Equal("v3.7.0", reply!.TagName);
        Assert.Equal("https://github.com/cevans3505/PrintInvoicer/releases/tag/v3.7.0", reply.HtmlUrl);
    }

    [Fact]
    public async Task A_reply_of_exactly_the_cap_is_read()
    {
        var bytes = Reply(LatestReleaseReply.MaxBytes);
        Assert.Equal(LatestReleaseReply.MaxBytes, bytes.Length);
        var reply = await LatestReleaseReply.ReadAsync(new MemoryStream(bytes), CancellationToken.None);
        Assert.Equal("v3.7.0", reply!.TagName);
    }

    [Fact]
    public async Task A_reply_one_byte_over_the_cap_is_no_update_info()
    {
        var reply = await LatestReleaseReply.ReadAsync(new MemoryStream(Reply(LatestReleaseReply.MaxBytes + 1)), CancellationToken.None);
        Assert.Null(reply);
    }
}
