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
        // Simpler and race-free: block on IOEmulator's own WaitForKey on a background thread
        return Task.Run(() => _io.WaitForKey(cancellationToken), cancellationToken);
    }

    public Task SleepAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        if (duration <= TimeSpan.Zero) return Task.CompletedTask;
        return Task.Delay(duration, cancellationToken);
    }

    // Synchronous wrappers (for convenience or legacy style)
    public KeyEvent WaitForKey(CancellationToken cancellationToken = default)
        => WaitForKeyAsync(cancellationToken).GetAwaiter().GetResult();

    public void Sleep(TimeSpan duration, CancellationToken cancellationToken = default)
        => SleepAsync(duration, cancellationToken).GetAwaiter().GetResult();
}
