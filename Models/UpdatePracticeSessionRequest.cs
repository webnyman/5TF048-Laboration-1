using System.ComponentModel.DataAnnotations;

namespace PracticeLogger.Models
{
    public class UpdatePracticeSessionRequest
    {
        [Required] public int InstrumentId { get; set; }
        [Required] public DateTime PracticeDate { get; set; }
        [Range(1, 600)] public int Minutes { get; set; }
        [Range(1, 5)] public byte Intensity { get; set; }
        [Required, MaxLength(200)] public string Focus { get; set; } = "";
        public string? Comment { get; set; }

        public byte? PracticeType { get; set; }
        [MaxLength(200)] public string? Goal { get; set; }
        public bool? Achieved { get; set; }

        [Range(1, 5)] public byte? Mood { get; set; }
        [Range(1, 5)] public byte? Energy { get; set; }
        [Range(1, 5)] public byte? FocusScore { get; set; }

        [Range(20, 400)] public short? TempoStart { get; set; }
        [Range(20, 400)] public short? TempoEnd { get; set; }
        public bool? Metronome { get; set; }

        [Range(0, 1000)] public short? Reps { get; set; }
        [Range(0, 1000)] public short? Errors { get; set; }
    }
}
