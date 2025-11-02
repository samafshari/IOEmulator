namespace Neat;

public partial class CodePage
{
    public static CodePage IBM8x8()
    {
        var fl = new FontLoader();
    return fl.LoadFromResource("Neat.Fonts.IBM8x8.bin");
    }

    public static CodePage IBM8x14()
    {
        var fl = new FontLoader();
    return fl.LoadFromResource("Neat.Fonts.IBM8x14.bin");
    }

    public static CodePage IBM9x14()
    {
        var fl = new FontLoader();
    return fl.LoadFromResource("Neat.Fonts.IBM9x14.bin");
    }

    public static CodePage IBM8x16()
    {
        var fl = new FontLoader();
    return fl.LoadFromResource("Neat.Fonts.IBM8x16.bin");
    }

    public static CodePage IBM9x16()
    {
        var fl = new FontLoader();
    return fl.LoadFromResource("Neat.Fonts.IBM9x16.bin");
    }
}