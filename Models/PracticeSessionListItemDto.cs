using PracticeLogger.Models.Api;

namespace PracticeLogger.Models
{
    public class PracticeSessionListItemDto
    {
        public int SessionId { get; set; }
        public DateTime PracticeDate { get; set; }
        public int Minutes { get; set; }
        public byte Intensity { get; set; }
        public string Focus { get; set; } = "";
        public int InstrumentId { get; set; }
        public string InstrumentName { get; set; } = "";

        public byte? PracticeType { get; set; }
        public string? Goal { get; set; }
        public bool Achieved { get; set; }
        public IEnumerable<LinkDto>? Links { get; set; }
    }
}
