using System;
using System.Threading.Tasks;

#if WINDOWS
using Velopack;
using Velopack.Sources;
#endif

namespace kal_sync.Services;

/// <summary>
/// Checks GitHub for a new Velopack release and applies it on Windows.
/// On non-Windows platforms all methods are no-ops.
/// </summary>
public class UpdateService
{
    private const string RepoUrl = "https://github.com/VladislavKesler/kal-sync";

#if WINDOWS
    private readonly UpdateManager _manager = new(new GithubSource(RepoUrl, null, false));
    private UpdateInfo? _pendingUpdate;

    /// <summary>
    /// Returns the new version string if an update is available, otherwise null.
    /// Never throws — network errors are silently swallowed.
    /// </summary>
    public async Task<string?> CheckForUpdateAsync()
    {
        try
        {
            _pendingUpdate = await _manager.CheckForUpdatesAsync();
            return _pendingUpdate?.TargetFullRelease.Version.ToString();
        }
        catch
        {
            _pendingUpdate = null;
            return null;
        }
    }

    /// <summary>
    /// Downloads the pending update and restarts the app into the new version.
    /// App data in AppDataDirectory and Preferences is preserved by Velopack.
    /// </summary>
    public async Task DownloadAndRestartAsync(IProgress<int>? progress = null)
    {
        if (_pendingUpdate is null) return;
        await _manager.DownloadUpdatesAsync(_pendingUpdate, progress is null ? null : p => progress.Report(p));
        _manager.ApplyUpdatesAndRestart(_pendingUpdate);
    }
#else
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Instance method for DI / testability")]
    public Task<string?> CheckForUpdateAsync() => Task.FromResult<string?>(null);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Instance method for DI / testability")]
    public Task DownloadAndRestartAsync(IProgress<int>? progress = null) => Task.CompletedTask;
#endif
}
