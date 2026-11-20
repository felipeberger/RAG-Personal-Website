using System.Numerics.Tensors;
using System.Text.Json;

// These two records must match tools/ingest (IndexedChunk / IndexFile). Change them together.
internal sealed record IndexedChunk(
    string Id, string Type, string Title, string Text, string? Org, string? Period,
    string Source, string[] Tags, string[] Related, string File, string Hash, float[] Vector);

internal sealed record IndexFile(
    string EmbeddingDeployment, int Dimensions, DateTime CreatedUtc, List<IndexedChunk> Chunks);

/// <summary>In-memory knowledge store loaded from data/index.json. Swappable for Azure AI Search later.</summary>
internal sealed class KnowledgeStore
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private readonly IndexFile _index;
    private readonly Dictionary<string, IndexedChunk> _byId;

    public KnowledgeStore(string path)
    {
        _index = JsonSerializer.Deserialize<IndexFile>(File.ReadAllText(path), Json)
                 ?? throw new InvalidDataException($"Could not read index file: {path}");
        _byId = _index.Chunks.ToDictionary(c => c.Id);
    }

    public string EmbeddingDeployment => _index.EmbeddingDeployment;
    public int Dimensions => _index.Dimensions;
    public int Count => _index.Chunks.Count;

    public IReadOnlyList<(IndexedChunk Chunk, float Score)> Search(ReadOnlyMemory<float> query, int top)
    {
        if (query.Length != _index.Dimensions)
            throw new InvalidOperationException(
                $"Query vector has {query.Length} dimensions but the index has {_index.Dimensions}. " +
                "Embed with the same deployment the index was built with.");

        return _index.Chunks
            .Select(c => (Chunk: c, Score: TensorPrimitives.CosineSimilarity(query.Span, c.Vector)))
            .OrderByDescending(x => x.Score)
            .Take(top)
            .ToList();
    }

    public IEnumerable<IndexedChunk> Related(IndexedChunk chunk) =>
        chunk.Related.Where(_byId.ContainsKey).Select(id => _byId[id]);
}
