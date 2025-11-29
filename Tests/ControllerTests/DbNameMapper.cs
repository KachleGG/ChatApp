using System.Collections.Concurrent;

namespace Tests.ControllerTests;

public static class DbNameMapper
{
    private static readonly ConcurrentDictionary<string, string> s_map = new();

    /// <summary>
    /// Return a stable, unique database name for a given logical base name.
    /// Multiple calls with the same baseName will return the same mapped name within a test run,
    /// avoiding cross-test collisions while preserving shared DBs for contexts that pass the same baseName.
    /// </summary>
    public static string GetDbName(string baseName)
    {
        if (string.IsNullOrWhiteSpace(baseName)) return Guid.NewGuid().ToString();
        return s_map.GetOrAdd(baseName, _ => baseName + "-" + Guid.NewGuid().ToString());
    }
}
