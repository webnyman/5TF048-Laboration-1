namespace PracticeLogger.Models
{
    public class NextWeekPlanItem
    {
        public string Day { get; set; } = "";        // t.ex. "Mån"
        public string Focus { get; set; } = "";      // t.ex. "Teknik – tonglidningar"
        public int Minutes { get; set; }             // t.ex. 30
        public int Intensity { get; set; }           // 1–5
        public bool Metronome { get; set; }          // true/false
        public int? TempoTarget { get; set; }        // BPM om tillgängligt
        public string Notes { get; set; } = "";      // korta instruktioner
    }
}
