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
                // Only set if not already defined in real environment.
                // Explicitly empty ("") counts as defined — don't override with .env value.
                if (Environment.GetEnvironmentVariable(key) is null)
                    Environment.SetEnvironmentVariable(key, value);

            return path;
        }

        return null;
    }

    /// <summary>
    /// Parses KEY=VALUE lines. Supports blank lines, # comments, an optional "export " prefix,
    /// single or double quoted values, and trailing comments on unquoted values.
    /// </summary>
    /// <remarks>
    /// Comment parsing rules:
    /// <list type="bullet">
    /// <item><description>Full-line comments: lines starting with <c>#</c> (after optional whitespace) are skipped entirely.</description></item>
    /// <item><description>Quoted values (single <c>'</c> or double <c>"</c> quotes): <c>#</c> is always literal; no comment stripping.</description></item>
    /// <item><description>Unquoted values: only trailing comments starting with <c> #</c> (space + hash) are stripped. A <c>#</c> without a preceding space is treated as part of the value.</description></item>
    /// <item><description>There is no escape mechanism for <c>#</c> in unquoted values. Use quoted values to include literal <c>#</c>.</description></item>
    /// </list>
    /// </remarks>
    internal static IEnumerable<(string Key, string Value)> Parse(IEnumerable<string> lines)
    {
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("export ", StringComparison.Ordinal)) line = line[7..].TrimStart();

            var eq = line.IndexOf('=');
            if (eq < 0) continue; // no equals sign, skip silently (like original)
            if (eq == 0)
                throw new FormatException("Invalid key in .env: empty key. Keys must contain only letters, digits, or underscore.");

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();

            // Validate key format: only alphanumeric and underscore allowed (no hyphens, no whitespace)
            if (!IsValidKey(key))
                throw new FormatException($"Invalid key in .env: '{key}'. Keys must contain only letters, digits, or underscore.");

            // Check for duplicates
            if (!seenKeys.Add(key))
                throw new FormatException($"Duplicate key in .env: '{key}'. Each key can only be defined once.");

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

    /// <summary>
    /// Validates an environment variable key format.
    /// Keys must contain only letters, digits, or underscore.
    /// </summary>
    private static bool IsValidKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        foreach (var c in key)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
                return false;
        }
        return true;
    }
}
