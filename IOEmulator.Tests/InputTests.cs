using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Neat;

public class InputTests
{
    [Fact]
    public void InjectKey_Then_TryReadKey_Works()
    {
        var io = new IOEmulator();
        var ev = new KeyEvent(KeyEventType.Down, KeyCode.Unknown, 'A');
        io.InjectKey(ev);
        Assert.True(io.TryReadKey(out var read));
        Assert.Equal('A', read.Char);
        Assert.Equal(KeyEventType.Down, read.Type);
    }

    [Fact]
    public void WaitForKey_Unblocks_On_Inject()
    {
        var io = new IOEmulator();
        // Inject immediately; WaitForKey should return without delay
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, 'Z'));
        var ev = io.WaitForKey();
        Assert.Equal('Z', ev.Char);
    }

    [Fact]
    public void QBasic_INKEY_Returns_String()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        Assert.Equal(string.Empty, qb.INKEY());
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, 'q'));
        Assert.Equal("q", qb.INKEY());
        // Queue is now empty -> returns empty again
        Assert.Equal(string.Empty, qb.INKEY());
    }

    [Fact]
    public void QBasic_SLEEP_Waits_Or_TimesOut()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        // Should timeout with no exception
        qb.SLEEP(0);
        // Should wait for key without seconds
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, 'X'));
        qb.SLEEP(null); // returns immediately because key is already available
        // If reached here, it unblocked on key
        Assert.True(true);
    }
}
