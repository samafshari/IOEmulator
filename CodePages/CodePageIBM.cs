namespace Neat;

public partial class CodePage
{
    public static CodePage IBM8x8()
    {
        Glyph[] glyphs = new Glyph[256];

        // TODO: Populate glyphs with IBM 8x8 font data

        var cp = new CodePage(glyphs);
        return cp;
    }

    public static CodePage IBM8x14()
    {
        Glyph[] glyphs = new Glyph[256];

        // TODO: Populate glyphs with IBM 8x14 font data

        var cp = new CodePage(glyphs);
        return cp;
    }

    public static CodePage IBM9x14()
    {
        Glyph[] glyphs = new Glyph[256];

        // TODO: Populate glyphs with IBM 9x14 font data

        var cp = new CodePage(glyphs);
        return cp;
    }

    public static CodePage IBM8x16()
    {
        Glyph[] glyphs = new Glyph[256];

        // TODO: Populate glyphs with IBM 8x16 font data

        var cp = new CodePage(glyphs);
        return cp;
    }

    public static CodePage IBM9x16()
    {
        Glyph[] glyphs = new Glyph[256];

        // TODO: Populate glyphs with IBM 9x16 font data

        var cp = new CodePage(glyphs);
        return cp;
    }
}