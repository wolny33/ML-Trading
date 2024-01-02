using System.Net;
using System.Reflection;
using Alpaca.Markets;
using FluentAssertions;
using NSubstitute;
using Serilog;
using TradingBot.Services;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class AlpacaCallQueueTests : IAsyncDisposable
{
    private readonly AlpacaCallQueue _callQueue;
    private readonly DelaySource _delay = new();

    public AlpacaCallQueueTests()
    {
        var logger = Substitute.For<ILogger>();
        _callQueue = new AlpacaCallQueue(logger, _delay.GetDelay);
    }

    public async ValueTask DisposeAsync()
    {
        await _callQueue.DisposeAsync();
    }

    [Fact]
    public void ShouldNotEnqueueCallIfItImmediatelySucceeds()
    {
        var result = _callQueue.SendRequestWithRetriesAsync(() => Task.FromResult(new Response()));
        result.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldEnqueueCallIfItFailsAndRetryAfterDelay()
    {
        var responseSequence = new ResponseSequence(Task.FromException<Response>(TooManyRequests()),
            Task.FromResult(new Response()));
        var result = _callQueue.SendRequestWithRetriesAsync(responseSequence.GetNext);

        await result.Awaiting(r => r.WaitAsync(TimeSpan.FromMilliseconds(10))).Should().ThrowAsync<TimeoutException>();

        _delay.Advance();

        await result.Awaiting(r => r.WaitAsync(TimeSpan.FromMilliseconds(10))).Should().NotThrowAsync();
    }

    [Fact]
    public async Task ShouldReturnCallToQueueIfRetryFails()
    {
        var responseSequence = new ResponseSequence(Task.FromException<Response>(TooManyRequests()),
            Task.FromException<Response>(TooManyRequests()), Task.FromResult(new Response()));
        var result = _callQueue.SendRequestWithRetriesAsync(responseSequence.GetNext);

        await result.Awaiting(r => r.WaitAsync(TimeSpan.FromMilliseconds(10))).Should().ThrowAsync<TimeoutException>();

        _delay.Advance();

        await result.Awaiting(r => r.WaitAsync(TimeSpan.FromMilliseconds(10))).Should().ThrowAsync<TimeoutException>();

        _delay.Advance();

        await result.Awaiting(r => r.WaitAsync(TimeSpan.FromMilliseconds(10))).Should().NotThrowAsync();
    }

    [Fact]
    public async Task ShouldNotTryToImmediatelyExecuteCallIfPreviousFailed()
    {
        var responseSequence = new ResponseSequence(Task.FromException<Response>(TooManyRequests()),
            Task.FromResult(new Response()));
        var firstResult = _callQueue.SendRequestWithRetriesAsync(responseSequence.GetNext);
        var secondResult = _callQueue.SendRequestWithRetriesAsync(() => Task.FromResult(new Response()));

        await firstResult.Awaiting(r => r.WaitAsync(TimeSpan.FromMilliseconds(10))).Should()
            .ThrowAsync<TimeoutException>();
        await secondResult.Awaiting(r => r.WaitAsync(TimeSpan.FromMilliseconds(10))).Should()
            .ThrowAsync<TimeoutException>();

        _delay.Advance();

        await firstResult.Awaiting(r => r.WaitAsync(TimeSpan.FromMilliseconds(10))).Should().NotThrowAsync();
        await secondResult.Awaiting(r => r.WaitAsync(TimeSpan.FromMilliseconds(10))).Should().NotThrowAsync();
    }

    private static RestClientErrorException TooManyRequests()
    {
        return (RestClientErrorException)Activator.CreateInstance(typeof(RestClientErrorException),
            BindingFlags.NonPublic | BindingFlags.Instance, null,
            new object[] { new HttpResponseMessage(HttpStatusCode.TooManyRequests) }, null)!;
    }

    private sealed record ResponseSequence(params Task<Response>[] Responses)
    {
        private int _next;

        public Task<Response> GetNext()
        {
            return Responses[_next == Responses.Length - 1 ? _next : _next++];
        }
    }

    private sealed class DelaySource
    {
        private readonly List<TaskCompletionSource> _taskSources = new();

        public Task GetDelay(TimeSpan timeSpan)
        {
            var taskSource = new TaskCompletionSource();
            _taskSources.Add(taskSource);
            return taskSource.Task;
        }

        public void Advance()
        {
            var sources = _taskSources.ToList();
            _taskSources.Clear();
            sources.ForEach(source => source.SetResult());
        }
    }

    private sealed record Response;
}
