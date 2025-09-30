using Microsoft.Data.SqlClient;
using PracticeLogger.Models;
using System;
using System.Data;

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
                Family = Enum.Parse<InstrumentFamily>(r.GetString(2))

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
        return new Instrument { InstrumentId = r.GetInt32(0), Name = r.GetString(1), Family = Enum.Parse<InstrumentFamily>(r.GetString(2)) };
    }

    public async Task<int> CreateAsync(Instrument instrument)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_Instruments_Create", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 100) { Value = instrument.Name });
        cmd.Parameters.Add(new SqlParameter("@Family", SqlDbType.NVarChar, 50)
        {
            Value = instrument.Family.ToString()
        });



        await con.OpenAsync();
        var scalar = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(scalar);
    }



    public async Task<bool> UpdateAsync(Instrument instrument)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_Instruments_Update", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.Add(new SqlParameter("@InstrumentId", SqlDbType.Int) { Value = instrument.InstrumentId });
        cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 100) { Value = instrument.Name });
        cmd.Parameters.Add(new SqlParameter("@Family", SqlDbType.NVarChar, 50)
        {
            Value = instrument.Family.ToString()
        });


        await con.OpenAsync();
        try
        {
            var rows = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
            return rows > 0;
        }
        catch (SqlException ex) when (ex.Number == 50041 || ex.Number == 2627 || ex.Number == 2601)
        {
            // Namn redan upptaget
            throw;
        }
    }


    public async Task<bool> DeleteAsync(int id)
    {
        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.usp_Instruments_Delete", con)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.Add(new SqlParameter("@InstrumentId", SqlDbType.Int) { Value = id });

        await con.OpenAsync();
        try
        {
            var rows = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
            return rows > 0;
        }
        catch (SqlException ex) when (ex.Number == 50043 || ex.Number == 547)
        {
            // FK-krock: instrument används i sessions
            throw;
        }
    }

}
