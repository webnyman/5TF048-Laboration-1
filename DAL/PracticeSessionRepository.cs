using Microsoft.Data.SqlClient;
using PracticeLogger.DAL;
using PracticeLogger.Models;
using System.Data;

public class PracticeSessionRepository : IPracticeSessionRepository
{
    private readonly string _cs;
    public PracticeSessionRepository(IConfiguration cfg)
        => _cs = cfg.GetConnectionString("Lab2")!;

    public async Task<int> CreateAsync(PracticeSession s)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.sp_CreatePracticeSession", con)
        { CommandType = CommandType.StoredProcedure };

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
        using var cmd = new SqlCommand("dbo.sp_GetPracticeSessionById", con)
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
        using var cmd = new SqlCommand("dbo.sp_UpdatePracticeSession", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@SessionId", s.SessionId);
        cmd.Parameters.AddWithValue("@InstrumentId", s.InstrumentId);
        cmd.Parameters.AddWithValue("@PracticeDate", s.PracticeDate);
        cmd.Parameters.AddWithValue("@Minutes", s.Minutes);
        cmd.Parameters.AddWithValue("@Intensity", s.Intensity);
        cmd.Parameters.AddWithValue("@Focus", s.Focus);
        cmd.Parameters.AddWithValue("@Comment", (object?)s.Comment ?? DBNull.Value);

        await con.OpenAsync();
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows == 1;
    }

    public async Task<bool> DeleteAsync(int sessionId)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.sp_DeletePracticeSession", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@SessionId", sessionId);

        await con.OpenAsync();
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows == 1;
    }

    public async Task<(IEnumerable<PracticeSessionListItem>, int)> SearchAsync(
        string? query, int? instrumentId, string? sort, bool desc, int page, int pageSize)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.sp_SearchPracticeSessions", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@Query", (object?)query ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@InstrumentId", (object?)instrumentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Sort", sort ?? "date");   // "date" | "minutes" | "intensity"
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

        // andra resultset: total count
        int total = 0;
        if (await r.NextResultAsync() && await r.ReadAsync())
            total = r.GetInt32(0);

        return (list, total);
    }
}