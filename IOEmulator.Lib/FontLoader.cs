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

        // Detect header size dynamically by probing plausible sizes so that the remaining
        // data is an exact multiple of bytes-per-glyph.
        int bytesPerRow = (width + 7) / 8; // packed bits per row
        if (bytesPerRow <= 0 || height <= 0)
            throw new InvalidDataException($"Invalid font header: width={width}, height={height}");

        int bytesPerGlyph = bytesPerRow * height;
        long initialPos = stream.Position; // after width+height (typically 2)
        int[] headerSkips = new[] { 0, 6, 8, 12, 14, 16, 18, 20, 24, 28, 32 };
        long chosenPos = -1;
        int count = 0;
        foreach (var skip in headerSkips)
        {
            long pos = initialPos + skip;
            if (pos > stream.Length) continue;
            long rem = stream.Length - pos;
            if (rem > 0 && bytesPerGlyph > 0 && rem % bytesPerGlyph == 0)
            {
                int c = (int)(rem / bytesPerGlyph);
                if (c >= 128 && c <= 1024)
                {
                    chosenPos = pos;
                    count = c;
                    break;
                }
            }
        }
        if (chosenPos < 0)
        {
            // Fallback: accept truncated remainder by flooring the count
            long rem = stream.Length - initialPos;
            count = (int)(rem / Math.Max(1, bytesPerGlyph));
            if (count <= 0)
                throw new InvalidDataException($"Font payload not found or invalid (rem={rem}, bpg={bytesPerGlyph}).");
            chosenPos = initialPos;
        }
        stream.Position = chosenPos;
        var glyphs = new Glyph[count];
        for (int gi = 0; gi < count; gi++)
        {
            // Expand packed bits into a width*height bitmap (0=bg,1=fg)
            var expanded = new byte[width * height];
            for (int row = 0; row < height; row++)
            {
                var rowBytes = reader.ReadBytes(bytesPerRow);
                if (rowBytes.Length != bytesPerRow)
                    throw new EndOfStreamException($"Unexpected EOF reading glyph {gi} row {row}");
                for (int col = 0; col < width; col++)
                {
                    int byteIndex = col / 8;
                    int bitIndex = 7 - (col % 8);
                    int bit = (rowBytes[byteIndex] >> bitIndex) & 0x1;
                    expanded[row * width + col] = (byte)bit;
                }
            }
            glyphs[gi] = new Glyph(width, expanded);
        }
        return new CodePage(glyphs);
    }
}