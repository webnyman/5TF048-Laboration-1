using Microsoft.AspNetCore.Mvc;
using PracticeLogger.Models;

public interface IInstrumentRepository
{
    Task<IEnumerable<Instrument>> GetAllAsync(string? search = null);
    Task<Instrument?> GetAsync(int id);
    Task<int> CreateAsync(Instrument instrument);
    Task<bool> UpdateAsync(Instrument instrument);
    Task<bool> DeleteAsync(int id);
}
