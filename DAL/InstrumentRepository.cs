using Microsoft.Data.SqlClient;
using PracticeLogger.Models;
using System;
using System.Data;

/// <summary>
/// Repository for managing instrument data in the database using stored procedures.
/// Implements the <see cref="IInstrumentRepository"/> interface.
/// </summary>
public class InstrumentRepository : IInstrumentRepository
{
    private readonly string _cs;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstrumentRepository"/> class.
    /// </summary>
    /// <param name="cfg">The application configuration used to retrieve the connection string.</param>
    public InstrumentRepository(IConfiguration cfg) => _cs = cfg.GetConnectionString("DefaultConnection")!;

    /// <summary>
    /// Retrieves all instruments from the database, optionally filtered by a search string.
    /// </summary>
    /// <param name="search">An optional search string to filter instruments by name or other criteria.</param>
    /// <returns>A collection of <see cref="Instrument"/> objects.</returns>
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

    /// <summary>
    /// Retrieves a single instrument by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the instrument.</param>
    /// <returns>The <see cref="Instrument"/> if found; otherwise, <c>null</c>.</returns>
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

    /// <summary>
    /// Creates a new instrument in the database.
    /// </summary>
    /// <param name="instrument">The instrument to create.</param>
    /// <returns>The unique identifier of the newly created instrument.</returns>
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

    /// <summary>
    /// Updates an existing instrument in the database.
    /// </summary>
    /// <param name="instrument">The instrument with updated data.</param>
    /// <returns><c>true</c> if the update was successful; otherwise, <c>false</c>.</returns>
    /// <exception cref="SqlException">Thrown if the instrument name is already taken.</exception>
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
            throw;
        }
    }

    /// <summary>
    /// Deletes an instrument from the database by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the instrument to delete.</param>
    /// <returns><c>true</c> if the deletion was successful; otherwise, <c>false</c>.</returns>
    /// <exception cref="SqlException">Thrown if the instrument is referenced by other data (foreign key constraint).</exception>
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
            throw;
        }
    }
}
