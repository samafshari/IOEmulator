using System.Runtime.CompilerServices;

namespace Neat;

internal static class Prewarm
{
    [ModuleInitializer]
    internal static void Init()
    {
        // Preload commonly used code pages so first use is instant
        _ = CodePage.IBM8x8();
        _ = CodePage.IBM8x16();
        _ = CodePage.IBM9x14();
        _ = CodePage.IBM9x16();
    }
}
