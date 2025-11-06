using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neat;

public class QBasicScheduler
{
    private readonly IOEmulator _io;

    public QBasicScheduler(IOEmulator io)
    {
        _io = io ?? throw new ArgumentNullException(nameof(io));
    }

    public Task<KeyEvent> WaitForKeyAsync(CancellationToken cancellationToken = default)
    {
        // On browser/WASM, avoid Task.Run and blocking waits that rely on monitors.
        if (OperatingSystem.IsBrowser())
        {
            // This will block the current thread until a key arrives (IOEmulator.WaitForKey is WASM-safe)
            // but avoids unsupported monitor waits used by Task synchronously.
            var ev = _io.WaitForKey(cancellationToken);
            return Task.FromResult(ev);
        }
        // On native/desktop, use a background thread to keep UI responsive.
        return Task.Run(() => _io.WaitForKey(cancellationToken), cancellationToken);
    }

    public Task SleepAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        if (duration <= TimeSpan.Zero) return Task.CompletedTask;
        if (OperatingSystem.IsBrowser())
        {
            // Emulate async sleep without monitors by polling time; return a completed task
            var end = Environment.TickCount64 + (long)duration.TotalMilliseconds;
            while (!cancellationToken.IsCancellationRequested && Environment.TickCount64 < end)
            {
                try { System.Threading.Thread.Sleep(5); } catch { }
            }
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);
            return Task.CompletedTask;
        }
        return Task.Delay(duration, cancellationToken);
    }

    // Synchronous wrappers (for convenience or legacy style)
    public KeyEvent WaitForKey(CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsBrowser())
        {
            // Direct, WASM-safe key wait
            return _io.WaitForKey(cancellationToken);
        }
        return WaitForKeyAsync(cancellationToken).GetAwaiter().GetResult();
    }

    public void Sleep(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsBrowser())
        {
            if (duration <= TimeSpan.Zero) return;
            var end = Environment.TickCount64 + (long)duration.TotalMilliseconds;
            while (!cancellationToken.IsCancellationRequested && Environment.TickCount64 < end)
            {
                try { System.Threading.Thread.Sleep(5); } catch { }
            }
            cancellationToken.ThrowIfCancellationRequested();
            return;
        }
        SleepAsync(duration, cancellationToken).GetAwaiter().GetResult();
    }
}
