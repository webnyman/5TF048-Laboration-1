using Microsoft.AspNetCore.Mvc;
using PracticeLogger.Models;

namespace PracticeLogger.DAL
{
    public interface IPracticeSessionRepository
    {
        Task<int> CreateAsync(PracticeSession s);
        Task<PracticeSession?> GetAsync(int sessionId);
        Task<bool> UpdateAsync(PracticeSession s);
        Task<bool> DeleteAsync(int sessionId);

        // Sök + filter + sort + paging. Returnerar items + total count.
        Task<IEnumerable<PracticeSessionListItem>> SearchAsync(
    string? query, int? instrumentId, string? sort, bool desc, int page, int pageSize);

        Task<PracticeSummary> GetSummaryAsync(int? instrumentId = null);
    }

}
