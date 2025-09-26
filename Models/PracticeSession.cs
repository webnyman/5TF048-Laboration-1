using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace PracticeLogger.Models
{
    public class PracticeSession
    {
        public int SessionId { get; set; }

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
    }

    public class Instrument { public int InstrumentId { get; set; } public string Name { get; set; } = ""; }

}
