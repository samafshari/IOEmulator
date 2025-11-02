namespace Neat;

public class FontLoader
{
    public CodePage LoadFromResource(string resourceName)
    {
        var assembly = this.GetType().Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Resource {resourceName} not found");
        }
        using var reader = new BinaryReader(stream);
        byte width = reader.ReadByte();
        byte height = reader.ReadByte();
        int count = reader.ReadInt32();
        var glyphs = new Glyph[count];
        for (int i = 0; i < count; i++)
        {
            var glyphData = reader.ReadBytes(height);
            glyphs[i] = new Glyph(width, glyphData);
        }
        return new CodePage(glyphs);
    }
}