using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace PracticeLogger.Models
{
    public class PracticeSession
    {
        public int SessionId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public int InstrumentId { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime PracticeDate { get; set; } = DateTime.Today;

        [Range(1, 600, ErrorMessage = "Minuter 1–600.")]
        public int Minutes { get; set; }

        [Range(1, 5, ErrorMessage = "Intensitet 1–5.")]
        public byte Intensity { get; set; }

        [Required, StringLength(200)]
        public string Focus { get; set; } = "";

        public string? Comment { get; set; }

        public byte? PracticeType { get; set; }     // 1–6
        [MaxLength(200)]
        public string? Goal { get; set; }
        public bool Achieved { get; set; } = false;

        [Range(1, 5)] public byte? Mood { get; set; }
        [Range(1, 5)] public byte? Energy { get; set; }
        [Range(1, 5)] public byte? FocusScore { get; set; }

        [Range(20, 400)] public short? TempoStart { get; set; }
        [Range(20, 400)] public short? TempoEnd { get; set; }
        public bool Metronome { get; set; } = false;

        [Range(0, 1000)] public short? Reps { get; set; }
        [Range(0, 1000)] public short? Errors { get; set; }
    }

}
