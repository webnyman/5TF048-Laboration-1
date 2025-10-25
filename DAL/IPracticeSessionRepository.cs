using Microsoft.AspNetCore.Mvc;
using PracticeLogger.Models;

namespace PracticeLogger.DAL
{
    public interface IPracticeSessionRepository
    {
        Task<IEnumerable<PracticeSessionListItem>> SearchAsync(Guid userId, string? query, int? instrumentId, string sort, bool desc, int page, int pageSize);
        Task<PracticeSession?> GetAsync(Guid userId, int sessionId);
        Task<int> CreateAsync(PracticeSession s); // s.UserId ska redan vara satt
        Task<bool> UpdateAsync(Guid userId, PracticeSession s);
        Task<bool> DeleteAsync(Guid userId, int sessionId);
        Task<PracticeSummary> GetSummaryAsync(Guid userId, int? instrumentId, DateTime? from = null, DateTime? to = null);
        Task<PracticeSummary> GetSummaryByIdsAsync(Guid userId, IEnumerable<int> sessionIds);
    }


}
