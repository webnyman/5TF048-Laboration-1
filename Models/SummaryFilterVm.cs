using System.ComponentModel.DataAnnotations;

public class SummaryFilterVm
{
    public int? InstrumentId { get; set; }
    [DataType(DataType.Date)] public DateTime? From { get; set; }
    [DataType(DataType.Date)] public DateTime? To { get; set; }
}
