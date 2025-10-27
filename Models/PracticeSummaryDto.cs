namespace PracticeLogger.Models
{
    public class PracticeSummaryDto
    {
        public int TotalMinutes { get; set; }
        public double AvgPerDay { get; set; }
        public int DistinctActiveDays { get; set; }
        public int EntriesCount { get; set; }

        public Dictionary<string, int> MinutesPerInstrument { get; set; } = new();
        public Dictionary<int, int> MinutesPerIntensity { get; set; } = new();
        public Dictionary<byte, int>? MinutesPerPracticeType { get; set; }

        public int? PassWithTempo { get; set; }
        public double? AvgTempoDelta { get; set; }
        public double? AvgMood { get; set; }
        public double? AvgEnergy { get; set; }
    }
}
