using Microsoft.AspNetCore.Mvc;
using PracticeLogger.Models;

namespace PracticeLogger.DAL
{
    /// <summary>
    /// Defines methods for managing and accessing practice session data in the data store.
    /// </summary>
    public interface IPracticeSessionRepository
    {
        /// <summary>
        /// Searches for practice sessions for a specific user, with optional filtering, sorting, and pagination.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="query">An optional search query to filter sessions.</param>
        /// <param name="instrumentId">An optional instrument ID to filter sessions.</param>
        /// <param name="sort">The field to sort by (e.g., "date", "minutes").</param>
        /// <param name="desc">Whether to sort in descending order.</param>
        /// <param name="page">The page number for pagination (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A collection of <see cref="PracticeSessionListItem"/> objects matching the criteria.</returns>
        Task<IEnumerable<PracticeSessionListItem>> SearchAsync(Guid userId, string? query, int? instrumentId, string sort, bool desc, int page, int pageSize);

        /// <summary>
        /// Retrieves a single practice session by its unique identifier for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="sessionId">The unique identifier of the practice session.</param>
        /// <returns>The <see cref="PracticeSession"/> if found; otherwise, <c>null</c>.</returns>
        Task<PracticeSession?> GetAsync(Guid userId, int sessionId);

        /// <summary>
        /// Creates a new practice session in the data store.
        /// </summary>
        /// <param name="s">The practice session to create. The <c>UserId</c> property must already be set.</param>
        /// <returns>The unique identifier of the newly created practice session.</returns>
        Task<int> CreateAsync(PracticeSession s);

        /// <summary>
        /// Updates an existing practice session in the data store for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="s">The practice session with updated data.</param>
        /// <returns><c>true</c> if the update was successful; otherwise, <c>false</c>.</returns>
        Task<bool> UpdateAsync(Guid userId, PracticeSession s);

        /// <summary>
        /// Deletes a practice session from the data store by its unique identifier for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="sessionId">The unique identifier of the practice session to delete.</param>
        /// <returns><c>true</c> if the deletion was successful; otherwise, <c>false</c>.</returns>
        Task<bool> DeleteAsync(Guid userId, int sessionId);

        /// <summary>
        /// Retrieves a summary of practice sessions for a specific user, optionally filtered by instrument and date range.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="instrumentId">An optional instrument ID to filter the summary.</param>
        /// <param name="from">An optional start date for filtering.</param>
        /// <param name="to">An optional end date for filtering.</param>
        /// <returns>A <see cref="PracticeSummary"/> object containing summary data.</returns>
        Task<PracticeSummary> GetSummaryAsync(Guid userId, int? instrumentId, DateTime? from = null, DateTime? to = null);

        /// <summary>
        /// Retrieves a summary of specific practice sessions by their IDs for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="sessionIds">A collection of practice session IDs to include in the summary.</param>
        /// <returns>A <see cref="PracticeSummary"/> object containing summary data for the selected sessions.</returns>
        Task<PracticeSummary> GetSummaryByIdsAsync(Guid userId, IEnumerable<int> sessionIds);
    }
}
