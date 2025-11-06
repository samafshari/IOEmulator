using System;
using Xunit;

namespace Neat.Test;

public class InputExtendedTests
{
    [Fact]
    public void Key_Functions_Report_Arrows_And_Modifiers()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        // Program sets specific pixels when key states are true
        string src = @"SCREEN 13
COLOR 15,0
CLS
IF KEY(""LEFT"") THEN PSET 1,1, 15
IF KEY(""RIGHT"") THEN PSET 2,2, 15
IF KEY(""UP"") THEN PSET 3,3, 15
IF KEY(""DOWN"") THEN PSET 4,4, 15
IF SHIFT() THEN PSET 5,5, 15
IF CTRL() THEN PSET 6,6, 15
IF ALT() THEN PSET 7,7, 15
";
        // Inject key down with modifiers
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Left, null, shift:true));
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Right, null, ctrl:true));
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Up, null, alt:true));
        io.InjectKey(new KeyEvent(KeyEventType.Down, KeyCode.Down));
        interp.Run(src);
        var bg = io.GetColor(io.BackgroundColorIndex);
        Assert.NotEqual(bg, io.ReadPixelAt(1,1));
        Assert.NotEqual(bg, io.ReadPixelAt(2,2));
        Assert.NotEqual(bg, io.ReadPixelAt(3,3));
        Assert.NotEqual(bg, io.ReadPixelAt(4,4));
        Assert.NotEqual(bg, io.ReadPixelAt(5,5));
        Assert.NotEqual(bg, io.ReadPixelAt(6,6));
        Assert.NotEqual(bg, io.ReadPixelAt(7,7));
    }

    [Fact]
    public void Mouse_Functions_Draw_At_Position_And_Buttons()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
    string src = @"SCREEN 13
COLOR 15,0
CLS
 X = MOUSEX(): Y = MOUSEY(): PSET X, Y, 10
IF MOUSE_LEFT() THEN PSET 0,0, 15
";
    io.SetMouseState(100, 50, left:true, right:false, middle:false);
        interp.Run(src);
        Assert.Equal(10, io.ReadPaletteIndexAt(100, 50));
        Assert.Equal(15, io.ReadPaletteIndexAt(0,0));
    }
}
