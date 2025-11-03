using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Neat;

public static class QBasicSamples
{
    // Embedded resource prefix
    private const string Prefix = "Neat.Samples.";

    public static IEnumerable<string> List()
    {
        var asm = typeof(QBasicSamples).Assembly;
        return asm.GetManifestResourceNames()
            .Where(n => n.StartsWith(Prefix, StringComparison.Ordinal))
            .Select(n => n.Substring(Prefix.Length));
    }

    public static string Load(string name)
    {
        var asm = typeof(QBasicSamples).Assembly;
        var resName = name.Contains('.') ? Prefix + name : Prefix + name + ".bas";
        using var s = asm.GetManifestResourceStream(resName)
            ?? throw new InvalidOperationException($"Sample not found: {name}");
        using var sr = new StreamReader(s, Encoding.UTF8);
        return sr.ReadToEnd();
    }
}
