using Microsoft.AspNetCore.Mvc;
using PracticeLogger.Models;

/// <summary>
/// Defines methods for managing and accessing instrument data in the data store.
/// </summary>
public interface IInstrumentRepository
{
    /// <summary>
    /// Retrieves all instruments, optionally filtered by a search string.
    /// </summary>
    /// <param name="search">An optional search string to filter instruments by name or other criteria.</param>
    /// <returns>A collection of <see cref="Instrument"/> objects.</returns>
    Task<IEnumerable<Instrument>> GetAllAsync(string? search = null);

    /// <summary>
    /// Retrieves a single instrument by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the instrument.</param>
    /// <returns>The <see cref="Instrument"/> if found; otherwise, <c>null</c>.</returns>
    Task<Instrument?> GetAsync(int id);

    /// <summary>
    /// Creates a new instrument in the data store.
    /// </summary>
    /// <param name="instrument">The instrument to create.</param>
    /// <returns>The unique identifier of the newly created instrument.</returns>
    Task<int> CreateAsync(Instrument instrument);

    /// <summary>
    /// Updates an existing instrument in the data store.
    /// </summary>
    /// <param name="instrument">The instrument with updated data.</param>
    /// <returns><c>true</c> if the update was successful; otherwise, <c>false</c>.</returns>
    Task<bool> UpdateAsync(Instrument instrument);

    /// <summary>
    /// Deletes an instrument from the data store by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the instrument to delete.</param>
    /// <returns><c>true</c> if the deletion was successful; otherwise, <c>false</c>.</returns>
    Task<bool> DeleteAsync(int id);
}
