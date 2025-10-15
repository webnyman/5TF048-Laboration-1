using Microsoft.Data.SqlClient;
using PracticeLogger.DAL;
using PracticeLogger.Models;
using System.Data;

public class PracticeSessionRepository : IPracticeSessionRepository
{
    private readonly string _cs;
    private static readonly Guid DummyUserId = Guid.Parse("13392206-460D-4FBA-BB5C-88210BC1437A");

    public PracticeSessionRepository(IConfiguration cfg)
        => _cs = cfg.GetConnectionString("DefaultConnection")!;

    public async Task<int> CreateAsync(PracticeSession s)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_PracticeSession_Create", con)
        { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@UserId", DummyUserId);
        cmd.Parameters.AddWithValue("@InstrumentId", s.InstrumentId);
        cmd.Parameters.AddWithValue("@PracticeDate", s.PracticeDate);
        cmd.Parameters.AddWithValue("@Minutes", s.Minutes);
        cmd.Parameters.AddWithValue("@Intensity", s.Intensity);
        cmd.Parameters.AddWithValue("@Focus", s.Focus);
        cmd.Parameters.AddWithValue("@Comment", (object?)s.Comment ?? DBNull.Value);

        await con.OpenAsync();
        var scalar = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(scalar);
    }

    public async Task<PracticeSession?> GetAsync(int sessionId)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_GetPracticeSessionById", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@SessionId", sessionId);

        await con.OpenAsync();
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;

        return new PracticeSession
        {
            SessionId = r.GetInt32(r.GetOrdinal("SessionId")),
            InstrumentId = r.GetInt32(r.GetOrdinal("InstrumentId")),
            PracticeDate = r.GetDateTime(r.GetOrdinal("PracticeDate")),
            Minutes = r.GetInt32(r.GetOrdinal("Minutes")),
            Intensity = (byte)r.GetByte(r.GetOrdinal("Intensity")),
            Focus = r.GetString(r.GetOrdinal("Focus")),
            Comment = r.IsDBNull(r.GetOrdinal("Comment")) ? null : r.GetString(r.GetOrdinal("Comment"))
        };
    }

    public async Task<bool> UpdateAsync(PracticeSession s)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_PracticeSession_Update", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@SessionId", s.SessionId);
        cmd.Parameters.AddWithValue("@UserId", DummyUserId); // tills vidare
        cmd.Parameters.AddWithValue("@InstrumentId", s.InstrumentId);
        cmd.Parameters.AddWithValue("@PracticeDate", s.PracticeDate);
        cmd.Parameters.AddWithValue("@Minutes", s.Minutes);
        cmd.Parameters.AddWithValue("@Intensity", s.Intensity);
        cmd.Parameters.AddWithValue("@Focus", s.Focus);
        cmd.Parameters.AddWithValue("@Comment", (object?)s.Comment ?? DBNull.Value);

        await con.OpenAsync();
        var rows = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
        return rows > 0;
    }



    public async Task<bool> DeleteAsync(int sessionId)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_PracticeSession_Delete", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@SessionId", sessionId);
        cmd.Parameters.AddWithValue("@UserId", DummyUserId);

        await con.OpenAsync();
        var rows = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
        return rows > 0;
    }

    public async Task<IEnumerable<PracticeSessionListItem>> SearchAsync(
    string? query, int? instrumentId, string? sort, bool desc, int page, int pageSize)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_SearchPracticeSessions", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@Query", (object?)query ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@InstrumentId", (object?)instrumentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Sort", sort ?? "date");
        cmd.Parameters.AddWithValue("@Desc", desc);
        cmd.Parameters.AddWithValue("@Page", page <= 0 ? 1 : page);
        cmd.Parameters.AddWithValue("@PageSize", pageSize <= 0 ? 10 : pageSize);

        await con.OpenAsync();
        using var r = await cmd.ExecuteReaderAsync();

        var list = new List<PracticeSessionListItem>();
        while (await r.ReadAsync())
        {
            list.Add(new PracticeSessionListItem
            {
                SessionId = r.GetInt32(r.GetOrdinal("SessionId")),
                PracticeDate = r.GetDateTime(r.GetOrdinal("PracticeDate")),
                Minutes = r.GetInt32(r.GetOrdinal("Minutes")),
                Intensity = (byte)r.GetByte(r.GetOrdinal("Intensity")),
                Focus = r.GetString(r.GetOrdinal("Focus")),
                InstrumentId = r.GetInt32(r.GetOrdinal("InstrumentId")),
                InstrumentName = r.GetString(r.GetOrdinal("InstrumentName"))
            });
        }
        return list;
    }
    public async Task<PracticeSummary> GetSummaryAsync(int? instrumentId = null)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_PracticeSessions_Summary", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@InstrumentId", (object?)instrumentId ?? DBNull.Value);

        await con.OpenAsync();
        using var r = await cmd.ExecuteReaderAsync();

        var totalMinutes = 0;
        var distinctDays = 0;
        var entriesCount = 0;

        // 1) totals
        if (await r.ReadAsync())
        {
            totalMinutes = r.IsDBNull(0) ? 0 : r.GetInt32(0);
            distinctDays = r.IsDBNull(1) ? 0 : r.GetInt32(1);
            entriesCount = r.IsDBNull(2) ? 0 : r.GetInt32(2);
        }

        // 2) minutes per instrument
        var minutesPerInstrument = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        await r.NextResultAsync();
        while (await r.ReadAsync())
        {
            var name = r.GetString(0);
            var minutes = r.GetInt32(1);
            minutesPerInstrument[name] = minutes;
        }

        // 3) minutes per intensity (fyll även luckor 1..5 med 0)
        var minutesPerIntensity = new Dictionary<int, int>();
        await r.NextResultAsync();
        while (await r.ReadAsync())
        {
            var intensity = r.GetByte(0); // TINYINT => byte
            var minutes = r.GetInt32(1);
            minutesPerIntensity[intensity] = minutes;
        }
        for (int i = 1; i <= 5; i++)
            if (!minutesPerIntensity.ContainsKey(i)) minutesPerIntensity[i] = 0;

        var avgPerDay = distinctDays == 0 ? 0 : Math.Round((double)totalMinutes / distinctDays, 1);

        return new PracticeSummary
        {
            TotalMinutes = totalMinutes,
            AvgPerDay = avgPerDay,
            MinutesPerInstrument = minutesPerInstrument,
            MinutesPerIntensity = minutesPerIntensity
                                 .OrderBy(kv => kv.Key)
                                 .ToDictionary(k => k.Key, v => v.Value),
            DistinctActiveDays = distinctDays,
            EntriesCount = entriesCount
        };
    }

}