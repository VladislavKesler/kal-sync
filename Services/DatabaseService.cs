using kal_sync.Models;
using SQLite;

namespace kal_sync.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is null)
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "measurements.db3");
            _db = new SQLiteAsyncConnection(path);
            await _db.CreateTableAsync<BodyMeasurement>();
            await _db.CreateTableAsync<TennisSession>();
            await _db.CreateTableAsync<RunSession>();
        }
        return _db;
    }

    public async Task<List<BodyMeasurement>> GetAllAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<BodyMeasurement>()
                       .OrderByDescending(m => m.Date)
                       .ToListAsync();
    }

    public async Task AddAsync(BodyMeasurement measurement)
    {
        var db = await GetConnectionAsync();
        await db.InsertAsync(measurement);
    }

    public async Task DeleteAsync(BodyMeasurement measurement)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(measurement);
    }

    public async Task<BodyMeasurement?> GetLatestAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<BodyMeasurement>()
                       .OrderByDescending(m => m.Date)
                       .FirstOrDefaultAsync();
    }

    // ── Tennis sessions ──────────────────────────────────────────────────────

    public async Task<List<TennisSession>> GetAllTennisSessionsAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<TennisSession>()
                       .OrderByDescending(s => s.Date)
                       .ToListAsync();
    }

    public async Task AddTennisSessionAsync(TennisSession session)
    {
        var db = await GetConnectionAsync();
        await db.InsertAsync(session);
    }

    public async Task DeleteTennisSessionAsync(TennisSession session)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(session);
    }

    // ── Run sessions ─────────────────────────────────────────────────────────

    public async Task<List<RunSession>> GetAllRunSessionsAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<RunSession>()
                       .OrderByDescending(s => s.Date)
                       .ToListAsync();
    }

    public async Task AddRunSessionAsync(RunSession session)
    {
        var db = await GetConnectionAsync();
        await db.InsertAsync(session);
    }

    public async Task DeleteRunSessionAsync(RunSession session)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(session);
    }
}
