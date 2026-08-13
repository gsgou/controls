using Shiny.Maui.Controls.Images;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The behaviour that makes a list of remote images survive scrolling: one download per URL no
/// matter how many cells ask, a hard cap on how many run at once, and a progress stream that says
/// "queued" rather than pretending to be at 0%.
/// </summary>
public class ImageServiceTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "shinyimage-svc-" + Guid.NewGuid().ToString("n"));

    public void Dispose()
    {
        if (Directory.Exists(this.root))
            Directory.Delete(this.root, true);
    }

    ImageOptions Options(Action<ImageOptions>? configure = null)
    {
        var options = new ImageOptions { CacheDirectory = this.root };
        configure?.Invoke(options);
        return options;
    }


    [Fact]
    public async Task Get_DownloadsOnce_ThenServesFromCache()
    {
        var downloader = new FakeDownloader();
        var service = new ImageService(this.Options(), downloader);

        var first = await service.GetAsync(new ImageRequest("https://example.com/a.png"));
        first.Success.ShouldBeTrue();
        first.Origin.ShouldBe(ImageOrigin.Network);

        var second = await service.GetAsync(new ImageRequest("https://example.com/a.png"));
        second.Origin.ShouldBe(ImageOrigin.Memory);

        downloader.Calls.ShouldBe(1);
    }


    [Fact]
    public async Task Get_ReadsFromDisk_WhenMemoryIsDisabled()
    {
        var downloader = new FakeDownloader();
        var service = new ImageService(this.Options(o => o.MemoryCacheEnabled = false), downloader);

        await service.GetAsync(new ImageRequest("https://example.com/a.png"));
        var second = await service.GetAsync(new ImageRequest("https://example.com/a.png"));

        second.Origin.ShouldBe(ImageOrigin.Disk);
        second.FilePath.ShouldNotBeNull();
        downloader.Calls.ShouldBe(1);
    }


    [Fact]
    public async Task Get_BypassesCache_OnReload()
    {
        var downloader = new FakeDownloader();
        var service = new ImageService(this.Options(), downloader);

        await service.GetAsync(new ImageRequest("https://example.com/a.png"));
        await service.GetAsync(new ImageRequest("https://example.com/a.png") { BypassCache = true });

        downloader.Calls.ShouldBe(2);
    }


    [Fact]
    public async Task ConcurrentRequestsForOneUri_ShareASingleDownload()
    {
        // The scenario this exists for: the same avatar bound into a dozen visible cells. Without
        // de-duplication each one takes a download slot for a picture already being fetched, and the
        // rest of the list stalls behind duplicates.
        var downloader = new FakeDownloader { Gate = new TaskCompletionSource() };
        var service = new ImageService(this.Options(), downloader);

        var requests = Enumerable
            .Range(0, 12)
            .Select(_ => service.GetAsync(new ImageRequest("https://example.com/avatar.png")))
            .ToArray();

        await downloader.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        downloader.Gate.SetResult();

        var results = await Task.WhenAll(requests);

        results.ShouldAllBe(r => r.Success);
        downloader.Calls.ShouldBe(1);
    }


    [Fact]
    public async Task Downloads_AreCappedAtTheConfiguredConcurrency()
    {
        var downloader = new FakeDownloader { Gate = new TaskCompletionSource(), HoldEach = true };
        var service = new ImageService(this.Options(o => o.MaxConcurrentDownloads = 2), downloader);

        var requests = Enumerable
            .Range(0, 8)
            .Select(i => service.GetAsync(new ImageRequest($"https://example.com/{i}.png")))
            .ToArray();

        // Give the queue a moment to admit everything it is willing to admit at once.
        await Task.Delay(250);
        downloader.MaxConcurrent.ShouldBeLessThanOrEqualTo(2);

        downloader.Gate.SetResult();
        await Task.WhenAll(requests);

        downloader.MaxConcurrent.ShouldBeLessThanOrEqualTo(2);
        downloader.Calls.ShouldBe(8);
    }


    [Fact]
    public async Task Progress_ReportsQueuedBeforeDownloading()
    {
        var downloader = new FakeDownloader();
        var service = new ImageService(this.Options(), downloader);
        var progress = new RecordingProgress();

        await service.GetAsync(new ImageRequest("https://example.com/a.png"), progress);

        var states = progress.States;
        states.ShouldContain(ImageLoadState.Queued);
        states.ShouldContain(ImageLoadState.Downloading);
        states[^1].ShouldBe(ImageLoadState.Loaded);
        states.IndexOf(ImageLoadState.Queued).ShouldBeLessThan(states.IndexOf(ImageLoadState.Downloading));
    }


    [Fact]
    public async Task Progress_IsIndeterminate_WhenTheServerSendsNoLength()
    {
        // No Content-Length is the single fact that decides between a ring that fills and one that
        // spins, so it gets its own test rather than being inferred from a percentage of zero.
        var downloader = new FakeDownloader { ReportContentLength = false };
        var service = new ImageService(this.Options(), downloader);
        var progress = new RecordingProgress();

        await service.GetAsync(new ImageRequest("https://example.com/chunked.png"), progress);

        progress.Snapshots
            .Where(p => p.State == ImageLoadState.Downloading)
            .ShouldAllBe(p => p.Percent == null && p.IsIndeterminate);
    }


    [Fact]
    public async Task Progress_IsDeterminate_WhenTheServerSendsALength()
    {
        var downloader = new FakeDownloader { PayloadSize = 300_000 };
        var service = new ImageService(this.Options(), downloader);
        var progress = new RecordingProgress();

        await service.GetAsync(new ImageRequest("https://example.com/big.png"), progress);

        var downloading = progress.Snapshots.Where(p => p.State == ImageLoadState.Downloading).ToList();
        downloading.ShouldNotBeEmpty();
        downloading.ShouldAllBe(p => p.TotalBytes == 300_000);
        downloading.ShouldAllBe(p => p.Percent != null);
    }


    [Fact]
    public async Task Get_ReturnsFailure_RatherThanThrowing()
    {
        // A broken image URL in a list is an ordinary event that should render error artwork, not
        // unwind the caller.
        var service = new ImageService(this.Options(), new ThrowingDownloader());

        var result = await service.GetAsync(new ImageRequest("https://example.com/gone.png"));

        result.Success.ShouldBeFalse();
        result.Error.ShouldBeOfType<HttpRequestException>();
    }


    [Fact]
    public async Task ClearCache_EmptiesBothTiers()
    {
        var downloader = new FakeDownloader();
        var service = new ImageService(this.Options(), downloader);

        await service.GetAsync(new ImageRequest("https://example.com/a.png"));
        (await service.GetCacheSizeAsync()).ShouldBeGreaterThan(0);

        await service.ClearCacheAsync();

        (await service.GetCacheSizeAsync()).ShouldBe(0);
        service.MemoryCache.Count.ShouldBe(0);

        await service.GetAsync(new ImageRequest("https://example.com/a.png"));
        downloader.Calls.ShouldBe(2);
    }


    [Fact]
    public async Task ClearCache_ForOneUri_LeavesTheRest()
    {
        var downloader = new FakeDownloader();
        var service = new ImageService(this.Options(), downloader);

        await service.GetAsync(new ImageRequest("https://example.com/a.png"));
        await service.GetAsync(new ImageRequest("https://example.com/b.png"));

        await service.ClearCacheAsync("https://example.com/a.png");

        (await service.GetAsync(new ImageRequest("https://example.com/b.png"))).Origin.ShouldBe(ImageOrigin.Memory);
        (await service.GetAsync(new ImageRequest("https://example.com/a.png"))).Origin.ShouldBe(ImageOrigin.Network);
    }


    [Fact]
    public async Task CacheDisabled_KeepsBytesInHandButWritesNothing()
    {
        var downloader = new FakeDownloader();
        var service = new ImageService(this.Options(), downloader);

        var result = await service.GetAsync(new ImageRequest("https://example.com/private.png") { CacheEnabled = false });

        result.Success.ShouldBeTrue();
        result.Bytes.ShouldNotBeNull();
        result.FilePath.ShouldBeNull();
        (await service.GetCacheSizeAsync()).ShouldBe(0);
    }


    sealed class RecordingProgress : IProgress<ImageLoadProgress>
    {
        readonly Lock gate = new();
        readonly List<ImageLoadProgress> snapshots = [];

        public IReadOnlyList<ImageLoadProgress> Snapshots
        {
            get { lock (this.gate) return [.. this.snapshots]; }
        }

        public List<ImageLoadState> States
        {
            get { lock (this.gate) return [.. this.snapshots.Select(s => s.State)]; }
        }

        public void Report(ImageLoadProgress value)
        {
            lock (this.gate)
                this.snapshots.Add(value);
        }
    }


    sealed class FakeDownloader : IImageDownloader
    {
        int calls;
        int concurrent;
        int maxConcurrent;

        public TaskCompletionSource? Gate { get; init; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool HoldEach { get; init; }
        public bool ReportContentLength { get; init; } = true;
        public int PayloadSize { get; init; } = 4096;

        public int Calls => Volatile.Read(ref this.calls);
        public int MaxConcurrent => Volatile.Read(ref this.maxConcurrent);

        public async Task<ImageDownloadResult> DownloadAsync(ImageRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref this.calls);

            var now = Interlocked.Increment(ref this.concurrent);
            int observed;
            do
            {
                observed = Volatile.Read(ref this.maxConcurrent);
            }
            while (now > observed && Interlocked.CompareExchange(ref this.maxConcurrent, now, observed) != observed);

            this.Started.TrySetResult();

            try
            {
                if (this.Gate is not null && (this.HoldEach || this.Calls == 1))
                    await this.Gate.Task.ConfigureAwait(false);

                var bytes = new byte[this.PayloadSize];
                Random.Shared.NextBytes(bytes);

                return new ImageDownloadResult(
                    new MemoryStream(bytes),
                    this.ReportContentLength ? bytes.Length : null,
                    "image/png"
                );
            }
            finally
            {
                Interlocked.Decrement(ref this.concurrent);
            }
        }
    }


    sealed class ThrowingDownloader : IImageDownloader
    {
        public Task<ImageDownloadResult> DownloadAsync(ImageRequest request, CancellationToken cancellationToken)
            => Task.FromException<ImageDownloadResult>(new HttpRequestException("404"));
    }
}
