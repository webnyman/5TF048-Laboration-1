using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace PracticeLogger.Models
{
    public class Instrument
    {
        public int InstrumentId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [Display(Name = "Familj")]
        public InstrumentFamily Family { get; set; }

    }
    public enum InstrumentFamily
    {
        Brass,
        Träblås,
        Stråk,
        Slagverk,
        Sträng,
        Sång,
        Övrigt
    }
}
