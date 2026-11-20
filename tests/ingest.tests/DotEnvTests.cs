using System;
using System.Collections.Generic;
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
    public void Parse_InvalidKeyWithWhitespace_Throws()
    {
        // Key with embedded whitespace (after trim)
        var lines = new[] { "INVALID KEY=value" };

        var ex = Assert.Throws<FormatException>(() => DotEnv.Parse(lines).ToList());
        Assert.Contains("Invalid key", ex.Message);
        Assert.Contains("INVALID KEY", ex.Message);
    }

    [Fact]
    public void Parse_InvalidKeyWithHyphen_Throws()
    {
        // Hyphens are not allowed in keys
        var lines = new[] { "A-B=value" };

        var ex = Assert.Throws<FormatException>(() => DotEnv.Parse(lines).ToList());
        Assert.Contains("Invalid key", ex.Message);
        Assert.Contains("A-B", ex.Message);
    }

    [Fact]
    public void Parse_InvalidKeyWithSpecialChars_Throws()
    {
        // Other special chars not allowed
        var lines = new[] { "KEY@=value" };

        var ex = Assert.Throws<FormatException>(() => DotEnv.Parse(lines).ToList());
        Assert.Contains("Invalid key", ex.Message);
        Assert.Contains("KEY@", ex.Message);
    }

    [Fact]
    public void Parse_InvalidKeyEmpty_Throws()
    {
        var lines = new[] { "=value" };

        var ex = Assert.Throws<FormatException>(() => DotEnv.Parse(lines).ToList());
        Assert.Contains("Invalid key", ex.Message);
        Assert.Contains("empty key", ex.Message);
    }

    [Fact]
    public void Parse_ValidKeysWithUnderscore_Accepted()
    {
        var lines = new[]
        {
            "VALID_KEY=value",
            "VALIDKEY=value2",
            "VALID123=value3",
            "_PRIVATE=value4"
        };

        var result = DotEnv.Parse(lines).ToDictionary(p => p.Key, p => p.Value);

        Assert.Equal("value", result["VALID_KEY"]);
        Assert.Equal("value2", result["VALIDKEY"]);
        Assert.Equal("value3", result["VALID123"]);
        Assert.Equal("value4", result["_PRIVATE"]);
    }

    [Fact]
    public void Parse_DuplicateKey_Throws()
    {
        var lines = new[]
        {
            "KEY=first",
            "KEY=second"
        };

        var ex = Assert.Throws<FormatException>(() => DotEnv.Parse(lines).ToList());
        Assert.Contains("Duplicate key", ex.Message);
        Assert.Contains("KEY", ex.Message);
    }

    [Fact]
    public void Parse_DuplicateKeyLaterInFile_Throws()
    {
        var lines = new[]
        {
            "KEY=first",
            "OTHER=value",
            "KEY=second"
        };

        var ex = Assert.Throws<FormatException>(() => DotEnv.Parse(lines).ToList());
        Assert.Contains("Duplicate key", ex.Message);
        Assert.Contains("KEY", ex.Message);
    }

    [Fact]
    public void Parse_NoEqualsSign_SkipsSilently()
    {
        // Lines without = are skipped (not an error)
        var lines = new[]
        {
            "KEY=value",
            "NOEQUALS",
            "OTHER=value2"
        };

        var result = DotEnv.Parse(lines).ToDictionary(p => p.Key, p => p.Value);

        Assert.Equal(2, result.Count);
        Assert.Equal("value", result["KEY"]);
        Assert.Equal("value2", result["OTHER"]);
    }

    [Fact]
    public void Parse_CommentStripping_UnquotedOnlyWithSpaceHash()
    {
        // Only " #" (space + hash) strips comments in unquoted values
        var lines = new[]
        {
            "HAS_SPACE_HASH=value # comment",   // stripped
            "NO_SPACE=value#notcomment",        // NOT stripped - # is literal
            "HASH_AT_START=#notcomment",        // NOT stripped - no space before #
            "MULTIPLE=value #c1 #c2",           // strips from first " #"
            "TRAILING_SPACE=value # ",          // strips, result "value"
        };

        var result = DotEnv.Parse(lines).ToDictionary(p => p.Key, p => p.Value);

        Assert.Equal("value", result["HAS_SPACE_HASH"]);
        Assert.Equal("value#notcomment", result["NO_SPACE"]);
        Assert.Equal("#notcomment", result["HASH_AT_START"]);
        Assert.Equal("value", result["MULTIPLE"]);
        Assert.Equal("value", result["TRAILING_SPACE"]);
    }

    [Fact]
    public void Parse_CommentStripping_QuotedValuesPreserveHash()
    {
        // Quoted values NEVER strip comments - # is always literal
        var lines = new[]
        {
            "DOUBLE_HASH=\"value # comment\"",
            "SINGLE_HASH='value # comment'",
            "DOUBLE_NO_SPACE=\"value#nospace\"",
            "SINGLE_NO_SPACE='value#nospace'",
            "DOUBLE_URL=\"https://example.com?a=1#fragment\"",
        };

        var result = DotEnv.Parse(lines).ToDictionary(p => p.Key, p => p.Value);

        Assert.Equal("value # comment", result["DOUBLE_HASH"]);
        Assert.Equal("value # comment", result["SINGLE_HASH"]);
        Assert.Equal("value#nospace", result["DOUBLE_NO_SPACE"]);
        Assert.Equal("value#nospace", result["SINGLE_NO_SPACE"]);
        Assert.Equal("https://example.com?a=1#fragment", result["DOUBLE_URL"]);
    }

    [Fact]
    public void Parse_CommentStripping_FullLineComments()
    {
        // Lines starting with # are full-line comments (skipped entirely)
        var lines = new[]
        {
            "# full line comment",
            "KEY=value",
            "  # indented comment",
            "OTHER=value2",
        };

        var result = DotEnv.Parse(lines).ToDictionary(p => p.Key, p => p.Value);

        Assert.Equal(2, result.Count);
        Assert.Equal("value", result["KEY"]);
        Assert.Equal("value2", result["OTHER"]);
    }

    [Fact]
    public void Parse_CommentStripping_NoEscapeMechanism()
    {
        // There is NO escape mechanism for # in unquoted values.
        // To include a literal # in an unquoted value, you cannot.
        // Use quoted values instead.
        var lines = new[]
        {
            "UNQUOTED_HASH=value # this is a comment",
            "QUOTED_HASH=\"value # this is literal\"",
        };

        var result = DotEnv.Parse(lines).ToDictionary(p => p.Key, p => p.Value);

        Assert.Equal("value", result["UNQUOTED_HASH"]);
        Assert.Equal("value # this is literal", result["QUOTED_HASH"]);
    }

    [Fact]
    public void Parse_CommentStripping_ExportPrefixWithComment()
    {
        // export prefix works with comments
        var lines = new[]
        {
            "export EXPORTED=value # comment",
            "EXPORTED_NO_COMMENT=value",
        };

        var result = DotEnv.Parse(lines).ToDictionary(p => p.Key, p => p.Value);

        Assert.Equal("value", result["EXPORTED"]);
        Assert.Equal("value", result["EXPORTED_NO_COMMENT"]);
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

    [Fact]
    public void LoadNearest_DoesNotOverrideExplicitlyEmptyEnvironmentVariable()
    {
        var root = TestTempDir.Create();
        var key = $"INGEST_TEST_{Guid.NewGuid():N}";
        try
        {
            // Explicitly set to empty string (not null)
            Environment.SetEnvironmentVariable(key, "");
            File.WriteAllText(Path.Combine(root, ".env"), $"{key}=from-file\n");

            DotEnv.LoadNearest(root);

            // Empty string should be preserved, not replaced by .env value
            Assert.Equal("", Environment.GetEnvironmentVariable(key));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
            TestTempDir.Cleanup(root);
        }
    }

    [Fact]
    public void LoadNearest_OverridesWhenVariableNotSet()
    {
        var root = TestTempDir.Create();
        var key = $"INGEST_TEST_{Guid.NewGuid():N}";
        try
        {
            // Ensure not set
            Environment.SetEnvironmentVariable(key, null);
            File.WriteAllText(Path.Combine(root, ".env"), $"{key}=from-file\n");

            DotEnv.LoadNearest(root);

            Assert.Equal("from-file", Environment.GetEnvironmentVariable(key));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
            TestTempDir.Cleanup(root);
        }
    }
}
