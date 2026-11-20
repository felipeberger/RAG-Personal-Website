/// <summary>
/// Minimal .env loader, shared by tools/ingest and api (the API project links this file).
/// Kept in-house to stay dependency-light. Real environment variables always win over .env,
/// so CI and Azure (which set real variables and have no .env) behave exactly as before.
/// </summary>
internal static class DotEnv
{
    /// <summary>
    /// Finds the nearest .env, searching from <paramref name="startDir"/> (default: the current
    /// directory) up through its parents, and loads it. Returns the path loaded, or null if none.
    /// </summary>
    public static string? LoadNearest(string? startDir = null)
    {
        for (var dir = new DirectoryInfo(startDir ?? Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, ".env");
            if (!File.Exists(path)) continue;

            foreach (var (key, value) in Parse(File.ReadLines(path)))
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                    Environment.SetEnvironmentVariable(key, value);

            return path;
        }

        return null;
    }

    /// <summary>
    /// Parses KEY=VALUE lines. Supports blank lines, # comments, an optional "export " prefix,
    /// single or double quoted values, and trailing " # comments" on unquoted values.
    /// </summary>
    internal static IEnumerable<(string Key, string Value)> Parse(IEnumerable<string> lines)
    {
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("export ", StringComparison.Ordinal)) line = line[7..].TrimStart();

            var eq = line.IndexOf('=');
            if (eq <= 0) continue;

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();

            var quoted = value.Length >= 2 &&
                         ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\''));
            if (quoted)
            {
                value = value[1..^1];
            }
            else
            {
                var comment = value.IndexOf(" #", StringComparison.Ordinal);
                if (comment >= 0) value = value[..comment].TrimEnd();
            }

            yield return (key, value);
        }
    }
}
