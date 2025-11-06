namespace Neat;

public static class IOPalettes
{
    // Packed ABGR palette entries (A=255)
    public static readonly int[] CGA = new int[]
    {
        Pack(0, 0, 0),
        Pack(0, 170, 170),
        Pack(170, 0, 170),
        Pack(170, 170, 170)
    };

    public static readonly int[] EGA = new int[]
    {
        Pack(0, 0, 0),
        Pack(0, 0, 170),
        Pack(0, 170, 0),
        Pack(0, 170, 170),
        Pack(170, 0, 0),
        Pack(170, 0, 170),
        Pack(170, 85, 0),
        Pack(170, 170, 170),
        Pack(85, 85, 85),
        Pack(85, 85, 255),
        Pack(85, 255, 85),
        Pack(85, 255, 255),
        Pack(255, 85, 85),
        Pack(255, 85, 255),
        Pack(255, 255, 85),
        Pack(255, 255, 255)
    };

    public static readonly int[] VGA = GenerateVGA256Palette();

    static int[] GenerateVGA256Palette()
    {
        int[] palette = new int[256];
        // EGA colors 0-15
        Array.Copy(EGA, 0, palette, 0, 16);
        // RGB cube 16-231
        int index = 16;
        for (int r = 0; r < 6; r++)
            for (int g = 0; g < 6; g++)
                for (int b = 0; b < 6; b++)
                    palette[index++] = Pack(r * 51, g * 51, b * 51);
        // Grays 232-255
        for (int i = 232; i <= 255; i++)
        {
            int k = 8 + 10 * (i - 232);
            palette[i] = Pack(k, k, k);
        }
        return palette;
    }

    private static int Pack(int r, int g, int b)
    {
        r = r & 255; g = g & 255; b = b & 255;
        return (255 << 24) | (b << 16) | (g << 8) | r;
    }
}
