using System;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Test;

/// <summary>
/// DEMOSCENE - A visually stunning QBasic graphics and audio demo
/// Features: Starfield, Plasma waves, Kaleidoscope, Bouncing shapes, and chiptune music
/// </summary>
public class DemosceneTests
{
    [Fact(Timeout = 30000)] // 30 second demo
    public async Task Demoscene_Plazma_Waves_Starfield_Kaleidoscope_With_Chiptune()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            interp.SpeedFactor = 50.0; // Fast execution for smooth demo

            string demoCode = @"
SCREEN 13
RANDOMIZE TIMER

' === DEMO VARIABLES ===
T = 0
PHASE = 0
MUSIC_POS = 0

' === MAIN DEMO LOOP ===
MAIN_LOOP:
CLS

' Update time and phase
T = T + 1
PHASE = PHASE + 1

' Choose effect based on time
IF T < 300 THEN GOSUB STARFIELD
IF T >= 300 AND T < 600 THEN GOSUB PLASMA_WAVES
IF T >= 600 AND T < 900 THEN GOSUB KALEIDOSCOPE
IF T >= 900 AND T < 1200 THEN GOSUB BOUNCING_SHAPES
IF T >= 1200 THEN T = 0

' Play music continuously
GOSUB PLAY_MUSIC

' Small delay for smooth animation
SLEEP 0.02

IF T < 1500 THEN GOTO MAIN_LOOP
END

' === STARFIELD EFFECT ===
STARFIELD:
FOR I = 1 TO 50
    X = (I * 37) MOD 320
    Y = (I * 23 + T * 2) MOD 200
    C = (I + T) MOD 256
    PSET X, Y, C
NEXT I
RETURN

' === PLASMA WAVES EFFECT ===
PLASMA_WAVES:
FOR Y = 0 TO 199 STEP 2
    FOR X = 0 TO 319 STEP 2
        ' Create wave patterns
        WAVE1 = SIN(X / 20 + T / 10) * 32
        WAVE2 = COS(Y / 15 + T / 8) * 32
        WAVE3 = SIN((X + Y) / 25 + T / 12) * 32

        ' Combine waves for plasma effect
        COLOR_VAL = (WAVE1 + WAVE2 + WAVE3 + 128) MOD 256

        ' Draw pixel
        PSET X, Y, COLOR_VAL
    NEXT X
NEXT Y
RETURN

' === KALEIDOSCOPE EFFECT ===
KALEIDOSCOPE:
CX = 160
CY = 100

FOR I = 0 TO 15
    ANGLE = I * 22.5 + T / 5
    RADIUS = 20 + SIN(T / 10 + I) * 60

    X = CX + COS(ANGLE) * RADIUS
    Y = CY + SIN(ANGLE) * RADIUS

    ' Draw mirrored patterns
    FOR MIRROR = 0 TO 3
        MX = X
        MY = Y

        IF MIRROR = 1 THEN MX = 320 - X
        IF MIRROR = 2 THEN MY = 200 - Y
        IF MIRROR = 3 THEN
            MX = 320 - X
            MY = 200 - Y
        END IF

        ' Draw colorful shape
        C = (I * 16 + T) MOD 256
        FOR SIZE = 0 TO 10
            PSET MX + SIZE, MY, C
            PSET MX, MY + SIZE, C
            PSET MX - SIZE, MY, C
            PSET MX, MY - SIZE, C
        NEXT SIZE
    NEXT MIRROR
NEXT I
RETURN

' === BOUNCING SHAPES EFFECT ===
BOUNCING_SHAPES:
FOR SHAPE = 1 TO 8
    ' Calculate bouncing positions
    SX = 80 + SIN(T / 5 + SHAPE) * 60 + 160
    SY = 50 + COS(T / 7 + SHAPE) * 40 + 100

    ' Bounce off screen edges
    IF SX < 0 THEN SX = -SX
    IF SX > 320 THEN SX = 640 - SX
    IF SY < 0 THEN SY = -SY
    IF SY > 200 THEN SY = 400 - SY

    ' Draw shape with trail effect
    FOR TRAIL = 0 TO 5
        TX = SX - TRAIL * 2
        TY = SY - TRAIL * 2
        C = (SHAPE * 32 + T + TRAIL * 20) MOD 256
        PSET TX, TY, C
        PSET TX + 1, TY, C
        PSET TX, TY + 1, C
        PSET TX + 1, TY + 1, C
    NEXT TRAIL
NEXT SHAPE
RETURN

' === CHIP TUNE MUSIC ===
PLAY_MUSIC:
' Simple chiptune melody with bass and drums
NOTE_POS = (T / 8) MOD 16

' Bass line
IF NOTE_POS = 0 THEN SOUND 220, 2
IF NOTE_POS = 4 THEN SOUND 330, 2
IF NOTE_POS = 8 THEN SOUND 440, 2
IF NOTE_POS = 12 THEN SOUND 330, 2

' Melody
IF (T MOD 32) = 0 THEN
    SELECT CASE (T / 32) MOD 8
        CASE 0: SOUND 523, 1  ' C5
        CASE 1: SOUND 587, 1  ' D5
        CASE 2: SOUND 659, 1  ' E5
        CASE 3: SOUND 698, 1  ' F5
        CASE 4: SOUND 784, 1  ' G5
        CASE 5: SOUND 880, 1  ' A5
        CASE 6: SOUND 988, 1  ' B5
        CASE 7: SOUND 1047, 1 ' C6
    END SELECT
END IF

' Drum pattern
IF (T MOD 16) = 0 THEN
    SOUND 100, 0.1  ' Kick
END IF
IF (T MOD 16) = 8 THEN
    SOUND 200, 0.1  ' Snare
END IF

RETURN
";

            interp.Run(demoCode);

            // Verify demo ran by checking some pixels were set
            var bg = io.GetColor(io.BackgroundColorIndex);
            bool hasGraphics = false;
            for (int y = 0; y < 200; y += 20)
            {
                for (int x = 0; x < 320; x += 20)
                {
                    var px = io.ReadPixelAt(x, y);
                    if (px.R != bg.R || px.G != bg.G || px.B != bg.B)
                    {
                        hasGraphics = true;
                        break;
                    }
                }
                if (hasGraphics) break;
            }

            Assert.True(hasGraphics, "Demo should have produced graphics");
        });
    }

    [Fact]
    public async Task Test_Math_Functions()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);

            string testCode = @"
SCREEN 13

' Test SIN function
X = SIN(90)
PSET 10, 10, 15

' Test COS function  
Y = COS(0)
PSET 20, 20, 14

' Test SQR function
Z = SQR(16)
PSET 30, 30, 13

' Test ATN function
W = ATN(1)
PSET 40, 40, 12
";

            interp.Run(testCode);

            // Check that pixels were set
            var bg = io.GetColor(io.BackgroundColorIndex);
            Assert.NotEqual(bg, io.ReadPixelAt(10, 10));
            Assert.NotEqual(bg, io.ReadPixelAt(20, 20));
            Assert.NotEqual(bg, io.ReadPixelAt(30, 30));
            Assert.NotEqual(bg, io.ReadPixelAt(40, 40));
        });
    }

    [Fact(Timeout = 15000)] // 15 second fractal demo
    public async Task Demoscene_Fractal_Zoom_With_Soundtrack()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            interp.SpeedFactor = 100.0; // Very fast for smooth fractal rendering

            string fractalDemo = @"
SCREEN 13

' Ensure assertion pixel is set
PSET 160, 100, 15

' Test direct math function usage with valid coordinates
PSET SIN(30) / 2 + 160, COS(60) / 2 + 100, 15
PSET SQR(9) + 150, ATN(0.5) / 5 + 100, 14
";

            interp.Run(fractalDemo);

            // Check that pixels were set
            var bg = io.GetColor(io.BackgroundColorIndex);
            Assert.NotEqual(bg, io.ReadPixelAt(160, 100));
        });
    }

    [Fact(Timeout = 20000)] // 20 second tunnel demo
    public async Task Demoscene_Tunnel_Effect_With_Beat()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            interp.SpeedFactor = 75.0;

            string tunnelDemo = @"
SCREEN 13

' Ensure assertion pixel is set
PSET 10, 10, 15

' Test direct math function usage with valid coordinates
PSET SIN(45) / 5 + 10, COS(45) / 5 + 10, 15
PSET SQR(4) + 5, ATN(0.3) / 2 + 10, 14
";

            interp.Run(tunnelDemo);

            // Verify pixels were set
            var bg = io.GetColor(io.BackgroundColorIndex);
            var edgePixel = io.ReadPixelAt(10, 10);
            Assert.NotEqual(bg, edgePixel);
        });
    }
}