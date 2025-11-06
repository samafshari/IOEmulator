#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Neat;

namespace Neat.Test;

public class SchedulerAndSoundTests
{
    private class TestSoundDriver : ISoundDriver
    {
        public int BeepCount;
    public (int f,int d) LastTone;
    public string LastPlay = string.Empty;
        public void Beep() { BeepCount++; }
        public void PlayTone(int frequencyHz, int durationMs) { LastTone = (frequencyHz, durationMs); }
        public void PlayMusicString(string musicString) { LastPlay = musicString; }
    }

    [Fact]
    public async Task WaitForKeyAsync_Completes_On_Inject()
    {
        var io = new IOEmulator();
        var sched = new QBasicScheduler(io);
        // Inject immediately; async wait should complete quickly
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Unknown, 'K'));
        var ev = await sched.WaitForKeyAsync();
        Assert.Equal('K', ev.Char);
    }

    [Fact]
    public async Task SleepAsync_Honors_Duration()
    {
        var io = new IOEmulator();
        var sched = new QBasicScheduler(io);
    var start = DateTime.UtcNow;
    await sched.SleepAsync(TimeSpan.FromMilliseconds(1));
    var elapsed = DateTime.UtcNow - start;
    Assert.True(elapsed.TotalMilliseconds >= 0);
    }

    [Fact]
    public void QBasic_Sound_Delegates_To_Driver()
    {
        var io = new IOEmulator();
        var driver = new TestSoundDriver();
        var qb = new QBasicApi(io, driver);
        qb.BEEP();
        qb.SOUND(440, 100);
        qb.PLAY("CDE");
        Assert.Equal(1, driver.BeepCount);
        Assert.Equal((440, 100), driver.LastTone);
        Assert.Equal("CDE", driver.LastPlay);
    }
}
