using System.Text;
using System.Text.Json;
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

    private static MemoryStream Text(string s) => new(Encoding.UTF8.GetBytes(s));

    /// <summary>A reply that never ends, counting how much was read.</summary>
    private sealed class EndlessStream : Stream
    {
        public long BytesRead { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Array.Fill(buffer, (byte)'a', offset, count);
            BytesRead += count;
            return count;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            buffer.Span.Fill((byte)'a');
            BytesRead += buffer.Length;
            return new ValueTask<int>(buffer.Length);
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
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

    [Fact]
    public async Task An_endless_reply_stops_soon_after_the_cap()
    {
        var endless = new EndlessStream();
        var reply = await LatestReleaseReply.ReadAsync(endless, CancellationToken.None);
        Assert.Null(reply);
        Assert.True(endless.BytesRead < 2L * LatestReleaseReply.MaxBytes, $"read {endless.BytesRead} bytes");
    }

    [Fact]
    public async Task A_cancelled_read_stops_with_operation_canceled()
    {
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => LatestReleaseReply.ReadAsync(new MemoryStream(Reply(2000)), new CancellationToken(true)));
    }

    [Fact]
    public async Task A_damaged_reply_throws_json_exception()
    {
        await Assert.ThrowsAnyAsync<JsonException>(
            () => LatestReleaseReply.ReadAsync(Text("{\"tag_name\":"), CancellationToken.None));
    }

    [Fact]
    public async Task A_reply_without_the_fields_gives_empty_values()
    {
        var reply = await LatestReleaseReply.ReadAsync(Text("{}"), CancellationToken.None);
        Assert.NotNull(reply);
        Assert.Null(reply!.TagName);
        Assert.Null(reply.HtmlUrl);
    }

    [Fact]
    public async Task A_null_reply_is_no_update_info()
    {
        Assert.Null(await LatestReleaseReply.ReadAsync(Text("null"), CancellationToken.None));
    }
}
