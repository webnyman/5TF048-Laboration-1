using Microsoft.Data.SqlClient;
using PracticeLogger.DAL;
using PracticeLogger.Models;
using System.Data;

public class PracticeSessionRepository : IPracticeSessionRepository
{
    private readonly string _cs;

    public PracticeSessionRepository(IConfiguration cfg)
        => _cs = cfg.GetConnectionString("DefaultConnection")!;

    public async Task<int> CreateAsync(PracticeSession s)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_PracticeSession_Create", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@UserId", s.UserId);              // ⬅️ ändrat
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


    public async Task<PracticeSession?> GetAsync(Guid userId, int sessionId)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_GetPracticeSessionById", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@UserId", userId);       // ⬅️ nytt
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
            Intensity = r.GetByte(r.GetOrdinal("Intensity")),
            Focus = r.GetString(r.GetOrdinal("Focus")),
            Comment = r.IsDBNull(r.GetOrdinal("Comment")) ? null : r.GetString(r.GetOrdinal("Comment")),
            UserId = userId
        };
    }


    public async Task<bool> UpdateAsync(Guid userId, PracticeSession s)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_PracticeSession_Update", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@SessionId", s.SessionId);
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



    public async Task<bool> DeleteAsync(Guid userId, int sessionId)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_PracticeSession_Delete", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@SessionId", sessionId);

        await con.OpenAsync();
        var rows = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
        return rows > 0;
    }


    public async Task<IEnumerable<PracticeSessionListItem>> SearchAsync(
     Guid userId, string? query, int? instrumentId, string sort, bool desc, int page, int pageSize)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_SearchPracticeSessions", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Query", (object?)query ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@InstrumentId", (object?)instrumentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Sort", sort);
        cmd.Parameters.AddWithValue("@Desc", desc);
        cmd.Parameters.AddWithValue("@Page", page);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);

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
    public async Task<PracticeSummary> GetSummaryAsync(Guid userId, int? instrumentId = null)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_PracticeSessions_Summary", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@InstrumentId", (object?)instrumentId ?? DBNull.Value);

        await con.OpenAsync();
        using var r = await cmd.ExecuteReaderAsync();

        var summary = new PracticeSummary();

        // 1) Totals: TotalMinutes, DistinctDays, EntriesCount
        if (await r.ReadAsync())
        {
            summary.TotalMinutes = r.IsDBNull(0) ? 0 : r.GetInt32(0);
            summary.DistinctActiveDays = r.IsDBNull(1) ? 0 : r.GetInt32(1);
            summary.EntriesCount = r.IsDBNull(2) ? 0 : r.GetInt32(2);
        }
        summary.AvgPerDay = summary.DistinctActiveDays == 0
            ? 0
            : Math.Round((double)summary.TotalMinutes / summary.DistinctActiveDays, 1);

        // 2) Minutes per instrument: (InstrumentName nvarchar, Minutes int)
        await r.NextResultAsync();
        var byInstrument = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        while (await r.ReadAsync())
        {
            var name = r.GetString(0);
            var minutes = r.GetInt32(1);
            byInstrument[name] = minutes;
        }
        summary.MinutesPerInstrument = byInstrument;

        // 3) Minutes per intensity: (Intensity tinyint, Minutes int)
        await r.NextResultAsync();
        var byIntensity = new Dictionary<int, int>();
        while (await r.ReadAsync())
        {
            var intensity = r.GetByte(0);   // TINYINT
            var minutes = r.GetInt32(1);
            byIntensity[intensity] = minutes;
        }
        // Fyll luckor 1..5
        for (int i = 1; i <= 5; i++)
            if (!byIntensity.ContainsKey(i)) byIntensity[i] = 0;
        summary.MinutesPerIntensity = byIntensity
            .OrderBy(kv => kv.Key)
            .ToDictionary(k => k.Key, v => v.Value);

        // 4) Minutes per practice type: (PracticeType tinyint, Minutes int)
        await r.NextResultAsync();
        if (!r.IsClosed)
        {
            var byType = new Dictionary<byte, int>();
            while (await r.ReadAsync())
            {
                var pt = r.GetByte(0);
                var minutes = r.GetInt32(1);
                byType[pt] = minutes;
            }
            summary.MinutesPerPracticeType = byType;
        }

        // 5) Tempo stats: (PassWithTempo int, AvgTempoDelta float)
        await r.NextResultAsync();
        if (!r.IsClosed && await r.ReadAsync())
        {
            summary.PassWithTempo = r.IsDBNull(0) ? 0 : r.GetInt32(0);
            summary.AvgTempoDelta = r.IsDBNull(1) ? (double?)null : r.GetDouble(1);
        }

        // 6) Mood/Energy: (AvgMood float, AvgEnergy float)
        await r.NextResultAsync();
        if (!r.IsClosed && await r.ReadAsync())
        {
            summary.AvgMood = r.IsDBNull(0) ? (double?)null : r.GetDouble(0);
            summary.AvgEnergy = r.IsDBNull(1) ? (double?)null : r.GetDouble(1);
        }

        return summary;
    }



}