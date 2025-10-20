namespace PracticeLogger.Models
{
    public static class PracticeTypeExtensions
    {
        public static string Label(this PracticeType pt) => pt switch
        {
            PracticeType.Warmup => "Uppvärmning",
            PracticeType.Teknik => "Teknik",
            PracticeType.Skalor => "Skalor",
            PracticeType.Etyder => "Etyder",
            PracticeType.Repertoar => "Repertoar",
            PracticeType.Ovrigt => "Övrigt",
            _ => $"Typ {(byte)pt}"
        };
    }
}
