using System;
using System.IO;
using System.Linq;
using Xunit;
using IngestTests.Helpers;

namespace IngestTests;

public class DotEnvTests
{
    [Fact]
    public void Parse_HandlesCommentsBlankLinesExportAndQuotes()
    {
        var lines = new[]
        {
            "# a comment",
            "",
            "PLAIN=value",
            "export EXPORTED=yes",
            "DOUBLE=\"quoted value\"",
            "SINGLE='single quoted'",
            "INLINE=abc # trailing comment",
            "QUOTED_HASH=\"keep # this\"",
            "URL=https://x.openai.azure.com/?a=b",
            "NOEQUALS",
            "  SPACED  =  padded  "
        };

        var result = DotEnv.Parse(lines).ToDictionary(p => p.Key, p => p.Value);

        Assert.Equal("value", result["PLAIN"]);
        Assert.Equal("yes", result["EXPORTED"]);
        Assert.Equal("quoted value", result["DOUBLE"]);
        Assert.Equal("single quoted", result["SINGLE"]);
        Assert.Equal("abc", result["INLINE"]);
        Assert.Equal("keep # this", result["QUOTED_HASH"]);
        Assert.Equal("https://x.openai.azure.com/?a=b", result["URL"]); // only the first '=' splits
        Assert.Equal("padded", result["SPACED"]);
        Assert.DoesNotContain("NOEQUALS", result.Keys);
    }

    [Fact]
    public void LoadNearest_FindsEnvInParentDirectory()
    {
        var root = TestTempDir.Create();
        var key = $"INGEST_TEST_{Guid.NewGuid():N}";
        try
        {
            File.WriteAllText(Path.Combine(root, ".env"), $"{key}=from-file\n");
            var nested = Directory.CreateDirectory(Path.Combine(root, "a", "b")).FullName;

            var loaded = DotEnv.LoadNearest(nested);

            Assert.Equal(Path.Combine(root, ".env"), loaded);
            Assert.Equal("from-file", Environment.GetEnvironmentVariable(key));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
            TestTempDir.Cleanup(root);
        }
    }

    [Fact]
    public void LoadNearest_DoesNotOverrideExistingEnvironmentVariables()
    {
        var root = TestTempDir.Create();
        var key = $"INGEST_TEST_{Guid.NewGuid():N}";
        try
        {
            Environment.SetEnvironmentVariable(key, "from-real-env");
            File.WriteAllText(Path.Combine(root, ".env"), $"{key}=from-file\n");

            DotEnv.LoadNearest(root);

            Assert.Equal("from-real-env", Environment.GetEnvironmentVariable(key));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
            TestTempDir.Cleanup(root);
        }
    }
}
