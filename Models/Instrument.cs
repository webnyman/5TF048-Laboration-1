using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace PracticeLogger.Models
{
    public class Instrument
    {
        public int InstrumentId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = "";

        [Required, StringLength(50)]
        public string Family { get; set; } = "";
    }
}
