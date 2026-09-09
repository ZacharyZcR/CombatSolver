namespace CombatSolver;

internal static class BetterRouteFile
{
    internal static bool Store(string path, Func<string, bool> replaces, Func<string> serialize, CancellationToken token)
    {
        string directory = System.IO.Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        string identity = System.IO.Path.GetFullPath(directory);
        if (OperatingSystem.IsWindows()) identity = identity.ToUpperInvariant();
        string key = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identity)));
        using var lease = new Mutex(false, "CombatSolverRouteCache" + key);
        try
        {
            while (!lease.WaitOne(50)) token.ThrowIfCancellationRequested();
        }
        catch (AbandonedMutexException) { /* Atomic replacement leaves the previous file complete. */ }
        try { return StoreLocked(path, replaces, serialize, token); }
        finally { lease.ReleaseMutex(); }
    }

    private static bool StoreLocked(string path, Func<string, bool> replaces, Func<string> serialize, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (File.Exists(path) && !replaces(File.ReadAllText(path))) return false;
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, serialize());
            token.ThrowIfCancellationRequested();
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
