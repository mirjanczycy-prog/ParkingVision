using OpenCvSharp;

namespace ParkingVision.Vision;

public interface IFrameSource : IDisposable
{
    /// <summary>Latest decoded frame (caller owns/disposes the clone) or null when none yet.</summary>
    Mat? GetLatest();
    DateTime? LastFrameUtc { get; }
}

/// <summary>
/// Reads an RTSP URL or a video file on a background thread and keeps ONLY the newest frame.
/// This avoids OpenCV's internal buffering which otherwise hands you stale frames when you sample once every few seconds.
/// Files loop forever (handy for demos). Reconnects automatically.
/// </summary>
public sealed class CaptureFrameSource : IFrameSource
{
    private readonly string _url;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _lock = new();
    private Mat? _latest;
    private readonly Thread _thread;

    public DateTime? LastFrameUtc { get; private set; }

    public CaptureFrameSource(string url)
    {
        _url = url;
        _thread = new Thread(Loop) { IsBackground = true, Name = $"capture-{url.GetHashCode():x}" };
        _thread.Start();
    }

    private void Loop()
    {
        bool isFile = !_url.Contains("://");
        while (!_cts.IsCancellationRequested)
        {
            using var cap = new VideoCapture(_url);
            if (!cap.IsOpened()) { _cts.Token.WaitHandle.WaitOne(5000); continue; }

            double fps = cap.Fps > 1 ? cap.Fps : 25;
            while (!_cts.IsCancellationRequested)
            {
                using var f = new Mat();
                if (!cap.Read(f) || f.Empty())
                {
                    if (isFile) { cap.PosFrames = 0; continue; }
                    break; // stream dropped -> reconnect
                }
                lock (_lock) { _latest?.Dispose(); _latest = f.Clone(); LastFrameUtc = DateTime.UtcNow; }
                if (isFile) Thread.Sleep((int)(1000 / fps));
            }
            _cts.Token.WaitHandle.WaitOne(2000);
        }
    }

    public Mat? GetLatest()
    {
        lock (_lock) return _latest?.Clone();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _thread.Join(3000);
        lock (_lock) { _latest?.Dispose(); _latest = null; }
        _cts.Dispose();
    }
}
