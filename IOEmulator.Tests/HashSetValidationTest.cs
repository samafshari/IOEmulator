using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Neat.Test;

public class HashSetValidationTest
{
    private readonly ITestOutputHelper _output;
    
    public HashSetValidationTest(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void HashSet_OrdinalIgnoreCase_ShouldWork()
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LEN", "SQR", "SIN"
        };
        
        _output.WriteLine($"HashSet contains: {string.Join(", ", keywords)}");
        
        // Test various cases
        Assert.True(keywords.Contains("LEN"));
        Assert.True(keywords.Contains("len"));
        Assert.True(keywords.Contains("Len"));
        Assert.True(keywords.Contains("LeN"));
        
        _output.WriteLine("✓ All variations of LEN were found in HashSet");
    }
    
    [Fact]
    public void ValidateVariableName_TestLogic()
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LEN", "SQR", "SIN"
        };
        
        string testName = "LEN";
        string baseName = testName.TrimEnd('$', '%', '&', '!', '#');
        
        _output.WriteLine($"Input: '{testName}'");
        _output.WriteLine($"After TrimEnd: '{baseName}'");
        _output.WriteLine($"HashSet.Contains(baseName): {keywords.Contains(baseName)}");
        
        Assert.True(keywords.Contains(baseName), "LEN should be found in reserved keywords");
    }
}
