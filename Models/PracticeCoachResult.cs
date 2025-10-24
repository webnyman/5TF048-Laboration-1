namespace PracticeLogger.Models
{
    public class PracticeCoachResult
    {
        public int TotalSessions { get; set; }
        public int TotalMinutes { get; set; }
        public double AvgMinutes { get; set; }
        public double AvgIntensity { get; set; }
        public double GoalHitRate { get; set; }   // 0..1
        public double? AvgTempoDelta { get; set; } // kan vara null om för få datapunkter
        public int DaysActive { get; set; }
        public List<string> Tips { get; set; } = new();
    }
}
