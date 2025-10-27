namespace PracticeLogger.Models
{
    public class PracticeSessionDto
    {
        public int SessionId { get; set; }
        public Guid UserId { get; set; }

        public int InstrumentId { get; set; }
        public string InstrumentName { get; set; } = "";

        public DateTime PracticeDate { get; set; }
        public int Minutes { get; set; }
        public byte Intensity { get; set; }
        public string Focus { get; set; } = "";
        public string? Comment { get; set; }

        public byte? PracticeType { get; set; }
        public string? Goal { get; set; }
        public bool? Achieved { get; set; }

        public byte? Mood { get; set; }
        public byte? Energy { get; set; }
        public byte? FocusScore { get; set; }

        public short? TempoStart { get; set; }
        public short? TempoEnd { get; set; }
        public bool? Metronome { get; set; }

        public short? Reps { get; set; }
        public short? Errors { get; set; }
    }
}
