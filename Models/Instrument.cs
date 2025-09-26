using Microsoft.AspNetCore.Mvc;

namespace PracticeLogger.Models
{
    public class Instrument
    {
        public int InstrumentId { get; set; }       // PK
        public string Name { get; set; } = "";      // ex. Trumpet, Flute
        public string Family { get; set; } = "";    // ex. Brass, Träblås, Slagverk
    }
}
