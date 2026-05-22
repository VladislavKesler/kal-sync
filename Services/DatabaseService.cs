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
}
