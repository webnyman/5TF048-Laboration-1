using Microsoft.Data.SqlClient;
using System.Data;
using PracticeLogger.Models;

public class InstrumentRepository : IInstrumentRepository
{
    private readonly string _cs;
    public InstrumentRepository(IConfiguration cfg) => _cs = cfg.GetConnectionString("Lab2")!;

    public async Task<IEnumerable<Instrument>> GetAllAsync(string? search = null)
    {
        var list = new List<Instrument>();
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_Instruments_GetAll", con) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@Query", (object?)search ?? DBNull.Value);
        await con.OpenAsync();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new Instrument
            {
                InstrumentId = r.GetInt32(0),
                Name = r.GetString(1),
                Family = r.GetString(2)
            });
        }
        return list;
    }

    public async Task<Instrument?> GetAsync(int id)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_Instruments_GetById", con) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@InstrumentId", id);
        await con.OpenAsync();
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new Instrument { InstrumentId = r.GetInt32(0), Name = r.GetString(1), Family = r.GetString(2) };
    }

    public async Task<int> CreateAsync(Instrument instrument)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_Instruments_Create", con) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@Name", instrument.Name);
        cmd.Parameters.AddWithValue("@Family", instrument.Family);
        await con.OpenAsync();
        var id = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        return id;
    }

    public async Task<bool> UpdateAsync(Instrument instrument)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_Instruments_Update", con) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@InstrumentId", instrument.InstrumentId);
        cmd.Parameters.AddWithValue("@Name", instrument.Name);
        cmd.Parameters.AddWithValue("@Family", instrument.Family);
        await con.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_Instruments_Delete", con) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@InstrumentId", id);
        await con.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() == 1;
    }
}
