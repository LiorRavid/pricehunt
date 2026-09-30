using System.Net.ServerSentEvents;
using System.Text.Json;

namespace PriceHunt.Api.Tests;

/// <summary>Reads a server-sent event stream one event at a time with the BCL parser.</summary>
internal sealed class ServerSentEventReader(Stream stream) : IAsyncDisposable
{
    private static readonly TimeSpan s_guard = TimeSpan.FromSeconds(10);

    private readonly IAsyncEnumerator<SseItem<string>> _events =
        SseParser.Create(stream).EnumerateAsync(TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken);

    /// <summary>Waits for the next event; fails the test if none arrives or the stream ends.</summary>
    public async Task<ReceivedEvent> NextAsync()
    {
        bool moved = await _events.MoveNextAsync().AsTask().WaitAsync(s_guard, TestContext.Current.CancellationToken);
        moved.Should().BeTrue("another event was expected");
        SseItem<string> item = _events.Current;
        return new ReceivedEvent(item.EventType, item.EventId, JsonDocument.Parse(item.Data).RootElement.Clone());
    }

    /// <summary>Waits for the server to close the stream.</summary>
    public async Task<bool> EndedAsync() => !await _events.MoveNextAsync().AsTask().WaitAsync(s_guard, TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _events.DisposeAsync();
        await stream.DisposeAsync();
    }
}

/// <summary>One parsed server-sent event.</summary>
internal sealed record ReceivedEvent(string Type, string? Id, JsonElement Data)
{
    public string GetString(string property) => Data.GetProperty(property).GetString() ?? string.Empty;
}
