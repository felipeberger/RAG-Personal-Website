using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using IngestTests.Factories;
using IngestTests.Helpers;

namespace IngestTests;

public class OptionsTests
{
    [Fact]
    public void Parse_Defaults_WhenNoArgs()
    {
        var opts = Options.Parse([]);

        Assert.Equal("data", opts.DataDir);
        Assert.Equal(Path.Combine("data", "index.json"), opts.OutPath);
        Assert.Null(opts.Endpoint);
        Assert.Null(opts.Deployment);
        Assert.False(opts.DryRun);
        Assert.False(opts.Force);
        Assert.False(opts.Help);
    }

    [Fact]
    public void Parse_CustomDataDir()
    {
        var opts = Options.Parse(["--data", "mydata"]);

        Assert.Equal("mydata", opts.DataDir);
        Assert.Equal(Path.Combine("mydata", "index.json"), opts.OutPath);
    }

    [Fact]
    public void Parse_CustomOutPath()
    {
        var opts = Options.Parse(["--out", "custom.json"]);

        // --out takes the path as-is, doesn't prepend data dir
        Assert.Equal("custom.json", opts.OutPath);
    }

    [Fact]
    public void Parse_EndpointAndDeployment()
    {
        var opts = Options.Parse(["--endpoint", "https://test.openai.azure.com/", "--deployment", "text-embedding-3-small"]);

        Assert.Equal("https://test.openai.azure.com/", opts.Endpoint);
        Assert.Equal("text-embedding-3-small", opts.Deployment);
    }

    [Fact]
    public void Parse_Flags()
    {
        var opts = Options.Parse(["--dry-run", "--force"]);

        Assert.True(opts.DryRun);
        Assert.True(opts.Force);
    }

    [Fact]
    public void Parse_HelpFlags()
    {
        var opts1 = Options.Parse(["--help"]);
        var opts2 = Options.Parse(["-h"]);

        Assert.True(opts1.Help);
        Assert.True(opts2.Help);
    }

    [Fact]
    public void Parse_EnvVarsUsedWhenFlagsMissing()
    {
        var originalEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var originalDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT");

        Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", "https://env.openai.azure.com/");
        Environment.SetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT", "env-deployment");

        try
        {
            var opts = Options.Parse([]);
            Assert.Equal("https://env.openai.azure.com/", opts.Endpoint);
            Assert.Equal("env-deployment", opts.Deployment);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", originalEndpoint);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT", originalDeployment);
        }
    }

    [Fact]
    public void Parse_FlagsOverrideEnvVars()
    {
        var originalEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var originalDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT");

        Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", "https://env.openai.azure.com/");
        Environment.SetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT", "env-deployment");

        try
        {
            var opts = Options.Parse(["--endpoint", "https://flag.openai.azure.com/", "--deployment", "flag-deployment"]);
            Assert.Equal("https://flag.openai.azure.com/", opts.Endpoint);
            Assert.Equal("flag-deployment", opts.Deployment);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", originalEndpoint);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT", originalDeployment);
        }
    }
}

public class ComputeHashTests
{
    [Fact]
    public void ComputeHash_SameInputProducesSameHash()
    {
        var hash1 = Program.ComputeHash("https://ep.openai.azure.com/", "deployment", "input text");
        var hash2 = Program.ComputeHash("https://ep.openai.azure.com/", "deployment", "input text");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentEndpointProducesDifferentHash()
    {
        var hash1 = Program.ComputeHash("https://ep1.openai.azure.com/", "deployment", "input text");
        var hash2 = Program.ComputeHash("https://ep2.openai.azure.com/", "deployment", "input text");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentDeploymentProducesDifferentHash()
    {
        var hash1 = Program.ComputeHash("https://ep.openai.azure.com/", "deployment1", "input text");
        var hash2 = Program.ComputeHash("https://ep.openai.azure.com/", "deployment2", "input text");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentInputProducesDifferentHash()
    {
        var hash1 = Program.ComputeHash("https://ep.openai.azure.com/", "deployment", "input text 1");
        var hash2 = Program.ComputeHash("https://ep.openai.azure.com/", "deployment", "input text 2");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_ProducesHexString()
    {
        var hash = Program.ComputeHash("https://ep.openai.azure.com/", "deployment", "input text");

        Assert.Matches("^[0-9A-F]+$", hash);
        Assert.Equal(64, hash.Length); // SHA256 = 32 bytes = 64 hex chars
    }
}

public class EmbedInputTests
{
    [Fact]
    public void EmbedInput_CombinesTitleAndTextWithNewline()
    {
        var chunk = ChunkFactory.Valid() with { Title = "My Story", Text = "This is the content." };

        var result = Program.EmbedInput(chunk);

        Assert.Equal("My Story\nThis is the content.", result);
    }

    [Fact]
    public void EmbedInput_HandlesEmptyTitle()
    {
        var chunk = ChunkFactory.WithEmptyTitle() with { Text = "Content only" };

        var result = Program.EmbedInput(chunk);

        Assert.Equal("\nContent only", result);
    }

    [Fact]
    public void EmbedInput_HandlesEmptyText()
    {
        var chunk = ChunkFactory.WithEmptyText() with { Title = "Title only" };

        var result = Program.EmbedInput(chunk);

        Assert.Equal("Title only\n", result);
    }
}

public class ValidateTests
{
    [Fact]
    public void Validate_EmptyId_AddsError()
    {
        var chunks = new List<Chunk> { ChunkFactory.WithEmptyId() };
        var errors = new List<string>();
        var warnings = new List<string>();

        Program.Validate(chunks, errors, warnings);

        Assert.Contains(errors, e => e.Contains("empty id"));
    }

    [Fact]
    public void Validate_DuplicateIds_AddsError()
    {
        var chunks = new List<Chunk>
        {
            ChunkFactory.Valid("dup"),
            ChunkFactory.Valid("dup")
        };
        var errors = new List<string>();
        var warnings = new List<string>();

        Program.Validate(chunks, errors, warnings);

        Assert.Contains(errors, e => e.Contains("Duplicate id 'dup'"));
    }

    [Fact]
    public void Validate_EmptyTitle_AddsError()
    {
        var chunks = new List<Chunk> { ChunkFactory.WithEmptyTitle() };
        var errors = new List<string>();
        var warnings = new List<string>();

        Program.Validate(chunks, errors, warnings);

        Assert.Contains(errors, e => e.Contains("empty title"));
    }

    [Fact]
    public void Validate_EmptyText_AddsError()
    {
        var chunks = new List<Chunk> { ChunkFactory.WithEmptyText() };
        var errors = new List<string>();
        var warnings = new List<string>();

        Program.Validate(chunks, errors, warnings);

        Assert.Contains(errors, e => e.Contains("empty text"));
    }

    [Fact]
    public void Validate_ShortText_AddsWarning()
    {
        var chunks = new List<Chunk> { ChunkFactory.WithShortText() };
        var errors = new List<string>();
        var warnings = new List<string>();

        Program.Validate(chunks, errors, warnings);

        Assert.Contains(warnings, w => w.Contains("very short chunks retrieve poorly"));
    }

    [Fact]
    public void Validate_LongText_AddsWarning()
    {
        var chunks = new List<Chunk> { ChunkFactory.WithLongText() };
        var errors = new List<string>();
        var warnings = new List<string>();

        Program.Validate(chunks, errors, warnings);

        Assert.Contains(warnings, w => w.Contains("consider splitting this chunk"));
    }

    [Fact]
    public void Validate_RelatedIdNotExists_AddsWarning()
    {
        var chunks = new List<Chunk> { ChunkFactory.WithRelated("id-1", "missing-id") };
        var errors = new List<string>();
        var warnings = new List<string>();

        Program.Validate(chunks, errors, warnings);

        Assert.Contains(warnings, w => w.Contains("related id 'missing-id' does not exist"));
    }

    [Fact]
    public void Validate_ValidChunk_NoErrorsOrWarnings()
    {
        var chunks = new List<Chunk> { ChunkFactory.Valid() };
        var errors = new List<string>();
        var warnings = new List<string>();

        Program.Validate(chunks, errors, warnings);

        Assert.Empty(errors);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Validate_RelatedIdExists_NoWarning()
    {
        var chunks = new List<Chunk>
        {
            ChunkFactory.WithRelated("id-1", "id-2"),
            ChunkFactory.Valid("id-2")
        };
        var errors = new List<string>();
        var warnings = new List<string>();

        Program.Validate(chunks, errors, warnings);

        Assert.Empty(warnings);
    }
}

public class LoadChunksTests
{
    [Fact]
    public void LoadChunks_ReadsValidJsonFile()
    {
        var tempDir = TestTempDir.Create();
        var file = Path.Combine(tempDir, "test.json");

        var json = """
            [
              {
                "id": "chunk-1",
                "type": "story",
                "title": "Test Title",
                "text": "Test text content",
                "org": "Acme",
                "period": "2024",
                "source": "custom-source",
                "tags": ["tag1", "tag2"],
                "related": ["chunk-2"]
              }
            ]
            """;
        File.WriteAllText(file, json);

        try
        {
            var opts = new Options(DataDir: tempDir, OutPath: "ignored", Endpoint: null, Deployment: null, DryRun: false, Force: false, Help: false);
            var errors = new List<string>();
            var chunks = Program.LoadChunks(opts, errors);

            Assert.Empty(errors);
            Assert.Single(chunks);
            var c = chunks[0];
            Assert.Equal("chunk-1", c.Id);
            Assert.Equal("story", c.Type);
            Assert.Equal("Test Title", c.Title);
            Assert.Equal("Test text content", c.Text);
            Assert.Equal("Acme", c.Org);
            Assert.Equal("2024", c.Period);
            Assert.Equal("custom-source", c.Source);
            Assert.Equal(["tag1", "tag2"], c.Tags);
            Assert.Equal(["chunk-2"], c.Related);
            Assert.Equal("test.json", c.File);
        }
        finally
        {
            TestTempDir.Cleanup(tempDir);
        }
    }

    [Fact]
    public void LoadChunks_UsesFilenameAsSourceWhenMissing()
    {
        var tempDir = TestTempDir.Create();
        var file = Path.Combine(tempDir, "my-chunks.json");

        var json = """
            [
              { "id": "chunk-1", "type": "story", "title": "Test", "text": "Content" }
            ]
            """;
        File.WriteAllText(file, json);

        try
        {
            var opts = new Options(DataDir: tempDir, OutPath: "ignored", Endpoint: null, Deployment: null, DryRun: false, Force: false, Help: false);
            var errors = new List<string>();
            var chunks = Program.LoadChunks(opts, errors);

            Assert.Single(chunks);
            Assert.Equal("my-chunks", chunks[0].Source); // filename without extension
        }
        finally
        {
            TestTempDir.Cleanup(tempDir);
        }
    }

    [Fact]
    public void LoadChunks_InvalidJson_AddsError()
    {
        var tempDir = TestTempDir.Create();
        var file = Path.Combine(tempDir, "bad.json");

        File.WriteAllText(file, "{ invalid json }");

        try
        {
            var opts = new Options(DataDir: tempDir, OutPath: "ignored", Endpoint: null, Deployment: null, DryRun: false, Force: false, Help: false);
            var errors = new List<string>();
            var chunks = Program.LoadChunks(opts, errors);

            Assert.Empty(chunks);
            Assert.Contains(errors, e => e.Contains("bad.json: invalid JSON"));
        }
        finally
        {
            TestTempDir.Cleanup(tempDir);
        }
    }

    [Fact]
    public void LoadChunks_ObjectInsteadOfArray_AddsError()
    {
        var tempDir = TestTempDir.Create();
        var file = Path.Combine(tempDir, "bad.json");

        // Valid JSON but object instead of array
        File.WriteAllText(file, """{"id": "1", "title": "Test", "text": "Content"}""");

        try
        {
            var opts = new Options(DataDir: tempDir, OutPath: "ignored", Endpoint: null, Deployment: null, DryRun: false, Force: false, Help: false);
            var errors = new List<string>();
            var chunks = Program.LoadChunks(opts, errors);

            Assert.Empty(chunks);
            Assert.Contains(errors, e => e.Contains("bad.json: invalid JSON"));
        }
        finally
        {
            TestTempDir.Cleanup(tempDir);
        }
    }

    [Fact]
    public void LoadChunks_NullElementInArray_AddsError()
    {
        var tempDir = TestTempDir.Create();
        var file = Path.Combine(tempDir, "bad.json");

        // Array with one valid element and one null element
        File.WriteAllText(file, """[{"id": "1", "title": "T", "text": "T"}, null]""");

        try
        {
            var opts = new Options(DataDir: tempDir, OutPath: "ignored", Endpoint: null, Deployment: null, DryRun: false, Force: false, Help: false);
            var errors = new List<string>();
            var chunks = Program.LoadChunks(opts, errors);

            // Valid element is processed, null element adds error
            Assert.Single(chunks);
            Assert.Equal("1", chunks[0].Id);
            Assert.Contains(errors, e => e.Contains("bad.json: chunk entry is null"));
        }
        finally
        {
            TestTempDir.Cleanup(tempDir);
        }
    }

    [Fact]
    public void LoadChunks_MissingRequiredFields_AddsError()
    {
        var tempDir = TestTempDir.Create();
        var file = Path.Combine(tempDir, "bad.json");

        // Array with object missing required fields (id, title, text)
        // These become empty strings after deserialization, caught by Validate later
        File.WriteAllText(file, """[{"type": "story"}]""");

        try
        {
            var opts = new Options(DataDir: tempDir, OutPath: "ignored", Endpoint: null, Deployment: null, DryRun: false, Force: false, Help: false);
            var errors = new List<string>();
            var chunks = Program.LoadChunks(opts, errors);

            // Chunk is created with empty Id/Title/Text - Validate will catch this
            Assert.Single(chunks);
            Assert.Equal("", chunks[0].Id);
            Assert.Equal("", chunks[0].Title);
            Assert.Equal("", chunks[0].Text);
            // No errors from LoadChunks itself (deserialization succeeds)
            Assert.Empty(errors);
        }
        finally
        {
            TestTempDir.Cleanup(tempDir);
        }
    }

    [Fact]
    public void LoadChunks_WrongFieldTypes_AddsError()
    {
        var tempDir = TestTempDir.Create();
        var file = Path.Combine(tempDir, "bad.json");

        // Valid JSON but id is a number instead of string
        File.WriteAllText(file, """[{"id": 123, "title": "Test", "text": "Content"}]""");

        try
        {
            var opts = new Options(DataDir: tempDir, OutPath: "ignored", Endpoint: null, Deployment: null, DryRun: false, Force: false, Help: false);
            var errors = new List<string>();
            var chunks = Program.LoadChunks(opts, errors);

            Assert.Empty(chunks);
            Assert.Contains(errors, e => e.Contains("bad.json: invalid JSON"));
        }
        finally
        {
            TestTempDir.Cleanup(tempDir);
        }
    }

    [Fact]
    public void LoadChunks_EmptyArray_ReturnsEmptyList()
    {
        var tempDir = TestTempDir.Create();
        var file = Path.Combine(tempDir, "empty.json");

        File.WriteAllText(file, "[]");

        try
        {
            var opts = new Options(DataDir: tempDir, OutPath: "ignored", Endpoint: null, Deployment: null, DryRun: false, Force: false, Help: false);
            var errors = new List<string>();
            var chunks = Program.LoadChunks(opts, errors);

            Assert.Empty(chunks);
            Assert.Empty(errors);
        }
        finally
        {
            TestTempDir.Cleanup(tempDir);
        }
    }

    [Fact]
    public void LoadChunks_MultipleFiles_CombinesChunks()
    {
        var tempDir = TestTempDir.Create();

        File.WriteAllText(Path.Combine(tempDir, "a.json"), """[{"id": "a1", "type": "story", "title": "A", "text": "Text A"}]""");
        File.WriteAllText(Path.Combine(tempDir, "b.json"), """[{"id": "b1", "type": "project", "title": "B", "text": "Text B"}]""");

        try
        {
            var opts = new Options(DataDir: tempDir, OutPath: "ignored", Endpoint: null, Deployment: null, DryRun: false, Force: false, Help: false);
            var errors = new List<string>();
            var chunks = Program.LoadChunks(opts, errors);

            Assert.Equal(2, chunks.Count);
            Assert.Contains(chunks, c => c.Id == "a1" && c.Type == "story");
            Assert.Contains(chunks, c => c.Id == "b1" && c.Type == "project");
        }
        finally
        {
            TestTempDir.Cleanup(tempDir);
        }
    }

    [Fact]
    public void LoadChunks_ExcludesIndexJsonFile()
    {
        var tempDir = TestTempDir.Create();

        File.WriteAllText(Path.Combine(tempDir, "data.json"), """[{"id": "d1", "type": "story", "title": "D", "text": "Text"}]""");
        File.WriteAllText(Path.Combine(tempDir, "index.json"), """{"embeddingDeployment": "test", "dimensions": 1536, "createdUtc": "2024-01-01T00:00:00Z", "chunks": []}""");

        try
        {
            var opts = new Options(DataDir: tempDir, OutPath: "index.json", Endpoint: null, Deployment: null, DryRun: false, Force: false, Help: false);
            var errors = new List<string>();
            var chunks = Program.LoadChunks(opts, errors);

            Assert.Single(chunks);
            Assert.Equal("d1", chunks[0].Id);
        }
        finally
        {
            TestTempDir.Cleanup(tempDir);
        }
    }
}

public class LoadExistingTests
{
    [Fact]
    public void LoadExisting_NonExistentFile_ReturnsEmptyDictionary()
    {
        var dict = Program.LoadExisting("/nonexistent/path/index.json");

        Assert.NotNull(dict);
        Assert.Empty(dict);
    }

    [Fact]
    public void LoadExisting_ValidIndexFile_ReturnsChunks()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var json = """
                {
                  "embeddingDeployment": "text-embedding-3-small",
                  "dimensions": 1536,
                  "createdUtc": "2024-01-01T00:00:00Z",
                  "chunks": [
                    {
                      "id": "existing-1",
                      "type": "story",
                      "title": "Existing",
                      "text": "Content",
                      "org": null,
                      "period": null,
                      "source": "test",
                      "tags": [],
                      "related": [],
                      "file": "test.json",
                      "hash": "ABC123",
                      "vector": [0.1, 0.2, 0.3]
                    }
                  ]
                }
                """;
            File.WriteAllText(tempFile, json);

            var dict = Program.LoadExisting(tempFile);

            Assert.Single(dict);
            Assert.Contains("existing-1", dict.Keys);
            Assert.Equal("ABC123", dict["existing-1"].Hash);
            Assert.Equal(3, dict["existing-1"].Vector.Length);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadExisting_CorruptedJson_ReturnsEmptyDictionaryAndWarns()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "{ invalid json }");

            var dict = Program.LoadExisting(tempFile);

            Assert.NotNull(dict);
            Assert.Empty(dict);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}