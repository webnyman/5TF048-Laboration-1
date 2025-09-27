using Microsoft.AspNetCore.Mvc;

namespace PracticeLogger.Models
{
    public class PracticeSessionListItem
    {
        public int SessionId { get; set; }
        public DateTime PracticeDate { get; set; }
        public int Minutes { get; set; }
        public byte Intensity { get; set; }
        public string Focus { get; set; } = "";
        public int InstrumentId { get; set; }
        public string InstrumentName { get; set; } = "";
    }
}
