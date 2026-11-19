using System.IO;

namespace IngestTests.Helpers;

internal static class TestTempDir
{
    public static string Create()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ingest-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    public static void Cleanup(string path)
    {
        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}