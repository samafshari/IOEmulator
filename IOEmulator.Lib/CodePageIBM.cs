namespace Neat;

public partial class CodePage
{
    private static readonly Lazy<CodePage> _ibm8x8 = new Lazy<CodePage>(() => new FontLoader().LoadFromResource("Neat.Fonts.IBM8x8.bin"));
    private static readonly Lazy<CodePage> _ibm8x14 = new Lazy<CodePage>(() => new FontLoader().LoadFromResource("Neat.Fonts.IBM8x14.bin"));
    private static readonly Lazy<CodePage> _ibm9x14 = new Lazy<CodePage>(() => new FontLoader().LoadFromResource("Neat.Fonts.IBM9x14.bin"));
    private static readonly Lazy<CodePage> _ibm8x16 = new Lazy<CodePage>(() => new FontLoader().LoadFromResource("Neat.Fonts.IBM8x16.bin"));
    private static readonly Lazy<CodePage> _ibm9x16 = new Lazy<CodePage>(() => new FontLoader().LoadFromResource("Neat.Fonts.IBM9x16.bin"));

    public static CodePage IBM8x8() => _ibm8x8.Value;

    public static CodePage IBM8x14() => _ibm8x14.Value;

    public static CodePage IBM9x14() => _ibm9x14.Value;

    public static CodePage IBM8x16() => _ibm8x16.Value;

    public static CodePage IBM9x16() => _ibm9x16.Value;
}