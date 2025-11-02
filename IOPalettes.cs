namespace Neat;

public static class IOPalettes
{
    public static readonly RGB[] CGA = [
        new RGB(0, 0, 0),
        new RGB(0, 170, 170),
        new RGB(170, 0, 170),
        new RGB(170, 170, 170)
    ];

    public static readonly RGB[] EGA = [
        new RGB(0, 0, 0),
        new RGB(0, 0, 170),
        new RGB(0, 170, 0),
        new RGB(0, 170, 170),
        new RGB(170, 0, 0),
        new RGB(170, 0, 170),
        new RGB(170, 85, 0),
        new RGB(170, 170, 170),
        new RGB(85, 85, 85),
        new RGB(85, 85, 255),
        new RGB(85, 255, 85),
        new RGB(85, 255, 255),
        new RGB(255, 85, 85),
        new RGB(255, 85, 255),
        new RGB(255, 255, 85),
        new RGB(255, 255, 255)
    ];

    public static readonly RGB[] VGA = GenerateVGA256Palette();

    static RGB[] GenerateVGA256Palette()
    {
        RGB[] palette = new RGB[256];
        // EGA colors 0-15
        Array.Copy(EGA, 0, palette, 0, 16);
        // RGB cube 16-231
        int index = 16;
        for (int r = 0; r < 6; r++)
            for (int g = 0; g < 6; g++)
                for (int b = 0; b < 6; b++)
                    palette[index++] = new RGB((byte)(r * 51), (byte)(g * 51), (byte)(b * 51));
        // Grays 232-255
        for (int i = 232; i <= 255; i++)
        {
            byte k = (byte)(8 + 10 * (i - 232));
            palette[i] = new RGB(k, k, k);
        }
        return palette;
    }
}
