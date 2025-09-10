using Microsoft.AspNetCore.Mvc;

namespace PracticeLogger.Models
{
    /// <summary>
    /// Represents a summary of practice log data, including totals, averages, and breakdowns by instrument and intensity.
    /// </summary>
    public class PracticeSummary
    {
        /// <summary>
        /// Gets or sets the total number of minutes practiced across all entries.
        /// </summary>
        public int TotalMinutes { get; set; }

        /// <summary>
        /// Gets or sets the average number of minutes practiced per active day.
        /// </summary>
        public double AvgPerDay { get; set; }

        /// <summary>
        /// Gets or sets a dictionary containing the total minutes practiced for each instrument.
        /// The key is the instrument name, and the value is the total minutes practiced on that instrument.
        /// </summary>
        public Dictionary<string, int> MinutesPerInstrument { get; set; } = new();

        /// <summary>
        /// Gets or sets a dictionary containing the total minutes practiced for each intensity level.
        /// The key is the intensity value, and the value is the total minutes practiced at that intensity.
        /// </summary>
        public Dictionary<int, int> MinutesPerIntensity { get; set; } = new();

        /// <summary>
        /// Gets or sets the number of distinct days with at least one practice entry.
        /// </summary>
        public int DistinctActiveDays { get; set; }

        /// <summary>
        /// Gets or sets the total number of practice entries.
        /// </summary>
        public int EntriesCount { get; set; }
    }
}
