using System.Linq;

namespace IngestTests.Factories;

internal static class ChunkFactory
{
    // 17 words - passes the "at least 15 words" validation
    private const string ValidText = "Valid text with enough words to pass validation without triggering any warnings here now today";

    public static Chunk Valid(string id = "id-1") => new Chunk(
        Id: id,
        Type: "story",
        Title: "Valid Title",
        Text: ValidText,
        Org: null,
        Period: null,
        Source: "test",
        Tags: [],
        Related: [],
        File: "test.json");

    public static Chunk WithEmptyId(string id = "") => Valid(id) with { Id = id };
    public static Chunk WithEmptyTitle(string id = "id-1") => Valid(id) with { Title = "" };
    public static Chunk WithEmptyText(string id = "id-1") => Valid(id) with { Text = "" };
    public static Chunk WithShortText(string id = "id-1") => Valid(id) with { Text = "Short text" };
    public static Chunk WithLongText(string id = "id-1") => Valid(id) with { Text = string.Join(" ", Enumerable.Repeat("word", 250)) };
    public static Chunk WithRelated(string id = "id-1", params string[] related) => Valid(id) with { Related = related };
    public static Chunk WithType(string id = "id-1", string type = "project") => Valid(id) with { Type = type };
    public static Chunk WithSource(string id = "id-1", string source = "custom-source") => Valid(id) with { Source = source };
    public static Chunk WithFile(string id = "id-1", string file = "custom.json") => Valid(id) with { File = file };
}