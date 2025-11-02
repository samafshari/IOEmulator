using System;
namespace Neat;

public static class IOScreenModes
{
    public static ScreenMode GetQBasicScreenMode(int i)
    {
        return i switch
        {
            0 => new ScreenMode(40, 25, 320, 200, IOPalettes.EGA, CodePage.IBM8x8()),
            1 => new ScreenMode(0, 0, 320, 200, IOPalettes.EGA, CodePage.IBM8x8()),
            2 => new ScreenMode(0, 0, 640, 200, IOPalettes.EGA, CodePage.IBM8x8()),
            3 => new ScreenMode(0, 0, 320, 200, IOPalettes.EGA, CodePage.IBM8x8()),
            4 => new ScreenMode(0, 0, 640, 200, IOPalettes.EGA, CodePage.IBM8x8()),
            5 => new ScreenMode(0, 0, 320, 200, IOPalettes.EGA, CodePage.IBM8x8()),
            6 => new ScreenMode(0, 0, 640, 200, IOPalettes.EGA, CodePage.IBM8x8()),
            7 => new ScreenMode(0, 0, 320, 200, IOPalettes.EGA, CodePage.IBM8x8()),
            8 => new ScreenMode(0, 0, 640, 200, IOPalettes.EGA, CodePage.IBM8x8()),
            9 => new ScreenMode(0, 0, 640, 350, IOPalettes.EGA, CodePage.IBM8x16()),
            10 => new ScreenMode(0, 0, 640, 350, IOPalettes.EGA, CodePage.IBM8x16()),
            11 => new ScreenMode(0, 0, 640, 480, IOPalettes.EGA, CodePage.IBM8x16()),
            12 => new ScreenMode(0, 0, 640, 480, IOPalettes.EGA, CodePage.IBM8x16()),
            13 => new ScreenMode(0, 0, 320, 200, IOPalettes.VGA, CodePage.IBM8x8()),
            _ => throw new ArgumentOutOfRangeException(nameof(i), $"Unsupported QBASIC screen mode: {i}")
        };
    }
}