using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace PracticeLogger.Models
{
    /// <summary>
    /// Represents a single practice log entry, including instrument, minutes, focus, intensity and date.
    /// </summary>
    public class PracticeEntry
    {
        /// <summary>
        /// Gets or sets the name of the instrument practiced.
        /// </summary>
        [Required]
        public string Instrument { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of minutes practiced.
        /// Must be between 1 and 600.
        /// </summary>
        [Range(1, 600, ErrorMessage = "Ange minuter 1–600.")]
        public int Minutes { get; set; }

        /// <summary>
        /// Gets or sets the focus or topic of the practice session.
        /// </summary>
        [Required]
        public string Focus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date of the practice session.
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        /// <summary>
        /// Practice intensity: scale 1-5
        /// </summary>
        [Display(Name = "Intensitet (1-5)")]
        [Range(1, 5, ErrorMessage = "Välj en intensitet mellan 1 och 5.")]
        public int Intensity { get; set; } = 3; // standard = Medel
    }
}
