using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chatter.Services;

/// <summary>
/// Hosted backup service. Reads cron expression and other settings from configuration under
/// `ServerSettings:Backup*`. Checks once per minute and triggers backups when cron is due.
/// Uses VACUUM INTO through EF Core to create a safe snapshot, zips it, and enforces retention.
/// </summary>
public class BackupService : IHostedService, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BackupService> _logger;
    private readonly IConfiguration _config;
    private System.Threading.Tasks.Task? _executingTask;
    private CancellationTokenSource? _cts;
    private readonly object _lock = new();
    private bool _running = false;

    public BackupService(IServiceProvider services, IConfiguration config, ILogger<BackupService> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    // Public helper methods for controllers
    public async Task<List<string>> ListBackupsAsync()
    {
        var path = GetBackupPath();
        if (!Directory.Exists(path)) return new List<string>();
        var files = Directory.EnumerateFiles(path, "*.zip")
            .OrderByDescending(f => File.GetCreationTimeUtc(f))
            .Select(Path.GetFileName)
            .ToList();
        return files;
    }

    public async Task<Stream?> DownloadBackupAsync(string fileName)
    {
        var file = Path.Combine(GetBackupPath(), fileName);
        if (!File.Exists(file)) return null;
        return File.OpenRead(file);
    }

    public async Task<bool> DeleteBackupAsync(string fileName)
    {
        var file = Path.Combine(GetBackupPath(), fileName);
        if (!File.Exists(file)) return false;
        File.Delete(file);
        return true;
    }

    public async Task<bool> RestoreBackupAsync(string fileName)
    {
        var path = GetBackupPath();
        var zipFile = Path.Combine(path, fileName);
        if (!File.Exists(zipFile)) return false;

        var tempDir = Path.Combine(path, "restore_tmp");
        Directory.CreateDirectory(tempDir);
        try
        {
            // Extract the backup .db to temp
            using (var z = ZipFile.OpenRead(zipFile))
            {
                var entry = z.Entries.FirstOrDefault(e => e.Name.EndsWith('.' + "db", StringComparison.OrdinalIgnoreCase) || e.Name.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase));
                if (entry == null) return false;
                var outPath = Path.Combine(tempDir, entry.Name);
                entry.ExtractToFile(outPath, overwrite: true);

                // Determine main DB path from a scoped DbContext
                using var scope = ((IServiceProvider)_services).CreateScope();
                var db = scope.ServiceProvider.GetService(typeof(Chatter.Data.ChatterDbContext)) as Chatter.Data.ChatterDbContext;
                if (db == null) throw new InvalidOperationException("ChatterDbContext not available for restore.");
                var conn = db.Database.GetDbConnection();
                var mainDataSource = GetDataSourceFromConnectionString(conn.ConnectionString);
                if (string.IsNullOrEmpty(mainDataSource)) throw new InvalidOperationException("Could not determine main DB path.");

                // Try to replace the main DB file
                // Note: this is a best-effort approach. If the application has open connections
                // the copy may fail or produce inconsistent results. The safest approach is
                // to restore when the app is offline.
                db.Dispose();
                try
                {
                    File.Copy(outPath, mainDataSource, overwrite: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to copy restored DB over main DB");
                    return false;
                }
            }
            return true;
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    public async Task<(bool Enabled, string? Schedule, string Path, int Retention, DateTime? LastRunUtc)> GetStatusAsync()
    {
        var enabled = _config.GetValue<bool?>("ServerSettings:BackupEnabled") ?? false;
        var schedule = _config.GetValue<string?>("ServerSettings:BackupSchedule");
        var path = GetBackupPath();
        var retention = _config.GetValue<int?>("ServerSettings:BackupRetention") ?? 7;
        var lastRun = GetLastRunUtc();
        return (enabled, schedule, path, retention, lastRun);
    }

    // IHostedService
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BackupService starting.");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cts.Token;
        // Start background loop
        _executingTask = System.Threading.Tasks.Task.Run(async () =>
        {
            _logger.LogInformation("BackupService background loop started.");
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await TickAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled exception in backup tick");
                    }
                    await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(60), token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BackupService background loop error");
            }
            _logger.LogInformation("BackupService background loop exiting.");
        }, token);

        _logger.LogInformation("BackupService started.");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BackupService stopping.");
        if (_cts != null)
        {
            _cts.Cancel();
            try
            {
                if (_executingTask != null)
                {
                    await System.Threading.Tasks.Task.WhenAny(_executingTask, System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while stopping BackupService");
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }
        }
        _logger.LogInformation("BackupService stopped.");
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { }
        try { _cts?.Dispose(); } catch { }
    }

    private async Task TickAsync()
    {
        // ensure only one tick runs at a time
        if (!Monitor.TryEnter(_lock)) return;
        try
        {
            if (_running) return;
            _running = true;
            try
            {
                var enabled = _config.GetValue<bool?>("ServerSettings:BackupEnabled") ?? false;
                var schedule = _config.GetValue<string?>("ServerSettings:BackupSchedule");
                var lastRun = GetLastRunUtc() ?? DateTime.MinValue;
                var now = DateTime.UtcNow;
                _logger.LogInformation("Tick: enabled={Enabled} schedule='{Schedule}' lastRun={LastRun} now={Now}", enabled, schedule, lastRun == DateTime.MinValue ? null : (object)lastRun.ToString("o"), now.ToString("o"));
                if (!enabled)
                {
                    _logger.LogInformation("Tick: backups disabled in configuration");
                    return;
                }
                if (string.IsNullOrWhiteSpace(schedule))
                {
                    _logger.LogInformation("Tick: no schedule configured");
                    return;
                }

                var isDue = TimingService.IsDueSince(schedule, lastRun, now);
                _logger.LogInformation("Tick: IsDueSince -> {IsDue}", isDue);
                if (isDue)
                {
                    _logger.LogInformation("Backup schedule due; creating backup now.");
                    var ok = await CreateBackupInternalAsync();
                    if (ok)
                    {
                        SetLastRunUtc(now);
                    }
                    else
                    {
                        _logger.LogWarning("Scheduled backup attempt failed");
                    }
                }
            }
            finally
            {
                _running = false;
            }
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    public async Task<bool> CreateBackupAsync(bool force = false)
    {
        if (!force)
        {
            var enabled = _config.GetValue<bool?>("ServerSettings:BackupEnabled") ?? false;
            if (!enabled) return false;
        }
        return await CreateBackupInternalAsync();
    }

    private async Task<bool> CreateBackupInternalAsync()
    {
        _logger.LogInformation("CreateBackupInternalAsync invoked");
        try
        {
            var path = GetBackupPath();
            Directory.CreateDirectory(path);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var baseName = $"backup-{timestamp}";
            var snapshotFile = Path.Combine(path, baseName + ".db");

            // Create a vacuumed copy of the DB using a scoped DbContext
            using (var scope = ((IServiceProvider)_services).CreateScope())
            {
                var db = scope.ServiceProvider.GetService(typeof(Chatter.Data.ChatterDbContext)) as Chatter.Data.ChatterDbContext;
                if (db == null)
                {
                    _logger.LogWarning("ChatterDbContext not available; skipping backup.");
                    return false;
                }

                // VACUUM INTO requires a file path; ensure it's properly escaped
                try
                {
                    var escaped = snapshotFile.Replace("'", "''");
                    await db.Database.ExecuteSqlRawAsync($"VACUUM INTO '{escaped}'");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "VACUUM INTO failed, attempting fallback copy via connection backup API.");
                    try
                    {
                        // fallback: try to use the sqlite backup API if available via the connection
                        var conn = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
                        using var dest = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={snapshotFile}");
                        await dest.OpenAsync();
                        await conn.OpenAsync();
                        conn.BackupDatabase(dest);
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogError(ex2, "Fallback DB backup failed.");
                        return false;
                    }
                }
            }

            // Zip the snapshot
            var zipFile = Path.Combine(path, baseName + ".zip");
            using (var z = ZipFile.Open(zipFile, ZipArchiveMode.Create))
            {
                z.CreateEntryFromFile(snapshotFile, Path.GetFileName(snapshotFile), CompressionLevel.Optimal);
            }

            // Remove raw snapshot
            try { File.Delete(snapshotFile); } catch { }

            // Enforce retention
            EnforceRetention();

            _logger.LogInformation("Backup created: {Zip}", zipFile);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup creation failed.");
            return false;
        }
    }

    private void EnforceRetention()
    {
        try
        {
            var retention = _config.GetValue<int?>("ServerSettings:BackupRetention") ?? 7;
            var path = GetBackupPath();
            if (!Directory.Exists(path)) return;
            var files = Directory.EnumerateFiles(path, "*.zip")
                .OrderByDescending(f => File.GetCreationTimeUtc(f))
                .ToList();
            if (files.Count <= retention) return;
            var toDelete = files.Skip(retention).ToList();
            foreach (var f in toDelete)
            {
                try { File.Delete(f); } catch (Exception ex) { _logger.LogWarning(ex, "Failed deleting old backup {File}", f); }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error enforcing backup retention");
        }
    }

    private string GetBackupPath()
    {
        var configured = _config.GetValue<string?>("ServerSettings:BackupPath");
        if (!string.IsNullOrWhiteSpace(configured)) return Path.GetFullPath(configured!);
        // default to application base + backups
        return Path.Combine(AppContext.BaseDirectory, "backups");
    }

    private DateTime? GetLastRunUtc()
    {
        try
        {
            var path = GetBackupPath();
            var file = Path.Combine(path, "last_run.txt");
            if (!File.Exists(file)) return null;
            var s = File.ReadAllText(file);
            // Prefer the round-trip "o" format
            if (DateTime.TryParseExact(s, "o", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dtExact))
                return dtExact;
            if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
                return dt;
            return null;
        }
        catch { return null; }
    }

    private void SetLastRunUtc(DateTime utc)
    {
        try
        {
            var path = GetBackupPath();
            Directory.CreateDirectory(path);
            var file = Path.Combine(path, "last_run.txt");
            File.WriteAllText(file, utc.ToString("o"));
        }
        catch { }
    }

    private static string? GetDataSourceFromConnectionString(string? cs)
    {
        if (string.IsNullOrWhiteSpace(cs)) return null;
        try
        {
            var b = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(cs);
            return b.DataSource;
        }
        catch
        {
            return null;
        }
    }
}
